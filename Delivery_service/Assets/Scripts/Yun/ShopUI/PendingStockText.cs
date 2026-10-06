using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun.ShopUI
{
    // Extend the existing Canvas labels without editing Topping or its serialized references.
    [DisallowMultipleComponent, DefaultExecutionOrder(11000)]
    public sealed class PendingStockText : MonoBehaviour
    {
        private PizzaToppingData data;
        private Text label;
        private string display;

        public static void Install(Scene scene)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var dataField = typeof(Topping).GetField("toppingData", flags);
            var textField = typeof(Topping).GetField("stockText", flags);
            if (dataField == null || textField == null)
            {
                Debug.LogWarning("Yun: Topping stock label references could not be read.");
                return;
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var topping in root.GetComponentsInChildren<Topping>(true))
            {
                var material = dataField.GetValue(topping) as PizzaToppingData;
                var text = textField.GetValue(topping) as Text;
                if (material == null || text == null || material.toppingType == ToppingType.Dough
                    || material.toppingType == ToppingType.Sauce || material.toppingType == ToppingType.Cheese) continue;
                var binding = text.GetComponent<PendingStockText>();
                if (binding == null) binding = text.gameObject.AddComponent<PendingStockText>();
                binding.Bind(material, text);
            }
        }

        public void Bind(PizzaToppingData material, Text target)
        {
            data = material;
            label = target;
            Refresh();
        }

        private void OnEnable()
        {
            ToppingStockManager.OnStockChanged += Refresh;
            Refresh();
        }

        private void OnDisable() { ToppingStockManager.OnStockChanged -= Refresh; }

        private void Refresh()
        {
            if (data == null || label == null) return;
            int stock = ToppingStockManager.GetStock(data.toppingName);
            int pending = ToppingStockManager.GetPendingStock(data.toppingName);
            display = pending > 0 ? $"{stock}/100 (+{pending})" : $"{stock}/100";
            Apply();
        }

        // Topping.Start/OnEnable/stock events can rewrite the same label after our event callback.
        private void LateUpdate() { Apply(); }
        private void Apply()
        {
            if (label != null && display != null && label.text != display) label.text = display;
        }
    }
}
