using UnityEngine;

public class Oven : MonoBehaviour, IInteractable
{
    [Header("피자가 들어갈 위치")]
    [SerializeField] private Transform ovenPlacePoint;

    [Header("굽는 시간")]
    [SerializeField] private float bakingTime = 5f;

    private Pizza currentPizza;
    private float bakingTimer;
    private bool isBaking;
    private bool isFinished;

    public void Interact(ToppingInventory inventory)
    {
        // 굽는 중
        if (isBaking)
        {
            Debug.Log("피자를 굽고 있습니다.");
            return;
        }

        // 구워진 피자가 있음 → 꺼내기
        if (isFinished && currentPizza != null)
        {
            TakePizza(inventory);
            return;
        }

        // 피자가 없음 → 넣기
        if (!inventory.HasPizza())
        {
            Debug.Log("구울 피자를 들고 있어야 합니다.");
            return;
        }

        PutPizza(inventory);
    }

    private void PutPizza(ToppingInventory inventory)
    {
        GameObject pizzaObject = inventory.RemovePizza();

        if (pizzaObject == null)
            return;

        pizzaObject.transform.SetParent(ovenPlacePoint);

        pizzaObject.transform.localPosition = Vector3.zero;
        pizzaObject.transform.localRotation = Quaternion.identity;

        currentPizza = pizzaObject.GetComponent<Pizza>();

        if (currentPizza == null)
        {
            Debug.LogError("피자 오브젝트에 Pizza 스크립트가 없습니다.");
            return;
        }

        isBaking = true;
        isFinished = false;
        bakingTimer = bakingTime;

        Debug.Log("피자를 오븐에 넣었습니다.");
        Debug.Log("굽기 시작");
    }

    private void TakePizza(ToppingInventory inventory)
    {
        if (inventory.HasItem())
        {
            Debug.Log("손에 이미 아이템이 있습니다.");
            return;
        }

        GameObject pizzaObject = currentPizza.gameObject;

        currentPizza.SetPlaced(false);
        currentPizza.SetHeld(true);

        pizzaObject.transform.SetParent(inventory.HoldPoint);
        pizzaObject.transform.localPosition = Vector3.zero;
        pizzaObject.transform.localRotation = Quaternion.identity;

        inventory.AddPizza(pizzaObject);

        currentPizza = null;

        Debug.Log("피자를 들었습니다.");
    }

    private void Update()
    {
        if (!isBaking)
            return;

        bakingTimer -= Time.deltaTime;

        if (bakingTimer <= 0f)
        {
            FinishBaking();
        }
    }

    private void FinishBaking()
    {
        isBaking = false;
        isFinished = true;

        Debug.Log("피자가 구워졌습니다");
    }
}