using System.Collections.Generic;
using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable
{
    public void Interact(ToppingInventory inventory)
    {
        if (inventory.HasTopping())
        {
            inventory.RemoveItem();
            Debug.Log("토핑을 버렸습니다.");
            return;
        }

        if (inventory.HasPizza())
        {
            GameObject pizza = inventory.RemovePizza();
            Destroy(pizza);
            Debug.Log("피자를 버렸습니다.");
            return;
        }

        if (inventory.HasPackagedPizzas())
        {
            List<PizzaData> packages = inventory.ClearPackagedPizzas();
            Debug.Log($"포장된 피자를 버렸습니다.");
            return;
        }
    }
}