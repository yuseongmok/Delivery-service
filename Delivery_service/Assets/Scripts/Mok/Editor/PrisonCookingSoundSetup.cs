#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonCookingSoundSetup
    {
        static PrisonCookingSoundSetup(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonCookingSoundSetup"))return;File.Delete("Temp/PrisonCookingSoundSetup");try{Build();}catch(Exception e){File.WriteAllText("Logs/PrisonCookingSoundSetup.txt",e.ToString());}}
        public static void Build()
        {
            const string folder="Assets/DeliveryPrisonChapter/KitchenArt/CookingAudio";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string report="";
            AudioClip Prepare(string source,string name,float maxSeconds=0)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(source);
                if(!clip)throw new Exception("Missing "+source);
                clip.LoadAudioData();var data=new float[clip.samples*clip.channels];
                if(!clip.GetData(data,0))throw new Exception("Cannot read "+source);
                float peak=0;foreach(float value in data)peak=Mathf.Max(peak,Mathf.Abs(value));
                int first=0,last=data.Length-1;float threshold=Mathf.Max(.001f,peak*.025f);
                while(first<last&&Mathf.Abs(data[first])<threshold)first++;
                while(last>first&&Mathf.Abs(data[last])<threshold)last--;
                int rate=clip.frequency,channels=clip.channels;
                first=Mathf.Max(0,first/channels-rate/100)*channels;
                int end=Mathf.Min(data.Length,((last/channels)+rate/5)*channels);
                if(maxSeconds>0)end=Mathf.Min(end,first+(int)(rate*maxSeconds)*channels);
                int count=(end-first)/channels*channels;float gain=peak>.001f?Mathf.Min(3,.85f/peak):1;
                string path=folder+"/"+name+".wav";
                using(var writer=new BinaryWriter(File.Create(path))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);for(int i=0;i<count;i++){float fade=Mathf.Min(1,Mathf.Min(i,count-1-i)/(float)Mathf.Max(1,rate*channels/200));writer.Write((short)(Mathf.Clamp(data[first+i]*gain*fade,-1,1)*32767));}}
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                report+=name+": source="+clip.length.ToString("F2")+"s prepared="+(count/(float)(rate*channels)).ToString("F2")+"s\n";
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            var asset=AssetDatabase.LoadAssetAtPath<PrisonCookingSounds>("Assets/Resources/PrisonCookingSounds.asset");
            if(!asset){asset=ScriptableObject.CreateInstance<PrisonCookingSounds>();AssetDatabase.CreateAsset(asset,"Assets/Resources/PrisonCookingSounds.asset");}
            string root="Assets/Sounds/";
            asset.chop=Prepare(root+"도마 위 칼질.mp3","Chop",.3f);
            asset.slice=Prepare(root+"절단 사운드.mp3","Slice",.32f);
            asset.music=AssetDatabase.LoadAssetAtPath<AudioClip>(root+"주방 배경음.mp3");
            if(!asset.music)throw new Exception("Cooking music missing");
            asset.flour=Prepare(root+"가루 쏟는 소리.mp3","Flour",6);
            asset.liquid=Prepare(root+"액체 따르는 소리.mp3","Pour",6);
            asset.stir=Prepare(root+"반죽 섞는 소리.mp3","Stir",6);
            asset.fry=Prepare(root+"기름에 튀기는 소리.mp3","Fry",8);
            asset.brush=Prepare(root+"브러쉬 소리.mp3","Brush",5);
            asset.snore=Prepare(root+"코고는 소리.mp3","Snore");
            asset.pickup=Prepare(root+"돈가방.mp3","Pickup",.16f);
            asset.tick=Prepare(root+"금고 문 돌리기.mp3","Dial",.07f);
            asset.flip=Prepare(root+"총알의 차체 명중.mp3","Tongs",.12f);
            asset.confirm=Prepare("Assets/Scripts/Mok/Art/Feedback/Income.wav","Ready",.65f);
            asset.detected=AssetDatabase.LoadAssetAtPath<AudioClip>(root+"발각 소리.mp3");
            EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();File.WriteAllText("Logs/PrisonCookingSoundSetup.txt",report+"PASS: cooking sound library saved.\n");
        }
    }
}
#endif
