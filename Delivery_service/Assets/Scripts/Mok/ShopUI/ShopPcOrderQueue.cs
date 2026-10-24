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
        Button cancelButton; bool confirmCancel;
        public int VisibleCount { get; private set; }
        public IReadOnlyList<ShopPcOrderRow> Rows => rows;

        private void OnEnable()
        {
            if (manager == null) return;
            confirmCancel=false;
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
            if(cancelButton==null){
                cancelButton=Instantiate(template.decline,activeLabel.transform.parent);cancelButton.name="Cancel accepted order";
                var rt=(RectTransform)cancelButton.transform;rt.anchorMin=new Vector2(.76f,0);rt.anchorMax=new Vector2(.99f,1);rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
                activeLabel.rectTransform.anchorMax=new Vector2(.74f,activeLabel.rectTransform.anchorMax.y);
                cancelButton.onClick.RemoveAllListeners();cancelButton.onClick.AddListener(()=>{if(!confirmCancel){confirmCancel=true;Refresh();return;}manager.CancelActiveOrder();confirmCancel=false;Refresh();});
            }
            cancelButton.gameObject.SetActive(manager.HasActiveOrder&&!manager.StoryLesson);
            cancelButton.GetComponentInChildren<Text>().text=confirmCancel?"정말 취소?":"주문 취소";
            var orders = manager.PendingOrders;
            VisibleCount = orders.Count;
            countLabel.text = $"대기 {orders.Count:00}건 · 주문칸 {OrderManager.PendingCapacity}개";
            activeLabel.text = manager.HasActiveOrder
                ? $"진행 중  ·  {manager.ActiveOrder.orderName} 배달을 완료해 주세요"
                : "진행 중인 주문 없음  ·  대기 주문을 수락해 주세요";
            emptyLabel.text = "새 주문을 기다리고 있습니다.";
            hintLabel.text = "평점 4점: 4칸 · 5점: 5칸 · 수락/거절 후 게임 시간 5초 뒤 보충 · 거리별 권장 50~90분";
            if(manager.StoryLesson){countLabel.text=$"첫 배달 연습 · 대기 {orders.Count}건";hintLabel.text="민기와 함께하는 첫 주문 · 기본 재료 무료 · 지연 감점 없음";}
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
