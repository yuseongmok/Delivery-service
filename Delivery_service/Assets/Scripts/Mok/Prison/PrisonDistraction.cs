using UnityEngine;

namespace DeliveryService.Mok
{
    // 반복 사용 가능한 배관 두드리기. 간수를 유인하고 반대편으로 우회한다.
    public sealed class PrisonDistraction : MonoBehaviour, IInteractable
    {
        public PrisonGuard[] guards;
        public Transform soundPoint;
        [TextArea] public string feedback = "배관 소리가 울렸다. 간수가 돌아보는 동안 움직이자.";
        float nextUse;
        public void Interact(ToppingInventory inventory)
        {
            if (Time.time < nextUse || !PrisonMission.Current || PrisonMission.Current.Failing) return;
            nextUse = Time.time + 9; PrisonAudio.Clink(soundPoint.position,.6f);
            foreach (var guard in guards) if (guard) guard.Distract(soundPoint.position);
            PrisonMission.Current.MarkDistractionUsed();
            PrisonMission.Current.Say(feedback, 5);
        }
    }
}
