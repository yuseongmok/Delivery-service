using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private CurrentOrderUI currentOrderUI;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private CameraMove cameraMove;

    [Header("주문 생성 및 타이머")]
    [SerializeField] private OrderGenerator orderGenerator;
    [SerializeField] private DayNightCycle dayNightCycle;
    private const float orderIntervalSeconds = 30f;
    private const int dailyOrderLimit = 7;
    private int generatedToday;
    private float nextOrderWait = -1f;
    public int OrdersGeneratedToday => generatedToday;
    public int DailyOrderLimit => dailyOrderLimit;
    public bool DailyOrdersExhausted => generatedToday >= dailyOrderLimit && !HasActiveOrder && pendingOrders.Count == 0;

    private void ScheduleNextOrder()
    {
        if (generatedToday < dailyOrderLimit && !HasActiveOrder && pendingOrders.Count == 0)
            nextOrderWait = orderIntervalSeconds;
    }

    private void AdvanceOrderWait(float realSeconds)
    {
        if (dayNightCycle == null || !dayNightCycle.isShopOpen || nextOrderWait < 0f) return;
        if (HasActiveOrder || pendingOrders.Count != 0) return;
        nextOrderWait -= realSeconds;
        if (nextOrderWait > 0f) return;
        nextOrderWait = -1f;
        GenerateSingleOrder();
    }

    [Header("정산 설정")]
    [SerializeField] private int basePizzaPrice = 10000;

    [Header("토핑 데이터 목록")]
    [SerializeField] private List<PizzaToppingData> toppingDataList = new List<PizzaToppingData>();
    [SerializeField] private List<DeliveryPoint> deliveryPoints = new List<DeliveryPoint>();

    // 누적 대기 주문 목록
    private List<PizzaOrder> pendingOrders = new List<PizzaOrder>();

    // 현재 진행 중인 배달 주문
    private PizzaOrder activeOrder;
    private string currentTargetDeliveryID;
    // 코드의 밸런스 값을 사용하며 이전 Inspector 저장값은 무시한다.
    private const float deliveryTimeLimitMinutes = 80f;
    private double deliveryDeadlineMinutes;
    private double GameMinutes => dayNightCycle == null ? 0d
        : dayNightCycle.currentDay * 1440d + dayNightCycle.currentTime * 60d;
    public float RemainingDeliveryMinutes => !HasActiveOrder ? 0f
        : (float)System.Math.Max(0d, deliveryDeadlineMinutes - GameMinutes);

    private void UpdateDeliveryDeadline()
    {
        if (!HasActiveOrder || dayNightCycle == null) return;
        if (GameMinutes >= deliveryDeadlineMinutes)
        {
            PizzaOrder expired = activeOrder;
            activeOrder = null;
            currentTargetDeliveryID = "";
            deliveryDeadlineMinutes = 0;
            if (currentOrderUI != null) currentOrderUI.DisplayCancelledOrder(expired);
            var navigation = FindObjectOfType<NavigationManager>();
            if (navigation != null) navigation.ResetNavigationForNewDay();
            ScheduleNextOrder();
            OrdersChanged?.Invoke();
            return;
        }
        if (currentOrderUI != null)
            currentOrderUI.DisplayTimedOrder(activeOrder, currentTargetDeliveryID, RemainingDeliveryMinutes);
    }

    // 코루틴 및 영업 상태 감시 변수
    private bool prevShopOpenState = false;

    // UI는 기존 대기열을 표시하고 주문 생성과 배달은 여기서 관리한다.
    private int nextOrderNumber = 1;
    private int orderNumberDay = -1;
    public event System.Action OrdersChanged;
    public System.Collections.ObjectModel.ReadOnlyCollection<PizzaOrder> PendingOrders => pendingOrders.AsReadOnly();
    public PizzaOrder ActiveOrder => activeOrder;

    public bool TryAcceptOrder(PizzaOrder order)
    {
        if (order == null || HasActiveOrder || !pendingOrders.Contains(order)) return false;
        HandleAccept(order);
        return activeOrder == order;
    }

    public bool TryDeclineOrder(PizzaOrder order)
    {
        if (order == null || !pendingOrders.Contains(order)) return false;
        HandleDecline(order);
        return true;
    }

    private void UpdateOrderNumberDay()
    {
        int day = dayNightCycle != null ? dayNightCycle.currentDay : 1;
        if (orderNumberDay == day) return;
        orderNumberDay = day;
        nextOrderNumber = 1;
        generatedToday = 0;
        nextOrderWait = -1f;
    }

    public string CurrentTargetDeliveryID => currentTargetDeliveryID;
    public bool HasActiveOrder => activeOrder != null;

    public DeliveryPoint CurrentTargetDeliveryPoint
    {
        get
        {
            if (string.IsNullOrEmpty(currentTargetDeliveryID) || deliveryPoints == null)
                return null;

            return deliveryPoints.Find(dp => dp != null && dp.DeliveryPointID == currentTargetDeliveryID);
        }
    }

    private void Start()
    {
        if (orderGenerator == null) orderGenerator = FindObjectOfType<OrderGenerator>();
        if (dayNightCycle == null) dayNightCycle = FindObjectOfType<DayNightCycle>();
    }

    private void OnEnable()
    {
        if (orderUI != null)
        {
            orderUI.OnOrderAccepted += HandleAccept;
            orderUI.OnOrderDeclined += HandleDecline;
        }
    }

    private void OnDisable()
    {
        if (orderUI != null)
        {
            orderUI.OnOrderAccepted -= HandleAccept;
            orderUI.OnOrderDeclined -= HandleDecline;
        }
    }

    private void Update()
    {
        // DayNightCycle 자동 감지 및 감시
        if (dayNightCycle == null) dayNightCycle = FindObjectOfType<DayNightCycle>();

        if (dayNightCycle != null)
        {
            // [상태 감지 1] 영업 시작 (false -> true)
            if (dayNightCycle.isShopOpen && !prevShopOpenState)
            {
                OnShopOpened();
            }
            // [상태 감지 2] 마감 및 다음 날 리셋 (true -> false)
            else if (!dayNightCycle.isShopOpen && prevShopOpenState)
            {
                // 마감 확인을 취소하면 주문과 대기시간을 유지한다.
            }

            prevShopOpenState = dayNightCycle.isShopOpen;
        }
        AdvanceOrderWait(Time.unscaledDeltaTime);
        UpdateDeliveryDeadline();
    }

    // 영업 시작 시 자동 호출 (1일차, 2일차 등 매일 영업 시작 때마다 실행)
    public void OnShopOpened()
    {
        UpdateOrderNumberDay();
        if (generatedToday == 0) GenerateSingleOrder();
        else if (!HasActiveOrder && pendingOrders.Count == 0 && nextOrderWait < 0f)
            ScheduleNextOrder();
    }

    // 마감 또는 날짜 전환 시 자동 호출되는 리셋 메서드
    public void ResetOrdersForNewDay()
    {
        nextOrderWait = -1f;
        prevShopOpenState = false;

        UpdateOrderNumberDay();
        pendingOrders.Clear();
        activeOrder = null;
        currentTargetDeliveryID = "";

        if (orderUI != null) orderUI.RefreshOrderList(pendingOrders);
        if (currentOrderUI != null) currentOrderUI.ClearOrder();
        OrdersChanged?.Invoke();

        // 내비게이션도 동시 리셋
        NavigationManager navManager = FindObjectOfType<NavigationManager>();
        if (navManager != null)
        {
            navManager.ResetNavigationForNewDay();
        }

        Debug.Log("[OrderManager] 마감 감지: 주문/배달타겟/네비게이션이 완전히 초기화되었습니다.");
    }

    private void GenerateSingleOrder()
    {
        UpdateOrderNumberDay();
        if (orderGenerator == null || generatedToday >= dailyOrderLimit || HasActiveOrder || pendingOrders.Count != 0) return;
        if (dayNightCycle == null || !dayNightCycle.isShopOpen) return;

        List<PizzaOrder> newOrders = orderGenerator.GenerateSinglePizzaOrder();
        if (newOrders == null) return;

        UpdateOrderNumberDay();
        foreach (var order in newOrders)
        {
            order.orderName = $"주문 {nextOrderNumber++:D2}";
            pendingOrders.Add(order);
            generatedToday++;
            break; // 생성기가 여러 주문을 반환해도 한 주기에는 한 번만 처리한다.
        }

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
        OrdersChanged?.Invoke();
    }

    private void HandleAccept(PizzaOrder acceptedOrder)
    {
        if (acceptedOrder == null || !pendingOrders.Contains(acceptedOrder)) return;
        if (HasActiveOrder)
        {
            Debug.Log("이미 진행 중인 배달 주문이 있습니다! 먼저 배달을 완료해주세요.");
            return;
        }

        if (dayNightCycle == null) dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (dayNightCycle == null) return;
        deliveryDeadlineMinutes = GameMinutes + deliveryTimeLimitMinutes;
        activeOrder = acceptedOrder;
        pendingOrders.Remove(acceptedOrder);

        if (deliveryPoints != null && deliveryPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, deliveryPoints.Count);
            currentTargetDeliveryID = deliveryPoints[randomIndex].DeliveryPointID;
        }

        if (currentOrderUI != null)
        {
            currentOrderUI.DisplayTimedOrder(activeOrder, currentTargetDeliveryID, RemainingDeliveryMinutes);
        }

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
        OrdersChanged?.Invoke();
    }

    private void HandleDecline(PizzaOrder declinedOrder)
    {
        if (declinedOrder == null || !pendingOrders.Remove(declinedOrder)) return;

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
        ScheduleNextOrder();
        OrdersChanged?.Invoke();
    }

    // 배달 완료 검사 및 정산
    public bool TryDeliverPackagedStack(List<PizzaData> pizzaDataList, string deliveryPointID)
    {
        // 다음 Update 전에 배달이 처리되지 않도록 즉시 검사한다.
        UpdateDeliveryDeadline();
        if (pizzaDataList == null || pizzaDataList.Count == 0 || !HasActiveOrder)
            return false;

        if (deliveryPointID != currentTargetDeliveryID)
        {
            Debug.Log("여기가 아닙니다.");
            return false;
        }

        PizzaData deliveredPizza = pizzaDataList[0];
        bool isBaked = deliveredPizza.isBaked;
        bool isBaseValid = HasBaseIngredients(deliveredPizza.toppings);
        bool isRuined = deliveredPizza.isRuined;

        if (!isRuined && isBaked && isBaseValid)
        {
            int totalEarnedMoney = basePizzaPrice;

            // 추가 토핑 정산 (기본 재료 제외)
            if (deliveredPizza.toppings != null)
            {
                foreach (string toppingName in deliveredPizza.toppings)
                {
                    if (IsBaseIngredient(toppingName)) continue;

                    PizzaToppingData matchedTopping = GetToppingData(toppingName);
                    if (matchedTopping != null)
                    {
                        totalEarnedMoney += matchedTopping.sellingPrice;
                    }
                }
            }

            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(totalEarnedMoney);
            }

            Debug.Log($"배달 성공 {totalEarnedMoney}원");
        }
        else
        {
            if (isRuined)
            {
                Debug.Log("망가진 피자를 배달해서 0원");
            }
            else
            {
                Debug.Log("안구워서 0원");
            }
        }

        activeOrder = null;
        currentTargetDeliveryID = "";

        if (currentOrderUI != null)
        {
            currentOrderUI.ClearOrder();
        }

        RobberyManager.NotifyDeliveryCompleted();
        DeliveryService.Mok.Stocks.StockMarket.Instance?.DeliveryClue();
        ScheduleNextOrder();
        OrdersChanged?.Invoke();
        return true;
    }

    // 필수 기본 재료(도우, 소스, 치즈) 포함 여부 검사
    private bool HasBaseIngredients(List<string> toppings)
    {
        if (toppings == null || toppings.Count == 0) return false;

        bool hasDough = false;
        bool hasSauce = false;
        bool hasCheese = false;

        foreach (string t in toppings)
        {
            if (string.IsNullOrEmpty(t)) continue;
            string lower = t.ToLower().Trim();

            if (lower.Contains("dough") || lower.Contains("도우")) hasDough = true;
            if (lower.Contains("sauce") || lower.Contains("소스")) hasSauce = true;
            if (lower.Contains("cheese") || lower.Contains("치즈")) hasCheese = true;
        }

        return hasDough && hasSauce && hasCheese;
    }

    private bool IsBaseIngredient(string toppingIdentifier)
    {
        if (string.IsNullOrEmpty(toppingIdentifier)) return false;
        string lower = toppingIdentifier.ToLower().Trim();
        return lower.Contains("dough") || lower.Contains("도우") ||
               lower.Contains("sauce") || lower.Contains("소스") ||
               lower.Contains("cheese") || lower.Contains("치즈");
    }

    private PizzaToppingData GetToppingData(string toppingIdentifier)
    {
        if (toppingDataList == null || string.IsNullOrEmpty(toppingIdentifier)) return null;

        string lowerTarget = toppingIdentifier.ToLower().Trim();

        return toppingDataList.Find(data =>
        {
            if (data == null) return false;
            if (!string.IsNullOrEmpty(data.toppingName) && lowerTarget.Contains(data.toppingName.ToLower().Trim())) return true;
            if (lowerTarget.Contains(data.toppingType.ToString().ToLower().Trim())) return true;
            return false;
        });
    }
}