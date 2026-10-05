using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    public sealed class ShopSupplyUiBridge : MonoBehaviour
    {
        private ShopPcUiView view;
        private ShopSupplyCatalog catalog;
        private bool purchasing;

        public void Bind(ShopPcUiView ui)
        {
            view = ui;
            catalog = Resources.Load<ShopSupplyCatalog>("YunShopSupplyCatalog");
            view.PurchaseRequested += Purchase;
            Refresh();
        }

        private PizzaToppingData Data(int index)
        {
            // Use the stable illustration identity, not translated labels or array order.
            if (catalog == null) return null;
            switch (view.ingredients[index].artworkKind)
            {
                case 3: return catalog.Find(ToppingType.Pepperoni);
                case 4: return catalog.Find(ToppingType.Chocolate);
                case 5: return catalog.Find(ToppingType.Pineapple);
                case 6: return catalog.Find(ToppingType.Jelly);
                default: return null;
            }
        }

        private void Purchase(int index)
        {
            if (purchasing || view == null || index < 0 || index >= view.ingredients.Count) return;
            purchasing = true;
            try
            {
                ShopSupplyPurchase.TryBuy(Data(index), view.ingredients[index].quantity, out string message);
                view.ShowFeedback(message);
                Refresh();
            }
            finally { purchasing = false; }
        }

        private void LateUpdate()
        {
            if (view != null && view.isActiveAndEnabled) Refresh();
        }

        private void Refresh()
        {
            if (view == null) return;
            view.SetBalance(MoneyManager.Instance == null ? "소지금 확인 불가" : $"보유 금액 {MoneyManager.Instance.currentMoney:N0}원");
            for (int i = 0; i < view.ingredients.Count; i++)
                view.SetPurchasePrice(i, ShopSupplyPurchase.Quote(Data(i), view.ingredients[i].quantity, out _, out int price)
                    ? $"구매하기\n{price:N0}원" : "구매 불가");
        }

        private void OnDestroy()
        {
            if (view != null) view.PurchaseRequested -= Purchase;
        }
    }
}
