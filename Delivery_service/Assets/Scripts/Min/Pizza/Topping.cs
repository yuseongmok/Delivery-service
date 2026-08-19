using UnityEngine;

public class Topping : MonoBehaviour, IInteractable
{
    [SerializeField] private PizzaToppingData toppingData;

    public void Interact(ToppingInventory inventory)
    {
        if (inventory.HasItem())
        {
            Debug.Log("손에 이미 아이템이 있습니다.");
            return;
        }

        inventory.AddItem(toppingData);

        Debug.Log($"{toppingData.toppingName} 획득");
    }
}