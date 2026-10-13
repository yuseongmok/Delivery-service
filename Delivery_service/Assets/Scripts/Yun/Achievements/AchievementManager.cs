using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Yun.Achievements
{
    // 달성 상태와 저장만 담당합니다. 보상은 아직 지급하지 않습니다.
    public sealed class AchievementManager : MonoBehaviour
    {
        public const string FirstPizzaId = "first_packaged_pizza";
        private string savePrefix = "Yun.Achievements.";

        [SerializeField] private AchievementNotification notification;
        private readonly HashSet<string> unlocked = new HashSet<string>();
        public event Action Changed;

        private void Awake()
        {
            if (PlayerPrefs.GetInt(savePrefix + FirstPizzaId, 0) == 1)
                unlocked.Add(FirstPizzaId);
        }

        public bool IsUnlocked(string id) { return unlocked.Contains(id); }

        // 여러 조건이 같은 프레임에 만족해도 알림 쪽에서 개수를 합산합니다.
        public bool TryUnlock(string id)
        {
            if (id != FirstPizzaId || !unlocked.Add(id)) return false;
            PlayerPrefs.SetInt(savePrefix + id, 1);
            PlayerPrefs.Save();
            if (notification != null) notification.Show(1);
            Changed?.Invoke();
            return true;
        }
    }
}
