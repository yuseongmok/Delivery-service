#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class ChallengeModeCheck
    {
        const string Key="Mok.Challenge.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static ChallengeModeCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/ChallengeModeCheck"))
            {
                File.Delete("Temp/ChallengeModeCheck");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/ChallengeModeCheck.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/Challenge-save-backup.json",JsonUtility.ToJson(backup));
                EditorSceneManager.SaveScene(scene,"Logs/Challenge-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                SessionState.SetBool(Key+".maximized",gameView.maximized);gameView.maximized=true;
                EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool(Key,false)||routine==null||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup-began>180)throw new Exception("Timed out");if(routine.MoveNext())return;}
            catch(Exception e){log+="FAIL: "+e+"\n";}
            File.WriteAllText("Logs/ChallengeModeCheck.txt",log);routine=null;EditorApplication.ExitPlaymode();
        }
        static void Changed(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;began=EditorApplication.timeSinceStartup;log="";routine=Run();}
            if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;
        }
        static IEnumerator Run()
        {
            for(int i=0;i<5;i++)yield return null;
            var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>();
            var button=lobby.modeMenu.transform.Find("Challenge mode").GetComponent<Button>();
            Check(button.interactable,"Lobby challenge card enabled");
            lobby.OpenModes();button.onClick.Invoke();
            while(!UnityEngine.Object.FindFirstObjectByType<ChallengeRun>())yield return null;
            for(int i=0;i<10;i++)yield return null;
            var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            var run=day.GetComponent<ChallengeRun>();var orders=UnityEngine.Object.FindFirstObjectByType<OrderManager>();
            var money=MoneyManager.Instance;
            Check(SceneManager.GetActiveScene().name=="JJinCity"&&GameModeSession.IsChallenge,"Challenge enters existing city");
            Check(day.currentDay==1&&day.currentTime==8&&!day.isShopOpen&&!day.IsNightPreparation,"Day 1 preparation initialized");
            Check(!StoryTutorial.Active&&!BrokerMission.Current.CanStart&&!VenueFinale.Current.CanStart&&StoryPhoneInbox.Current.Messages.Count==0,"Story missions and messages isolated");
            money.SpendMoney(1000,ExpenseType.PizzaTopping);
            var ledger=UnityEngine.Object.FindFirstObjectByType<ShopDayLedger>();
            Check(ledger.Expense==1000,"Pre-opening supplies included in ledger");
            day.OnClick_OpenShop();for(int i=0;i<4;i++)yield return null;
            Check(day.isShopOpen&&orders.PendingOrders.Count>0,"Opening generates normal orders");
            Check(money.dailyTotalExpense==1000,"Opening preserves preparation expenses");
            Check(orders.TryAcceptOrder(orders.PendingOrders[0]),"Accept real order");
            day.OnClick_CloseShop();Check(!day.summaryPanel.activeSelf,"Active delivery blocks early settlement");
            day.currentTime=20;run.Tick();Check(!day.isShopOpen&&orders.HasActiveOrder&&orders.PendingOrders.Count==0,"Closing hour stops new orders but retains delivery");
            int beforeDelivery=money.currentMoney;
            Check(orders.TryDeliverPackagedStack(new System.Collections.Generic.List<PizzaData>{new PizzaData(orders.ActiveOrder.toppings,true)},orders.CurrentTargetDeliveryID)&&money.currentMoney>beforeDelivery,"Delivery after closing pays revenue");
            var ingredient=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g))).First(d=>d.toppingType==ToppingType.Pepperoni);
            int priorStock=ToppingStockManager.GetStock(ingredient.toppingName);
            Check(DeliveryService.Yun.ShopUI.ShopSupplyPurchase.TryBuy(ingredient,1,out _),"Existing supply purchase charges funds");
            int pendingStock=ToppingStockManager.GetPendingStock(ingredient.toppingName);
            Check(pendingStock>0&&ToppingStockManager.GetStock(ingredient.toppingName)==priorStock,"Supply delivery waits until next day");
            day.OnClick_CloseShop();Check(day.summaryPanel.activeSelf&&Time.timeScale==0,"Settlement locks gameplay");
            int before=money.currentMoney;day.OnClick_ConfirmClose();
            Check(run.Settled&&money.currentMoney==before-4000,"Fixed costs charged once");
            int paid=money.currentMoney;Check(DailyRent.Current.SettleDay(1)==0&&money.currentMoney==paid,"Duplicate charge prevented");
            ScreenCapture.CaptureScreenshot("Logs/Challenge-settlement.png");yield return null;yield return null;
            day.OnClick_ConfirmClose();Check(day.currentDay==2&&day.currentTime==8&&!day.isShopOpen&&Time.timeScale==1,"Next day restores controls and calendar");
            Check(ToppingStockManager.GetStock(ingredient.toppingName)==priorStock+pendingStock&&ToppingStockManager.GetPendingStock(ingredient.toppingName)==0,"Next day delivers purchased supplies exactly once");
            Check(money.dailyIncome==0&&money.dailyTotalExpense==0&&DailyRent.Cost(2)==4500,"New daily accounts and rising cost");
            day.OnClick_OpenShop();for(int i=0;i<4;i++)yield return null;
            Check(orders.PendingOrders.Count>0,"Next day replenishes orders");
            day.OnClick_CloseShop();day.OnClick_CancelClose();Check(day.isShopOpen&&!day.summaryPanel.activeSelf&&Time.timeScale==1,"Cancel settlement resumes business");
            LoanAccount.Current.Borrow(10000,2,out _);int interest=LoanAccount.Current.NextInterest;
            day.OnClick_CloseShop();before=money.currentMoney;day.OnClick_ConfirmClose();
            Check(money.currentMoney==before-4500-interest,"Loan interest and fixed costs settle together");
            day.OnClick_ConfirmClose();money.SpendMoney(money.currentMoney,ExpenseType.Loss);
            day.OnClick_CloseShop();day.OnClick_ConfirmClose();Check(run.Bankrupt&&money.currentMoney<0,"Unpaid daily costs end challenge");
            day.OnClick_ConfirmClose();while(SceneManager.GetActiveScene().name!="Lobby")yield return null;
            for(int i=0;i<4;i++)yield return null;
            Check(!GameModeSession.IsChallenge,"Bankruptcy returns to lobby");
            lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>();lobby.StartGame();
            while(SceneManager.GetActiveScene().name!="BankPrologue")yield return null;
            Check(!GameModeSession.IsChallenge,"Story card still enters prologue in story mode");
            // Test original city story defaults without playing the prologue cinematic.
            SceneManager.LoadScene("JJinCity");for(int i=0;i<12;i++)yield return null;
            day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();int originalDay=day.currentDay;
            day.OnClick_ConfirmClose();Check(day.currentDay==originalDay&&DailyRent.Cost(1)==0&&!day.GetComponent<ChallengeRun>(),"Story city retains no-day/no-rent behavior");
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/Challenge-save-backup.json"));
                foreach(var v in b.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
                if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
                if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).maximized=SessionState.GetBool(Key+".maximized",false);
                EditorSceneManager.OpenScene(SessionState.GetString(Key+".scene","Assets/Scenes/Lobby.unity"));
            }finally{Application.runInBackground=SessionState.GetBool(Key+".background",false);SessionState.SetBool(Key,false);Time.timeScale=1;}
        }
    }
}
#endif
