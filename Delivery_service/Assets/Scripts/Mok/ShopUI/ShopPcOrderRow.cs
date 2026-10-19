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
            var layout=GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=112;
            number.rectTransform.anchorMin=new Vector2(.035f,.65f);
            number.rectTransform.anchorMax=new Vector2(.56f,.97f);
            number.resizeTextForBestFit=true;number.resizeTextMinSize=20;number.resizeTextMaxSize=26;
            description.rectTransform.anchorMax=new Vector2(.60f,.60f);
            description.resizeTextForBestFit=true;description.resizeTextMinSize=18;description.resizeTextMaxSize=23;
            number.text = order.orderName;
            description.text = $"{PizzaRecipes.Title(order)} · 예상 {order.quotedPrice:N0}원\n{order.districtName} · {order.destinationID}";
            accept.interactable = canAccept;
            accept.onClick.RemoveAllListeners();
            decline.onClick.RemoveAllListeners();
            accept.onClick.AddListener(() => onAccept(order));
            decline.onClick.AddListener(() => onDecline(order));
        }
    }
}
