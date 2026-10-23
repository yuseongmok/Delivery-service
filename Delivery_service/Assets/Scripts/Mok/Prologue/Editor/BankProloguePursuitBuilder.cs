#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void PolishPursuit(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)throw new Exception("Open bank first.");
        EditorSceneManager.SaveScene(scene,"Logs/Bank-before-pursuit-polish.unity",true);
        var cars=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProloguePursuitCar>(true)).OrderBy(c=>c.name).ToArray();
        for(int i=0;i<cars.Length;i++){
            var c=cars[i];c.transform.position=new Vector3(-i*10,0,i==0?-69:-73);c.delay=.3f+i*.5f;
            if(!c.headlights){var l=new GameObject("Pursuit headlights").AddComponent<Light>();l.transform.SetParent(c.transform,false);l.transform.localPosition=new Vector3(0,.7f,1.95f);l.transform.localRotation=Quaternion.Euler(5,0,0);l.type=LightType.Spot;l.spotAngle=65;l.range=24;l.intensity=3;l.color=new Color(.85f,.92f,1);l.shadows=LightShadows.None;c.headlights=l;}
        }
        EditorSceneManager.SaveScene(scene);
    }
    public static void BuildPursuit(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);Directory.CreateDirectory("Logs");
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-pursuit-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed.");
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        if(scene.GetRootGameObjects().Any(g=>g.GetComponentInChildren<ProloguePursuitCar>(true)))throw new Exception("Pursuit already installed.");
        string folder=Art+"/Pursuit";Directory.CreateDirectory(folder);
        // 반복 경계가 이어지는 두 음색의 사이렌을 에셋으로 저장한다.
        string path=folder+"/PoliceSiren.wav";int rate=22050,count=rate*4;double phase=0;
        using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            for(int i=0;i<count;i++){double t=(double)i/rate;phase+=2*Math.PI*(720+220*Math.Sin(2*Math.PI*t*.5))/rate;double edge=Math.Min(1,Math.Min(t,4-t)*40);w.Write((short)((Math.Sin(phase)+.18*Math.Sin(phase*3))*edge*13000));}}
        AssetDatabase.Refresh();var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        for(int i=0;i<2;i++){
            var car=Vehicle("SM_Veh_Car_Police_01",new Vector3(-9-i*13,0,i==0?-69:-62),90);car.name="Pursuit police car "+(i+1);
            var rb=car.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var col=car.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.75f,0);col.size=new Vector3(1.8f,1.5f,4);
            var chase=car.gameObject.AddComponent<ProloguePursuitCar>();chase.drive=d.drive;chase.followingDistance=12+i*8;chase.laneOffset=i==0?-3:3;chase.delay=.7f+i*.9f;
            chase.wheels=car.GetComponentsInChildren<Transform>().Where(t=>t.name.ToLowerInvariant().Contains("wheel")&&t.GetComponent<MeshFilter>()).ToArray();
            var audio=car.gameObject.AddComponent<AudioSource>();audio.clip=clip;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=9;audio.maxDistance=70;audio.rolloffMode=AudioRolloffMode.Linear;audio.dopplerLevel=.25f;audio.volume=.55f;chase.siren=audio;
            chase.flashes=new Light[2];chase.lenses=new Renderer[2];
            for(int k=0;k<2;k++){
                var l=LightAt("Pursuit beacon",Vector3.zero,k==0?new Color(1,.025f,.01f):new Color(.025f,.15f,1),4,7);l.transform.SetParent(car,false);l.transform.localPosition=new Vector3(k==0?-.4f:.4f,1.65f,0);chase.flashes[k]=l;
                var lens=Box("Flashing lens",new Vector3(k==0?-.4f:.4f,1.65f,0),new Vector3(.3f,.13f,.3f),AssetDatabase.LoadAssetAtPath<Material>(Art+"/BankDressing/"+(k==0?"Emergency red":"Emergency blue")+".mat"),car,false);chase.lenses[k]=lens.GetComponent<Renderer>();
            }
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Bank-pursuit-build.txt","PASS: two saved police cars, spatial sirens, flashing lights and wheels assigned.");
    }
}
}
#endif
