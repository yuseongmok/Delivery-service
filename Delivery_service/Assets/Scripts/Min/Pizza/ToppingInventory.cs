using System.Collections.Generic;
using UnityEngine;

public class ToppingInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    public Transform HoldPoint => holdPoint;

    public PizzaToppingData CurrentItem { get; private set; }
    public GameObject HeldPizza { get; private set; }

    // 데이터 구조로 관리
    private List<PizzaData> heldPackagedPizzas = new List<PizzaData>();

    public bool HasItem() => CurrentItem != null || HeldPizza != null || HasPackagedPizzas();
    public bool HasTopping() => CurrentItem != null;
    public bool HasPizza() => HeldPizza != null;
    public bool HasPackagedPizzas() => heldPackagedPizzas.Count > 0;

    public List<PizzaData> GetHeldPackagedPizzas() => heldPackagedPizzas;

    public void AddItem(PizzaToppingData item)
    {
        if (item == null) return;
        if (HasItem()) return;

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
        if (pizza == null) return;
        if (HasItem()) return;

        HeldPizza = pizza;
        Debug.Log("피자를 들었습니다.");
    }

    public GameObject RemovePizza()
    {
        GameObject pizza = HeldPizza;
        HeldPizza = null;
        return pizza;
    }

    // 포장 피자 데이터 등록
    public void AddPackagedDataStack(List<PizzaData> dataList)
    {
        if (HasItem()) return;

        heldPackagedPizzas = dataList;
        Debug.Log($"포장된 피자 {heldPackagedPizzas.Count}개 소지");
    }

    public List<PizzaData> ClearPackagedPizzas()
    {
        List<PizzaData> list = new List<PizzaData>(heldPackagedPizzas);
        heldPackagedPizzas.Clear();
        return list;
    }
}