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
    public static class AftermathReview
    {
        static AftermathReview(){EditorApplication.update+=Tick;}
        public static void Record()
        {
            Directory.CreateDirectory("Logs/AftermathReview");
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
            SessionState.SetBool("AftermathRecord",true);EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool("AftermathRecord",false)||!EditorApplication.isPlaying)return;
            var film=Object.FindFirstObjectByType<AftermathFilm>();
            if(film&&!film.GetComponent<AftermathReviewCapture>())film.gameObject.AddComponent<AftermathReviewCapture>().film=film;
            if(SceneManager.GetActiveScene().name=="NewHarborCity")
            {SessionState.SetBool("AftermathRecord",false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
        }
    }
    [DefaultExecutionOrder(32000)]
    public sealed class AftermathReviewCapture:MonoBehaviour
    {
        public AftermathFilm film;
        System.Diagnostics.Process encoder;
        RenderTexture target;
        Texture2D pixels;
        int frames,last=-1,localFrame;
        bool stopped;
        Canvas ui;
        string folder="Logs/AftermathReview";
        void Start()
        {
            Time.captureFramerate=24;
            target=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            ui=film.subtitle.GetComponentInParent<Canvas>();ui.renderMode=RenderMode.ScreenSpaceCamera;ui.worldCamera=film.film;ui.planeDistance=.15f;
            var info=new System.Diagnostics.ProcessStartInfo();info.FileName=Path.GetFullPath("../.codex-work/video-tools/ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe");
            info.Arguments="-y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 24 -i pipe:0 -vf vflip -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+Path.GetFullPath(folder+"/Aftermath-preview.mp4")+"\"";
            info.UseShellExecute=false;info.CreateNoWindow=true;info.RedirectStandardInput=true;info.RedirectStandardError=true;
            encoder=new System.Diagnostics.Process{StartInfo=info};encoder.ErrorDataReceived+=(s,e)=>{};encoder.Start();encoder.BeginErrorReadLine();
            File.WriteAllText(folder+"/playback.txt","Actual Unity Play Mode capture, 24 fps fixed frame step; subtitle preview without audio.\n");
        }
        void LateUpdate()
        {
            if(stopped||encoder==null||encoder.HasExited||film.CurrentShot<0)return;
            if(last!=film.CurrentShot){last=film.CurrentShot;localFrame=0;File.AppendAllText(folder+"/playback.txt","SHOT "+last+" "+film.shots[last].name+" frame="+frames+"\n");}
            var previous=film.film.targetTexture;var active=RenderTexture.active;
            Canvas.ForceUpdateCanvases();film.film.targetTexture=target;film.film.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);pixels.Apply(false);
            encoder.StandardInput.BaseStream.Write(pixels.GetRawTextureData(),0,1280*720*3);
            if(localFrame==24||localFrame==60||localFrame==90||localFrame==120||localFrame==150)File.WriteAllBytes(folder+"/shot-"+last.ToString("00")+"-"+localFrame+".png",pixels.EncodeToPNG());
            film.film.targetTexture=previous;RenderTexture.active=active;frames++;localFrame++;
        }
        void OnDestroy()
        {
            if(stopped)return;stopped=true;Time.captureFramerate=0;
            if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(30000);File.AppendAllText(folder+"/playback.txt","END frames="+frames+" encoder="+encoder.ExitCode+"\n");encoder.Dispose();}
            if(target){target.Release();Object.Destroy(target);}if(pixels)Object.Destroy(pixels);
        }
    }
}
#endif



