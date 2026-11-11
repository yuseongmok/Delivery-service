using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonMissionPoint : MonoBehaviour, IInteractable
    {
        public string prompt = "상호작용";
        public void Interact(ToppingInventory inventory) { PrisonMission.Current?.Use(this); }
    }
}
