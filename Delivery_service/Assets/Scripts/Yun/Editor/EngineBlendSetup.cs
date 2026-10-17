#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DeliveryService.Yun.Audio;
namespace DeliveryService.Yun.Editor
{
 public static class EngineBlendSetup
 {
  const string Folder="Assets/Scripts/Yun/Audio/Clips/", Key="Yun.EngineBlendTest";
  static double deadline;
  public static void RunBatch()
  {
   try {
    MakeLoop("engine-low"); MakeLoop("engine-high");
    var lib=AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/Scripts/Yun/SoundLibrary.asset");
    lib.FillMissingEntries();
    Set(lib,SoundId.MotorcycleEngineLoop,"engine-low-loop.wav");
    Set(lib,SoundId.MotorcycleEngineHighLoop,"engine-high-loop.wav");
    EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
    EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
    UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled=false;
    SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  static void Set(SoundLibrary lib,SoundId id,string file)
  {
   if(!lib.TryGetEntry(id,out var entry))throw new Exception("Missing slot");
   entry.clips=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+file)};
   entry.loop=true;entry.volume=.65f;entry.pitchRange=Vector2.one;entry.category=SoundCategory.Sfx;
  }
  static void MakeLoop(string name)
  {
   string path=Folder+name+"-source.mp3";
   var importer=(AudioImporter)AssetImporter.GetAtPath(path);
   var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
   importer.defaultSampleSettings=settings;importer.SaveAndReimport();
   var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();
   var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Decode failed");
   var mono=new float[clip.samples];
   for(int i=0;i<mono.Length;i++)for(int c=0;c<clip.channels;c++)mono[i]+=data[i*clip.channels+c]/clip.channels;
   int length=Math.Min((int)(2.5f*clip.frequency),mono.Length), block=Math.Max(1,clip.frequency/10), best=0;
   double bestScore=double.MaxValue;
   // Avoid silence and choose a region with relatively steady loudness.
   for(int start=0;start+length<=mono.Length;start+=block){
    double sum=0,squares=0;int count=0;
    for(int j=start;j+block<=start+length;j+=block){double energy=0;for(int k=j;k<j+block;k++)energy+=mono[k]*mono[k];double rms=Math.Sqrt(energy/block);sum+=rms;squares+=rms*rms;count++;}
    double mean=sum/Math.Max(1,count);if(mean<.025)continue;
    double score=Math.Max(0,squares/count-mean*mean)/(mean*mean);
    if(score<bestScore){bestScore=score;best=start;}
   }
   int fade=Math.Min(clip.frequency/5,length/4), n=length-fade;
   var output=new float[n];Array.Copy(mono,best+fade,output,0,length-2*fade);
   for(int i=0;i<fade;i++){float t=i/(float)(fade-1);t=t*t*(3-2*t);output[length-2*fade+i]=Mathf.Lerp(mono[best+length-fade+i],mono[best+i],t);}
   double rmsOut=Math.Sqrt(output.Select(v=>(double)v*v).Average());
   float peak=output.Max(v=>Mathf.Abs(v));float gain=Mathf.Min((float)(.16/Math.Max(.0001,rmsOut)),.85f/Mathf.Max(.001f,peak));
   string target=Folder+name+"-loop.wav";
   using(var w=new BinaryWriter(File.Create(target))){
    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(clip.frequency);w.Write(clip.frequency*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(n*2);
    foreach(var v in output)w.Write((short)Mathf.RoundToInt(Mathf.Clamp(v*gain,-1,1)*32767));
   }
   AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
   Debug.Log("ENGINE_LOOP: "+name+" source="+clip.length+" selected="+best/(float)clip.frequency+" length="+n/(float)clip.frequency);
  }
  [InitializeOnLoadMethod]static void Resume(){if(!SessionState.GetBool(Key,false))return;deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Test;}
  static void Test(){
   if(!EditorApplication.isPlaying||Time.frameCount<5){if(EditorApplication.timeSinceStartup>deadline)Finish(1);return;}
   try{
    var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var driver=bike.GetComponent<MotorcycleDrivingAudio>();var manager=AudioManager.Instance;
    var flags=BindingFlags.Instance|BindingFlags.NonPublic;var speed=typeof(MotorcycleController).GetField("currentSpeed",flags);var tick=typeof(MotorcycleDrivingAudio).GetMethod("Tick",flags);
    Action<float,int> step=(v,n)=>{speed.SetValue(bike,v);for(int i=0;i<n;i++)tick.Invoke(driver,new object[]{.02f});};
    manager.StopAll();manager.MasterVolume=1;manager.MasterMuted=false;manager.SetCategoryVolume(SoundCategory.Sfx,1);manager.SetCategoryMuted(SoundCategory.Sfx,false);bike.isDriven=true;
    step(3,30);
    var sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
    var low=sources.Single(s=>s.clip!=null&&s.clip.name=="engine-low-loop");var high=sources.Single(s=>s.clip!=null&&s.clip.name=="engine-high-loop");
    if(low.volume<=high.volume||manager.ActiveCount!=2)throw new Exception("Low speed blend");
    step(30,60);if(high.volume<=low.volume||manager.ActiveCount!=2)throw new Exception("High speed blend");
    step(3,10);if(manager.ActiveCount!=3)throw new Exception("Brake missing");
    step(0,1);if(manager.ActiveCount!=0)throw new Exception("Stop must be immediate");
    step(10,30);manager.SetCategoryVolume(SoundCategory.Sfx,0);if(low.volume!=0||high.volume!=0)throw new Exception("SFX setting");
    Time.timeScale=0;step(10,1);if(manager.ActiveCount!=0)throw new Exception("Pause");Time.timeScale=1;
    step(10,30);bike.isDriven=false;step(0,1);if(manager.ActiveCount!=0)throw new Exception("Dismount");
    Debug.Log("ENGINE_BLEND_TEST_PASSED: two layers, speed blend, brake, immediate stop, settings, pause, dismount");Finish(0);
   }catch(Exception e){Debug.LogException(e);Finish(1);}
  }
  static void Finish(int code){Time.timeScale=1;SessionState.SetBool(Key,false);EditorApplication.update-=Test;EditorApplication.Exit(code);}
 }
}
#endif
