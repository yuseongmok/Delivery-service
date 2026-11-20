using UnityEngine;

namespace DeliveryService.Yun
{
    // A scene collider forwards existing F interaction to the single mission controller.
    public sealed class ChapterThreeArmsPoint : MonoBehaviour, IInteractable
    {
        public ChapterThreeArmsMission mission;
        public string code;
        public int fragment = -1;
        public void Interact(ToppingInventory inventory) { if (mission) mission.Inspect(this, inventory); }
    }
}
