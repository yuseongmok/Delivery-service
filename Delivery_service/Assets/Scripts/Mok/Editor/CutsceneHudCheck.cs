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
    [InitializeOnLoad] public static class CutsceneHudCheck
    {
        const string Key="Mok.CutsceneHud.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static CutsceneHudCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/CutsceneHudCheck"))
            {
                File.Delete("Temp/CutsceneHudCheck");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/CutsceneHudCheck.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/CutsceneHud-save-backup.json",JsonUtility.ToJson(backup));
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
            File.WriteAllText("Logs/CutsceneHudCheck.txt",log);routine=null;EditorApplication.ExitPlaymode();
        }
        static void Changed(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;began=EditorApplication.timeSinceStartup;log="";routine=Run();}
            if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;
        }
        static void Complete(object mission)
        {
            var property=mission.GetType().GetProperty("State");
            property.SetValue(mission,Enum.Parse(property.PropertyType,"Complete"));
        }
        static IEnumerator Run()
        {
            var tracker=new MissionMilestoneTracker(false);
            Check(tracker.Observe(true,false)==MissionMilestoneTracker.Change.Started,"Start edge");
            Check(tracker.Observe(true,false)==MissionMilestoneTracker.Change.None,"No repeated start");
            Check(tracker.Observe(false,false)==MissionMilestoneTracker.Change.None,"Cancel is not completion");
            Check(tracker.Observe(true,false)==MissionMilestoneTracker.Change.Started,"Retry announces start");
            Check(tracker.Observe(false,true)==MissionMilestoneTracker.Change.Completed,"Completion edge");
            Check(tracker.Observe(false,true)==MissionMilestoneTracker.Change.None,"Completion once");
            Check(new MissionMilestoneTracker(true).Observe(false,true)==MissionMilestoneTracker.Change.None,"No historical replay");
            for(int i=0;i<5;i++)yield return null;
            GameModeSession.Select(false);SceneManager.LoadScene("JJinCity");for(int i=0;i<20;i++)yield return null;
            StoryPhoneInbox.Current.AutoOpenEnabled=false;
            var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.ClosePhone();
            var hud=UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>();
            Check(hud,"Story observer installed");
            BrokerMission.Current.Begin();for(int i=0;i<10;i++)yield return null;
            Check(hud.IsVisible&&hud.DisplayedTitle=="연락이 끊긴 중개인","Actual mission start displays title");
            Check(!hud.GetComponent<CanvasGroup>().blocksRaycasts,"Notice never consumes input");
            var broker=BrokerMission.Current;
            var state=broker.GetType().GetProperty("State");
            state.SetValue(broker,Enum.Parse(state.PropertyType,"Conversation"));
            yield return null;yield return null;
            Check(StoryCutsceneHUD.Active&&!hud.IsVisible,"Dialogue hides milestone");
            Check(!broker.objective.GetComponent<StoryObjectiveCard>().Visible,"Dialogue hides objective card");
            Check(broker.marker.GetComponents<CanvasGroup>().Any(g=>g.alpha==0),"Dialogue hides marker");
            Check(!broker.subtitle.GetComponents<CanvasGroup>().Any(g=>g.alpha==0),"Subtitle is preserved");
            state.SetValue(broker,Enum.Parse(state.PropertyType,"Find"));
            yield return null;yield return null;
            Check(!StoryCutsceneHUD.Active&&broker.objective.GetComponent<StoryObjectiveCard>().Visible,"Gameplay restores objective");
            Check(broker.marker.GetComponents<CanvasGroup>().All(g=>g.alpha==1),"Gameplay restores marker");

            phone.OpenPhone();yield return null;yield return null;
            Check(!hud.IsVisible&&BrokerMission.Current.Running,"Phone hides notice without changing mission");
            phone.ClosePhone();yield return null;yield return null;
            Time.timeScale=0;yield return null;yield return null;Check(!hud.IsVisible,"Pause hides notice");Time.timeScale=1;
            BrokerMission.Current.Cancel();yield return null;yield return null;Check(!hud.IsVisible,"Cancel hides start without success");
            Complete(BrokerMission.Current);for(int i=0;i<8;i++)yield return null;
            Check(hud.IsVisible&&hud.GetComponentsInChildren<Text>().Any(t=>t.text=="미션 완료"),"Completion displays result");
            Check(hud.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("실장 구출")),"Completion states gained result");
            Check(BrokerMission.Current.Completed,"Observer preserves completed state");
            ScreenCapture.CaptureScreenshot("Logs/Milestone-result.png");yield return null;yield return null;
            GameModeSession.Select(true);SceneManager.LoadScene("JJinCity");for(int i=0;i<20;i++)yield return null;
            Check(!UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>(),"Challenge excludes announcements");
            GameModeSession.Select(false);SceneManager.LoadScene("PrisonChapter");for(int i=0;i<20;i++)yield return null;
            hud=UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>();Check(hud,"Prison observer installed");
            Check(!hud.IsVisible,"Prison introduction cinematic stays unobstructed");
            Check(StoryCutsceneHUD.Active,"Prison cinematic shares visibility policy");
        }
        static void CapturePhone()
        {
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var rt=camera.targetTexture;if(!rt||rt.width>2048)continue;
                var old=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                File.WriteAllBytes("Logs/MissionPhone-"+camera.GetInstanceID()+".png",image.EncodeToPNG());
                log+="CAPTURE: "+camera.name+" -> "+camera.GetInstanceID()+"\n";
                UnityEngine.Object.Destroy(image);RenderTexture.active=old;
            }
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/CutsceneHud-save-backup.json"));
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
