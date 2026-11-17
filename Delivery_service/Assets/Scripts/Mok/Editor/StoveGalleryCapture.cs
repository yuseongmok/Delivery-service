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
    [InitializeOnLoad] public static class StoveGalleryCapture
    {
        const string Key="Mok.StoveGallery.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static StoveGalleryCapture(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        public static void Begin(){File.WriteAllText("Temp/StoveGalleryCapture","run");Poll();}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/StoveGalleryCapture"))
            {
                File.Delete("Temp/StoveGalleryCapture");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/StoveGalleryCapture.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/StoveGallery-save-backup.json",JsonUtility.ToJson(backup));
                EditorSceneManager.SaveScene(scene,"Logs/Challenge-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                SessionState.SetBool(Key+".maximized",gameView.maximized);if(!Application.isBatchMode)gameView.maximized=true;
                EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool(Key,false)||routine==null||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup-began>600)throw new Exception("Timed out");if(routine.MoveNext())return;}
            catch(Exception e){log+="FAIL: "+e+"\n";}
            File.WriteAllText("Logs/StoveGalleryCapture.txt",log);routine=null;EditorApplication.ExitPlaymode();
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
        static readonly string Output="../Documents/STOVE_Gallery_v2_raw/";
        static void Set(object o,string p,object v)=>o.GetType().GetProperty(p).SetValue(o,v);
        static void Invoke(object o,string m,params object[] args)=>o.GetType().GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(o,args);
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);
            GameModeSession.Select(false);SceneManager.LoadScene("BankPrologue");for(int i=0;i<8;i++)yield return null;
            var bank=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();bank.autoEnterGame=false;bank.StopAllCoroutines();bank.accomplice.Board();
            bank.fade.alpha=0;bank.filmCamera.enabled=true;bank.player.view.enabled=false;bank.player.weapon.SetActive(false);
            Invoke(bank,"BeginDriving");bank.drive.ExternalInput=true;bank.drive.SetInput(1,0);
            float start=Time.time;while(Time.time-start<1.4f)yield return null;Capture(bank.filmCamera,"drive-fixed");
            bank.drive.Stop();Set(bank,"Current",BankPrologue.Phase.CityReveal);
            bank.cityVista.SetActive(true);RenderSettings.fog=false;bank.filmCamera.farClipPlane=3000;bank.bars.alpha=1;
            bank.objective.text="";bank.prompt.text="Space  컷씬 건너뛰기";bank.ammo.text="";bank.health.text="";
            bank.StartCoroutine(bank.shopArrival.Play());
            start=Time.time;while(bank.shopArrival.Stage<4&&Time.time-start<70)yield return null;
            start=Time.time;while(Time.time-start<1.3f)yield return null;Capture(bank.filmCamera,"cut-shop-fixed");
            GameModeSession.Select(false);SceneManager.LoadScene("JJinCity");for(int i=0;i<60;i++)yield return null;
            StoryPhoneInbox.Current.AutoOpenEnabled=false;UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
            var camera=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>().View;
            var maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();
            var center=maker.transform.position;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.isRootCanvas&&canvas.renderMode==RenderMode.ScreenSpaceOverlay)canvas.enabled=false;
            Shot(camera,center+new Vector3(12,2.2f,12),center+Vector3.up*1.5f,"street-a");
            Shot(camera,center+new Vector3(-12,2.2f,12),center+Vector3.up*1.5f,"street-b");
            Shot(camera,center+new Vector3(12,2.2f,-12),center+Vector3.up*1.5f,"street-c");
            Shot(camera,center+new Vector3(-12,2.2f,-12),center+Vector3.up*1.5f,"street-d");
            log+="Captured driving HUD, shop dialogue, current city street.";
        }
        static void Shot(Camera camera,Vector3 position,Vector3 target,string name){
            var p=camera.transform.position;var q=camera.transform.rotation;
            camera.transform.position=position;camera.transform.LookAt(target);Capture(camera,name);
            camera.transform.SetPositionAndRotation(p,q);
        }
        static void Capture(Camera camera,string name){
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.001f;}
            Canvas.ForceUpdateCanvases();
            var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1720,966,24);rt.antiAliasing=4;camera.targetTexture=rt;camera.aspect=1720f/966;
            try{camera.Render();RenderTexture.active=rt;var t=new Texture2D(1720,966,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1720,966),0,0);t.Apply();File.WriteAllBytes(Output+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);foreach(var c in canvases){if(c){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}}}
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/StoveGallery-save-backup.json"));
                foreach(var v in b.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
                if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
                if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).maximized=SessionState.GetBool(Key+".maximized",false);
                EditorSceneManager.OpenScene(SessionState.GetString(Key+".scene","Assets/Scenes/Lobby.unity"));
            }finally{Application.runInBackground=SessionState.GetBool(Key+".background",false);SessionState.SetBool(Key,false);Time.timeScale=1;if(Application.isBatchMode)EditorApplication.Exit(log.Contains("FAIL:")?1:0);}
        }
    }
}
#endif
