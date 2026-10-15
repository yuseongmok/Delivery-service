using System.Collections.Generic;

[System.Serializable]
public class PizzaData
{
    public List<string> toppings = new List<string>();
    public bool isBaked; // 구워졌는지
    public bool isRuined = false;   // 망가졌는지

    public PizzaData(List<string> toppings, bool isBaked, bool isRuined = false)
    {
        this.toppings = new List<string>(toppings);
        this.isBaked = isBaked;
        this.isRuined = isRuined;
    }

    public PizzaData(PizzaData other)
    {
        if (other != null)
        {
            this.toppings = new List<string>(other.toppings);
            this.isBaked = other.isBaked;
            this.isRuined = other.isRuined;
        }
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