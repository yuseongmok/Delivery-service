using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonOfficeEvidencePoint : MonoBehaviour, IInteractable
    {
        public PrisonOfficeEvidence mission;
        [Range(0, 3)] public int index;
        public void Interact(ToppingInventory inventory) { mission.Inspect(index); }
    }
}
