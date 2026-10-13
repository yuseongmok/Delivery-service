using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Achievements
{
    // 항상 켜진 호스트에 붙이고, 저장된 알림 패널만 표시/숨김 처리합니다.
    public sealed class AchievementNotification : MonoBehaviour
    {
        [SerializeField] private GameObject popup;
        [SerializeField] private Text message;
        [SerializeField, Min(.1f)] private float duration = 5f;
        private int count;
        private float hideAt;

        private void Awake() { Hide(); }

        public void Show(int achievedCount)
        {
            if (achievedCount <= 0 || popup == null || message == null) return;
            if (!popup.activeSelf || Time.unscaledTime >= hideAt) count = 0;
            count += achievedCount;
            message.text = "업적 " + count + "개 달성됨!";
            hideAt = Time.unscaledTime + duration;
            popup.SetActive(true);
        }

        private void Update()
        {
            if (popup != null && popup.activeSelf && Time.unscaledTime >= hideAt) Hide();
        }

        private void OnDisable() { Hide(); }
        private void Hide()
        {
            count = 0;
            if (popup != null) popup.SetActive(false);
        }
    }
}
