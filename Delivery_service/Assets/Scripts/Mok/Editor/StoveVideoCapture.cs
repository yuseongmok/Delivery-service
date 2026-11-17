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
using UnityEditor.Media;
using Unity.Collections;
using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class StoveVideoCapture
    {
        const string Key="Mok.StoveVideo.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static StoveVideoCapture(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        public static void Begin(){File.WriteAllText("Temp/StoveVideoCapture","run");Poll();}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/StoveVideoCapture"))
            {
                File.Delete("Temp/StoveVideoCapture");
                var scene=SceneManager.GetActiveScene();
                if(scene.isDirty){File.WriteAllText("Logs/StoveVideoCapture.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/StoveVideo-save-backup.json",JsonUtility.ToJson(backup));
                EditorSceneManager.SaveScene(scene,"Logs/Challenge-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                SessionState.SetBool(Key+".maximized",gameView.maximized);if(!Application.isBatchMode)gameView.maximized=true;
                EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool(Key,false)||routine==null||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup-began>1800)throw new Exception("Timed out");if(routine.MoveNext())return;}
            catch(Exception e){log+="FAIL: "+e+"\n";}
            FinishVideo();File.WriteAllText("Logs/StoveVideoCapture.txt",log);routine=null;EditorApplication.ExitPlaymode();
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
        static readonly string Output="../Documents/STOVE_Video_Work/";
        static void Set(object o,string p,object v)=>o.GetType().GetProperty(p).SetValue(o,v);
        static void Invoke(object o,string m,params object[] args)=>o.GetType().GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(o,args);
        static MediaEncoder encoder;
        static RenderTexture rt;
        static Texture2D pixels;
        static int totalFrames;
        static double audioEnergy;
        static long audioSamples;
        static float audioPeak;
        static void OpenVideo()
        {
            Directory.CreateDirectory(Output);
            Time.captureFramerate=30;
            var config=AudioSettings.GetConfiguration();config.speakerMode=AudioSpeakerMode.Stereo;AudioSettings.Reset(config);
            encoder=new MediaEncoder(Output+"capture.mp4",new VideoTrackAttributes{frameRate=new MediaRational(30),width=1280,height=720,includeAlpha=false},new AudioTrackAttributes{sampleRate=new MediaRational(AudioSettings.outputSampleRate),channelCount=2,language="ko"});
            rt=new RenderTexture(1280,720,24){antiAliasing=2};pixels=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            Check(AudioRenderer.Start(),"Audio capture started");
            totalFrames=0;audioEnergy=0;audioSamples=0;audioPeak=0;
        }
        static void FinishVideo()
        {
            if(encoder==null)return;
            encoder.Dispose();encoder=null;AudioRenderer.Stop();Time.captureFramerate=0;
            if(rt){rt.Release();UnityEngine.Object.Destroy(rt);}if(pixels)UnityEngine.Object.Destroy(pixels);
            log+="Frames="+totalFrames+" Samples="+audioSamples+" RMS="+Math.Sqrt(audioEnergy/Math.Max(1,audioSamples))+" Peak="+audioPeak+"\n";
        }
        static IEnumerator Clip(string name,int frames,Func<Camera> camera,Action<int> action=null)
        {
            File.AppendAllText(Output+"shots.txt",name+" start="+(totalFrames/30f)+" length="+(frames/30f)+"\n");
            for(int i=0;i<frames;i++){
                yield return null;
                action?.Invoke(i);
                CaptureFrame(camera());
                if(i==frames/2)File.WriteAllBytes(Output+name+".png",pixels.EncodeToPNG());
            }
        }
        static void CaptureFrame(Camera camera)
        {
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            camera.targetTexture=rt;camera.aspect=1280f/720;
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.001f;}
            Canvas.ForceUpdateCanvases();
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            CheckFrame(encoder.AddFrame(pixels),"video");
            int count=AudioRenderer.GetSampleCountForCaptureFrame()*2;
            using(var audio=new NativeArray<float>(count,Allocator.Temp)){
                bool rendered=AudioRenderer.Render(audio);if(count>0&&rendered){for(int i=0;i<count;i++){audioEnergy+=audio[i]*audio[i];audioPeak=Mathf.Max(audioPeak,Mathf.Abs(audio[i]));}audioSamples+=count;CheckFrame(encoder.AddSamples(audio),"audio");}
            }
            totalFrames++;
            camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;
            foreach(var c in canvases)if(c){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
        }
        static void CheckFrame(bool ok,string message){if(!ok)throw new Exception("Encoder failed "+message);}
        static void Place(PlayerCombat p,Vector3 at){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at+Vector3.up*.1f;cc.enabled=true;}
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"shots.txt","");
            GameModeSession.Select(false);SceneManager.LoadScene("JJinCity");for(int i=0;i<50;i++)yield return null;
            StoryPhoneInbox.Current.AutoOpenEnabled=false;UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
            Complete(BrokerMission.Current);Complete(SpecialOrderMission.Current);Complete(BackdoorMission.Current);Complete(MechanicMission.Current);
            var oldHud=UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>();if(oldHud)UnityEngine.Object.DestroyImmediate(oldHud.gameObject);
            new GameObject("Capture milestone observer").AddComponent<MissionMilestoneHUD>();for(int i=0;i<6;i++)yield return null;
            var venue=VenueFinale.Current;Check(venue.DeveloperStart(),"Begin venue");for(int i=0;i<5;i++)yield return null;
            Set(venue,"State",VenueFinale.Phase.Admission);Invoke(venue,"FinishCinematic");
            for(int i=0;i<90;i++)yield return null;
            OpenVideo();var view=venue.Player.View;
            var clip=Clip("04-final-infiltration",120,()=>view,i=>{
                view.transform.rotation=Quaternion.Slerp(view.transform.rotation,Quaternion.LookRotation(venue.deliveryTable.position+Vector3.up*.8f-view.transform.position),.02f);
            });while(clip.MoveNext())yield return null;
            Set(venue,"State",VenueFinale.Phase.Observe);
            bool safe=false;
            foreach(var point in venue.stealth.listeningPoints.Reverse()){
                venue.PlacePlayer(point);view.transform.LookAt(venue.target.position+Vector3.up*1.25f);Physics.SyncTransforms();
                log+="Point "+point.name+" at "+point.position+" spotted="+venue.stealth.guards.Any(g=>g.CanSee(venue.Player))+" target="+venue.stealth.TargetSeesPlayer()+"\n";
                if(!venue.stealth.guards.Any(g=>g.CanSee(venue.Player))&&!venue.stealth.TargetSeesPlayer()){safe=true;break;}
            }
            Check(safe,"Hidden listening point");
            clip=Clip("05-final-observation",180,()=>view,i=>{
                if(venue.State==VenueFinale.Phase.Observe)view.transform.LookAt(venue.target.position+Vector3.up*1.25f);
            });while(clip.MoveNext())yield return null;
            log+="Final venue state "+venue.State+"\n";
            FinishVideo();
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/StoveVideo-save-backup.json"));
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
