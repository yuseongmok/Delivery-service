namespace DeliveryService.Yun.ShopUI
{
    public static class ShopSupplyPurchase
    {
        public const int MaximumStock = 100;
        public static bool Quote(PizzaToppingData data, int bundles, out int units, out int price)
        {
            units = price = 0;
            if (data == null || string.IsNullOrWhiteSpace(data.toppingName) || data.cost < 0 || bundles <= 0) return false;
            if (data.toppingType != ToppingType.Pepperoni && data.toppingType != ToppingType.Chocolate
                && data.toppingType != ToppingType.Pineapple && data.toppingType != ToppingType.Jelly) return false;
            long count = (long)bundles * ShopPcUiView.IngredientRow.UnitsPerBundle;
            if (count > int.MaxValue) return false;
            long total = count * data.cost;
            if (total > int.MaxValue) return false;
            units = (int)count;
            price = (int)total;
            return true;
        }

        public static bool TryBuy(PizzaToppingData data, int bundles, out string message)
        {
            if (!Quote(data, bundles, out int units, out int price))
            { message = "재료 가격 또는 수량을 확인해 주세요."; return false; }
            var money = MoneyManager.Instance;
            if (money == null) { message = "소지금 정보를 불러올 수 없습니다."; return false; }
            int stock = ToppingStockManager.GetStock(data.toppingName);
            int pending = ToppingStockManager.GetPendingStock(data.toppingName);
            if (stock < 0 || pending < 0 || (long)stock + pending + units > MaximumStock)
            { message = "보유 재고와 입고 예정 수량은 합계 100개까지 가능합니다."; return false; }
            if (money.currentMoney < price)
            { message = $"잔액이 부족합니다. 필요 금액: {price:N0}원"; return false; }
            if ((long)money.dailyTotalExpense + price > int.MaxValue || (long)money.expensePizza + price > int.MaxValue)
            { message = "지출 한도를 초과하여 발주할 수 없습니다."; return false; }
            if (!money.SpendMoney(price, ExpenseType.PizzaTopping))
            { message = "잔액이 부족합니다."; return false; }
            ToppingStockManager.OrderStock(data.toppingName, units);
            message = $"{data.toppingName} {units}개 발주 완료 · {price:N0}원 · 다음 날 입고";
            return true;
        }
    }
}
