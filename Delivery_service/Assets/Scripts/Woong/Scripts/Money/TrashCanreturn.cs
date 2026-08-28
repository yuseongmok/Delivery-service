using System.Collections.Generic;
using UnityEngine;

public class TrashCanreturn //: MonoBehaviour, IInteractable
{
  // public void Interact(ToppingInventory inventory)
  // {
  //     if (inventory.HasTopping())
  //     {
  //         PizzaToppingData discardedItem = inventory.RemoveItem();
  //         MoneyManager.Instance.AddMoney(discardedItem.cost);
  //
  //         Debug.Log($"{discardedItem.toppingName}을(를) 버리고 {discardedItem.cost}원을 환불받았습니다.");
  //         return;
  //     }
  //
  //     if (inventory.HasPizza())
  //     {
  //         GameObject pizza = inventory.RemovePizza();
  //         Destroy(pizza);
  //         Debug.Log("피자를 버렸습니다.");
  //         return;
  //     }
  //
  //     if (inventory.HasPackagedPizzas())
  //     {
  //         List<PizzaData> packages = inventory.ClearPackagedPizzas();
  //         Debug.Log($"포장된 피자를 버렸습니다.");
  //         return;
  //     }
  // }
}