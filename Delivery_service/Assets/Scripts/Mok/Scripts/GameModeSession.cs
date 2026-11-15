using UnityEngine;
namespace DeliveryService.Mok
{
    public static class GameModeSession
    {
        public static bool IsChallenge { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsChallenge = false;
        public static void Select(bool challenge)
        {
            IsChallenge = challenge;
            StoryTutorial.Requested = false;
        }
    }
}
