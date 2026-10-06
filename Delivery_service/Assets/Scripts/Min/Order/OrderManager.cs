using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.Examples.CameraController;

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

    // 코루틴 제어용 변수
    private Coroutine autoOrderCoroutine;

    public string CurrentTargetDeliveryID => currentTargetDeliveryID;
    public bool HasActiveOrder => activeOrder != null;

    // NavigationManager 연동용 : ID에 해당하는 DeliveryPoint 반환
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
        if (orderGenerator == null)
        {
            orderGenerator = FindObjectOfType<OrderGenerator>();
        }

        // 초기 루틴 시작
        StartAutoOrderRoutine();
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

    /// <summary>
    /// 영업 시작 버튼을 눌렀을 때 호출되는 메서드
    /// </summary>
    public void OnShopOpened()
    {
        // 1. 영업 시작 즉시 주문 1개 생성
        GenerateSingleOrder();

        // 2. 타이머를 즉시 리셋하여 영업 시작 시점부터 125초 카운트다운 시작
        StartAutoOrderRoutine();
    }

    private void StartAutoOrderRoutine()
    {
        if (autoOrderCoroutine != null)
        {
            StopCoroutine(autoOrderCoroutine);
        }
        autoOrderCoroutine = StartCoroutine(AutoOrderAccumulateRoutine());
    }

    // 125초마다 주문 생성 루틴
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

    // 단일 주문 생성 공통 로직
    private void GenerateSingleOrder()
    {
        if (orderGenerator == null) return;

        List<PizzaOrder> newOrders = orderGenerator.GenerateSinglePizzaOrder();

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

    // 특정 주문 수락
    private void HandleAccept(PizzaOrder acceptedOrder)
    {
        if (HasActiveOrder)
        {
            Debug.Log("이미 진행 중인 배달 주문이 있습니다! 먼저 배달을 완료해주세요.");
            return;
        }

        activeOrder = acceptedOrder;
        pendingOrders.Remove(acceptedOrder);

        // 배달지 선정
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

            if (pendingOrders.Count == 0)
            {
                orderUI.CloseUI();
            }
        }
    }

    // 특정 주문 거절
    private void HandleDecline(PizzaOrder declinedOrder)
    {
        pendingOrders.Remove(declinedOrder);

        if (orderUI != null)
        {
            orderUI.RefreshOrderList(pendingOrders);

            if (pendingOrders.Count == 0)
            {
                orderUI.CloseUI();
            }
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

        if (!deliveredPizza.isBaked)
        {
            Debug.Log("덜 구워진 피자입니다!");
            return false;
        }

        int totalEarnedMoney = basePizzaPrice;

        foreach (string toppingName in deliveredPizza.toppings)
        {
            PizzaToppingData matchedTopping = GetToppingData(toppingName);
            if (matchedTopping != null)
            {
                totalEarnedMoney += matchedTopping.sellingPrice;
            }
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalEarnedMoney);
        }

        Debug.Log($"배달 성공! 정산 금액: {totalEarnedMoney}원");

        activeOrder = null;
        currentTargetDeliveryID = "";

        if (currentOrderUI != null)
        {
            currentOrderUI.ClearOrder();
        }

        return true;
    }

    private PizzaToppingData GetToppingData(string toppingIdentifier)
    {
        if (toppingDataList == null) return null;

        return toppingDataList.Find(data =>
            data != null &&
            (data.toppingName == toppingIdentifier || data.toppingType.ToString() == toppingIdentifier)
        );
    }
}