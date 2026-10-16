#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Mok.Editor
{
    public static class NewGameStockResetCheck
    {
        public static void Run()
        {
            var names = AssetDatabase.FindAssets("t:PizzaToppingData")
                .Select(g => AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName)
                .Concat(PlayerPrefs.GetString("PendingToppingList", "").Split(','))
                .Append("Mok_ResetTest_Retired").Where(n => !string.IsNullOrEmpty(n)).Distinct().ToArray();
            var keys = names.SelectMany(n => new[] { "Stock_" + n, "Pending_" + n }).Append("Mok_ResetTest_Setting").ToArray();
            var exists = keys.Select(PlayerPrefs.HasKey).ToArray();
            var values = keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
            bool hadList = PlayerPrefs.HasKey("PendingToppingList");
            string list = PlayerPrefs.GetString("PendingToppingList", "");
            try
            {
                foreach (var name in names) { PlayerPrefs.SetInt("Stock_" + name, 12); PlayerPrefs.SetInt("Pending_" + name, 7); }
                PlayerPrefs.SetString("PendingToppingList", list + ",Mok_ResetTest_Retired");
                PlayerPrefs.SetInt("Mok_ResetTest_Setting", 123);
                typeof(NewGameStockReset).GetMethod("ResetStock", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                foreach (var name in names)
                    if (ToppingStockManager.GetStock(name) != 0 || ToppingStockManager.GetPendingStock(name) != 0)
                        throw new Exception("Stock not reset: " + name);
                if (PlayerPrefs.HasKey("PendingToppingList") || PlayerPrefs.GetInt("Mok_ResetTest_Setting") != 123)
                    throw new Exception("Pending list or unrelated setting regression");
                string item = names.First(n => n != "Mok_ResetTest_Retired");
                PlayerPrefs.SetInt("Stock_" + item, 4);
                ToppingStockManager.OrderStock(item, 3);
                ToppingStockManager.DeliverAllPendingStock();
                if (ToppingStockManager.GetStock(item) != 7 || ToppingStockManager.GetPendingStock(item) != 0)
                    throw new Exception("Next-day delivery regression");
                File.WriteAllText("Temp/Mok-stock-reset-check.txt", "PASS: All ingredient assets and retired pending stock reset.\nPASS: Unrelated settings preserved.\nPASS: Next-day stock delivery keeps existing inventory (4 + 3 = 7).\n");
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++) { if (exists[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]); }
                if (hadList) PlayerPrefs.SetString("PendingToppingList", list); else PlayerPrefs.DeleteKey("PendingToppingList");
                PlayerPrefs.Save();
            }
        }
    }
}
#endif
