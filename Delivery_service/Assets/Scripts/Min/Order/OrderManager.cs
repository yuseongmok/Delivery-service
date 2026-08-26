using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private CurrentOrderUI currentOrderUI;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private CameraMove cameraMove;

    private List<PizzaOrder> currentOrders;

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
        currentOrders = orders;

        orderUI.ShowUI(orders);
        SetPlayerControl(false);
    }

    private void HandleAccept()
    {
        if (currentOrderUI != null)
        {
            currentOrderUI.DisplayOrders(currentOrders);
        }

        orderUI.CloseUI();
        SetPlayerControl(true);
    }

    private void HandleDecline()
    {
        orderUI.CloseUI();
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool enable)
    {
        if (playerMove != null) playerMove.isControllable = enable;
        if (cameraMove != null) cameraMove.SetControl(enable);
    }
}