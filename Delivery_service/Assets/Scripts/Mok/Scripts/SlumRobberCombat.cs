using UnityEngine;
namespace DeliveryService.Mok {
// 빈민촌에서 생성된 강도는 시민 전투를 사용하고 접촉 강탈 AI는 정지한다.
public sealed class SlumRobberCombat : MonoBehaviour {
 CitizenCombat combat;PlayerCombat player;DayNightCycle clock;RobberyDistrict district;int day;bool ended;
 public CitizenCombat Combat=>combat;
 void Awake(){GetComponent<RobberAI>().enabled=false;combat=GetComponent<CitizenCombat>();if(combat==null)combat=gameObject.AddComponent<CitizenCombat>();}
 void Start(){player=FindFirstObjectByType<PlayerCombat>();clock=FindFirstObjectByType<DayNightCycle>();district=FindFirstObjectByType<RobberyDistrict>();day=clock!=null?clock.currentDay:0;if(player!=null)combat.Provoke(player);}
 void Update(){if(ended)return;if(player==null||clock==null||clock.currentDay!=day||(!GetComponent<RobberAI>().IsTestSpawn&&!clock.isShopOpen)||district==null||!district.Contains(player.transform.position))EndFight();}
 public bool ReceiveKick(Vector3 direction){return !ended&&player!=null&&combat.ReceiveKick(player,direction,player.kickDamage);}
 public void EndFight(){if(ended)return;ended=true;combat.EndFight();Destroy(gameObject,2f);}
 // 패배 당시 손에 든 피자만 잃는다. 주차한 오토바이 배달통은 보존한다.
 public static void ApplyDefeatLoss(PlayerCombat player){
  var inventory=player.GetComponent<ToppingInventory>();bool pizza=inventory!=null&&(inventory.HasPizza()||inventory.HasPackagedPizzas());
  if(pizza){inventory.ClearPackagedPizzas();var held=inventory.RemovePizza();if(held!=null)Destroy(held);}
  else {var money=MoneyManager.Instance;if(money!=null&&money.currentMoney>0){int amount=Mathf.Clamp((int)System.Math.Floor((double)money.currentMoney*Random.Range(.1f,.2f)),1,money.currentMoney);money.SpendMoney(amount,ExpenseType.Loss);}}
  foreach(var robber in FindObjectsByType<SlumRobberCombat>(FindObjectsSortMode.None))robber.EndFight();
 }
}
}

