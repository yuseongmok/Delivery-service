using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class PrisonPizzaSupply:MonoBehaviour,IInteractable
    {
        public PrisonKeysMission mission;
        public PizzaToppingData ingredient;
        public void Interact(ToppingInventory inventory){if(mission.State==PrisonKeysMission.Stage.Cook&&!inventory.HasItem())inventory.AddItem(ingredient);}
    }
}
