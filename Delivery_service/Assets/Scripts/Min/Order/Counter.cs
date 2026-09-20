using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private OrderGenerator orderGenerator;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private DayNightCycle dayNightCycle; // 작성자 : 박진웅

    public void Interact(ToppingInventory inventory)
    {
        if (dayNightCycle != null && !dayNightCycle.isShopOpen)
        {
            return; //가게 영업하기 버튼 누르기 전에는 주문 못받게끔 했어요 // 박진웅
        }
        if (orderGenerator == null || orderManager == null) return;

        // 1~3개의 주문 생성 후 OrderManager에 전달
        List<PizzaOrder> orders = orderGenerator.GenerateMultipleOrders();
        orderManager.StartOrderProcess(orders);
    }
}
