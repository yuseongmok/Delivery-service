using UnityEngine;
namespace DeliveryService.Mok {
// 빈민촌에서 생성된 강도는 시민 전투를 사용하고 접촉 강탈 AI는 정지한다.
public sealed class SlumRobberCombat : MonoBehaviour {
 [SerializeField, Min(0f)] float chaseSpeed=7f;
 CitizenCombat combat;PlayerCombat player;DayNightCycle clock;RobberyDistrict district;int day;bool ended;
 public CitizenCombat Combat=>combat;
 public bool FixedGuard {get;private set;}
 public bool Engaged {get;private set;}
 public PlayerCombat Target=>player;
 Vector3 home;float restUntil,repath;
 UnityEngine.AI.NavMeshAgent agent;Animator animator;
 public void SetPost(Vector3 position){FixedGuard=true;home=position;}
 void Awake(){GetComponent<RobberAI>().enabled=false;combat=GetComponent<CitizenCombat>();if(combat==null)combat=gameObject.AddComponent<CitizenCombat>();combat.chaseSpeed=chaseSpeed;combat.attackStartRange=3.2f;combat.punchRange=3.4f;combat.combatSpacing=2.5f;agent=GetComponent<UnityEngine.AI.NavMeshAgent>();animator=GetComponent<Animator>();}
 void Start(){player=FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);clock=FindFirstObjectByType<DayNightCycle>();district=FindFirstObjectByType<RobberyDistrict>();day=clock!=null?clock.currentDay:0;if(!FixedGuard&&player!=null){combat.Provoke(player);Engaged=true;}}
 void Update(){
  if(ended||combat.Npc.IsDead)return;
  bool inside=player!=null&&clock!=null&&clock.currentDay==day&&(clock.isShopOpen||GetComponent<RobberAI>().IsTestSpawn)&&district!=null&&district.Contains(player.transform.position);
  if(!FixedGuard){if(!inside)EndFight();return;}
  bool pursue=inside&&Time.time>=restUntil&&!player.WaterRescueActive&&!PoliceControl.IsArrestInProgress;
  if(pursue){if(!Engaged||!combat.IsAggressive){combat.Provoke(player);Engaged=true;}return;}
  if(Engaged||combat.IsAggressive){combat.EndFight();Engaged=false;}
  combat.Npc.ExternalControl=true;
  if(!combat.Npc.IsKnockedDown&&agent.enabled&&agent.isOnNavMesh){
   agent.speed=chaseSpeed;agent.stoppingDistance=.3f;agent.isStopped=false;
   if(Time.time>=repath){repath=Time.time+.5f;agent.SetDestination(home);}
   if(animator!=null&&animator.enabled)animator.SetFloat("MoveSpeed",agent.velocity.magnitude);
  }
 }
 public bool ReceiveKick(Vector3 direction){return !ended&&player!=null&&combat.ReceiveKick(player,direction,player.kickDamage);}
 public void EndFight(){if(ended)return;combat.EndFight();Engaged=false;if(FixedGuard){combat.Npc.ExternalControl=true;restUntil=Time.time+8;return;}ended=true;Destroy(gameObject,2f);}
 // 패배 당시 손에 든 피자만 잃는다. 주차한 오토바이 배달통은 보존한다.
 public static void ApplyDefeatLoss(PlayerCombat player){
  var inventory=player.GetComponent<ToppingInventory>();bool pizza=inventory!=null&&(inventory.HasPizza()||inventory.HasPackagedPizzas());
  if(pizza){inventory.ClearPackagedPizzas();var held=inventory.RemovePizza();if(held!=null)Destroy(held);}
  else {var money=MoneyManager.Instance;if(money!=null&&money.currentMoney>0){int amount=Mathf.Clamp((int)System.Math.Floor((double)money.currentMoney*Random.Range(.1f,.2f)),1,money.currentMoney);money.SpendMoney(amount,ExpenseType.Loss);}}
  foreach(var robber in FindObjectsByType<SlumRobberCombat>(FindObjectsSortMode.None))robber.EndFight();
 }
}
}

