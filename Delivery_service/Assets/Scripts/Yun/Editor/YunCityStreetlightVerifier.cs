#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DeliveryService.Yun.Streetlights;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeliveryService.Yun.Editor
{
    public static class YunCityStreetlightVerifier
    {
        private const string Output = "C:/Users/q2244/.codex/visualizations/2026/09/09/01a08608-c058-7f91-9893-3ff4324de64c";
        public static void RunBatch()
        {
            try { Verify(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Verify()
        {
            byte[] original = File.ReadAllBytes(YunCityStreetlightSetup.SourceScene);
            EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
            var controller = UnityEngine.Object.FindObjectsByType<StreetlightTimeController>(FindObjectsSortMode.None).Single(c => c.LampCount == 96);
            var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => l.name == "Yun Streetlight").ToArray();
            Require(controller != null && clock != null && controller.LampCount == 96 && lights.Length == 96, "Saved references and all 96 lights");
            clock.isShopOpen = true;
            foreach (float hour in new[] { 6f, 15.999f, 16f, 20f, 23.99f, 6f, 16f })
            {
                clock.currentTime = hour;
                controller.SendMessage("LateUpdate");
                Require(lights.All(l => l.enabled == (hour >= 16)), "Light state at " + hour);
                Require(controller.IsLit == (hour >= 16), "Controller state at " + hour);
            }
            clock.isShopOpen = false;
            controller.Refresh();
            Require(lights.All(l => !l.enabled), "Closing immediately turns off street lighting");
            clock.isShopOpen = true;
            controller.Refresh();
            controller.enabled = false;
            controller.Refresh(); // Edit mode does not invoke normal MonoBehaviour lifecycle callbacks.
            Require(lights.All(l => !l.enabled), "Disable releases all lights");
            controller.enabled = true;
            controller.Refresh();
            Require(lights.All(l => l.enabled), "Re-enable restores clock state");
            Require(lights.All(l => l.shadows == LightShadows.None && l.lightmapBakeType == LightmapBakeType.Realtime), "Realtime shadow-free lights");

            // Keep exposure and daylight identical to compare illumination, not the sun cycle.
            clock.sun.intensity = .08f;
            clock.moon.intensity = .10f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.09f, .11f, .15f);
            var cameraObject = new GameObject("Streetlight verification camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .045f, .07f);
            camera.fieldOfView = 58;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 150;
            camera.allowHDR = true;
            var lamp = lights.Where(l => l.type == LightType.Spot)
                .OrderBy(l => Vector3.Distance(l.transform.position, clock.player.transform.position)).First();
            var pole = lamp.transform.parent;
            Debug.Log($"STREETLIGHT_RENDER_SAMPLE {pole.name} at {lamp.transform.position} direction {lamp.transform.forward} intensity {lamp.intensity}");
            Vector3 target = pole.TransformPoint(new Vector3(0, 3f, 1.96f));
            camera.transform.position = target + pole.forward * 10 + pole.right * 7 + Vector3.up * 2;
            camera.transform.LookAt(target);
            clock.currentTime = 15.99f; controller.Refresh();
            Capture(camera, "yuncity-streetlights-off.png");
            clock.currentTime = 16f; controller.Refresh();
            Capture(camera, "yuncity-streetlights-on.png");
            UnityEngine.Object.DestroyImmediate(cameraObject);
            // Discard verification-only times and camera; never save them.
            EditorSceneManager.OpenScene(YunCityStreetlightSetup.SourceScene);
            Require(original.SequenceEqual(File.ReadAllBytes(YunCityStreetlightSetup.SourceScene)), "JJinCity bytes unchanged");
            Debug.Log("YUNCITY_STREETLIGHT_VERIFICATION_PASSED: 96 saved lights, 15:59 off, 16:00 on, night, next-day reset, closed-shop state, disable/re-enable, original scene unchanged.");
        }

        internal static void Capture(Camera camera, string filename)
        {
            var texture = new RenderTexture(1280, 900, 24, RenderTextureFormat.ARGBHalf);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output, filename), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }

        private const string PlayKey = "Yun.Streetlight.PlayCheck";
        private static int phase;
        private static int lastFrame = -1;
        private static double deadline;
        private static readonly float[] Hours = { 6, 15.999f, 16, 23.99f, 6, 16 };

        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
            // Prevent InitializeDay from delivering the user's real pending purchases during verification.
            UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled = false;
            SessionState.SetBool(PlayKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(PlayKey, false)) return;
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Play verification timed out");
                if (!EditorApplication.isPlaying || Time.frameCount < 5 || Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
                var controller = UnityEngine.Object.FindObjectsByType<StreetlightTimeController>(FindObjectsSortMode.None).Single(c => c.LampCount == 96);
                var lamps = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                    .Where(l => l.name == "Yun Streetlight").ToArray();
                Require(!clock.enabled, "Verification must not initialize real stock");
                if (phase > 0)
                {
                    bool expected = phase <= Hours.Length ? Hours[phase - 1] >= 16 : true;
                    Require(lamps.Length == 96 && lamps.All(l => l.enabled == expected), "Runtime light state phase " + phase);
                }
                if (phase < Hours.Length) { clock.isShopOpen = true; clock.currentTime = Hours[phase++]; }
                else if (phase == Hours.Length)
                {
                    controller.enabled = false;
                    Require(lamps.All(l => !l.enabled), "Runtime OnDisable turns off lamps");
                    controller.enabled = true;
                    Require(lamps.All(l => l.enabled), "Runtime OnEnable restores lamps");
                    clock.currentTime = 15.999f;
                    clock.isShopOpen = true;
                    clock.timeMultiplier = 3600;
                    typeof(DayNightCycle).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(clock, null);
                    Require(clock.currentTime >= 16, "Existing business clock crosses 16:00");
                    phase++;
                }
                else
                {
                    Debug.Log("YUNCITY_STREETLIGHT_PLAY_PASSED: actual LateUpdate, 16:00 boundary, morning reset, disable/re-enable and existing business time advancement.");
                    Finish(0);
                }
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(PlayKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
#endif
