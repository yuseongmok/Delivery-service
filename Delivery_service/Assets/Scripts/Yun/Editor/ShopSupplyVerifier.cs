#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using DeliveryService.Yun.ShopUI;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Yun.Editor
{
    public static class ShopSupplyVerifier
    {
        public static void RunBatch()
        {
            string company = PlayerSettings.companyName, product = PlayerSettings.productName;
            int code = 0;
            try
            {
                PlayerSettings.companyName = "YunVerification";
                PlayerSettings.productName = "ShopCanvasIsolatedTest";
                Verify();
                ShopPcUiVerifier.Run();
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; }
            EditorApplication.Exit(code);
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }

        private static void Verify()
        {
            var catalog = Resources.Load<ShopSupplyCatalog>("YunShopSupplyCatalog");
            Require(catalog != null, "Build-loadable catalog exists");
            foreach (var type in new[] { ToppingType.Pepperoni, ToppingType.Chocolate, ToppingType.Pineapple, ToppingType.Jelly })
            {
                var item = catalog.Find(type);
                Require(ShopSupplyPurchase.Quote(item, 2, out int n, out int p) && n == 20 && p == item.cost * 20,
                    "Original ScriptableObject cost is used for " + type);
            }

            var instanceField = typeof(MoneyManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            var original = MoneyManager.Instance;
            bool hadList = PlayerPrefs.HasKey("PendingToppingList");
            string originalList = PlayerPrefs.GetString("PendingToppingList", "");
            string key = "YunPurchaseTest_" + Guid.NewGuid().ToString("N");
            string file = Path.GetTempFileName();
            var go = new GameObject("Isolated purchase verification");
            go.SetActive(false);
            var data = ScriptableObject.CreateInstance<PizzaToppingData>();
            GameObject uiRoot = null;
            GameObject stockLabelObject = null;
            PendingStockText stockBinding = null;
            try
            {
                var money = go.AddComponent<MoneyManager>();
                instanceField.SetValue(null, money);
                typeof(MoneyManager).GetField("saveFilePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(money, file);
                data.toppingName = key;
                data.toppingType = ToppingType.Pepperoni;
                data.cost = 7;
                money.currentMoney = 139;
                Require(!ShopSupplyPurchase.TryBuy(data, 2, out _) && money.currentMoney == 139
                    && ToppingStockManager.GetPendingStock(key) == 0, "Insufficient funds leave balance and stock unchanged");
                money.currentMoney = 140;
                Require(ShopSupplyPurchase.TryBuy(data, 2, out _) && money.currentMoney == 0, "Exact balance buys two bundles");
                Require(money.expensePizza == 140 && money.dailyTotalExpense == 140, "Ingredient expense is recorded");
                Require(ToppingStockManager.GetPendingStock(key) == 20 && ToppingStockManager.GetStock(key) == 0,
                    "Purchase queues twenty units for next-day delivery");
                money.currentMoney = 10000;
                PlayerPrefs.SetInt("Stock_" + key, 80);
                Require(!ShopSupplyPurchase.TryBuy(data, 1, out _) && money.currentMoney == 10000
                    && ToppingStockManager.GetPendingStock(key) == 20, "Current plus pending stock enforces capacity");
                PlayerPrefs.SetInt("Stock_" + key, 70);
                Require(ShopSupplyPurchase.TryBuy(data, 1, out _) && ToppingStockManager.GetPendingStock(key) == 30
                    && money.currentMoney == 9930, "Exact capacity and repeated orders work");
                data.cost = -1;
                Require(!ShopSupplyPurchase.TryBuy(data, 1, out _), "Negative prices rejected");
                data.cost = int.MaxValue;
                Require(!ShopSupplyPurchase.Quote(data, 1, out _, out _), "Price overflow rejected");
                Require(!ShopSupplyPurchase.Quote(data, int.MaxValue, out _, out _), "Quantity overflow rejected");
                Require(!ShopSupplyPurchase.Quote(data, 0, out _, out _), "Zero quantity rejected");

                // Exercise the actual button-to-bridge path against an isolated catalog and wallet.
                data.cost = 7;
                PlayerPrefs.DeleteKey("Stock_" + key);
                PlayerPrefs.DeleteKey("Pending_" + key);
                var testCatalog = ScriptableObject.CreateInstance<ShopSupplyCatalog>();
                testCatalog.materials = new[] { data };
                uiRoot = ShopPcUiSceneSetup.InstantiateForVerification();
                var view = uiRoot.GetComponent<ShopPcUiView>();
                var bridge = uiRoot.AddComponent<ShopSupplyUiBridge>();
                bridge.Bind(view);
                typeof(ShopSupplyUiBridge).GetField("catalog", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bridge, testCatalog);
                view.ChangeQuantity(0, 1);
                int before = money.currentMoney;
                uiRoot.transform.Find("Background/Page_2/IngredientsScroll/Content/Ingredient_0/Card/Purchase")
                    .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Require(money.currentMoney == before - 140 && ToppingStockManager.GetPendingStock(key) == 20,
                    "UI purchase button routes chosen bundle count to existing wallet and stock");
                stockLabelObject = new GameObject("Stock label test", typeof(RectTransform), typeof(UnityEngine.UI.Text));
                var stockLabel = stockLabelObject.GetComponent<UnityEngine.UI.Text>();
                stockBinding = stockLabelObject.AddComponent<PendingStockText>();
                stockBinding.Bind(data, stockLabel);
                // Explicit lifecycle calls are necessary in this edit-mode batch verification.
                stockBinding.SendMessage("OnEnable");
                Require(stockLabel.text == "0/100 (+20)", "Pending units appear on the existing stock label");
                ToppingStockManager.OrderStock(key, 10);
                Require(stockLabel.text == "0/100 (+30)", "Additional orders accumulate on label");
                stockLabel.text = "0/100";
                stockBinding.SendMessage("LateUpdate");
                Require(stockLabel.text == "0/100 (+30)", "Legacy label refresh cannot erase pending count");
                uiRoot.SetActive(true);
                view.SendMessage("OnEnable");
                Require(view.ingredients.TrueForAll(row => row.quantity == 1), "Opening resets all bundle selectors");
                view.ChangeQuantity(0, 3);
                view.SendMessage("OnDisable");
                view.SendMessage("OnEnable");
                Require(view.ingredients[0].quantity == 1 && ToppingStockManager.GetPendingStock(key) == 30,
                    "Reopening resets selection without canceling pending purchases");
                view.SendMessage("OnDisable");
                PlayerPrefs.SetInt("Stock_" + key, 30);
                PlayerPrefs.DeleteKey("Pending_" + key);
                ToppingStockManager.ConsumeStock(key, 0);
                Require(stockLabel.text == "30/100", "Arrival removes pending suffix and updates stock");
                Debug.Log("SHOP_PENDING_UI_VERIFICATION_PASSED: pending counts, accumulation, competing label refresh, reopening selection reset, arrival.");
                UnityEngine.Object.DestroyImmediate(testCatalog);
                Debug.Log("SHOP_SUPPLY_VERIFICATION_PASSED: original assets, unit cost x10, multiple bundles, wallet, expenses, pending delivery, capacity, validation and UI bridge.");
            }
            finally
            {
                if (stockBinding != null) stockBinding.SendMessage("OnDisable");
                if (stockLabelObject != null) UnityEngine.Object.DestroyImmediate(stockLabelObject);
                if (uiRoot != null) UnityEngine.Object.DestroyImmediate(uiRoot);
                instanceField.SetValue(null, original);
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(data);
                PlayerPrefs.DeleteKey("Stock_" + key);
                PlayerPrefs.DeleteKey("Pending_" + key);
                if (hadList) PlayerPrefs.SetString("PendingToppingList", originalList);
                else PlayerPrefs.DeleteKey("PendingToppingList");
                PlayerPrefs.Save();
                File.Delete(file);
            }
        }
    }
}
#endif
