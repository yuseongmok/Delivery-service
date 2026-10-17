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
    public static class MotorcycleDrivingAudioSetup
    {
        private const string TestKey = "Yun.BikeAudioRestoreTest";
        private static double deadline;
        public static void RunBatch()
        {
            try
            {
                var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/Scripts/Yun/SoundLibrary.asset");
                Configure(library, SoundId.MotorcycleEngineLoop, "mixkit-motorcycle-engine-doing-gearshift-2725.wav", true);
                Configure(library, SoundId.MotorcycleBrake, "motorcycle-brake.mp3", false);
                EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
                foreach (string name in new[] { "JJinCity", "YunCity" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    var bikes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MotorcycleController>(true)).ToArray();
                    if (bikes.Length == 0) throw new Exception("Missing motorcycle");
                    foreach (var bike in bikes)
                        if (bike.GetComponent<MotorcycleDrivingAudio>() == null) bike.gameObject.AddComponent<MotorcycleDrivingAudio>();
                    var managers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AudioManager>(true)).ToArray();
                    if (managers.Length == 0) managers = new[] { new GameObject("Yun Audio Manager").AddComponent<AudioManager>() };
                    foreach (var manager in managers) { manager.SetLibrary(library); EditorUtility.SetDirty(manager); }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    Debug.Log("BIKE_AUDIO_RESTORED: " + name);
                }
                EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
                UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled = false;
                SessionState.SetBool(TestKey, true); EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void Configure(SoundLibrary library, SoundId id, string file, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Yun/Audio/Clips/" + file);
            if (clip == null || !library.TryGetEntry(id, out var entry)) throw new Exception("Missing slot or clip: " + id);
            entry.clips = new[] { clip }; entry.category = SoundCategory.Sfx; entry.loop = loop;
            entry.volume = .65f; entry.pitchRange = Vector2.one;
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(TestKey, false)) return;
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update += Test;
        }
        private static void Test()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < 5)
            { if (EditorApplication.timeSinceStartup > deadline) Finish(1); return; }
            try
            {
                var bike = UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
                var bridge = bike.GetComponent<MotorcycleDrivingAudio>();
                var manager = AudioManager.Instance;
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var tick = typeof(MotorcycleDrivingAudio).GetMethod("Tick", flags);
                var speed = typeof(MotorcycleController).GetField("currentSpeed", flags);
                Action<float,int> step = (v,n) => { speed.SetValue(bike,v); for(int i=0;i<n;i++) tick.Invoke(bridge,new object[]{.02f}); };
                manager.MasterVolume=1; manager.MasterMuted=false;
                manager.SetCategoryVolume(SoundCategory.Sfx,1); manager.SetCategoryMuted(SoundCategory.Sfx,false);
                bike.isDriven=true; step(10,1);
                var source=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s=>s.clip!=null && s.clip.name=="mixkit-motorcycle-engine-doing-gearshift-2725");
                float initial=source.volume; step(10,30);
                if(!source.isPlaying || !source.loop || source.volume<=initial) throw new Exception("Fade in");
                float low=source.volume, pitch=source.pitch; step(30,40);
                if(source.volume<=low || source.pitch<=pitch) throw new Exception("Speed response");
                int count=manager.ActiveCount;step(30,10);if(manager.ActiveCount!=count)throw new Exception("Duplicate sound");
                step(3,10);
                var brake=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s=>s.clip!=null && s.clip.name=="motorcycle-brake");
                if(!brake.isPlaying || brake.loop)throw new Exception("Brake");
                step(0,60);if(source.isPlaying || brake.isPlaying)throw new Exception("Fade out");
                step(10,30);manager.SetCategoryVolume(SoundCategory.Sfx,0);if(source.volume!=0)throw new Exception("Settings volume");
                Time.timeScale=0;step(10,1);if(manager.ActiveCount!=count-1)throw new Exception("Pause");
                Time.timeScale=1;step(10,30);bike.isDriven=false;step(0,60);if(manager.ActiveCount!=count-1)throw new Exception("Dismount");
                Debug.Log("BIKE_AUDIO_TEST_PASSED: speed pitch/volume, fades, brake, no duplication, pause, dismount, SFX setting");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        private static void Finish(int code){Time.timeScale=1;SessionState.SetBool(TestKey,false);EditorApplication.update-=Test;EditorApplication.Exit(code);}
    }
}
#endif
