using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok.ShopUI
{
    // 별도 타이머나 주문 목록 없이 기존 OrderManager 대기열을 표시한다.
    public sealed class ShopPcOrderQueue : MonoBehaviour
    {
        public OrderManager manager;
        public Text countLabel, activeLabel, emptyLabel, hintLabel;
        public RectTransform content;
        public ScrollRect scroll;
        public ShopPcOrderRow template;
        private readonly List<ShopPcOrderRow> rows = new List<ShopPcOrderRow>();
        public int VisibleCount { get; private set; }
        public IReadOnlyList<ShopPcOrderRow> Rows => rows;

        private void OnEnable()
        {
            if (manager == null) return;
            manager.OrdersChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (manager != null) manager.OrdersChanged -= Refresh;
        }
        public void Refresh()
        {
            if (manager == null || template == null) return;
            var orders = manager.PendingOrders;
            VisibleCount = orders.Count;
            countLabel.text = $"대기 {orders.Count:00}건";
            activeLabel.text = manager.HasActiveOrder
                ? $"진행 중  ·  {manager.ActiveOrder.orderName} 배달을 완료해 주세요"
                : "진행 중인 주문 없음  ·  대기 주문을 수락해 주세요";
            emptyLabel.text = "새 주문을 기다리고 있습니다.";
            hintLabel.text = "접수 횟수 무제한 · 수락/거절 후 게임 시간 5초 뒤 보충 · 권장 배달 30분 · 지연 시 평점 하락";
            emptyLabel.gameObject.SetActive(orders.Count == 0);
            Vector2 offset = content.anchoredPosition;
            for (int i = 0; i < orders.Count; i++)
            {
                if (i == rows.Count)
                {
                    var row = Instantiate(template, content);
                    row.name = "QueuedOrderRow_" + i;
                    rows.Add(row);
                }
                rows[i].Bind(orders[i], !manager.HasActiveOrder, Accept, Decline);
                rows[i].gameObject.SetActive(true);
            }
            for (int i = orders.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            // 새 주문이 들어와도 읽고 있던 행의 위치를 유지한다.
            offset.y = Mathf.Clamp(offset.y, 0, Mathf.Max(0, content.rect.height - scroll.viewport.rect.height));
            content.anchoredPosition = offset;
        }
        private void Accept(PizzaOrder order)
        {
            if (!manager.TryAcceptOrder(order)) Refresh();
        }
        private void Decline(PizzaOrder order)
        {
            if (!manager.TryDeclineOrder(order)) Refresh();
        }
    }
}
