using UnityEngine;

namespace DeliveryService.Mok.ShopUI
{
    // PC 본체에 붙어 있으므로 화면을 닫아도 회계를 계속 기록한다.
    public sealed class ShopDayLedger : MonoBehaviour
    {
        public DayNightCycle cycle;
        public int Day { get; private set; } = -1;
        public long Income { get; private set; }
        public long Expense { get; private set; }
        public long Ingredients { get; private set; }
        public long Fuel { get; private set; }
        public long Loss { get; private set; }
        public long Other => Expense - Ingredients - Fuel - Loss;
        public long Profit => Income - Expense;
        public bool HasOpenedToday => cycle != null && Day == cycle.currentDay;
        private MoneyManager money;

        private void OnEnable() { Bind(); }
        private void Update()
        {
            Bind();
            if (cycle != null && cycle.isShopOpen) BeginDay();
        }
        private void Bind()
        {
            if (money == MoneyManager.Instance) return;
            if (money != null) money.TransactionRecorded -= Record;
            money = MoneyManager.Instance;
            if (money != null) money.TransactionRecorded += Record;
        }
        public void BeginDay()
        {
            Bind();
            if (cycle == null || HasOpenedToday) return;
            Day = cycle.currentDay;
            Income = Expense = Ingredients = Fuel = Loss = 0;
        }
        private void Record(int amount, ExpenseType type)
        {
            // 개점 버튼을 누르기 전 거래는 오늘 통계에 포함하지 않는다.
            if (cycle == null || !cycle.isShopOpen) return;
            BeginDay();
            if (amount > 0) Income += amount;
            else
            {
                long cost = -(long)amount;
                Expense += cost;
                if (type == ExpenseType.PizzaTopping) Ingredients += cost;
                else if (type == ExpenseType.Fuel) Fuel += cost;
                else if (type == ExpenseType.Loss) Loss += cost;
            }
        }
        private void OnDisable()
        {
            if (money != null) money.TransactionRecorded -= Record;
            money = null;
        }
    }
}
