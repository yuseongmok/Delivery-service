#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class AftermathCheck
    {
        static double began, changed, pauseAt;
        static int last=-1, pass;
        static bool captured, pauseDone, paused, skipping;
        static bool escapeStarted;
        static Vector3 frozen;
        static AftermathCheck(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            File.WriteAllText("Logs/Aftermath-playcheck.txt", "Starting real-time cinematic playback\n");
            EditorSceneManager.OpenScene(AftermathSetup.ScenePath);
            SessionState.SetBool("AftermathCheck",true);EditorApplication.EnterPlaymode();
        }
        public static void RunFromEscape()
        {
            File.WriteAllText("Logs/Aftermath-playcheck.txt", "Starting at transport completion boundary (prior missions seeded)\n");
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
            SessionState.SetBool("AftermathCheck",true);EditorApplication.EnterPlaymode();
        }
        static void Log(string message,string stack,LogType type)
        {if(SessionState.GetBool("AftermathCheck",false)&&(type==LogType.Exception||type==LogType.Error))File.AppendAllText("Logs/Aftermath-playcheck.txt","ERROR "+message+"\n"+stack+"\n");}
        static void Tick()
        {
            if(!SessionState.GetBool("AftermathCheck",false)||!EditorApplication.isPlaying)return;
            if(began==0)began=EditorApplication.timeSinceStartup;
            try
            {
                if(EditorApplication.timeSinceStartup-began>180)throw new Exception("Playback timed out");
                var active=SceneManager.GetActiveScene();
                if(active.name=="PrisonChapter")
                {
                    if(escapeStarted||EditorApplication.timeSinceStartup-began<3)return;
                    escapeStarted=true;
                    var t=Object.FindFirstObjectByType<PrisonTransportEscape>();
                    var o=t.previous.previous.office;
                    o.previous.previous.prison.introduction.DeveloperComplete();
                    Object.FindFirstObjectByType<PrisonChickenMission>().DeveloperComplete();
                    typeof(PrisonKeysMission).GetProperty("State").SetValue(o.previous,PrisonKeysMission.Stage.Complete);
                    o.evidence.DeveloperComplete();o.Collect(PrisonOfficePoint.Kind.Uniforms);
                    t.previous.previous.DeveloperComplete();t.previous.DeveloperComplete();t.DeveloperComplete();
                    t.dialogue=Object.Instantiate(t.dialogue);t.dialogue.lineSeconds=.15f;
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    typeof(PrisonTransportEscape).GetMethod("Lock",flags).Invoke(t,null);
                    t.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonTransportEscape).GetMethod("Escape",flags).Invoke(t,null));
                    File.AppendAllText("Logs/Aftermath-playcheck.txt","Invoked actual transport Escape completion coroutine; destination="+t.aftermathScene+"\n");return;
                }
                if(active.name=="NewHarborCity")
                {
                    File.AppendAllText("Logs/Aftermath-playcheck.txt",(pass==0?"PASS complete playback":"PASS skip")+" -> NewHarborCity; timeScale="+Time.timeScale+"\n");
                    if(pass==0){pass++;SceneManager.LoadScene("PrisonAftermath");last=-1;skipping=true;return;}
                    File.AppendAllText("Logs/Aftermath-playcheck.txt","FINISHED\n");SessionState.SetBool("AftermathCheck",false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return;
                }
                var f=Object.FindFirstObjectByType<AftermathFilm>();if(!f)return;
                if(skipping&&f.CurrentShot>=0){f.RequestSkip();return;}
                if(f.CurrentShot!=last)
                {
                    last=f.CurrentShot;changed=EditorApplication.timeSinceStartup;captured=false;
                    File.AppendAllText("Logs/Aftermath-playcheck.txt","Shot "+last+" "+(last>=0?f.shots[last].name:"")+"\n");
                }
                if(last==1&&!pauseDone&&!paused){f.SetPaused(true);paused=true;pauseAt=EditorApplication.timeSinceStartup;frozen=f.film.transform.position;return;}
                if(paused)
                {
                    if(EditorApplication.timeSinceStartup-pauseAt<.7)return;
                    if(Vector3.Distance(frozen,f.film.transform.position)>.001)throw new Exception("Camera moved during pause");
                    f.SetPaused(false);paused=false;pauseDone=true;File.AppendAllText("Logs/Aftermath-playcheck.txt","PASS pause/resume freezes camera and resumes sequence\n");
                }
                if(last>=0&&!captured&&EditorApplication.timeSinceStartup-changed>2)
                {
                    captured=true;
                    PrisonChickenCheck.Capture(f.film,"Logs/Aftermath-play-"+last.ToString("00")+".png");
                    var ui=f.subtitle.GetComponentInParent<Canvas>();
                    ui.renderMode=RenderMode.ScreenSpaceCamera;ui.worldCamera=f.film;ui.planeDistance=.15f;
                    Canvas.ForceUpdateCanvases();
                    PrisonChickenCheck.Capture(f.film,"Logs/Aftermath-game-"+last.ToString("00")+".png");
                    ui.renderMode=RenderMode.ScreenSpaceOverlay;ui.worldCamera=null;
                }
            }
            catch(Exception e){File.AppendAllText("Logs/Aftermath-playcheck.txt","FAIL "+e+"\n");SessionState.SetBool("AftermathCheck",false);EditorApplication.Exit(1);}
        }
    }
}
#endif
