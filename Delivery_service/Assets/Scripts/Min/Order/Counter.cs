using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private DayNightCycle dayNightCycle;

    public void Interact(ToppingInventory inventory)
    {
        if (dayNightCycle != null && !dayNightCycle.isShopOpen)
        {
            return; // 가게 영업 전 상호작용 방지
        }

        // 카운터 상호작용 시 대기 중인 주문 목록 UI 열기
        if (orderUI != null)
        {
            orderUI.OpenUI();
        }
    }
}