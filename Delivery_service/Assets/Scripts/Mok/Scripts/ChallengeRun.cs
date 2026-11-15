using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;

namespace DeliveryService.Mok
{
    // 도전 모드의 영업일과 결산만 관리한다. 주문/조리/배달은 기존 시스템을 사용한다.
    public sealed class ChallengeRun : MonoBehaviour
    {
        public const float OpeningHour = 8f, ClosingHour = 20f;
        private DayNightCycle cycle;
        private OrderManager orders;
        private Text heading, details, confirmLabel, cancelLabel;
        private Button confirmButton, cancelButton;
        private bool settled, closing, wasOpen;
        public bool Bankrupt { get; private set; }
        public bool Settled => settled;

        public void Initialize(DayNightCycle owner)
        {
            cycle = owner;
            orders = FindFirstObjectByType<OrderManager>();
            BuildSummary();
            cycle.currentDay = 1;
            cycle.timeMultiplier = (ClosingHour - OpeningHour) * 3600f / Mathf.Max(60, ChallengeRules.Current.businessSeconds);
            BeginDay();
        }

        private void BeginDay()
        {
            settled = closing = Bankrupt = false;
            cycle.currentTime = OpeningHour;
            cycle.isShopOpen = false;
            MoneyManager.Instance?.ResetDailyStats();
            FindFirstObjectByType<ShopDayLedger>()?.BeginDay();
            ToppingStockManager.DeliverAllPendingStock();
            orders?.ResetOrdersForNewDay();
        }

        public void OpenShop()
        {
            if (closing || settled || Bankrupt || cycle.IsTransitioning) return;
            cycle.isShopOpen = true;
        }

        public void Tick()
        {
            if (Time.timeScale <= 0 || settled) return;
            if (cycle.isShopOpen)
                cycle.currentTime = Mathf.Min(ClosingHour, cycle.currentTime + Time.deltaTime * cycle.timeMultiplier / 3600f);
            if (cycle.currentTime >= ClosingHour && !closing)
            {
                closing = true;
                cycle.isShopOpen = false;
                orders?.CloseOrderReception();
            }
            // 배달 중인 피자를 갑자기 취소하거나 도로에서 결산으로 끊지 않는다.
            if (closing && (!orders || !orders.HasActiveOrder) && !PoliceControl.IsArrestInProgress)
            {
                var combat = FindFirstObjectByType<PlayerCombat>();
                if (combat && (combat.IsDown || combat.RiddenBike || combat.WaterRescueActive)) return;
                RequestClose();
            }
        }

        public void RequestClose()
        {
            if (settled || cycle.summaryPanel.activeSelf) return;
            if (orders && orders.HasActiveOrder) { DeliveryRequestNotice.Show("진행 중인 배달을 완료한 뒤 마감할 수 있습니다."); return; }
            wasOpen = cycle.isShopOpen;
            cycle.isShopOpen = false;
            if (cycle.pcUIPanel) cycle.pcUIPanel.SetActive(false);
            ShowSummary();
        }

        private void ShowSummary()
        {
            var money = MoneyManager.Instance;
            heading.text = Bankrupt ? "도전 종료" : settled ? $"DAY {cycle.currentDay} · 영업 완료" : $"DAY {cycle.currentDay} · 마감 정산";
            int income = money ? money.dailyIncome : 0;
            int expense = money ? money.dailyTotalExpense : 0;
            string costs = settled ? "오늘의 고정비와 이자를 납부했습니다." : DailyRent.Preview(cycle.currentDay);
            details.text = $"매출  {income:N0}원\n지출  {expense:N0}원\n영업 손익  {income - expense:N0}원\n\n{costs}\n\n현재 잔액  {(money ? money.currentMoney : 0):N0}원";
            if (Bankrupt) details.text += $"\n\n{cycle.currentDay - 1}일 생존 · 비용을 감당하지 못했습니다.";
            confirmLabel.text = Bankrupt ? "로비로 돌아가기" : settled ? "다음 영업일" : "비용 납부하기";
            cancelLabel.text = "영업 계속";
            cancelButton.gameObject.SetActive(!settled && !closing);
            cycle.summaryPanel.SetActive(true);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Confirm()
        {
            if (!cycle.summaryPanel.activeSelf) return;
            if (!settled)
            {
                orders?.CloseOrderReception();
                DailyRent.Current?.SettleDay(cycle.currentDay);
                LoanAccount.Current?.SettleDay(cycle.currentDay);
                settled = true;
                Bankrupt = MoneyManager.Instance && MoneyManager.Instance.currentMoney < 0;
                int survived = Bankrupt ? cycle.currentDay - 1 : cycle.currentDay;
                if (survived > PlayerPrefs.GetInt("ChallengeBestDays", 0))
                {
                    PlayerPrefs.SetInt("ChallengeBestDays", survived);
                    PlayerPrefs.Save();
                }
                ShowSummary();
                return;
            }
            HideSummary();
            if (Bankrupt)
            {
                GameModeSession.Select(false);
                SceneManager.LoadScene("Lobby");
                return;
            }
            cycle.currentDay++;
            BeginDay();
        }

        public void CancelClose()
        {
            if (settled || closing || !cycle.summaryPanel.activeSelf) return;
            HideSummary();
            cycle.isShopOpen = wasOpen;
        }

        private void HideSummary()
        {
            cycle.summaryPanel.SetActive(false);
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void BuildSummary()
        {
            // 같은 패널 참조를 유지하되 화면 비율에 맞는 독립 캔버스에 표시한다.
            var overlay = new GameObject("Challenge settlement canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
            var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var panel = (RectTransform)cycle.summaryPanel.transform; panel.SetParent(overlay.transform, false);
            panel.localScale = Vector3.one; panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            var shade = panel.GetComponent<Image>(); if(shade)shade.color = new Color(0,0,0,.72f);
            // 기존 설정/PC 입력 잠금이 인식하는 결산 패널을 그대로 사용한다.
            foreach (Transform child in cycle.summaryPanel.transform) child.gameObject.SetActive(false);
            var card = Rect(cycle.summaryPanel.transform, "Challenge settlement", new Vector2(720, 640));
            var background = card.gameObject.AddComponent<Image>();
            background.color = new Color(.045f, .08f, .12f, .99f);
            heading = Label(card, new Vector2(0, 250), new Vector2(650, 60), 34);
            details = Label(card, new Vector2(0, 35), new Vector2(630, 355), 25);
            details.alignment = TextAnchor.UpperLeft;
            confirmButton = Button(card, "Confirm", new Vector2(0, -200), out confirmLabel);
            cancelButton = Button(card, "Cancel", new Vector2(0, -270), out cancelLabel);
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(CancelClose);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            return rect;
        }
        private static Text Label(Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(parent, "Label", size); rect.anchoredPosition = position;
            var text = rect.gameObject.AddComponent<Text>();
            text.font = MokUIFont.Font; text.fontSize = fontSize; text.color = new Color(1, .94f, .82f);
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string name, Vector2 position, out Text label)
        {
            var rect = Rect(parent, name, new Vector2(610, 56)); rect.anchoredPosition = position;
            rect.gameObject.AddComponent<Image>().color = new Color(.27f, .18f, .12f);
            var button = rect.gameObject.AddComponent<Button>();
            label = Label(rect, Vector2.zero, new Vector2(590, 52), 25);
            return button;
        }
    }
}
