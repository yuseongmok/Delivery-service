using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentOrderUI : MonoBehaviour
{
    [SerializeField] private Text currrentOrderText;
    [SerializeField] private Text locationText;

    private void Awake() { ClearOrder(); }

    private static string OrderHeading(PizzaOrder order)
    {
        string number = order.orderName;
        if (number.StartsWith("주문 ")) number = number.Substring(3);
        return "주문번호 " + number + ".";
    }

    private void SetText(string value)
    {
        // Destination is now part of the left order block.
        if (locationText != null) locationText.text = "";
        if (currrentOrderText == null) return;
        currrentOrderText.verticalOverflow = VerticalWrapMode.Overflow;
        currrentOrderText.text = value;
    }

    public void DisplayTimedOrder(PizzaOrder order, string destination, float minutes)
    {
        if (order == null) { ClearOrder(); return; }
        int seconds = Mathf.Max(0, Mathf.CeilToInt(minutes * 60f - 0.01f));
        SetText(OrderHeading(order) + "\n" + destination + "\n" + $"{seconds / 60:00}:{seconds % 60:00}");
    }

    public void DisplayCancelledOrder(PizzaOrder order)
    {
        SetText(OrderHeading(order) + "\n주문 취소\n피자를 폐기하세요");
    }

    // Retain the legacy entry point for other scenes.
    public void DisplayOrders(List<PizzaOrder> orders, string targetLocation = "")
    {
        if (orders == null || orders.Count == 0) { ClearOrder(); return; }
        SetText(OrderHeading(orders[0]) + "\n" + targetLocation);
    }

    public void ClearOrder() { SetText("<b>[현재 진행 중인 주문]</b>\n없음"); }
}
