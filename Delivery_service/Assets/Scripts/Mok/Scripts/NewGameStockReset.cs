using System.Collections.Generic;
using DeliveryService.Yun.ShopUI;
using UnityEngine;

namespace DeliveryService.Mok
{
    public static class NewGameStockReset
    {
        // 게임 또는 에디터 플레이 시작 시 재고를 읽기 전에 한 번 호출한다.
        // 씬 이동이나 다음 영업일 전환에는 호출하지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStock() => ResetForNewGame();

        // 로비에서 새 게임을 시작할 때에도 같은 초기화 절차를 사용한다.
        public static void ResetForNewGame()
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
