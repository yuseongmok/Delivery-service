#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class FeedbackTest {
static void Check(bool b,string s,Action<string> log){if(!b)throw new Exception(s);log("PASS: "+s);}
static int Seed(bool success){for(int i=0;i<1000;i++){UnityEngine.Random.InitState(i);if((UnityEngine.Random.value<CitizenCrime.PoliceChance)==success)return i;}throw new Exception("Seed missing");}
public static IEnumerator RunCore(Action<string> log){
var f=UnityEngine.Object.FindFirstObjectByType<IncomeFeedback>();var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var money=MoneyManager.Instance;var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
Check(f!=null&&f.popupText!=null&&f.incomeSound!=null,"Income popup and audio saved in scene",log);
Check(p.hospitalFade!=null&&p.hospitalFade.overlay!=null&&p.hospitalFade.Alpha==0,"Hospital fade saved and starts transparent",log);
int original=money.currentMoney;money.AddMoney(12000);yield return null;
Check(f.PopupAmount==12000&&f.PopupVisible,"Income creates positive popup",log);Check(f.DisplayedBalance<original+12000,"Balance animates rather than jumping",log);
money.AddMoney(300);yield return null;Check(f.PopupAmount==12300,"Successive income merges popup",log);
float until=Time.unscaledTime+.6f;while(Time.unscaledTime<until)yield return null;
Check(Mathf.Abs(f.DisplayedBalance-money.currentMoney)<1,"Animated balance reaches real balance",log);
ScreenCapture.CaptureScreenshot("Temp/Mok-feedback-income.png");yield return null;
money.SpendMoney(100);yield return null;Check(f.PopupAmount==-100&&f.PopupVisible,"Expense shows a negative popup",log);Check(f.DisplayedBalance>money.currentMoney,"Expense balance counts down",log);Check(f.popupText.color.r>f.popupText.color.g,"Expense popup is red",log);Check(f.GetComponent<AudioSource>().pitch<1,"Expense audio has distinct lower pitch",log);
until=Time.unscaledTime+1.6f;while(Time.unscaledTime<until)yield return null;Check(!f.PopupVisible&&f.popupText.text=="","Income popup clears",log);
clock.currentDay=1;clock.currentTime=6;clock.isShopOpen=false;clock.timeMultiplier=0;
var move=p.GetComponent<PlayerMove>();move.enabled=false;move.isControllable=true;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;Cursor.lockState=CursorLockMode.Locked;UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();
Check(NavMesh.SamplePosition(new Vector3(138,0,76),out var spot,20,NavMesh.AllAreas),"Test location available",log);
var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=spot.position+Vector3.forward*1.2f;cc.enabled=true;
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
var n=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;n.enabled=false;n.GetComponent<NavMeshAgent>().isStopped=true;
original=money.currentMoney;Check(!n.CanBePickpocketed&&!n.Pickpocket(p),"Before opening: hint eligibility and theft disabled",log);Check(money.currentMoney==original&&float.IsNegativeInfinity(n.RobberyReadyAt),"Blocked theft changes neither money nor cooldown",log);clock.isShopOpen=true;Check(n.CanBePickpocketed,"Opening enables pickpocket hint",log);Check(n.Pickpocket(p),"First pickpocket accepted",log);int gain=money.currentMoney-original;Check(gain>=100&&gain<=5000,"Pickpocket reward in 100..5000",log);
Check(!n.Pickpocket(p),"Immediate repeat rejected",log);clock.currentTime=6.49f;Check(!n.Pickpocket(p),"Repeat rejected before game 30 minutes",log);clock.currentTime=6.5f;Check(n.Pickpocket(p),"Same civilian can be robbed at game 30 minutes",log);
clock.currentTime=23.9f;Check(n.Pickpocket(p),"Later same-day theft accepted",log);clock.currentDay=2;clock.currentTime=6;Check(n.Pickpocket(p),"Day rollover counts toward cooldown",log);
// Natural/non-player death must not dispatch police.
int initial=UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Length;n.Npc.Die();Check(UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Length==initial,"NPC death alone does not report player crime",log);
var rng=UnityEngine.Random.state;int no=Seed(false),yes=Seed(true);
UnityEngine.Random.InitState(no);Check(!CitizenCrime.ReportPlayerKill(n.Npc),"Probability miss does not dispatch",log);UnityEngine.Random.InitState(yes);Check(!CitizenCrime.ReportPlayerKill(n.Npc),"Same death never rerolls probability",log);
var victim=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;UnityEngine.Random.InitState(yes);victim.ReceiveKick(p,Vector3.back,100);
Check(UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Length>initial,"Lethal player kick dispatches existing police on probability hit",log);Check(!CitizenCrime.ReportPlayerKill(victim.Npc),"Lethal kick is reported once",log);UnityEngine.Random.state=rng;
foreach(var police in UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(police.gameObject);UnityEngine.Object.Destroy(n.gameObject);UnityEngine.Object.Destroy(victim.gameObject);yield return null;
money.currentMoney=5000;int expenseBefore=money.dailyTotalExpense;int hospitalDebits=0;money.TransactionRecorded+=(amount,kind)=>{if(amount==-20000&&kind==ExpenseType.Loss)hospitalDebits++;};
for(int hit=1;hit<=3;hit++){
until=Time.time+1.4f;while(Time.time<until)yield return null;
p.ReceivePunch(10,Vector3.forward);Check(p.IsDown,"Player knocked down on hit "+hit,log);
bool darkening=false,black=false,lightening=false,teleported=false;until=Time.time+7;
while(p.IsDown&&Time.time<until){float alpha=p.hospitalFade.Alpha;
if(hit==3){if(alpha>0&&alpha<1&&!black)darkening=true;if(alpha>.999f)black=true;
if(Vector3.Distance(p.transform.position,p.hospital.position)<.1f&&!teleported){Check(alpha>.999f,"Hospital teleport occurs behind fully black screen",log);teleported=true;}
if(teleported&&alpha>0&&alpha<.99f)lightening=true;}
yield return null;}
if(hit<3){Check(p.hospitalFade.Alpha==0,"Nonfatal hit does not blackout",log);Check(money.currentMoney==5000&&hospitalDebits==0,"Nonfatal knockdown has no hospital fee",log);}
else Check(darkening&&black&&teleported&&lightening&&!p.IsDown&&p.hospitalFade.Alpha==0,"Third hit fades out, teleports, and fades in",log);
}
Check(money.currentMoney==-15000&&hospitalDebits==1,"Hospital charges 20000 exactly once even with insufficient funds",log);Check(money.dailyTotalExpense==expenseBefore+20000,"Hospital fee included in daily expenses",log);clock.isShopOpen=false;
Check(p.HitsTaken==0&&cc.enabled,"Hospital wake-up restores counters and controller",log);
Check(UnityEngine.Object.FindFirstObjectByType<BankruptcyGameOver>()==null&&Time.timeScale==1,"Negative balance does not end the current day",log);
money.currentMoney=0;Check(!BankruptcyGameOver.TryEndDay(clock),"Zero balance is not bankruptcy",log);
money.currentMoney=100;Check(!BankruptcyGameOver.TryEndDay(clock),"Positive balance continues",log);
money.currentMoney=-1;int lastDay=clock.currentDay;clock.OnClick_ConfirmClose();
until=Time.unscaledTime+5;while(UnityEngine.Object.FindFirstObjectByType<BankruptcyGameOver>()==null&&Time.unscaledTime<until)yield return null;
var over=UnityEngine.Object.FindFirstObjectByType<BankruptcyGameOver>();
Check(over!=null&&over.SurvivedDays==lastDay&&clock.currentDay==lastDay,"Closing with debt ends game before next-day reset and shows survived days",log);
Check(Time.timeScale==0&&!p.enabled&&!p.GetComponent<PlayerInteraction>().enabled&&!clock.isShopOpen,"Game over stops world and player input",log);
yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-bankruptcy.png");yield return null;

}
}}
#endif
