#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using DeliveryService.Yun.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Yun.Editor
{
    public static class MotorcycleCollisionAudioSetup
    {
        private const string Key="Yun.ImpactAudioTest";
        private static double deadline;
        public static void RunBatch()
        {
            try
            {
                var lib=AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/Scripts/Yun/SoundLibrary.asset");
                Configure(lib,SoundId.MotorcycleNpcHit,"motorcycle-npc-impact.mp3");
                Configure(lib,SoundId.MotorcycleObstacleHit,"motorcycle-object-impact.mp3");
                EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
                foreach(var name in new[]{"JJinCity","YunCity"})
                {
                    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                    var bikes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MotorcycleController>(true)).ToArray();
                    if(bikes.Length==0)throw new Exception("Missing motorcycle");
                    foreach(var bike in bikes)
                    {
                        var owner=bike.GetComponent<MotorcycleCollisionAudio>()??bike.gameObject.AddComponent<MotorcycleCollisionAudio>();
                        owner.Bind(bike);EditorUtility.SetDirty(owner);
                        var nodes=bike.GetComponentsInChildren<Collider>(true).Select(c=>c.gameObject)
                            .Concat(bike.GetComponentsInChildren<Rigidbody>(true).Select(r=>r.gameObject))
                            .Concat(new[]{bike.gameObject}).Distinct().ToList();
                        if(bike.sphereRB!=null)nodes.Add(bike.sphereRB.gameObject);
                        if(bike.bikeBody!=null)nodes.Add(bike.bikeBody.gameObject);
                        foreach(var node in nodes.Distinct())
                        {
                            var receiver=node.GetComponent<MotorcycleCollisionAudioReceiver>()??node.AddComponent<MotorcycleCollisionAudioReceiver>();
                            receiver.Bind(owner);EditorUtility.SetDirty(receiver);
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    Debug.Log("IMPACT_AUDIO_CONFIGURED: "+name);
                }
                EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
                UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled=false;
                SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        private static void Configure(SoundLibrary lib,SoundId id,string file)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Yun/Audio/Clips/"+file);
            if(clip==null||!lib.TryGetEntry(id,out var entry))throw new Exception("Missing clip/slot");
            entry.clips=new[]{clip};entry.loop=false;entry.category=SoundCategory.Sfx;
            entry.volume=.7f;entry.pitchRange=Vector2.one;
        }
        [InitializeOnLoadMethod]private static void Resume()
        {
            if(!SessionState.GetBool(Key,false))return;
            deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Test;
        }
        private static void Test()
        {
            if(!EditorApplication.isPlaying||Time.frameCount<5){if(EditorApplication.timeSinceStartup>deadline)Finish(1);return;}
            try
            {
                var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
                var owner=bike.GetComponent<MotorcycleCollisionAudio>();var manager=AudioManager.Instance;
                var fixture=new GameObject("Impact verification collider",typeof(BoxCollider));var col=fixture.GetComponent<Collider>();
                bike.isDriven=true;manager.StopAll();
                owner.Contact(col,bike.transform.position,1,false);
                if(manager.ActiveCount!=0)throw new Exception("Tiny contact should be silent");
                owner.Contact(col,bike.transform.position,10,true);
                if(manager.ActiveCount!=0)throw new Exception("Non-NPC trigger should be silent");
                owner.Contact(col,bike.transform.position,10,false);
                var src=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s=>s.isPlaying);
                if(src.clip.name!="motorcycle-object-impact"||src.loop)throw new Exception("Object clip routing");
                owner.Contact(col,bike.transform.position,10,false);
                if(manager.ActiveCount!=1)throw new Exception("Duplicate collider impact");
                manager.StopAll();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(MotorcycleCollisionAudio).GetField("nextImpact",flags).SetValue(owner,0f);
                var npc=UnityEngine.Object.FindFirstObjectByType<NPCControl>();
                if(npc==null)throw new Exception("Missing spawned NPC test target");
                owner.Contact(npc.GetComponentInChildren<Collider>(),npc.transform.position,10,false);
                src=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s=>s.isPlaying);
                if(src.clip.name!="motorcycle-npc-impact")throw new Exception("NPC clip routing");
                manager.SetCategoryVolume(SoundCategory.Sfx,0);if(src.volume!=0)throw new Exception("SFX volume");
                manager.StopAll();Time.timeScale=0;
                owner.Contact(col,bike.transform.position,10,false);if(manager.ActiveCount!=0)throw new Exception("Paused impact");
                UnityEngine.Object.Destroy(fixture);
                Debug.Log("IMPACT_AUDIO_TEST_PASSED: NPC/object routing, low speed filter, trigger filter, shared cooldown, SFX volume, pause");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){Time.timeScale=1;SessionState.SetBool(Key,false);EditorApplication.update-=Test;EditorApplication.Exit(code);}
    }
}
#endif
