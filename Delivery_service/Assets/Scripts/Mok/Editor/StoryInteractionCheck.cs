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
    [InitializeOnLoad] public static class StoryInteractionCheck
    {
        const string Key="Mok.StoryInteraction.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static StoryInteractionCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/StoryInteractionCheck"))
            {
                File.Delete("Temp/StoryInteractionCheck");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/StoryInteractionCheck.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/StoryInteraction-save-backup.json",JsonUtility.ToJson(backup));
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
            File.WriteAllText("Logs/StoryInteractionCheck.txt",log);routine=null;EditorApplication.ExitPlaymode();
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
            var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>();lobby.StartGame();
            while(SceneManager.GetActiveScene().name!="BankPrologue")yield return null;
            for(int i=0;i<5;i++)yield return null;
            var prologue=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
            Check(prologue.objective.GetComponent<StoryObjectiveCard>()!=null,"Prologue objective bound");
            Check(prologue.IsCinematic&&prologue.objective.enabled,"Cinematic title retains original presentation");
            SceneManager.LoadScene("JJinCity");for(int i=0;i<12;i++)yield return null;
            var hud=UnityEngine.Object.FindFirstObjectByType<CompactHUD>();
            Check(!hud.transform.Find("Balance").gameObject.activeSelf&&!hud.transform.Find("Time").gameObject.activeSelf,"Story hides persistent cash and calendar");
            var sources=new[]{BrokerMission.Current.objective,SpecialOrderMission.Current.objective,BackdoorMission.Current.objective,MechanicMission.Current.objective,VenueFinale.Current.objective};
            foreach(var source in sources)Check(source.GetComponent<StoryObjectiveCard>()!=null,"Mission objective binding: "+source.transform.parent.name);
            StoryObjectiveCard.Split("특별 주문 · 치즈피자 전달하기\n[F] 상호작용","미션",out var caption,out var instruction);
            Check(caption=="특별 주문"&&instruction=="치즈피자 전달하기\n[F] 상호작용","Mission title split retains interaction hint");
            var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;
            UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();
            var broker=BrokerMission.Current;Check(broker.Begin(),"Existing broker mission starts normally");
            for(int i=0;i<5;i++)yield return null;
            var presenter=broker.objective.GetComponent<StoryObjectiveCard>();
            string original=broker.objective.text;var phase=broker.State;
            Check(presenter.Visible&&!broker.objective.enabled&&presenter.DisplayedGoal.Contains("실장"),"Active story objective visible without duplicate old text");
            Check(!hud.transform.Find("Order").gameObject.activeSelf,"Story order card hidden during mission");
            Check(broker.State==phase&&broker.objective.text==original,"Presentation preserves mission state and source text");
            ScreenCapture.CaptureScreenshot("Logs/StoryHud-city.png");yield return null;yield return null;
            var cue=broker.marker.GetComponent<StoryInteractionCue>();Check(cue,"Broker marker has interaction presenter");
            var update=typeof(StoryInteractionCue).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            string savedObjective=broker.objective.text;
            broker.objective.text="연락이 끊긴 중개인 · 실장 찾기\n[F] 실장과 대화";
            cue.Record(20,true,"실장 · 20m");update.Invoke(cue,null);
            Check(!cue.ShowingAction&&broker.marker.text=="실장 · 20m","Far target retains distance");
            cue.Record(2,true,"실장 · 2m");update.Invoke(cue,null);
            Check(cue.ShowingAction&&broker.marker.text=="[F] 실장과 대화","Near target replaces distance with action");
            Check(!StoryInteractionCue.WithoutDuplicate(broker.objective,broker.objective.text).Contains("[F]"),"Action removed from objective card only when marker displays it");
            cue.Record(2,false,"실장 · 2m");update.Invoke(cue,null);
            Check(!cue.ShowingAction&&StoryInteractionCue.WithoutDuplicate(broker.objective,broker.objective.text).Contains("[F]"),"Offscreen target keeps objective hint available");
            Check(StoryInteractionCue.ResolveAction("마지막 배달 · 피자 전달하기","[F] 상호작용")=="[F] 피자 전달하기","Generic interaction resolves to mission action");
            Check(StoryInteractionCue.ResolveAction("마지막 배달 · 서비스 테이블","[F] 상호작용")=="[F] 피자 놓기","Service table describes its action");
            Check(StoryInteractionCue.ResolveAction("매장 탈출 · 권총 챙기기 [F]","매장 탈출 · 권총 챙기기 [F]")=="권총 챙기기 [F]","Inline interaction omits duplicated mission title");
            Check(StoryInteractionCue.ResolveAction("관찰","바라보고 [F] 길게 · 50%") == "바라보고 [F] 길게 · 50%","Hold and look conditions preserved");
            StoryTargetDiamond.Place(broker.marker,broker.player.View,broker.player.View.transform.position+broker.player.View.transform.forward*2,broker.player.View.transform.position,"경유지",30);
            update.Invoke(cue,null);Check(!cue.ShowingAction,"Nearby route waypoint does not imply final destination arrival");
            broker.objective.text=savedObjective;
            Check(broker.State==phase,"Interaction presentation does not change mission phase");
            broker.Cancel();Check(!broker.Running,"Existing mission cancellation retained");
            SceneManager.LoadScene("PrisonChapter");for(int i=0;i<15;i++)yield return null;
            var prison=UnityEngine.Object.FindFirstObjectByType<PrisonSharedPresentation>();
            Check(prison&&prison.objective.GetComponent<StoryObjectiveCard>(),"Shared prison objective binding");
            hud=UnityEngine.Object.FindFirstObjectByType<CompactHUD>();Check(hud&&!hud.transform.Find("Balance").gameObject.activeSelf,"Prison persistent cash hidden");
            GameModeSession.Select(true);SceneManager.LoadScene("JJinCity");for(int i=0;i<15;i++)yield return null;
            hud=UnityEngine.Object.FindFirstObjectByType<CompactHUD>();
            Check(hud.transform.Find("Balance").gameObject.activeSelf&&hud.transform.Find("Time").gameObject.activeSelf&&hud.transform.Find("Order").gameObject.activeSelf,"Challenge cash, time and orders retained");
            Check(!UnityEngine.Object.FindFirstObjectByType<StoryObjectiveCard>(),"No story objective restyling in challenge");
            var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();day.OnClick_OpenShop();for(int i=0;i<4;i++)yield return null;
            Check(UnityEngine.Object.FindFirstObjectByType<OrderManager>().PendingOrders.Count>0,"Challenge order generation retained");
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/StoryInteraction-save-backup.json"));
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
