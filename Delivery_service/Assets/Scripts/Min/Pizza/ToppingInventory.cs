using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ToppingInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Text currentTopping;
    public Transform HoldPoint => holdPoint;

    [Header("시작 시 피자 1박스 소지 설정")]
    [SerializeField] private bool startWithPizzaBox = false;
    [SerializeField] private string targetSceneName = "";

    public PizzaToppingData CurrentItem { get; private set; }
    public GameObject HeldPizza { get; private set; }

    // 데이터 구조로 관리
    private List<PizzaData> heldPackagedPizzas = new List<PizzaData>();

    public bool HasItem() => CurrentItem != null || HeldPizza != null || HasPackagedPizzas();
    public bool HasTopping() => CurrentItem != null;
    public bool HasPizza() => HeldPizza != null;
    public bool HasPackagedPizzas() => heldPackagedPizzas.Count > 0;

    public List<PizzaData> GetHeldPackagedPizzas() => heldPackagedPizzas;

    public event System.Action ContentsChanged;

    private void Awake()
    {
        if (GetComponent<DeliveryService.Mok.CarriedItemVisual>() == null)
            gameObject.AddComponent<DeliveryService.Mok.CarriedItemVisual>();
    }

    private void Start()
    {
        if (ShouldStartWithPizzaBox() && !HasItem())
        {
            PizzaData initialBox = CreateStartingPizzaData();
            if (initialBox != null)
            {
                AddPackagedDataStack(new List<PizzaData> { initialBox });
                return;
            }
        }

        UpdateUI();
    }

    private bool ShouldStartWithPizzaBox()
    {
        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            return SceneManager.GetActiveScene().name.Trim() == targetSceneName.Trim();
        }

        return startWithPizzaBox;
    }

    private PizzaData CreateStartingPizzaData()
    {
        var type = typeof(PizzaData);
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ctors.Length > 0)
        {
            ConstructorInfo chosen = ctors[0];
            for (int i = 1; i < ctors.Length; i++)
            {
                if (ctors[i].GetParameters().Length < chosen.GetParameters().Length)
                    chosen = ctors[i];
            }

            var parameters = chosen.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var pType = parameters[i].ParameterType;
                if (pType == typeof(List<PizzaToppingData>))
                    args[i] = new List<PizzaToppingData>();
                else if (pType == typeof(PizzaToppingData[]))
                    args[i] = new PizzaToppingData[0];
                else if (pType == typeof(string))
                    args[i] = "기본 피자";
                else if (pType == typeof(bool))
                    args[i] = true;
                else if (pType.IsValueType)
                    args[i] = System.Activator.CreateInstance(pType);
                else
                    args[i] = null;
            }

            if (chosen.Invoke(args) is PizzaData created)
            {
                EnsurePizzaDataFields(created);
                return created;
            }
        }

        return null;
    }

    private void EnsurePizzaDataFields(object data)
    {
        if (data == null) return;
        foreach (var field in typeof(PizzaData).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.FieldType == typeof(List<PizzaToppingData>) && field.GetValue(data) == null)
            {
                field.SetValue(data, new List<PizzaToppingData>());
            }
        }
    }

    private void UpdateUI()
    {
        // HUD가 없어도 RemovePizza가 반환되기 전에 이벤트를 보낸다.
        ContentsChanged?.Invoke();
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

    // 피자 강탈
    public bool TryStealAllPizzas()
    {
        bool hadPizza = false;

        if (HasPizza())
        {
            GameObject pizzaObj = RemovePizza();
            if (pizzaObj != null)
            {
                Destroy(pizzaObj);
            }
            hadPizza = true;
        }

        if (HasPackagedPizzas())
        {
            ClearPackagedPizzas();
            hadPizza = true;
        }

        return hadPizza;
    }
}