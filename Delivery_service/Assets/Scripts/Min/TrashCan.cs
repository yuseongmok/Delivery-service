using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable
{
    public void Interact(ToppingInventory inventory)
    {
        if (inventory.HasPizza())
        {
            GameObject pizza = inventory.RemovePizza();

            if (pizza != null)
            {
                Destroy(pizza);
                Debug.Log("피자를 쓰레기통에 버렸습니다.");
            }
            return;
        }

        if (!inventory.HasItem())
        {
            PizzaToppingData item = inventory.RemoveItem();
            Debug.Log($"{item.toppingName}을 버렸습니다.");
            return;
        }

        Debug.Log("버릴 아이템이나 피자가 없습니다.");
    }
}