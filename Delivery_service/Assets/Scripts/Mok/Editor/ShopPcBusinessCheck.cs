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
[InitializeOnLoad] public static class ShopPcBusinessCheck {
const string Key="Mok.Business.Check";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
[Serializable]public class SavedKey{public string key;public bool exists;public int value;}
[Serializable]public class Backup{public SavedKey[] values;public bool listExists;public string list;public string economyPath;public bool economyExists;public string economy;}
static ShopPcBusinessDashboard dashboard; static ShopDayLedger ledger; static long expectedIncome, expectedExpense; static int day; static double captureAt; static int screenshotFrame;
static int stage;static double began;static string log;static ShopPC pc;static OrderManager manager;static DayNightCycle cycle;static ShopPcUiView view;static ShopPcOrderQueue queue;
static ShopPcBusinessCheck(){EditorApplication.playModeStateChanged+=Changed;if(File.Exists("Temp/Mok-business-test-request")&&!SessionState.GetBool(Key,false))EditorApplication.delayCall+=()=>{File.Delete("Temp/Mok-business-test-request");Start();};}
[MenuItem("Tools/Delivery Service/Mok/Verify PC Opening and Closing")]
public static void Start(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");EditorSceneManager.SaveOpenScenes();
var list=PlayerPrefs.GetString("PendingToppingList","");
var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
var backup=new Backup {values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
File.WriteAllText("Temp/Mok-business-save-backup.json",JsonUtility.ToJson(backup,true));

SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){stage=0;began=EditorApplication.timeSinceStartup;log="Opening and closing dashboard Play Mode regression.\n";EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;}
static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS: "+text+"\n";}
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Invoke(object o,string f)=>o.GetType().GetMethod(f,Flags).Invoke(o,null);
static void Open(){pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;Invoke(pc,"OpenPanel");view.SelectTab(0);Canvas.ForceUpdateCanvases();}
static void Tick(){if(!EditorApplication.isPlaying||Time.frameCount<5)return;
try{
if(EditorApplication.timeSinceStartup-began>45)throw new Exception("Order check timed out");
if(stage==0){
pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>(); manager=pc.GetComponentInChildren<OrderManager>(true);
view=pc.pcUIPanel.GetComponent<ShopPcUiView>();dashboard=view.GetComponent<ShopPcBusinessDashboard>();ledger=pc.GetComponent<ShopDayLedger>();cycle=dashboard.cycle;queue=pc.pcUIPanel.GetComponentInChildren<ShopPcOrderQueue>(true);day=cycle.currentDay;
var money=MoneyManager.Instance;Set(money,"saveFilePath",Path.GetFullPath("Temp/Mok-business-test-economy.json"));money.currentMoney=1000000;
money.AddMoney(100);money.SpendMoney(100,ExpenseType.PizzaTopping);
Open();dashboard.Refresh();
Check(!cycle.isShopOpen&&dashboard.openingGate.activeInHierarchy&&!dashboard.orderList.activeSelf,"closed shop shows opening gate and hides order list");
Check(dashboard.openButton.GetComponentInChildren<Text>().text=="개점하기","opening button renamed");
Check(view.transform.Find("Background/Sidebar/Tab_1").GetComponentInChildren<Text>().text=="마감","business tab renamed to closing");
Check(!dashboard.closeButton.interactable&&ledger.Income==0&&ledger.Expense==0,"pre-opening transactions excluded and closing disabled");
Check(!dashboard.openButton.transform.IsChildOf(view.transform.Find("Background/Page_1")),"opening button removed from closing page");
Screenshot("opening");stage=1;return;
}
if(stage==1){if(!Captured("opening"))return;
Set(manager,"orderIntervalSeconds",.35f);dashboard.openButton.onClick.Invoke();dashboard.Refresh();
Check(cycle.isShopOpen&&pc.pcUIPanel.activeSelf&&view.SelectedTab==0&&dashboard.orderList.activeInHierarchy&&!dashboard.openingGate.activeSelf,"opening keeps PC open and reveals order list");
Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None&&!pc.cameraLookScript.enabled,"opening keeps cursor available and camera suspended");
var money=MoneyManager.Instance;money.AddMoney(18000);money.SpendMoney(2000,ExpenseType.PizzaTopping);money.AccidentMoney(500);money.SpendMoney(300);
// 지정한 비용으로 실제 주유 호출을 검사한다.
var nozzle=UnityEngine.Object.FindFirstObjectByType<FuelNozzle>();var cap=UnityEngine.Object.FindObjectsByType<FuelCap>(FindObjectsSortMode.None).First(c=>c.bikeFuelSystem!=null);
nozzle.enabled=false;nozzle.costPerSecond=600;nozzle.refuelSpeed=15;cap.bikeFuelSystem.currentFuel=0;nozzle.PickUpNozzle();nozzle.AttachToBike(cap);typeof(FuelNozzle).GetMethod("TickFuel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(nozzle,new object[]{10f});nozzle.ReturnPump();
Check(ledger.Income==18000&&ledger.Expense==6800&&ledger.Ingredients==2000&&ledger.Fuel==4000&&ledger.Loss==500&&ledger.Other==300&&ledger.Profit==11200,"actual income, refueling, ingredients, fines/theft and other costs reconcile exactly");
long expense=ledger.Expense;Check(!money.SpendMoney(money.currentMoney+1)&&!money.SpendMoney(-1)&&ledger.Expense==expense,"failed and invalid purchases are not recorded");
// 기존 UI를 통해 실제 재료 구매를 검사한다.
view.SelectTab(2);var catalog=Resources.Load<ShopSupplyCatalog>("YunShopSupplyCatalog");var data=catalog.Find(ToppingType.Pepperoni);PlayerPrefs.SetInt("Stock_"+data.toppingName,0);PlayerPrefs.SetInt("Pending_"+data.toppingName,0);view.ingredients[0].quantity=1;view.RefreshQuantities();
view.transform.Find("Background/Page_2/IngredientsScroll/Content/Ingredient_0/Card/Purchase").GetComponent<Button>().onClick.Invoke();
Check(ledger.Ingredients==2000+data.cost*10&&ToppingStockManager.GetPendingStock(data.toppingName)==10,"real supply purchase enters ingredient statistics once");
Open();stage=2;return;
}
if(stage==2){if(manager.PendingOrders.Count<3)return;manager.StopAllCoroutines();
Check(manager.PendingOrders.Take(3).Select(o=>o.orderName).SequenceEqual(new[]{"주문 01","주문 02","주문 03"}),"opening starts original timed sequential order queue");
queue.Rows[0].accept.onClick.Invoke();long previousIncome=ledger.Income;int previousMoney=MoneyManager.Instance.currentMoney;
Check(manager.TryDeliverPackagedStack(new List<PizzaData>{new PizzaData(new List<string>{"Dough","Sauce","Cheese"},true)},manager.CurrentTargetDeliveryID),"accepted order completes with original delivery function");
Check(ledger.Income-previousIncome==MoneyManager.Instance.currentMoney-previousMoney&&ledger.Income>previousIncome,"delivery reward recorded once as income");
Invoke(pc,"ClosePanel");previousIncome=ledger.Income;MoneyManager.Instance.AddMoney(500);Check(ledger.Income==previousIncome+500,"transactions continue while PC UI is closed");Open();
expectedIncome=ledger.Income;expectedExpense=ledger.Expense;view.SelectTab(1);dashboard.Refresh();
Check(dashboard.incomeLabel.text==ledger.Income.ToString("N0")+"원"&&dashboard.expenseLabel.text==ledger.Expense.ToString("N0")+"원"&&dashboard.profitLabel.text==ledger.Profit.ToString("N0")+"원","closing screen displays reconciled live totals");
Check(view.transform.Find("Background/Page_1").GetComponentsInChildren<Button>(false).Length==1,"closing page has only the closing action button");
Screenshot("closing");stage=3;return;
}
if(stage==3){if(!Captured("closing"))return;dashboard.closeButton.onClick.Invoke();
Check(!cycle.isShopOpen&&cycle.summaryPanel.activeInHierarchy&&!pc.pcUIPanel.activeSelf,"closing opens original confirmation summary");cycle.OnClick_CancelClose();dashboard.Refresh();
Check(cycle.isShopOpen&&pc.pcUIPanel.activeSelf&&ledger.Income==expectedIncome&&ledger.Expense==expectedExpense,"cancel closing preserves same-day totals and resumes PC");
dashboard.OpenShop();Check(ledger.Income==expectedIncome,"repeated opening cannot reset today's report");
dashboard.closeButton.onClick.Invoke();cycle.fadeDuration=0;cycle.OnClick_ConfirmClose();stage=4;return;
}
if(stage==4){if(cycle.currentDay==day)return;Open();dashboard.Refresh();
Check(!cycle.isShopOpen&&dashboard.openingGate.activeInHierarchy&&!ledger.HasOpenedToday&&dashboard.incomeLabel.text=="0원","confirmed closing advances day and clears displayed current-day totals");
dashboard.openButton.onClick.Invoke();Check(ledger.Income==0&&ledger.Expense==0&&cycle.isShopOpen&&pc.pcUIPanel.activeSelf,"next opening starts a new report and keeps order screen open");stage=5;return;
}
if(stage==5){if(manager.PendingOrders.Count==0)return;Check(manager.PendingOrders[0].orderName=="주문 01","next day starts order numbering at 01");
MoneyManager.Instance.SpendMoney(100,ExpenseType.Fuel);dashboard.Refresh();Check(ledger.Profit==-100&&dashboard.profitLabel.text=="-100원","negative daily profit is displayed correctly");
log+="RESULT: PASS\n";Finish();}

}catch(Exception e){log+="RESULT: FAIL\n"+e;Debug.LogException(e);Finish();}}
static void Screenshot(string name){Canvas.ForceUpdateCanvases();string path="Temp/Mok-business-"+name+".png";if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);captureAt=EditorApplication.timeSinceStartup;screenshotFrame=Time.frameCount;}
static bool Captured(string name)=>File.Exists("Temp/Mok-business-"+name+".png")&&Time.frameCount>screenshotFrame+2&&EditorApplication.timeSinceStartup-captureAt>.3;
static void Finish(){EditorApplication.update-=Tick;File.WriteAllText("Temp/Mok-business-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();}
static void Restore(){try{
var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Temp/Mok-business-save-backup.json"));
foreach(var v in backup.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
if(backup.listExists)PlayerPrefs.SetString("PendingToppingList",backup.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
if(backup.economyExists){if(!File.Exists(backup.economyPath)||File.ReadAllText(backup.economyPath)!=backup.economy)File.WriteAllText(backup.economyPath,backup.economy);}else if(File.Exists(backup.economyPath))File.Delete(backup.economyPath);
File.AppendAllText("Temp/Mok-business-check.txt","Saved economy, stock and pending-order values restored after Play mode.\n");

}finally{SessionState.SetBool(Key,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
}}
#endif
