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
        // 배달지는 왼쪽 주문 영역에 함께 표시한다.
        if (locationText != null) locationText.text = "";
        if (currrentOrderText == null) return;
        currrentOrderText.verticalOverflow = VerticalWrapMode.Overflow;
        currrentOrderText.text = value;
    }

    public void DisplayTimedOrder(PizzaOrder order, string destination, float minutes)
    {
        if (order == null) { ClearOrder(); return; }
        int seconds = Mathf.Max(0, Mathf.CeilToInt(Mathf.Abs(minutes) * 60f - 0.01f));
        string discount=minutes<0?$" (-{(1f-DeliveryService.Mok.DeliveryPricing.LateMultiplier(DeliveryService.Mok.DeliveryPricing.PromisedMinutes-minutes))*100f:0}%)":"";
        SetText(OrderHeading(order) + "\n" + DeliveryService.Mok.PizzaRecipes.Title(order) + "\n" + destination + "\n" + (minutes<0?"지연 ":"") + $"{seconds / 60:00}:{seconds % 60:00}" + discount);
    }

    // 경과와 권장 시간은 모두 게임 시간의 분:초로 표시한다.
    public void DisplayElapsedOrder(PizzaOrder order,string destination,float elapsed,float recommended){
        if(order==null){ClearOrder();return;}
        int seconds=Mathf.Max(0,Mathf.FloorToInt(elapsed*60f));
        string time=$"경과 {seconds/60:00}:{seconds%60:00} / 권장 {recommended:00}:00";
        if(elapsed>recommended)time=$"<color=#FF963F>{time} · 배달 지연 (-{(1f-DeliveryService.Mok.DeliveryPricing.LateMultiplier(elapsed,recommended))*100f:0}%)</color>";
        SetText(OrderHeading(order)+"\n"+DeliveryService.Mok.PizzaRecipes.Title(order)+"\n"+destination+"\n"+time);
    }

    public void DisplayCancelledOrder(PizzaOrder order)
    {
        SetText(OrderHeading(order) + "\n주문 취소\n피자를 폐기하세요");
    }

    // 다른 씬에서 사용하는 기존 호출 경로를 유지한다.
    public void DisplayOrders(List<PizzaOrder> orders, string targetLocation = "")
    {
        if (orders == null || orders.Count == 0) { ClearOrder(); return; }
        SetText(OrderHeading(orders[0]) + "\n" + targetLocation);
    }

    public void ClearOrder() { SetText("<b>[현재 진행 중인 주문]</b>\n없음"); }
}
