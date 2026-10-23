#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class NightPreparationCheck {
 const string Flag="Mok.Night.Restore";
 static string log;
 static NightPreparationCheck(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);string path=Path.Combine(Application.persistentDataPath,"economySave.json");if(File.Exists("Temp/Night-economy-backup.json"))File.Copy("Temp/Night-economy-backup.json",path,true);else if(File.Exists(path))File.Delete(path);}};}
 public static void Backup(){string path=Path.Combine(Application.persistentDataPath,"economySave.json");if(File.Exists(path))File.Copy(path,"Temp/Night-economy-backup.json",true);else if(File.Exists("Temp/Night-economy-backup.json"))File.Delete("Temp/Night-economy-backup.json");SessionState.SetBool(Flag,true);}
 static void Check(bool ok,string message){if(!ok)throw new Exception("Night flow: "+message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Night-check.txt",log);}
 static IEnumerator Frames(){yield return null;yield return null;yield return null;}
 public static IEnumerator Verify(){
  log="Night preparation Play Mode verification\n";var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();var orders=UnityEngine.Object.FindFirstObjectByType<OrderManager>();var ui=day.pcUIPanel.GetComponent<NightPreparationView>();var dashboard=day.pcUIPanel.GetComponent<ShopPcBusinessDashboard>();dashboard.pc.interactRange=1000;
  // 첫 배달 튜토리얼과 별개인 일반 영업만 검사한다.
  Check(!StoryTutorial.Active,"main game starts without requesting tutorial");MoneyManager.Instance.currentMoney=200000;int initialMoney=MoneyManager.Instance.currentMoney;int initialDay=day.currentDay;
  Check(day.storyDayNightEnabled&&!day.IsNightPreparation&&day.currentTime==6,"saved scene starts in daytime");
  day.pcUIPanel.SetActive(true);dashboard.OpenShop();yield return Frames();
  Check(day.isShopOpen&&orders.PendingOrders.Count>0&&!ui.panel.activeSelf,"daytime opening still generates orders");
  var pending=orders.PendingOrders[0];Check(orders.TryAcceptOrder(pending),"accept ordinary daytime delivery");float elapsed=orders.ElapsedDeliveryMinutes;
  day.pcUIPanel.SetActive(false);day.currentTime=17.9999f;yield return Frames();
  Check(day.IsNightPreparation&&!day.isShopOpen&&day.currentDay==initialDay,"18:00 closes sales without advancing day");
  Check(orders.PendingOrders.Count==0&&orders.ActiveOrder==pending,"night clears waiting orders and preserves accepted delivery");
  Check(MoneyManager.Instance.currentMoney==initialMoney&&DailyRent.Current.LastSettledDay==0,"entering night does not charge daily rent");
  day.OnClick_OpenShop();day.OnClick_CancelClose();Check(!day.isShopOpen,"opening and cancel-close cannot reopen nighttime sales");
  yield return Frames();Check(orders.ElapsedDeliveryMinutes>elapsed,"accepted delivery timer continues during night");
  long income=dashboard.ledger.Income;var pizza=new PizzaData(pending.toppings,true);
  Check(orders.TryDeliverPackagedStack(new System.Collections.Generic.List<PizzaData>{pizza},pending.destinationID),"accepted pizza can actually be delivered after closing");
  Check(dashboard.ledger.Income>income,"night delivery income remains in daily ledger");initialMoney=MoneyManager.Instance.currentMoney;
  day.pcUIPanel.SetActive(true);yield return Frames();ui.Refresh();
  Check(ui.panel.activeInHierarchy&&ui.tabLabel.text=="영업 종료"&&ui.view.SelectedTab==0,"PC opens settlement screen");
  Check(ui.missionButtons.All(b=>!b.gameObject.activeSelf)&&!ui.startButton.gameObject.activeSelf,"mission controls moved out of PC");
  Check(ui.plan.missions.Count(m=>m.optional)==1,"plan contains three required and one optional mission");
  ui.view.SelectTab(1);yield return Frames();Check(!ui.panel.activeInHierarchy,"closing statistics remain accessible");ui.view.SelectTab(0);
  Capture(ui);
  day.fadeDuration=0;ui.restButton.onClick.Invoke();Check(day.currentDay==initialDay,"next-day button requires confirmation");ui.restButton.onClick.Invoke();ui.restButton.onClick.Invoke();
  float limit=Time.realtimeSinceStartup+8;while(day.currentDay==initialDay&&Time.realtimeSinceStartup<limit)yield return null;yield return Frames();
  Check(day.currentDay==initialDay+1&&!day.IsNightPreparation&&!day.isShopOpen,"confirmed rest advances exactly one day");
  Check(DailyRent.Current.LastSettledDay==initialDay&&MoneyManager.Instance.currentMoney==initialMoney-DailyRent.Cost(initialDay),"rent settled exactly once at next-day transition");
  day.pcUIPanel.SetActive(true);yield return Frames();dashboard.OpenShop();yield return Frames();Check(day.isShopOpen&&orders.PendingOrders.Count>0&&!ui.panel.activeSelf&&ui.tabLabel.text=="주문","next day restores order screen and opening");
  day.currentTime=10;day.OnClick_CloseShop();day.OnClick_CancelClose();Check(day.isShopOpen&&!day.IsNightPreparation,"early-close cancellation returns to daytime sales");
  day.OnClick_CloseShop();day.OnClick_ConfirmClose();yield return Frames();Check(day.IsNightPreparation&&day.currentDay==initialDay+1,"confirmed early closing enters night instead of skipping it");
  Check(DailyRent.Current.LastSettledDay==initialDay,"early closing does not settle new day prematurely");
 }
 static void Capture(NightPreparationView ui){
  var cam=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>().View;var canvas=ui.GetComponent<Canvas>();var mode=canvas.renderMode;var camera=canvas.worldCamera;float distance=canvas.planeDistance;
  var rt=new RenderTexture(1280,720,24);var prior=cam.targetTexture;var active=RenderTexture.active;
  try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.3f;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/Night-preparation.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance;cam.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}
}
#endif
