using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable] public class PizzaOrder {
 public string orderName, recipeName, destinationID, districtName;
 public int quotedPrice;
 public List<string> toppings;
}
public class OrderGenerator : MonoBehaviour {
 public List<PizzaOrder> GenerateSinglePizzaOrder() => new List<PizzaOrder>{DeliveryService.Mok.PizzaRecipes.Create()};
}