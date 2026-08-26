using UnityEngine;
using System.Collections.Generic;

public class PizzaPackage : MonoBehaviour
{
    // 포장 박스에 저장될 토핑 목록
    public List<string> toppings = new List<string>();
    public bool IsPackaged { get; private set; }

    public void SetPackaged(bool value)
    {
        IsPackaged = value;
    }

    // 피자에서 토핑 데이터 복사
    public void InitPackage(List<string> pizzaToppings)
    {
        toppings = new List<string>(pizzaToppings);
        IsPackaged = true;
    }

    // 주문서와 이 포장 상자의 토핑이 일치하는지 비교
    public bool MatchesOrder(PizzaOrder order)
    {
        if (order == null || order.toppings == null) return false;
        if (toppings.Count != order.toppings.Count) return false;

        HashSet<string> packageSet = new HashSet<string>(toppings);
        HashSet<string> orderSet = new HashSet<string>(order.toppings);

        return packageSet.SetEquals(orderSet);
    }
}
