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
    [InitializeOnLoad] public static class ChickenPlatePreview
    {
        const string Key="Mok.ChickenPlatePreview.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static ChickenPlatePreview(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        public static void Begin(){File.WriteAllText("Temp/ChickenPlatePreview","run");Poll();}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/ChickenPlatePreview"))
            {
                File.Delete("Temp/ChickenPlatePreview");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/ChickenPlatePreview.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/ChickenUIReview-save-backup.json",JsonUtility.ToJson(backup));
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
            File.WriteAllText("Logs/ChickenPlatePreview.txt",log);routine=null;EditorApplication.ExitPlaymode();
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
        static readonly string Output="../Documents/Chicken_Cooking_Flow/";
        static void Set(object o,string p,object v)=>o.GetType().GetProperty(p).SetValue(o,v);
        static void Invoke(object o,string m,params object[] args)=>o.GetType().GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(o,args);
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);
            var assembly=typeof(EditorWindow).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance").GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,new object[]{Enum.Parse(kind,"FixedResolution"),1280,720,"Chicken UI review"},null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int n=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null)-1;
            var gv=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            gv.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(gv,n);
            gv.Show();gv.Focus();
            GameModeSession.Select(false);SceneManager.LoadScene("PrisonChapter");for(int i=0;i<40;i++)yield return null;
            var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();var intro=m.prison.introduction;
            intro.StopAllCoroutines();Invoke(intro,"SetCinematic",false);Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(intro,"Fade",0f);Set(intro,"Card","");Set(intro,"Subtitle","");intro.mugshotActor.gameObject.SetActive(false);
            m.ResetKitchen();for(int i=0;i<m.Collected.Length;i++)m.Collected[i]=true;
            var player=m.prison.player;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=m.station.transform.position+Vector3.back;cc.enabled=true;
            Invoke(m,"Lock",true,m.cookView);m.cooking.Begin();
            m.cooking.Close();m.FinishCooking();
            player.GetComponentInChildren<CameraMove>().isControllable=false;
            player.View.transform.LookAt(m.chef.position+Vector3.up*.5f);
            for(int i=0;i<15;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Output+"plated-chicken-polished.png");
            for(int i=0;i<8;i++)yield return null;
            Check(m.carrying.activeInHierarchy,"plated chicken carry preview");
        }
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(target);
        static void Mouse(Vector2 gui,bool hold)
        {
            var state=new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(gui.x/1920*Screen.width,(1-gui.y/1080)*Screen.height)};
            if(hold)state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,state);
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
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/ChickenUIReview-save-backup.json"));
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










