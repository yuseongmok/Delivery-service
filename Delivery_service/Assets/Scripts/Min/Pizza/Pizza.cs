using System.Collections.Generic;
using UnityEngine;

public class Pizza : MonoBehaviour, IInteractable
{
    [Header("토핑 생성 위치")]
    [SerializeField] private Transform toppingPoint;

    [SerializeField] private Collider doughCollider;
    [SerializeField] private float toppingRadiusRatio = 0.4f;

    private List<PizzaToppingData> toppings = new List<PizzaToppingData>();
    public List<string> currentToppings = new List<string>();

    public bool IsPlaced { get; private set; }
    public bool IsBaked { get; private set; }

    private void Awake()
    {
        InitBasePizza();
    }

    public void InitBasePizza()
    {
        currentToppings.Clear();

        // 기본 3종
        currentToppings.Add("도우");
        currentToppings.Add("소스");
        currentToppings.Add("치즈");

        IsBaked = false;
    }

    public void SetPlaced(bool value)
    {
        IsPlaced = value;
    }
    public void SetBaked(bool value)
    {
        IsBaked = value;
    }
    public PizzaData ToData()
    {
        return new PizzaData(currentToppings, IsBaked);
    }

    public void Interact(ToppingInventory inventory)
    {
        if (!IsPlaced)
            return;

        if (!inventory.HasTopping())
            return;

        PizzaToppingData item = inventory.RemoveItem();

        if (item.toppingType == ToppingType.Dough)
        {
            Debug.Log("도우 위에 도우를 올릴 수 없습니다.");
            inventory.AddItem(item);
            return;
        }

        AddTopping(item);
    }

    private void AddTopping(PizzaToppingData topping)
    {
        toppings.Add(topping);

        if (!currentToppings.Contains(topping.toppingName))
        {
            currentToppings.Add(topping.toppingName);
        }

        SpawnTopping(topping);

        Debug.Log($"{topping.toppingName} 추가");
    }

    private void SpawnTopping(PizzaToppingData topping)
    {
        if (topping.toppingPrefab == null || toppingPoint == null)
            return;

        if (topping.toppingType == ToppingType.Sauce || topping.toppingType == ToppingType.Cheese)
        {
            Instantiate(topping.toppingPrefab, toppingPoint.position, toppingPoint.rotation, toppingPoint);
            return;
        }

        float radius = Mathf.Min(doughCollider.bounds.extents.x, doughCollider.bounds.extents.z) * toppingRadiusRatio;
        float angleOffset = toppingPoint.childCount * 47f;

        for (int i = 0; i < topping.spawnCount; i++)
        {
            float angle = ((360f / topping.spawnCount) * i) + angleOffset + Random.Range(-15f, 15f);
            float randomRadius = Random.Range(radius * 0.35f, radius);

            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * randomRadius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * randomRadius;

            Vector3 spawnPosition = toppingPoint.position + new Vector3(x, 0f, z);

            Instantiate(topping.toppingPrefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), toppingPoint);
        }
    }

    public void SetHeld(bool held)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = !held;
        }
    }

    public bool MatchesOrder(PizzaOrder order)
    {
        if (order == null || order.toppings == null) return false;

        // 안구워졌으면 실패
        if (!IsBaked) return false;

        // 주문서 목록을 가져와 기본 3종을 강제로 포함시킨 정답 세트 생성
        HashSet<string> expectedSet = new HashSet<string>(order.toppings);
        expectedSet.Add("도우");
        expectedSet.Add("소스");
        expectedSet.Add("치즈");

        // 현재 완성된 피자의 토핑
        HashSet<string> currentSet = new HashSet<string>(currentToppings);

        return currentSet.SetEquals(expectedSet);
    }
}