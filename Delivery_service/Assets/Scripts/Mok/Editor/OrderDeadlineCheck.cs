#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class OrderDeadlineCheck {
const string Key="Mok.Deadline.Check";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
[Serializable]public class SavedKey{public string key;public bool exists;public int value;}
[Serializable]public class Backup{public SavedKey[] values;public bool listExists;public string list;public string economyPath;public bool economyExists;public string economy;}
static CurrentOrderUI hud; static UnityEngine.UI.Text label, destinationLabel; static float acceptedAt; static string destination; static double screenshotAt; static int stage;static double began;static string log;static ShopPC pc;static OrderManager manager;static DayNightCycle cycle;static ShopPcUiView view;static ShopPcOrderQueue queue;
static OrderDeadlineCheck(){EditorApplication.playModeStateChanged+=Changed;if(File.Exists("Temp/Mok-deadline-test-request")&&!SessionState.GetBool(Key,false))EditorApplication.delayCall+=()=>{File.Delete("Temp/Mok-deadline-test-request");Start();};}
[MenuItem("Tools/Delivery Service/Mok/Verify Order Deadline")]
public static void Start(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");EditorSceneManager.SaveOpenScenes();
var list=PlayerPrefs.GetString("PendingToppingList","");
var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
var backup=new Backup {values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
File.WriteAllText("Temp/Mok-deadline-save-backup.json",JsonUtility.ToJson(backup,true));

SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){stage=0;began=EditorApplication.timeSinceStartup;log="Order deadline Play Mode regression.\n";EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;}
static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS: "+text+"\n";}
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Invoke(object o,string f)=>o.GetType().GetMethod(f,Flags).Invoke(o,null);
static void Open(){pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;Invoke(pc,"OpenPanel");view.SelectTab(0);Canvas.ForceUpdateCanvases();}
static void Tick(){if(!EditorApplication.isPlaying||Time.frameCount<5)return;
try{
if(EditorApplication.timeSinceStartup-began>45)throw new Exception("Order check timed out");
if(stage==0){
pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();manager=pc.GetComponentInChildren<OrderManager>(true);cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();view=pc.pcUIPanel.GetComponent<ShopPcUiView>();queue=pc.pcUIPanel.GetComponentInChildren<ShopPcOrderQueue>(true);hud=UnityEngine.Object.FindFirstObjectByType<CurrentOrderUI>();
label=(UnityEngine.UI.Text)typeof(CurrentOrderUI).GetField("currrentOrderText",Flags).GetValue(hud);destinationLabel=(UnityEngine.UI.Text)typeof(CurrentOrderUI).GetField("locationText",Flags).GetValue(hud);
Check(MoneyManager.Instance.currentMoney==20000,"game starts with 20,000 even with an existing save");Set(MoneyManager.Instance,"saveFilePath",Path.GetFullPath("Temp/Mok-deadline-test-economy.json"));cycle.timeMultiplier=0;Open();view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="OpenBusiness").onClick.Invoke();stage=1;return;
}
if(stage==1){if(manager.PendingOrders.Count==0)return;manager.StopAllCoroutines();acceptedAt=cycle.currentTime;Check(manager.TryAcceptOrder(manager.PendingOrders[0]),"accept order");destination=manager.CurrentTargetDeliveryID;
Check(label.text=="주문번호 01.\n"+destination+"\n25:00"&&destinationLabel.text=="","exact three-line HUD and separate destination cleared");
Invoke(pc,"ClosePanel");Capture("active");stage=2;return;
}
if(stage==2){if(!Captured("active"))return;cycle.currentTime=acceptedAt+10f/60f;Invoke(manager,"UpdateDeliveryDeadline");Check(label.text.EndsWith("15:00"),"ten in-game minutes consume ten countdown minutes");
cycle.currentTime=acceptedAt+24.98f/60f;Invoke(manager,"UpdateDeliveryDeadline");Check(manager.HasActiveOrder,"order remains valid before deadline");
cycle.currentTime=acceptedAt+25.01f/60f;int money=MoneyManager.Instance.currentMoney;
Check(!manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"Dough","Sauce","Cheese"},true)},destination),"delivery checks expiry immediately, before frame Update");
Check(MoneyManager.Instance.currentMoney==money&&!manager.HasActiveOrder&&string.IsNullOrEmpty(manager.CurrentTargetDeliveryID),"expired order cannot pay or retain a destination");
Check(label.text=="주문번호 01.\n주문 취소\n피자를 폐기하세요","expired HUD matches requested text");Capture("cancelled");stage=3;return;
}
if(stage==3){if(!Captured("cancelled"))return;Invoke(manager,"GenerateSingleOrder");Check(manager.TryAcceptOrder(manager.PendingOrders[0]),"next order can be accepted after expiry");Check(label.text.StartsWith("주문번호 02.")&&label.text.EndsWith("25:00"),"new order replaces cancellation and resets timer");
int before=MoneyManager.Instance.currentMoney;Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"Dough","Sauce","Cheese"},true)},manager.CurrentTargetDeliveryID)&&MoneyManager.Instance.currentMoney>before,"on-time delivery still pays normally");Check(!label.text.Contains("25:00"),"completed order clears timer");
Invoke(manager,"GenerateSingleOrder");manager.TryAcceptOrder(manager.PendingOrders[0]);cycle.currentTime+=26f/60f;stage=4;return;
}
if(stage==4){if(manager.HasActiveOrder)return;Check(label.text.Contains("주문 취소"),"normal Update also expires an undelivered order");
manager.ResetOrdersForNewDay();Check(!label.text.Contains("주문 취소"),"day reset clears cancellation");log+="RESULT: PASS\n";Finish();}

}catch(Exception e){log+="RESULT: FAIL\n"+e;Debug.LogException(e);Finish();}}
static void Capture(string name){Canvas.ForceUpdateCanvases();string p="Temp/Mok-deadline-"+name+".png";if(File.Exists(p))File.Delete(p);ScreenCapture.CaptureScreenshot(p);screenshotAt=EditorApplication.timeSinceStartup;}
static bool Captured(string name)=>File.Exists("Temp/Mok-deadline-"+name+".png")&&EditorApplication.timeSinceStartup-screenshotAt>.5;
static void Finish(){EditorApplication.update-=Tick;File.WriteAllText("Temp/Mok-deadline-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();}
static void Restore(){try{
var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Temp/Mok-deadline-save-backup.json"));
foreach(var v in backup.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
if(backup.listExists)PlayerPrefs.SetString("PendingToppingList",backup.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
if(backup.economyExists){if(!File.Exists(backup.economyPath)||File.ReadAllText(backup.economyPath)!=backup.economy)File.WriteAllText(backup.economyPath,backup.economy);}else if(File.Exists(backup.economyPath))File.Delete(backup.economyPath);
File.AppendAllText("Temp/Mok-deadline-check.txt","Saved economy, stock and pending-order values restored after Play mode.\n");

}finally{SessionState.SetBool(Key,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
}}
#endif
