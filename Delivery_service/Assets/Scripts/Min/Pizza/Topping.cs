using UnityEngine;
using UnityEngine.UI;

public interface IInteractable
{
    void Interact(ToppingInventory inventory);
}

public class Topping : MonoBehaviour, IInteractable
{
    [SerializeField] private PizzaToppingData toppingData;
    [SerializeField] private Text stockText;

    private void Start()
    {
        UpdateVisuals();
    }

    private void OnEnable()
    {
        ToppingStockManager.OnStockChanged += UpdateVisuals;
        UpdateVisuals();
    }

    private void OnDisable()
    {
        ToppingStockManager.OnStockChanged -= UpdateVisuals;
    }

    public void Interact(ToppingInventory inventory)
    {
        if (toppingData == null) return;
        if (inventory.HasItem()) return;

        bool isBasicTopping = toppingData.toppingType == ToppingType.Dough ||
                              toppingData.toppingType == ToppingType.Sauce ||
                              toppingData.toppingType == ToppingType.Cheese;

        if (isBasicTopping)
        {
            // 기본 재료 - cost 만 차감
            if (toppingData.cost <= 0)
            {
                inventory.AddItem(toppingData);
            }
            else if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(toppingData.cost, ExpenseType.PizzaTopping))
            {
                inventory.AddItem(toppingData);
            }
        }
        else
        {
            // 일반 토핑 - 재료 차감
            int currentStock = ToppingStockManager.GetStock(toppingData.toppingName);
            if (currentStock <= 0) return;

            // 수량 1개 차감 후 인벤토리에 지급
            ToppingStockManager.ConsumeStock(toppingData.toppingName, 1);
            inventory.AddItem(toppingData);
        }
    }

    public void UpdateVisuals()
    {
        if (toppingData == null) return;

        bool isBasicTopping = toppingData.toppingType == ToppingType.Dough ||
                              toppingData.toppingType == ToppingType.Sauce ||
                              toppingData.toppingType == ToppingType.Cheese;

        if (!isBasicTopping)
        {
            int currentStock = ToppingStockManager.GetStock(toppingData.toppingName);

            if (stockText != null)
            {
                stockText.text = $"{currentStock}/100";
            }
        }
    }
}