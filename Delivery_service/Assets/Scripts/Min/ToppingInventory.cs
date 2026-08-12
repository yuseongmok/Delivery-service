using UnityEngine;

public interface IInteractable
{
    void Interact(ToppingInventory inventory);
}

public class ToppingInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    public Transform HoldPoint => holdPoint;

    public PizzaToppingData CurrentItem { get; private set; }

    public GameObject HeldPizza { get; private set; }

    public bool HasItem()
    {
        return CurrentItem != null || HeldPizza != null;
    }

    public bool HasTopping()
    {
        return CurrentItem != null;
    }

    public bool HasPizza()
    {
        return HeldPizza != null;
    }

    public void AddItem(PizzaToppingData item)
    {
        if (item == null)
            return;

        if (HasItem())
        {
            Debug.Log("이미 아이템을 들고 있습니다.");
            return;
        }

        CurrentItem = item;

        Debug.Log($"획득 : {item.toppingName}");
    }

    public PizzaToppingData RemoveItem()
    {
        PizzaToppingData item = CurrentItem;

        CurrentItem = null;

        return item;
    }

    public void AddPizza(GameObject pizza)
    {
        if (pizza == null)
            return;

        if (HasItem())
        {
            Debug.Log("이미 아이템을 들고 있습니다.");
            return;
        }

        HeldPizza = pizza;

        Debug.Log("피자를 들었습니다.");
    }

    public GameObject RemovePizza()
    {
        GameObject pizza = HeldPizza;

        HeldPizza = null;

        return pizza;
    }
}