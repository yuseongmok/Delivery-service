using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ToppingStockManager
{
    public static event Action OnStockChanged;

    // 현재 보유 수량 조회
    public static int GetStock(string toppingName)
    {
        return PlayerPrefs.GetInt($"Stock_{toppingName}", 0);
    }
    public static int GetPendingStock(string toppingName)
    {
        return PlayerPrefs.GetInt($"Pending_{toppingName}", 0);
    }

    // 재료 사용
    public static bool ConsumeStock(string toppingName, int amount = 1)
    {
        int current = GetStock(toppingName);
        if (current >= amount)
        {
            PlayerPrefs.SetInt($"Stock_{toppingName}", current - amount);
            PlayerPrefs.Save();
            OnStockChanged?.Invoke();
            return true;
        }
        return false;
    }

    // 상점 주문
    public static void OrderStock(string toppingName, int amount)
    {
        int currentPending = GetPendingStock(toppingName);
        PlayerPrefs.SetInt($"Pending_{toppingName}", currentPending + amount);

        // 배송할 토핑 이름 목록 관리
        List<string> pendingList = GetPendingList();
        if (!pendingList.Contains(toppingName))
        {
            pendingList.Add(toppingName);
            SavePendingList(pendingList);
        }

        PlayerPrefs.Save();
        OnStockChanged?.Invoke();
    }

    // 다음날 배송 완료 처리
    public static void DeliverAllPendingStock()
    {
        List<string> pendingList = GetPendingList();
        if (pendingList.Count == 0) return;

        foreach (string name in pendingList)
        {
            int pending = GetPendingStock(name);
            if (pending > 0)
            {
                int currentStock = GetStock(name);
                // 기존 보유량 + 배송 수량 합산
                PlayerPrefs.SetInt($"Stock_{name}", currentStock + pending);
                PlayerPrefs.DeleteKey($"Pending_{name}");
            }
        }

        PlayerPrefs.DeleteKey("PendingToppingList");
        PlayerPrefs.Save();

        // 배송 완료 후 알림
        OnStockChanged?.Invoke();
    }

    private static List<string> GetPendingList()
    {
        string raw = PlayerPrefs.GetString("PendingToppingList", "");
        if (string.IsNullOrEmpty(raw)) return new List<string>();

        return raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static void SavePendingList(List<string> list)
    {
        string raw = string.Join(",", list);
        PlayerPrefs.SetString("PendingToppingList", raw);
    }
}