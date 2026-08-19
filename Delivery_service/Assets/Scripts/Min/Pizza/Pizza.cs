using System.Collections.Generic;
using UnityEngine;

public class Pizza : MonoBehaviour, IInteractable
{
    [Header("토핑 생성 위치")]
    [SerializeField] private Transform toppingPoint;

    [SerializeField] private Collider doughCollider;
    [SerializeField] private float toppingRadiusRatio = 0.4f;

    private List<PizzaToppingData> toppings = new List<PizzaToppingData>();

    public bool IsPlaced { get; private set; }

    public void SetPlaced(bool value)
    {
        IsPlaced = value;
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

        SpawnTopping(topping);

        Debug.Log($"{topping.toppingName} 추가");
    }

    private void SpawnTopping(PizzaToppingData topping)
    {
        if (topping.toppingPrefab == null)
            return;

        if (toppingPoint == null)
            return;

        if (topping.toppingType == ToppingType.Sauce || topping.toppingType == ToppingType.Cheese)
        {
            Instantiate(topping.toppingPrefab, toppingPoint.position, toppingPoint.rotation, toppingPoint);
            return;
        }

        float radius = Mathf.Min(doughCollider.bounds.extents.x, doughCollider.bounds.extents.z) * toppingRadiusRatio;

        for (int i = 0; i < topping.spawnCount; i++)
        {
            float angle = (360f / topping.spawnCount) * i + Random.Range(-15f, 15f);
            float randomRadius = Random.Range(radius * 0.2f, radius);

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
}