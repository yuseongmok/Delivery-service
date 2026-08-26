using System.Collections.Generic;

[System.Serializable]
public class PizzaData
{
    public List<string> toppings = new List<string>();
    public bool isBaked; // 구워졌는지

    public PizzaData(List<string> toppings, bool isBaked)
    {
        this.toppings = new List<string>(toppings);
        this.isBaked = isBaked;
    }

    // 주문서와 피자 상태 비교
    public bool MatchesOrder(PizzaOrder order)
    {
        if (order == null || order.toppings == null) return false;

        // 안 구워진 피자는 무조건 주문 일치 실패
        if (!isBaked) return false;

        HashSet<string> expectedSet = new HashSet<string>(order.toppings);
        expectedSet.Add("도우");
        expectedSet.Add("소스");
        expectedSet.Add("치즈");

        HashSet<string> currentSet = new HashSet<string>(toppings);

        return currentSet.SetEquals(expectedSet);
    }
}