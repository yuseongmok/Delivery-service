using System.Collections.Generic;
using UnityEngine;
public class DeliveryPoint : MonoBehaviour, IInteractable {
 [SerializeField] private string deliveryPointID="HouseA";
 [SerializeField] private OrderManager orderManager;
 public string DeliveryPointID=>deliveryPointID;
 public void Interact(ToppingInventory inventory){
  if(orderManager==null||inventory==null)return;
  var pizzas=inventory.GetHeldPackagedPizzas();
  if(!orderManager.TryDeliverPackagedStack(pizzas,deliveryPointID)){
   DeliveryService.Mok.DeliveryRequestNotice.Show(orderManager.LastDeliveryFailure);return;
  }
  // 주문에 맞는 한 상자만 전달하고 나머지는 손에 남긴다.
  var remaining=new List<PizzaData>(pizzas);remaining.RemoveAt(orderManager.LastDeliveredPizzaIndex);
  inventory.ClearPackagedPizzas();if(remaining.Count>0)inventory.AddPackagedDataStack(remaining);
 }
}