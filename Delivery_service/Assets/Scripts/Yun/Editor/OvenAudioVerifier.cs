#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DeliveryService.Yun.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryService.Yun.Editor
{
    // 빈 씬에서 검사하므로 실제 게임의 돈/재고/날짜 저장을 변경하지 않습니다.
    public static class OvenAudioVerifier
    {
        private const string Session = "Yun.OvenAudioVerification";
        private static IEnumerator routine;
        private static double deadline;
        private static int frame = -1;

        public static void RunBatch()
        {
            try
            {
                var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/Scripts/Yun/SoundLibrary.asset");
                Check(library != null && library.TryGetEntry(SoundId.OvenLoop, out _), "Oven slot exists");
                library.TryGetEntry(SoundId.OvenLoop, out var entry);
                Check(entry.PickClip() != null && entry.loop && entry.category == SoundCategory.Sfx, "Clip and loop configured");
                Check(entry.spatialMode == SoundSpatialMode.ThreeDimensional, "Spatial playback configured");
                Debug.Log("OVEN_CLIP: " + entry.PickClip().length + " seconds");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Listener", typeof(AudioListener));
                var manager = new GameObject("Audio Manager").AddComponent<AudioManager>(); manager.SetLibrary(library);
                for (int i = 0; i < 2; i++)
                {
                    var oven = new GameObject("Oven " + i).AddComponent<Oven>();
                    Set(oven, "ovenPlacePoint", oven.transform); Set(oven, "bakingTime", 3f);
                }
                new GameObject("Test weather").AddComponent<WeatherManager>();
                new GameObject("Test packing").AddComponent<PizzaPacking>();
                SessionState.SetBool(Session, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Session, false)) return;
            deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Oven audio test timeout");
                if (!EditorApplication.isPlaying || Time.frameCount < 3 || frame == Time.frameCount) return;
                frame = Time.frameCount;
                if (routine == null) routine = Verify();
                if (routine.MoveNext()) return;
                Debug.Log("GAMEPLAY_AUDIO_PASSED: oven lifecycle, rain lifecycle, box open/close, duplicate prevention, pause/resume, library settings.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Finish(int code)
        {
            Time.timeScale = 1; SessionState.SetBool(Session, false);
            EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }
        private static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        private static void Insert(Oven oven)
        {
            var inventory = new GameObject("Test inventory").AddComponent<ToppingInventory>();
            Set(inventory, "holdPoint", inventory.transform);
            var pizza = new GameObject("Test pizza").AddComponent<Pizza>();
            inventory.AddPizza(pizza.gameObject); oven.Interact(inventory);
        }
        private static IEnumerator Verify()
        {
            var manager = AudioManager.Instance;
            var ovens = UnityEngine.Object.FindObjectsByType<Oven>(FindObjectsSortMode.InstanceID);
            Check(ovens.Length == 2 && ovens.All(o => o.GetComponents<GameplayAudio>().Length == 1), "Scene hook attaches exactly one audio controller per oven");
            Check(manager.ActiveCount == 0, "Idle ovens are silent");
            Insert(ovens[0]);
            yield return null; yield return null;
            Check(manager.ActiveCount == 1, "Baking starts one loop");
            var voice = manager.GetComponentsInChildren<AudioSource>().Single(source => source.isPlaying);
            manager.Library.TryGetEntry(SoundId.OvenLoop, out var ovenEntry);
            Check(voice.spatialBlend == 1 && Mathf.Approximately(voice.volume, ovenEntry.volume), "Voice uses library volume and spatial playback");
            yield return null;
            Check(manager.ActiveCount == 1, "Repeated updates do not stack loops");
            manager.SetCategoryVolume(SoundCategory.Sfx, .5f);
            Check(Mathf.Approximately(voice.volume, ovenEntry.volume * .5f), "SFX setting controls oven volume");
            Insert(ovens[1]); yield return null; yield return null;
            Check(manager.ActiveCount == 2, "Two ovens have independent voices");
            Time.timeScale = 0; yield return null; yield return null;
            Check(manager.ActiveCount == 0, "Pause stops oven audio");
            Time.timeScale = 1; yield return null; yield return null;
            Check(manager.ActiveCount == 2, "Resume restarts active ovens");
            ovens[1].gameObject.SetActive(false); yield return null;
            Check(manager.ActiveCount == 1, "Disabling an oven stops only its voice");
            float until = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < until && manager.ActiveCount != 0) yield return null;
            Check(manager.ActiveCount == 0, "Natural baking completion stops loop");
            var collector = new GameObject("Cooked pizza collector").AddComponent<ToppingInventory>();
            Set(collector, "holdPoint", collector.transform);
            ovens[0].Interact(collector);
            Check(collector.HasPizza(), "Cooked pizza can be removed");
            Insert(ovens[0]); yield return null; yield return null;
            Check(manager.ActiveCount == 1, "Next batch starts audio again");
            UnityEngine.Object.Destroy(ovens[0].gameObject); yield return null; yield return null;
            Check(manager.ActiveCount == 0, "Destroying oven releases voice");
            var weather = UnityEngine.Object.FindFirstObjectByType<WeatherManager>();
            Check(weather.GetComponent<GameplayAudio>() != null, "Weather auto hookup");
            weather.isRaining = true; yield return null; yield return null;
            Check(manager.ActiveCount == 1, "Rain starts one loop");
            voice = manager.GetComponentsInChildren<AudioSource>().Single(s => s.isPlaying);
            Check(voice.loop && voice.spatialBlend == 0f, "Rain is a 2D loop");
            for (int i = 0; i < 10; i++) yield return null;
            Check(manager.ActiveCount == 1, "Rain does not stack");
            Time.timeScale = 0; yield return null; yield return null;
            Check(manager.ActiveCount == 0, "Rain pauses");
            Time.timeScale = 1; yield return null; yield return null;
            Check(manager.ActiveCount == 1, "Rain resumes");
            weather.isRaining = false; yield return null; yield return null;
            Check(manager.ActiveCount == 0, "Clear weather stops rain");
            weather.isRaining = true; yield return null; yield return null;
            weather.gameObject.SetActive(false); yield return null;
            Check(manager.ActiveCount == 0, "Disabling weather stops rain");

            var packing = UnityEngine.Object.FindFirstObjectByType<PizzaPacking>();
            Check(packing.GetComponent<GameplayAudio>() != null, "Packing auto hookup");
            var box = packing.GetComponent<DeliveryService.Mok.PizzaBoxStation>();
            var emptyInventory = new GameObject("Box test inventory").AddComponent<ToppingInventory>();
            packing.Interact(emptyInventory);
            float boxDeadline = Time.realtimeSinceStartup + 2f;
            while (manager.ActiveCount == 0 && Time.realtimeSinceStartup < boxDeadline) yield return null;
            Check(manager.ActiveCount == 1, "Opening box plays one sound");
            voice = manager.GetComponentsInChildren<AudioSource>().Single(s => s.isPlaying);
            manager.Library.TryGetEntry(SoundId.PizzaBoxOpen, out var openEntry);
            Check(openEntry.clips.Contains(voice.clip) && !voice.loop, "Opening uses library clip once");
            while (box.IsBusy && Time.realtimeSinceStartup < boxDeadline) yield return null;
            Check(box.IsOpen, "Box completes opening");
            manager.StopAll();
            packing.Interact(emptyInventory);
            boxDeadline = Time.realtimeSinceStartup + 2f;
            while (manager.ActiveCount == 0 && Time.realtimeSinceStartup < boxDeadline) yield return null;
            Check(manager.ActiveCount == 1, "Closing box plays one sound");
            voice = manager.GetComponentsInChildren<AudioSource>().Single(s => s.isPlaying);
            manager.Library.TryGetEntry(SoundId.PizzaBoxClose, out var closeEntry);
            Check(closeEntry.clips.Contains(voice.clip) && !voice.loop, "Closing uses library clip once");
            while (box.IsBusy && Time.realtimeSinceStartup < boxDeadline) yield return null;
            Check(!box.IsOpen, "Box completes closing");
            Check(manager.ActiveCount <= 1, "Box closes without stacked sounds: " + manager.ActiveCount);
        }
    }
}
#endif
