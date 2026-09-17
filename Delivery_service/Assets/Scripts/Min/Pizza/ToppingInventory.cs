using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToppingInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Text currentTopping;
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

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentTopping == null) return;

        if (CurrentItem != null)
        {
            currentTopping.text = $"들고있는 토핑 : {CurrentItem.toppingName}";
        }
        else if (HeldPizza != null)
        {
            currentTopping.text = " ";
        }
        else if (HasPackagedPizzas())
        {
            currentTopping.text = $"포장된 피자 : {heldPackagedPizzas.Count}개";
        }
        else
        {
            currentTopping.text = "들고있는 토핑 : 없음";
        }
    }

    public void AddItem(PizzaToppingData item)
    {
        if (item == null) return;
        if (HasItem()) return;

        CurrentItem = item;
        Debug.Log($"획득 : {item.toppingName}");
        UpdateUI();
    }

    public PizzaToppingData RemoveItem()
    {
        PizzaToppingData item = CurrentItem;
        CurrentItem = null;
        UpdateUI();
        return item;
    }

    public void AddPizza(GameObject pizza)
    {
        if (pizza == null) return;
        if (HasItem()) return;

        HeldPizza = pizza;
        Debug.Log("피자를 들었습니다.");
        UpdateUI();
    }

    public GameObject RemovePizza()
    {
        GameObject pizza = HeldPizza;
        HeldPizza = null;
        UpdateUI();
        return pizza;
    }

    // 포장 피자 데이터 등록
    public void AddPackagedDataStack(List<PizzaData> dataList)
    {
        if (HasItem()) return;

        heldPackagedPizzas = dataList;
        Debug.Log($"포장된 피자 {heldPackagedPizzas.Count}개 소지");
        UpdateUI();
    }

    public List<PizzaData> ClearPackagedPizzas()
    {
        List<PizzaData> list = new List<PizzaData>(heldPackagedPizzas);
        heldPackagedPizzas.Clear();
        UpdateUI();
        return list;
    }
}