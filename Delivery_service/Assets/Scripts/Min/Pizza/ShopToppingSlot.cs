using UnityEngine;
using UnityEngine.UI;

public class ShopToppingSlot : MonoBehaviour
{
    [SerializeField] private PizzaToppingData toppingData;
    [SerializeField] private Text stockText;

    private const int MAX_STOCK = 100;

    private void OnEnable()
    {
        ToppingStockManager.OnStockChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        ToppingStockManager.OnStockChanged -= UpdateUI;
    }

    // 구매 함수
    public void BuyTopping(int amount)
    {
        if (toppingData == null || amount <= 0) return;

        int currentStock = ToppingStockManager.GetStock(toppingData.toppingName);
        int pendingStock = ToppingStockManager.GetPendingStock(toppingData.toppingName);

        // 현재 수량 + 배송 예정 수량 + 구매 수량이 100개를 초과하면 차단
        if (currentStock + pendingStock + amount > MAX_STOCK) return;

        int unitPrice = toppingData.cost;
        int totalPrice = unitPrice * amount;

        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(totalPrice, ExpenseType.PizzaTopping))
        {
            // 즉시 지급이 아닌 다음날 배송 주문
            ToppingStockManager.OrderStock(toppingData.toppingName, amount);
        }
    }

    public void UpdateUI()
    {
        if (stockText != null && toppingData != null)
        {
            int currentStock = ToppingStockManager.GetStock(toppingData.toppingName);
            int pendingStock = ToppingStockManager.GetPendingStock(toppingData.toppingName);

            if (pendingStock > 0)
            {
                stockText.text = $"{currentStock}/{MAX_STOCK} (+{pendingStock})";
            }
            else
            {
                stockText.text = $"{currentStock}/{MAX_STOCK}";
            }
        }
    }
}