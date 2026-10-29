#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;
namespace DeliveryService.Mok.Editor {
public static class SuppliedAudioInstaller {
 public static void Install(){
  AssetDatabase.Refresh();Directory.CreateDirectory("Assets/Resources");
  const string path="Assets/Resources/SuppliedAudio.asset";var bank=AssetDatabase.LoadAssetAtPath<SuppliedAudio>(path);
  if(!bank){bank=ScriptableObject.CreateInstance<SuppliedAudio>();AssetDatabase.CreateAsset(bank,path);}
  AudioClip Clip(string name){string p="Assets/Scripts/Mok/Art/SuppliedAudio/"+name+".wav";var importer=(AudioImporter)AssetImporter.GetAtPath(p);var settings=importer.defaultSampleSettings;settings.loadType=name.EndsWith("Ambience")?AudioClipLoadType.Streaming:name.Contains("Engine")||name.Contains("Alarm")||name=="missionMusic"?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<AudioClip>(p);}
  bank.gunshot=Clip("Gunshot");bank.reload=Clip("Reload");bank.flyby=Clip("Flyby");bank.bankAlarm=Clip("BankAlarm");bank.policeSiren=Clip("PoliceSiren");bank.playerEngine=Clip("PlayerEngine");bank.traffic=new[]{Clip("TrafficEngineA"),Clip("TrafficEngineB")};
  bank.vaultLatch=Clip("vaultLatch");
  bank.vaultSwing=Clip("vaultSwing");
  bank.bodyFall=Clip("bodyFall");
  bank.bag=Clip("bag");
  bank.shutter=Clip("shutter");
  bank.missionMusic=Clip("missionMusic");
  bank.carCrash=Clip("carCrash");
  bank.carOpen=Clip("carOpen");
  bank.carClose=Clip("carClose");
  bank.wallImpact=Clip("wallImpact");
  bank.metalImpact=Clip("metalImpact");
  bank.cityAmbience=Clip("cityAmbience");bank.roomAmbience=Clip("roomAmbience");bank.detected=Clip("detected");bank.escalation=Clip("escalation");
  string steps="Assets/The_Sound_Guild_FREE_PACK_Footsteps_Volume_02/WAV - 44100 Hz - 16 Bit";
  AudioClip[] Steps(string prefix)=>Directory.GetFiles(steps,prefix+"*.wav").OrderBy(x=>x).Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p.Replace('\\','/'))).Where(c=>c).ToArray();
  bank.indoorSteps=Steps("Footstep_Sneakers_");bank.streetSteps=Steps("Footstep_Shoe_On_Street_");bank.boots=Steps("Footstep_Boots_");
  EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Supplied-audio-install.txt","PASS shared audio bank saved; effects=8; footsteps="+(bank.indoorSteps.Length+bank.streetSteps.Length+bank.boots.Length));
 }
 public static void Inspect(){
  Directory.CreateDirectory("Temp/SuppliedAudio");var report="";
  foreach(var path in Directory.GetFiles("Assets/Sounds","*.mp3")){
   var c=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!c)continue;c.LoadAudioData();var data=new float[c.samples*c.channels];if(!c.GetData(data,0))throw new Exception("Cannot decode "+path);
   using(var w=new BinaryWriter(File.Create("Temp/SuppliedAudio/"+Path.GetFileNameWithoutExtension(path)+".f32")))foreach(var sample in data)w.Write(sample);
   report+=Path.GetFileNameWithoutExtension(path)+"|"+c.frequency+"|"+c.channels+"|"+c.length+"\n";
  }
  File.WriteAllText("Temp/SuppliedAudio/clips.txt",report);
 }
}
}
#endif
