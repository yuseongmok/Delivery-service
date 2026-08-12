using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable
{
    public void Interact(ToppingInventory inventory)
    {
        if (!inventory.HasItem())
        {
            Debug.Log("버릴 아이템이 없습니다.");
            return;
        }

        PizzaToppingData item = inventory.RemoveItem();

        Debug.Log($"{item.toppingName}을 버렸습니다.");
    }
}