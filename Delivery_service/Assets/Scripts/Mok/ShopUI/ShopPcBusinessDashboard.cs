using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Yun.ShopUI;

namespace DeliveryService.Mok.ShopUI
{
    public sealed class ShopPcBusinessDashboard : MonoBehaviour
    {
        public ShopPC pc;
        public DayNightCycle cycle;
        public ShopDayLedger ledger;
        public ShopPcUiView view;
        public GameObject openingGate, orderList;
        public Button openButton, closeButton;
        public Text periodLabel, statusLabel, incomeLabel, expenseLabel, profitLabel;
        public Text ingredientLabel, fuelLabel, lossLabel, otherLabel, balanceLabel;

        private void OnEnable() { Refresh(); }
        private void Update() { Refresh(); }

        // Called by the existing business bridge instead of duplicating its listener.
        public void OpenShop()
        {
            if (cycle == null || cycle.isShopOpen) return;
            cycle.OnClick_OpenShop();
            if (!cycle.isShopOpen) return; // Preserve the transition guard.
            if (!ledger.HasOpenedToday && MoneyManager.Instance != null)
                MoneyManager.Instance.ResetDailyStats();
            ledger.BeginDay();
            gameObject.SetActive(true);
            view.SelectTab(0);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (pc.cameraLookScript != null) pc.cameraLookScript.enabled = false;
            Refresh();
        }

        public void Refresh()
        {
            if (cycle == null || ledger == null) return;
            bool open = cycle.isShopOpen;
            openingGate.SetActive(!open);
            orderList.SetActive(open);
            closeButton.interactable = open;
            bool current = ledger.HasOpenedToday;
            periodLabel.text = $"DAY {cycle.currentDay:00}  ·  오늘 개점부터 현재까지";
            statusLabel.text = open ? "영업 중  ·  거래가 발생하면 통계에 반영됩니다."
                : "개점 전입니다. 주문 탭에서 개점해 주세요.";
            incomeLabel.text = Won(current ? ledger.Income : 0);
            expenseLabel.text = Won(current ? ledger.Expense : 0);
            long profit = current ? ledger.Profit : 0;
            profitLabel.text = Won(profit);
            profitLabel.color = profit < 0 ? new Color32(180,70,70,255) : new Color32(21,143,164,255);
            ingredientLabel.text = Won(current ? ledger.Ingredients : 0);
            fuelLabel.text = Won(current ? ledger.Fuel : 0);
            lossLabel.text = Won(current ? ledger.Loss : 0);
            otherLabel.text = Won(current ? ledger.Other : 0);
            balanceLabel.text = MoneyManager.Instance == null ? "현재 잔액  —" : "현재 잔액  " + Won(MoneyManager.Instance.currentMoney);
        }
        private static string Won(long value) => value.ToString("N0") + "원";
    }
}
