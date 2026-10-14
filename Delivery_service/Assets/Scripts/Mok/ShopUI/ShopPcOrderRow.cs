using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok.ShopUI
{
    public sealed class ShopPcOrderRow : MonoBehaviour
    {
        public Text number, description;
        public Button accept, decline;
        public PizzaOrder Order { get; private set; }
        public void Bind(PizzaOrder order, bool canAccept, Action<PizzaOrder> onAccept, Action<PizzaOrder> onDecline)
        {
            Order = order;
            number.text = order.orderName;
            description.text = order.toppings != null && order.toppings.Count > 0
                ? "피자 1판 · " + string.Join(", ", order.toppings)
                : "피자 1판 · 자유 토핑";
            accept.interactable = canAccept;
            accept.onClick.RemoveAllListeners();
            decline.onClick.RemoveAllListeners();
            accept.onClick.AddListener(() => onAccept(order));
            decline.onClick.AddListener(() => onDecline(order));
        }
    }
}
