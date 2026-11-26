using System;
using DeliveryService.Mok;
using UnityEngine;

// 스토리 배달지: JJinCity의 DeliveryPoint처럼 손에 든 포장 피자를 [F]로 건넨다.
// 주문·정산(OrderManager, MoneyManager)과는 연결하지 않는다.
public sealed class StoryDeliveryTarget : MonoBehaviour, IInteractable
{
    [Tooltip("손에 피자가 없을 때 보여 줄 안내")]
    public string emptyHandNotice = "오토바이 배달통에서 피자를 꺼내 오세요.";

    public bool Accepting { get; set; }
    public bool Delivered { get; private set; }
    public event Action Handed;

    public void ResetDelivery()
    {
        Delivered = false;
    }

    public void Interact(ToppingInventory inventory)
    {
        if (!Accepting || Delivered || inventory == null) return;
        if (!inventory.HasPackagedPizzas())
        {
            DeliveryRequestNotice.Show(emptyHandNotice);
            return;
        }

        // 한 상자만 건네고 나머지는 손에 남긴다.
        var held = inventory.ClearPackagedPizzas();
        if (held.Count > 1)
        {
            held.RemoveAt(0);
            inventory.AddPackagedDataStack(held);
        }

        Delivered = true;
        Accepting = false;
        Handed?.Invoke();
    }
}
