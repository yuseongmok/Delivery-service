using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok.ShopUI
{
    // Presents the one existing OrderManager queue. No separate timer or order list.
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
            hintLabel.text = manager.HasActiveOrder
                ? "배달을 완료하면 다음 주문을 수락할 수 있습니다."
                : "새 주문은 목록 아래에 추가됩니다. 수락하면 배달 주문으로 등록됩니다.";
            emptyLabel.text = manager.DailyOrdersExhausted
                ? "오늘의 주문 접수가 모두 끝났습니다.\n마감 탭에서 영업을 마감할 수 있습니다."
                : manager.HasActiveOrder
                    ? "현재 배달을 완료하면\n30초 뒤 다음 주문이 들어옵니다."
                    : "다음 주문을 기다리고 있습니다.\n배달 완료·거절·취소 후 30초 뒤 접수됩니다.";
            hintLabel.text = $"오늘 접수 {manager.OrdersGeneratedToday}/{manager.DailyOrderLimit}건  ·  배달 제한: 게임 시간 80분";
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
            // New arrivals do not pull the player away from the row being read.
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
