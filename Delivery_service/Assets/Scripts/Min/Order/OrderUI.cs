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
        // 교체된 주문 화면의 빈 연결이 게임 진행을 중단하지 않도록 한다.
        if (contentTransform == null || orderItemPrefab == null || pendingOrders == null) return;
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
}