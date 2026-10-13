using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Achievements
{
    public sealed class AchievementPanelView : MonoBehaviour
    {
        [SerializeField] private AchievementManager achievements;
        [SerializeField] private Text progress;
        [SerializeField] private Text state;

        private void OnEnable()
        {
            if (achievements != null) achievements.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (achievements != null) achievements.Changed -= Refresh;
        }

        private void Refresh()
        {
            bool done = achievements != null && achievements.IsUnlocked(AchievementManager.FirstPizzaId);
            if (progress != null) progress.text = done ? "달성한 업적  1 / 1" : "달성한 업적  0 / 1";
            if (state != null) state.text = done ? "달성 완료" : "미달성";
        }
    }
}
