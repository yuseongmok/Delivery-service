using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Achievements
{
    public sealed class AchievementPanelView : MonoBehaviour
    {
        [SerializeField] private AchievementManager achievements;
        [SerializeField] private Text progress;
        [SerializeField] private Text state;
        [SerializeField] private Text firstDeliveryState;

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
            bool delivery=achievements!=null&&achievements.IsUnlocked(AchievementManager.FirstDeliveryId);
            if (progress != null) progress.text = $"달성한 업적  {(done?1:0)+(delivery?1:0)} / 2";
            if (state != null) state.text = done ? "달성 완료" : "미달성";
            if(firstDeliveryState!=null)firstDeliveryState.text=delivery?"달성 완료":"미달성";
        }
    }
}
