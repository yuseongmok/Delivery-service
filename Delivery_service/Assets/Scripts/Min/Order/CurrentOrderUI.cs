using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CurrentOrderUI : MonoBehaviour
{
    [SerializeField] private Text currrentOrderText;

    private void Awake()
    {
        ClearOrder();
    }

    public void DisplayOrders(List<PizzaOrder> orders)
    {
        if (currrentOrderText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>[현재 진행 중인 주문]</b>");

        for (int i = 0; i < orders.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {orders[i].orderName}");
        }

        currrentOrderText.text = sb.ToString();
    }

    public void ClearOrder()
    {
        if (currrentOrderText != null)
        {
            currrentOrderText.text = "<b>[현재 진행 중인 주문]</b>\n없음";
        }
    }
}
