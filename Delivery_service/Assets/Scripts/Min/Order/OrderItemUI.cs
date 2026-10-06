using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderItemUI : MonoBehaviour
{
    [SerializeField] private Text orderNameText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private PizzaOrder currentOrder;

    public void Setup(PizzaOrder order, Action<PizzaOrder> onAccept, Action<PizzaOrder> onDecline)
    {
        currentOrder = order;

        if (orderNameText != null)
        {
            orderNameText.text = order.orderName;
        }

        // 버튼 리스너 초기화 및 등록
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => onAccept?.Invoke(currentOrder));

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => onDecline?.Invoke(currentOrder));
    }
}