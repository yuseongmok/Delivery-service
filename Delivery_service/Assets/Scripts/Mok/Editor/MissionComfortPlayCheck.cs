#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class MissionComfortPlayCheck {
 static string log;static void Check(bool ok,string text){log+=(ok?"PASS ":"FAIL ")+text+"\n";File.WriteAllText("Logs/Mission-comfort-check.txt",log);if(!ok)throw new Exception(text);}
 static void Set(object o,string n,object v){o.GetType().GetProperty(n).SetValue(o,v);}
 static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
 public static IEnumerator Verify(){
  log="Focused mission comfort Play Mode checks (prerequisites seeded)\n";yield return Wait(.5f);
  var b=BrokerMission.Current;var s=SpecialOrderMission.Current;var p=b.player;var inv=p.GetComponent<ToppingInventory>();var k=s.kitchen;StoryPhoneInbox.Current.AutoOpenEnabled=false;
  Set(b,"State",BrokerMission.Phase.Complete);b.day.EnterNightPreparation();Check(s.Begin(),"special order starts after prerequisite");Set(s,"State",SpecialOrderMission.Phase.Prepare);MoneyManager.Instance.currentMoney=0;
  k.dough.Interact(inv);k.maker.Interact(inv);k.sauce.Interact(inv);k.maker.Interact(inv);k.cheese.Interact(inv);k.maker.Interact(inv);k.maker.Interact(inv);
  Check(inv.HasPizza()&&MoneyManager.Instance.currentMoney==0,"zero cash creates cheese pizza without charge");k.oven.Interact(inv);yield return Wait(7);k.oven.Interact(inv);Check(inv.HasPizza()&&inv.HeldPizza.GetComponent<Pizza>().IsBaked,"supported pizza bakes normally");
  k.packing.Interact(inv);yield return Wait(.45f);k.packing.Interact(inv);yield return Wait(.5f);k.packing.Interact(inv);yield return Wait(.5f);k.pickup.Interact(inv);Check(inv.HasPackagedPizzas()&&SpecialOrderMission.Valid(inv.GetHeldPackagedPizzas()[0]),"zero-cash sample is valid for mission");
  var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);app.Open(false);app.Move(1);app.Confirm();app.Confirm();Check(s.Running,"mission cancel requires confirmation");app.Confirm();Check(!s.Running&&!SpecialOrderMission.SuppliesProvided,"phone cancellation releases mission and free ingredients");Check(s.Begin(),"zero cash allows retry");s.Cancel();app.gameObject.SetActive(false);
  var cargo=k.cargo;var list=(List<PizzaData>)typeof(BikePizzaStorage).GetField("storedPizzas",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(cargo);var sample=new PizzaData(new(){"도우","소스","치즈"},true);list.Add(sample);cargo.currentShake=100;cargo.AddCollisionShock(10000);yield return null;Check(!sample.isRuined&&cargo.currentShake==0,"collision and update cannot ruin pizza");
  Check(cargo.pizzaUIGroup==null||!cargo.pizzaUIGroup.activeSelf,"legacy shake gauge hidden");
  var fuel=cargo.bikeController.GetComponent<MotorcycleFuel>();fuel.currentFuel=50;float expected=50-fuel.fuelConsumptionRate*.5f*Time.deltaTime;fuel.ConsumeFuel();Check(Mathf.Abs(fuel.currentFuel-expected)<.001f,"fuel consumption halved");
  var orders=b.orders;var order=new PizzaOrder{orderName="test cancel",destinationID="test",toppings=new(){"도우","소스","치즈"}};typeof(OrderManager).GetField("activeOrder",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(orders,order);typeof(OrderManager).GetField("currentTargetDeliveryID",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(orders,"test");
  var queue=UnityEngine.Object.FindFirstObjectByType<ShopUI.ShopPcOrderQueue>(FindObjectsInactive.Include);queue.Refresh();var button=queue.activeLabel.transform.parent.Find("Cancel accepted order").GetComponent<Button>();button.onClick.Invoke();Check(orders.HasActiveOrder,"order cancel first click retains order");button.onClick.Invoke();Check(!orders.HasActiveOrder&&orders.CurrentTargetDeliveryID==""&&MoneyManager.Instance.currentMoney==0&&inv.HasPackagedPizzas(),"confirmed order cancellation clears target without losing cash or pizza");Check(!orders.CancelActiveOrder(),"duplicate cancellation rejected");
  var m=MechanicMission.Current;var apron=m.mechanic.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Mechanic tool apron");Check(apron.bounds.size.magnitude<1,"saved mechanic apron human-sized");
  var enemy=UnityEngine.Object.Instantiate(m.enemyTemplate,m.enemyPosts[0].position,Quaternion.identity);enemy.gameObject.SetActive(true);enemy.ExternalAttackControl=true;enemy.Provoke(p);yield return null;Check(enemy.GetComponent<EnemyLocationMarker>()!=null,"mission opponent receives enemy marker");UnityEngine.Object.Destroy(enemy.gameObject);
  foreach(var light in UnityEngine.Object.FindObjectsByType<DeliveryService.Yun.Streetlights.StreetlightTimeController>(FindObjectsSortMode.None)){light.Refresh();if(light.LampCount>1)Check(light.IsLit,"district lights stay on after closing");}
 }
}}
#endif
