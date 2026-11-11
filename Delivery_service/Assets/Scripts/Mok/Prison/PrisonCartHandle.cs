using UnityEngine;
namespace DeliveryService.Mok
{
    // 손잡이도 본체와 동일한 적재 및 안전한 놓기 처리를 사용한다.
    public sealed class PrisonCartHandle : MonoBehaviour, IInteractable
    {
        public PrisonMealCart cart;
        public void Interact(ToppingInventory inventory) { cart.Interact(inventory); }
    }
}
