using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderUI : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Text orderText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    public event Action OnOrderAccepted;
    public event Action OnOrderDeclined;

    private void Awake()
    {
        acceptButton.onClick.AddListener(() => OnOrderAccepted?.Invoke());
        declineButton.onClick.AddListener(() => OnOrderDeclined?.Invoke());
        CloseUI();
    }

    public void ShowUI(List<PizzaOrder> orders)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>[신규 주문 목록]</b>\n");

        for (int i = 0; i < orders.Count; i++)
        {
            sb.AppendLine($"주문번호 {i + 1}. {orders[i].orderName}");
        }

        sb.AppendLine("\n수락하시겠습니까?");
        orderText.text = sb.ToString();

        uiPanel.SetActive(true);
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
    }
}