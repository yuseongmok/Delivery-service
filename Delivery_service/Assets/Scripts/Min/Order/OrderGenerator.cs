using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public class PizzaOrder
{
    public string orderName;
    public List<string> toppings;
}

public class OrderGenerator : MonoBehaviour
{
    private readonly string[] allToppings = { "치즈", "고추", "페퍼로니", "초콜릿", "젤리", "파인애플" };

    public List<PizzaOrder> GenerateMultipleOrders()
    {
        List<PizzaOrder> orderList = new List<PizzaOrder>();

        int roll = Random.Range(0, 100);
        int orderCount;

        if (roll < 60) orderCount = 1;
        else if (roll < 95) orderCount = 2;
        else orderCount = 3;

        for (int i = 0; i < orderCount; i++)
        {
            orderList.Add(GenerateSingleOrder());
        }

        return orderList;
    }

    // 무작위 주문 생성
    private PizzaOrder GenerateSingleOrder()
    {
        List<string> selectedToppings = new List<string>();

        int roll = Random.Range(0, 10);
        if (roll < 6)
        {
            string randomTopping = allToppings[Random.Range(0, allToppings.Length)];
            selectedToppings.Add(randomTopping);
        }
        else
        {
            int countToSelect = Random.Range(2, allToppings.Length + 1);
            List<string> shuffledList = new List<string>(allToppings);
            for (int i = 0; i < shuffledList.Count; i++)
            {
                int rndIndex = Random.Range(i, shuffledList.Count);
                string temp = shuffledList[i];
                shuffledList[i] = shuffledList[rndIndex];
                shuffledList[rndIndex] = temp;
            }
            selectedToppings = shuffledList.GetRange(0, countToSelect);
        }

        StringBuilder sb = new StringBuilder();
        foreach (string topping in selectedToppings)
        {
            sb.Append(topping).Append(" ");
        }
        sb.Append("피자");

        return new PizzaOrder
        {
            orderName = sb.ToString().Trim(),
            toppings = selectedToppings
        };
    }
}