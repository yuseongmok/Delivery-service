using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PizzaOrder
{
    public string orderName;
    public List<string> toppings;
}

public class OrderGenerator : MonoBehaviour
{
    private int totalOrderCount = 0;

    // 125초마다 호출 시 단 1개의 주문만 생성
    public List<PizzaOrder> GenerateSinglePizzaOrder()
    {
        totalOrderCount++;

        List<PizzaOrder> orderList = new List<PizzaOrder>();

        orderList.Add(new PizzaOrder
        {
            orderName = $"피자 #{totalOrderCount}",
            toppings = new List<string>() // 특정 토핑 요구 없음 (자유 주문)
        });

        return orderList;
    }
}