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

        Text loanSummary, openingRent;
        private void OnEnable() { Refresh(); }
        private void Update() { Refresh(); }

        // 이벤트를 중복 등록하지 않고 기존 영업 연결 코드에서 호출한다.
        public void OpenShop()
        {
            if (cycle == null || cycle.isShopOpen) return;
            cycle.OnClick_OpenShop();
            if (!cycle.isShopOpen) return; // 중복 전환 방지를 유지한다.
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
            if(openingRent==null&&openingGate!=null)openingRent=openingGate.transform.Find("OpeningCard/Description")?.GetComponent<Text>();
            if(openingRent!=null){openingRent.text="낮에는 주문을 골라 배달하고, 밤에는 휴대폰에서 다음 사건을 준비하세요.";openingRent.font=MokUIFont.Font;openingRent.resizeTextForBestFit=true;openingRent.resizeTextMinSize=18;}
            bool open = cycle.isShopOpen;
            openingGate.SetActive(!open && !cycle.IsNightPreparation);
            orderList.SetActive(open);
            closeButton.interactable = open;
            bool current = ledger.HasOpenedToday;
            periodLabel.text = $"DAY {cycle.currentDay:00}  ·  오늘 개점부터 현재까지";
            statusLabel.text = cycle.IsNightPreparation ? "낮 영업이 끝났습니다. 미션은 휴대폰에서 확인하세요. 영업 종료 탭에서 다음 날로 넘어갈 수 있습니다." : DailyRent.Preview(cycle.currentDay);
            statusLabel.resizeTextForBestFit=true;statusLabel.resizeTextMinSize=16;statusLabel.resizeTextMaxSize=22;
            statusLabel.color=DailyRent.ProjectedBalance(cycle.currentDay)<0?new Color(.7f,.2f,.2f):new Color(.1f,.17f,.23f);
            incomeLabel.text = Won(current ? ledger.Income : 0);
            expenseLabel.text = Won(current ? ledger.Expense : 0);
            long profit = current ? ledger.Profit : 0;
            profitLabel.text = Won(profit);
            profitLabel.color = profit < 0 ? new Color32(180,70,70,255) : new Color32(21,143,164,255);
            ingredientLabel.text = Won(current ? ledger.Ingredients : 0);
            fuelLabel.text = Won(current ? ledger.Fuel : 0);
            lossLabel.text = Won(current ? ledger.Loss : 0);
            otherLabel.text = Won(current ? ledger.Other : 0);
            if(loanSummary==null&&balanceLabel!=null){
                var go=new GameObject("Loan daily summary",typeof(RectTransform));go.layer=balanceLabel.gameObject.layer;go.transform.SetParent(balanceLabel.transform.parent,false);
                var rt=(RectTransform)go.transform;rt.anchorMin=new Vector2(.055f,.15f);rt.anchorMax=new Vector2(.945f,.19f);rt.offsetMin=rt.offsetMax=Vector2.zero;
                loanSummary=go.AddComponent<Text>();loanSummary.font=MokUIFont.Font;loanSummary.fontSize=21;loanSummary.resizeTextForBestFit=true;loanSummary.resizeTextMinSize=16;loanSummary.resizeTextMaxSize=21;loanSummary.color=new Color(.1f,.17f,.23f);loanSummary.raycastTarget=false;
            }
            var finance=LoanAccount.Current?.ForDay(cycle.currentDay);
            if(loanSummary!=null&&finance!=null)loanSummary.text=$"대출 +{finance.Borrowed:N0}원   원금 상환 {finance.Repaid:N0}원   이자 {finance.Interest:N0}원";
            balanceLabel.text = MoneyManager.Instance == null ? "현재 잔액  —" : "현재 잔액  " + Won(MoneyManager.Instance.currentMoney);
        }
        private static string Won(long value) => value.ToString("N0") + "원";
    }
}
