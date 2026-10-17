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
[InitializeOnLoad] public static class ShopPcOrdersCheck {
const string Key="Mok.Orders.Check";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
[Serializable]public class SavedKey{public string key;public bool exists;public int value;}
[Serializable]public class Backup{public SavedKey[] values;public bool listExists;public string list;public string economyPath;public bool economyExists;public string economy;}
static int stage;static double began;static string log;static ShopPC pc;static OrderManager manager;static DayNightCycle cycle;static ShopPcUiView view;static ShopPcOrderQueue queue;
static ShopPcOrdersCheck(){EditorApplication.playModeStateChanged+=Changed;if(File.Exists("Temp/Mok-orders-test-request")&&!SessionState.GetBool(Key,false))EditorApplication.delayCall+=()=>{File.Delete("Temp/Mok-orders-test-request");Start();};}
[MenuItem("Tools/Delivery Service/Mok/Verify PC Order Queue")]
public static void Start(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");EditorSceneManager.SaveOpenScenes();
var list=PlayerPrefs.GetString("PendingToppingList","");
var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
var backup=new Backup {values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
File.WriteAllText("Temp/Mok-orders-save-backup.json",JsonUtility.ToJson(backup,true));

SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){stage=0;began=EditorApplication.timeSinceStartup;log="Order queue Play Mode regression.\n";EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;}
static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS: "+text+"\n";}
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Invoke(object o,string f)=>o.GetType().GetMethod(f,Flags).Invoke(o,null);
static void Open(){pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;Invoke(pc,"OpenPanel");view.SelectTab(0);Canvas.ForceUpdateCanvases();}
static void Tick(){if(!EditorApplication.isPlaying||Time.frameCount<5)return;
try{
if(EditorApplication.timeSinceStartup-began>45)throw new Exception("Order check timed out");
if(stage==0){
pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();manager=pc.GetComponentInChildren<OrderManager>(true);cycle=UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsSortMode.None).Single(c=>c.pcUIPanel==pc.pcUIPanel);view=pc.pcUIPanel.GetComponent<ShopPcUiView>();queue=pc.pcUIPanel.GetComponentInChildren<ShopPcOrderQueue>(true);
Check(queue.manager==manager,"new PC uses the original OrderManager");Check(Mathf.Approximately((float)typeof(OrderManager).GetField("orderIntervalSeconds",Flags).GetValue(manager),125f),"saved production order interval is still 125 seconds");
var money=MoneyManager.Instance;if(money!=null)Set(money,"saveFilePath",Path.GetFullPath("Temp/Mok-orders-test-economy.json"));
// 테스트 인스턴스만 가속하고 실제 코루틴을 실행한다.
Set(manager,"orderIntervalSeconds",.35f);Open();view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="OpenBusiness").onClick.Invoke();stage=1;return;
}
if(stage==1){if(manager.PendingOrders.Count<1)return;Check(manager.PendingOrders[0].orderName=="주문 01","opening creates order 01");Open();Check(queue.VisibleCount==1,"first order appears in new PC");Invoke(pc,"ClosePanel");stage=2;return;}
if(stage==2){if(manager.PendingOrders.Count<3)return;manager.StopAllCoroutines();
Check(manager.PendingOrders.Take(3).Select(o=>o.orderName).SequenceEqual(new[]{"주문 01","주문 02","주문 03"}),"real timer accumulates orders in arrival order while PC is closed (test interval 0.35s)");Open();Check(queue.VisibleCount==manager.PendingOrders.Count,"reopening shows accumulated queue");
var first=manager.PendingOrders[0];queue.Rows[0].accept.onClick.Invoke();Check(manager.ActiveOrder==first&&!manager.PendingOrders.Contains(first)&&!string.IsNullOrEmpty(manager.CurrentTargetDeliveryID),"row accept activates the order and assigns existing delivery destination");Check(queue.activeLabel.text.Contains("01")&&queue.Rows.Take(queue.VisibleCount).All(r=>!r.accept.interactable),"active order displayed and other accept buttons locked");
Check(!manager.TryAcceptOrder(manager.PendingOrders[0])&&!manager.TryAcceptOrder(first),"busy or stale acceptance cannot replace active order");
var declined=manager.PendingOrders[0];queue.Rows[0].decline.onClick.Invoke();Check(!manager.PendingOrders.Contains(declined)&&!manager.TryDeclineOrder(declined),"row reject removes only its order and stale rejection is ignored");Invoke(manager,"GenerateSingleOrder");Check(manager.PendingOrders.Last().orderName=="주문 04","new arrivals do not reuse accepted or rejected order numbers");
int moneyBefore=MoneyManager.Instance.currentMoney;Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"Dough","Sauce","Cheese"},true)},manager.CurrentTargetDeliveryID),"accepted order completes through existing delivery function");Check(!manager.HasActiveOrder&&queue.Rows.Take(queue.VisibleCount).All(r=>r.accept.interactable)&&MoneyManager.Instance.currentMoney>moneyBefore,"delivery reward remains and next acceptance unlocks");
var retained=manager.PendingOrders.ToArray();Invoke(pc,"ClosePanel");Open();Check(manager.PendingOrders.SequenceEqual(retained)&&queue.VisibleCount==retained.Length,"close and reopen preserve order identities without duplicate callbacks");
for(int i=0;i<8;i++)Invoke(manager,"GenerateSingleOrder");Canvas.ForceUpdateCanvases();Check(queue.content.rect.height>queue.scroll.viewport.rect.height,"long queue scrolls instead of overlapping");
manager.ResetOrdersForNewDay();Invoke(manager,"GenerateSingleOrder");Check(manager.PendingOrders[0].orderName!="주문 01","same-day reset does not reuse order numbers");cycle.currentDay++;manager.ResetOrdersForNewDay();Check(queue.VisibleCount==0&&queue.emptyLabel.gameObject.activeSelf,"new day clears queue and shows empty state");Invoke(manager,"GenerateSingleOrder");Check(manager.PendingOrders[0].orderName=="주문 01","new day restarts at order 01");
for(int i=0;i<6;i++)Invoke(manager,"GenerateSingleOrder");Open();queue.scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Temp/Mok-orders-preview.png");stage=3;return;
}
if(stage==3){if(!File.Exists("Temp/Mok-orders-preview.png"))return;log+="RESULT: PASS\n";Finish();}
}catch(Exception e){log+="RESULT: FAIL\n"+e;Debug.LogException(e);Finish();}}
static void Finish(){EditorApplication.update-=Tick;File.WriteAllText("Temp/Mok-orders-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();}
static void Restore(){try{
var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Temp/Mok-orders-save-backup.json"));
foreach(var v in backup.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
if(backup.listExists)PlayerPrefs.SetString("PendingToppingList",backup.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
if(backup.economyExists){if(!File.Exists(backup.economyPath)||File.ReadAllText(backup.economyPath)!=backup.economy)File.WriteAllText(backup.economyPath,backup.economy);}else if(File.Exists(backup.economyPath))File.Delete(backup.economyPath);
File.AppendAllText("Temp/Mok-orders-check.txt","Saved economy, stock and pending-order values restored after Play mode.\n");

}finally{SessionState.SetBool(Key,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
}}
#endif
