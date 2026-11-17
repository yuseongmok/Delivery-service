using UnityEngine.SceneManagement;

namespace DeliveryService.Mok
{
    // Scene-scoped restriction, also valid before the prison mission's Awake has run.
    public static class PrisonFeaturePolicy
    {
        public static bool Active => SceneManager.GetActiveScene().name == "PrisonChapter";
        public static bool Cooking => Active && PrisonChickenQuest.Current &&
            PrisonChickenQuest.Current.IsPlayingCustomCooking;
    }
}
