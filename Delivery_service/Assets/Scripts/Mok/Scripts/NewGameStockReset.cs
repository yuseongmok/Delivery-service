using System.Collections.Generic;
using DeliveryService.Yun.ShopUI;
using UnityEngine;

namespace DeliveryService.Mok
{
    public static class NewGameStockReset
    {
        // Called once per application/Editor Play session, before any scene reads stock.
        // Scene changes and the next trading day do not invoke this callback.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStock()
        {
            var names = new HashSet<string>();
            var library = Resources.Load<PizzaIngredientLibrary>("MokPizzaArt/Ingredients");
            if (library != null && library.ingredients != null)
                foreach (var material in library.ingredients)
                    if (material != null && !string.IsNullOrEmpty(material.toppingName)) names.Add(material.toppingName);
            foreach (var catalog in Resources.LoadAll<ShopSupplyCatalog>(""))
                if (catalog.materials != null)
                    foreach (var material in catalog.materials)
                        if (material != null && !string.IsNullOrEmpty(material.toppingName))
                            names.Add(material.toppingName);
            foreach (var material in Resources.LoadAll<PizzaToppingData>(""))
                if (!string.IsNullOrEmpty(material.toppingName)) names.Add(material.toppingName);
            foreach (var name in PlayerPrefs.GetString("PendingToppingList", "").Split(','))
                if (!string.IsNullOrEmpty(name)) names.Add(name);

            foreach (var name in names)
            {
                PlayerPrefs.DeleteKey("Stock_" + name);
                PlayerPrefs.DeleteKey("Pending_" + name);
            }
            PlayerPrefs.DeleteKey("PendingToppingList");
            PlayerPrefs.Save();
        }
    }
}
