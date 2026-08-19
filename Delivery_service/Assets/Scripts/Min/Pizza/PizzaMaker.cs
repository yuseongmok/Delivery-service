using UnityEngine;

public class PizzaMaker : MonoBehaviour, IInteractable
{
    [Header("도우가 놓일 위치")]
    [SerializeField] private Transform doughPlacePoint;

    private Pizza currentPizza;

    public void Interact(ToppingInventory inventory)
    {
        // 제작대에 피자가 없는 경우
        if (currentPizza == null)
        {
            // 손에 피자가 있으면 제작대에 다시 내려놓기
            if (inventory.HasPizza())
            {
                PlacePizza(inventory);
                return;
            }

            // 손에 아무것도 없으면
            if (!inventory.HasItem())
            {
                Debug.Log("도우를 가져와야 합니다.");
                return;
            }

            // 손에 재료가 있으면
            if (inventory.HasTopping())
            {
                PizzaToppingData item = inventory.RemoveItem();

                if (item.toppingType != ToppingType.Dough)
                {
                    Debug.Log("여기에는 도우만 놓을 수 있습니다.");
                    inventory.AddItem(item);
                    return;
                }

                PlaceDough(item);
                return;
            }

            return;
        }

        // 제작대에 피자가 있는 경우

        // 손에 토핑이 있으면 토핑 추가
        if (inventory.HasTopping())
        {
            currentPizza.Interact(inventory);
            return;
        }

        // 손이 비어있으면 피자를 들기
        if (!inventory.HasItem())
        {
            TakePizza(inventory);
            return;
        }
    }

    private void PlaceDough(PizzaToppingData dough)
    {
        if (dough.toppingPrefab == null)
            return;

        GameObject doughObject = Instantiate(
            dough.toppingPrefab,
            doughPlacePoint.position,
            doughPlacePoint.rotation,
            doughPlacePoint
        );

        doughObject.transform.localPosition = Vector3.zero;
        doughObject.transform.localRotation = Quaternion.identity;

        currentPizza = doughObject.GetComponent<Pizza>();

        if (currentPizza == null)
        {
            Destroy(doughObject);
            return;
        }

        currentPizza.SetPlaced(true);

        Debug.Log("도우를 제작대에 놓았습니다.");
    }

    private void PlacePizza(ToppingInventory inventory)
    {
        GameObject pizzaObject = inventory.RemovePizza();

        if (pizzaObject == null)
            return;

        Pizza pizza = pizzaObject.GetComponent<Pizza>();

        pizzaObject.transform.SetParent(doughPlacePoint);
        pizzaObject.transform.localPosition = Vector3.zero;
        pizzaObject.transform.localRotation = Quaternion.identity;

        pizza.SetHeld(false);
        pizza.SetPlaced(true);

        currentPizza = pizza;

        Debug.Log("피자를 제작대에 다시 놓았습니다.");
    }

    private void TakePizza(ToppingInventory inventory)
    {
        if (currentPizza == null)
            return;

        if (inventory.HasItem())
        {
            Debug.Log("손에 이미 아이템이 있습니다.");
            return;
        }

        GameObject pizzaObject = currentPizza.gameObject;

        currentPizza = null;

        pizzaObject.transform.SetParent(inventory.HoldPoint);
        pizzaObject.transform.localPosition = Vector3.zero;
        pizzaObject.transform.localRotation = Quaternion.identity;

        inventory.AddPizza(pizzaObject);
    }
}