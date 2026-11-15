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
    [InitializeOnLoad] public static class MissionPhonePolishCheck
    {
        const string Key="Mok.MissionPhone.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static MissionPhonePolishCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/MissionPhonePolishCheck"))
            {
                File.Delete("Temp/MissionPhonePolishCheck");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/MissionPhonePolishCheck.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/MissionPhone-save-backup.json",JsonUtility.ToJson(backup));
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
            File.WriteAllText("Logs/MissionPhonePolishCheck.txt",log);routine=null;EditorApplication.ExitPlaymode();
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
            for(int i=0;i<5;i++)yield return null;
            GameModeSession.Select(false);SceneManager.LoadScene("JJinCity");for(int i=0;i<15;i++)yield return null;
            StoryPhoneInbox.Current.AutoOpenEnabled=false;
            var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();
            var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();
            phone.OpenPhone();for(int wait=0;wait<8;wait++)yield return null;planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);
            var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>();
            Check(app&&app.VisibleMissionCount==1&&app.cards.Count(c=>c.gameObject.activeSelf)==1,"Only first unlocked mission is listed");
            app.Confirm();Check(app.Detail&&app.body.text.Contains("현재 목표")&&app.body.text.Contains("이 미션을 하는 이유"),"Detail separates objective and motivation");
            app.Confirm();for(int i=0;i<5;i++)yield return null;
            Check(BrokerMission.Current.Running&&!phone.IsPhoneOpen(),"Existing phone start releases control into broker mission");
            phone.OpenPhone();for(int wait=0;wait<8;wait++)yield return null;planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);app.Confirm();
            Check(app.body.text.Contains("실장 찾기"),"Running mission uses live objective");
            app.Confirm();Check(BrokerMission.Current.Running,"First cancel confirmation preserves mission");app.Confirm();Check(!BrokerMission.Current.Running,"Second cancellation uses original mission logic");
            // 완료 단계는 UI 회귀 검사용으로만 주입한다. 실제 플레이 클리어를 주장하지 않는다.
            Complete(BrokerMission.Current);app.Open(false);
            Check(app.VisibleMissionCount==2&&app.Selection==1,"Unlock advances exactly one mission and selects it");
            app.Move(-1);app.Confirm();Check(app.body.text.Contains("완료한 미션")&&!app.action.transform.parent.gameObject.activeSelf,"Completed details show result and hide start action");
            app.Confirm();Check(BrokerMission.Current.Completed,"Completed mission cannot restart");
            Complete(SpecialOrderMission.Current);app.Open(false);Check(app.VisibleMissionCount==3&&app.Selection==2,"Recon unlock follows approval");
            Complete(BackdoorMission.Current);app.Open(false);Check(app.VisibleMissionCount==4&&app.Selection==3,"Mechanic unlock follows reconnaissance");
            Complete(MechanicMission.Current);app.Open(false);Check(app.VisibleMissionCount==5&&app.Selection==4,"Finale unlock follows vehicle support");
            app.Confirm();Check(app.body.text.Contains("납품 승인")&&app.body.text.Contains("현재 목표"),"Finale motivation references preparations");
            foreach(var mission in app.plan.missions)
            {
                Check(!string.IsNullOrWhiteSpace(mission.purpose)&&!string.IsNullOrWhiteSpace(mission.startingObjective),"Editable mission brief: "+mission.id);
                app.body.text=StoryMissionBrief.Detail(mission,false);app.body.resizeTextForBestFit=false;app.body.fontSize=21;
                Check(app.body.preferredHeight<=580,"Detail fits readable minimum size: "+mission.id);
            }
            planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);app.Confirm();app.Refresh();for(int wait=0;wait<4;wait++)yield return null;
            CapturePhone();ScreenCapture.CaptureScreenshot("Logs/MissionPhone-detail.png");yield return null;yield return null;
            app.Open(false);app.Move(-4);Check(app.Selection==0,"Completed history remains navigable");
            planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Messages);Check(app.MessagesMode,"Message screen retained");
            GameModeSession.Select(true);SceneManager.LoadScene("JJinCity");for(int i=0;i<15;i++)yield return null;
            phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.OpenPhone();
            planner=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);
            app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>();
            Check(app.heading.text=="도전 모드"&&app.body.text.Contains("고정비"),"Challenge guidance unchanged");app.Confirm();Check(!BrokerMission.Current.Running,"Challenge cannot start story from phone");
            GameModeSession.Select(false);SceneManager.LoadScene("PrisonChapter");for(int wait=0;wait<15;wait++)yield return null;
            phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.OpenPhone();for(int wait=0;wait<8;wait++)yield return null;
            planner=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);
            app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>();var presentation=UnityEngine.Object.FindFirstObjectByType<PrisonSharedPresentation>();
            Check(app.body.text.Contains(presentation.objective.text)&&app.body.text.Contains("이 미션을 하는 이유"),"Prison phone uses current shared objective");
            Check(!app.action.transform.parent.gameObject.activeSelf,"Prison story remains automatically controlled");
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
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/MissionPhone-save-backup.json"));
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
