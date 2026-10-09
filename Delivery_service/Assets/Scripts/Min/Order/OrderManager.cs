using System.Collections;
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
    [SerializeField] private float orderIntervalSeconds = 125f;

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

    // 코루틴 및 영업 상태 감시 변수
    private Coroutine autoOrderCoroutine;
    private bool prevShopOpenState = false;

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
                ResetOrdersForNewDay();
            }

            prevShopOpenState = dayNightCycle.isShopOpen;
        }
    }

    // 영업 시작 시 자동 호출 (1일차, 2일차 등 매일 영업 시작 때마다 실행)
    public void OnShopOpened()
    {
        // 1. 영업 시작 즉시 주문 1개 생성
        GenerateSingleOrder();

        // 2. 타이머 리셋 후 125초 주기 시작
        if (autoOrderCoroutine != null) StopCoroutine(autoOrderCoroutine);
        autoOrderCoroutine = StartCoroutine(AutoOrderAccumulateRoutine());

        Debug.Log("[OrderManager] 영업 시작 감지: 즉시 주문 1개 생성 및 125초 타이머 재시작");
    }

    // 마감 또는 날짜 전환 시 자동 호출되는 리셋 메서드
    public void ResetOrdersForNewDay()
    {
        if (autoOrderCoroutine != null)
        {
            StopCoroutine(autoOrderCoroutine);
            autoOrderCoroutine = null;
        }

        pendingOrders.Clear();
        activeOrder = null;
        currentTargetDeliveryID = "";

        if (orderUI != null) orderUI.RefreshOrderList(pendingOrders);
        if (currentOrderUI != null) currentOrderUI.ClearOrder();

        // 내비게이션도 동시 리셋
        NavigationManager navManager = FindObjectOfType<NavigationManager>();
        if (navManager != null)
        {
            navManager.ResetNavigationForNewDay();
        }

        Debug.Log("[OrderManager] 마감 감지: 주문/배달타겟/네비게이션이 완전히 초기화되었습니다.");
    }

    private IEnumerator AutoOrderAccumulateRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(orderIntervalSeconds);

            bool isShopOpen = dayNightCycle == null || dayNightCycle.isShopOpen;
            if (isShopOpen)
            {
                GenerateSingleOrder();
            }
        }
    }

    private void GenerateSingleOrder()
    {
        if (orderGenerator == null) return;

        List<PizzaOrder> newOrders = orderGenerator.GenerateSinglePizzaOrder();
        if (newOrders == null) return;

        foreach (var order in newOrders)
        {
            order.orderName = $"주문 #{pendingOrders.Count + 1}";
            pendingOrders.Add(order);
        }

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
    }

    private void HandleAccept(PizzaOrder acceptedOrder)
    {
        if (HasActiveOrder)
        {
            Debug.Log("이미 진행 중인 배달 주문이 있습니다! 먼저 배달을 완료해주세요.");
            return;
        }

        activeOrder = acceptedOrder;
        pendingOrders.Remove(acceptedOrder);

        if (deliveryPoints != null && deliveryPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, deliveryPoints.Count);
            currentTargetDeliveryID = deliveryPoints[randomIndex].DeliveryPointID;
        }

        if (currentOrderUI != null)
        {
            currentOrderUI.DisplayOrders(new List<PizzaOrder> { activeOrder }, currentTargetDeliveryID);
        }

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
    }

    private void HandleDecline(PizzaOrder declinedOrder)
    {
        pendingOrders.Remove(declinedOrder);

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);
        }
    }

    // 배달 완료 검사 및 정산
    public bool TryDeliverPackagedStack(List<PizzaData> pizzaDataList, string deliveryPointID)
    {
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

        if (isBaked && isBaseValid)
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

        activeOrder = null;
        currentTargetDeliveryID = "";

        if (currentOrderUI != null)
        {
            currentOrderUI.ClearOrder();
        }

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