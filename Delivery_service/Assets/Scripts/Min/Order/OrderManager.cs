using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private CurrentOrderUI currentOrderUI;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private CameraMove cameraMove;

    // 배달 지점들
    [SerializeField] private List<DeliveryPoint> deliveryPoints = new List<DeliveryPoint>();

    private List<PizzaOrder> currentOrders;
    private string currentTargetDeliveryID;

    public string CurrentTargetDeliveryID => currentTargetDeliveryID;
    // 아직 남은 주문이 있는지 확인
    public bool HasActiveOrder => currentOrders != null && currentOrders.Count > 0;

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

    public void StartOrderProcess(List<PizzaOrder> orders)
    {
        if (HasActiveOrder)
        {
            Debug.Log("이미 받은 주문이 있습니다.");
            return;
        }

        currentOrders = orders;

        // 씬 내 배달지 리스트에서 무작위 1곳 선정
        if (deliveryPoints != null && deliveryPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, deliveryPoints.Count);
            currentTargetDeliveryID = deliveryPoints[randomIndex].DeliveryPointID;
        }

        orderUI.ShowUI(orders);
        SetPlayerControl(false);
    }

    private void HandleAccept()
    {
        if (currentOrderUI != null)
        {
            currentOrderUI.DisplayOrders(currentOrders, currentTargetDeliveryID);
        }

        orderUI.CloseUI();
        SetPlayerControl(true);
    }

    private void HandleDecline()
    {
        if (currentOrders != null)
        {
            currentOrders.Clear();
        }
        currentTargetDeliveryID = "";

        orderUI.CloseUI();
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool enable)
    {
        if (playerMove != null) playerMove.isControllable = enable;
        if (cameraMove != null) cameraMove.SetControl(enable);
    }

    public bool TryDeliverPackagedStack(List<PizzaData> pizzaDataList, string deliveryPointID)
    {
        if (pizzaDataList == null || pizzaDataList.Count == 0 || currentOrders == null || currentOrders.Count == 0)
            return false;

        // 배달 장소 검사
        if (deliveryPointID != currentTargetDeliveryID)
        {
            Debug.Log($"여기가 아닙니다.");
            return false;
        }

        // 수량 검사
        if (pizzaDataList.Count != currentOrders.Count)
        {
            Debug.Log("주문한 피자가 아닙니다.");
            return false;
        }

        // 구움/토핑 일치 검사
        List<PizzaOrder> remainingOrders = new List<PizzaOrder>(currentOrders);

        foreach (PizzaData data in pizzaDataList)
        {
            PizzaOrder matchedOrder = null;

            foreach (var order in remainingOrders)
            {
                if (data.MatchesOrder(order))
                {
                    matchedOrder = order;
                    break;
                }
            }

            if (matchedOrder != null)
            {
                remainingOrders.Remove(matchedOrder);
            }
            else
            {
                if (!data.isBaked)
                    Debug.Log("굽X 주문한 피자가 아닙니다.");
                else
                    Debug.Log("토핑일치X 주문한 피자가 아닙니다.");

                return false;
            }
        }

        Debug.Log($"배달 성공");
        currentOrders.Clear();
        currentTargetDeliveryID = "";

        if (currentOrderUI != null)
        {
            currentOrderUI.ClearOrder();
        }

        return true;
    }
}