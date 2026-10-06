using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderUI : MonoBehaviour
{
    // OrderManager가 구독할 이벤트
    public event Action<PizzaOrder> OnOrderAccepted;
    public event Action<PizzaOrder> OnOrderDeclined;

    [Header("UI 연결")]
    [SerializeField] private Transform contentTransform; // ScrollView/Viewport/Content
    [SerializeField] private GameObject orderItemPrefab; // 주문 카드 프리팹

    public void RefreshOrderList(List<PizzaOrder> pendingOrders)
    {
        // 기존 UI 제거
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // 대기 주문 목록 새로 생성
        foreach (PizzaOrder order in pendingOrders)
        {
            GameObject newItem = Instantiate(orderItemPrefab, contentTransform);
            OrderItemUI itemUI = newItem.GetComponent<OrderItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(
                    order,
                    onAccept: (accepted) => OnOrderAccepted?.Invoke(accepted),
                    onDecline: (declined) => OnOrderDeclined?.Invoke(declined)
                );
            }
        }
    }

    public void OpenUI()
    {
        gameObject.SetActive(true);
    }

    public void CloseUI()
    {
        gameObject.SetActive(false);
    }
}