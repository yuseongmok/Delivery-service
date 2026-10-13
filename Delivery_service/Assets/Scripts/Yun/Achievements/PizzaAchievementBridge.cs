using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DeliveryService.Yun.Achievements
{
    // 기존 포장 코드에 이벤트가 없어 데이터 목록을 읽기만 합니다.
    // 팀원 코드에 이벤트가 추가되면 이 연결부만 교체할 수 있습니다.
    [DefaultExecutionOrder(-19000)]
    public sealed class PizzaAchievementBridge : MonoBehaviour
    {
        [SerializeField] private AchievementManager achievements;
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private PizzaPickUpZone[] pickupZones;
        [SerializeField] private ToppingInventory[] inventories;
        [SerializeField] private BikePizzaStorage[] bikeStorages;

        private static readonly FieldInfo PickupData = typeof(PizzaPickUpZone).GetField(
            "packagedDataList", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BikeData = typeof(BikePizzaStorage).GetField(
            "storedPizzas", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly HashSet<PizzaData> seen = new HashSet<PizzaData>();

        private void Start()
        {
            if (PickupData == null || BikeData == null)
            {
                Debug.LogError("[Yun 업적] 기존 포장 데이터 필드가 변경되었습니다. PizzaAchievementBridge를 확인하세요.", this);
                enabled = false;
                return;
            }
            // 씬에 이미 있던 피자, 저장 후 다시 시작한 피자는 새 제작이 아닙니다.
            Observe(false);
        }

        private void Update()
        {
            // 영업을 여는 Update 이전에 기존 포장분을 등록합니다.
            if (dayNightCycle != null && !dayNightCycle.isShopOpen) Observe(false);
        }

        private void LateUpdate()
        {
            if (achievements == null || dayNightCycle == null) return;
            if (achievements.IsUnlocked(AchievementManager.FirstPizzaId))
            {
                seen.Clear();
                enabled = false;
                return;
            }
            Observe(dayNightCycle.isShopOpen);
        }

        private void Observe(bool canUnlock)
        {
            if (PickupData == null || BikeData == null) return;
            if (pickupZones != null)
                foreach (var zone in pickupZones)
                    if (zone != null) ObserveList(PickupData.GetValue(zone) as List<PizzaData>, canUnlock);
            // 같은 프레임에 픽업하거나 적재해도 감지하고, 다시 꺼낸 피자는 중복 처리하지 않습니다.
            if (inventories != null)
                foreach (var inventory in inventories)
                    if (inventory != null) ObserveList(inventory.GetHeldPackagedPizzas(), canUnlock);
            if (bikeStorages != null)
                foreach (var bike in bikeStorages)
                    if (bike != null) ObserveList(BikeData.GetValue(bike) as List<PizzaData>, canUnlock);
        }

        private void ObserveList(List<PizzaData> data, bool canUnlock)
        {
            if (data == null) return;
            foreach (var pizza in data)
            {
                if (pizza == null || !seen.Add(pizza)) continue;
                if (canUnlock && pizza.isBaked && achievements != null)
                    achievements.TryUnlock(AchievementManager.FirstPizzaId);
            }
        }
    }
}
