#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.Streetlights;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryService.Yun.Editor
{
    public static class YunMotorcycleLightVerifier
    {
        private const string Key = "Yun.MotorcycleLight.Verify";
        private static int phase, lastFrame = -1;
        private static double deadline;

        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
            // Do not let InitializeDay deliver the user's real saved ingredient orders in this test.
            UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled = false;
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, "Timed out");
                if (!EditorApplication.isPlaying || Time.frameCount < 5 || lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
                var controllers = UnityEngine.Object.FindObjectsByType<StreetlightTimeController>(FindObjectsSortMode.None);
                var headlight = controllers.Single(c => c.LampCount == 1);
                var street = controllers.Single(c => c.LampCount == 96);
                var beam = headlight.GetComponentInChildren<Light>(true);
                var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                    .Where(l => l.name == "Yun Streetlight" || l.name == "Yun Motorcycle Beam").ToArray();
                Check(!clock.enabled && lights.Length == 97, "Isolated clock and all lamp references");
                if (phase > 0)
                {
                    bool expected = phase == 2 || phase == 4;
                    Check(lights.All(l => l.enabled == expected), "Shared schedule phase " + phase);
                    Check(headlight.IsLit == expected && street.IsLit == expected, "Controllers agree phase " + phase);
                }
                switch (phase++)
                {
                    case 0: clock.currentTime = 15.999f; clock.isShopOpen = true; break;
                    case 1: clock.currentTime = 16; break;
                    case 2: clock.OnClick_CloseShop(); break;
                    case 3: clock.OnClick_CancelClose(); break;
                    case 4: clock.currentTime = 6; break;
                    case 5: clock.currentTime = 20; clock.isShopOpen = false; break;
                    default:
                        clock.isShopOpen = true;
                        foreach (var c in controllers) c.Refresh();
                        headlight.enabled = false;
                        Check(!beam.enabled && street.IsLit, "Disabling bike headlight does not disable streets");
                        headlight.enabled = true;
                        Check(beam.enabled, "Re-enable restores headlight");
                        var body = headlight.transform.parent;
                        var localPosition = body.InverseTransformPoint(beam.transform.position);
                        var localDirection = body.InverseTransformDirection(beam.transform.forward);
                        Vector3 position = body.position;
                        Quaternion rotation = body.rotation;
                        body.position += new Vector3(3, 0, 2);
                        body.rotation = Quaternion.Euler(0, 75, 12) * rotation;
                        Check(Vector3.Distance(beam.transform.position, body.TransformPoint(localPosition)) < .001f,
                            "Beam position follows moving/leaning motorcycle");
                        Check(Vector3.Dot(beam.transform.forward, body.TransformDirection(localDirection)) > .999f,
                            "Beam direction follows turning/leaning motorcycle");
                        body.SetPositionAndRotation(position, rotation);
                        Debug.Log("MOTORCYCLE_LIGHT_PLAY_PASSED: 97 lights, 16h boundary, actual close/cancel APIs, morning, closed evening, lifecycle, translation/rotation/lean.");
                        Finish(0);
                        break;
                }
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }

        public static void RenderBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
                var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
                var controllers = UnityEngine.Object.FindObjectsByType<StreetlightTimeController>(FindObjectsSortMode.None);
                var headlight = controllers.Single(c => c.LampCount == 1);
                var body = headlight.transform.parent;
                clock.currentTime = 20;
                clock.isShopOpen = true;
                foreach (var c in controllers) c.Refresh();
                clock.sun.intensity = .08f;
                clock.moon.intensity = .1f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.09f, .11f, .15f);
                var go = new GameObject("Headlight preview", typeof(Camera));
                var camera = go.GetComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .045f, .07f);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                camera.fieldOfView = 55;
                camera.transform.position = body.TransformPoint(new Vector3(4, 2, 6));
                camera.transform.LookAt(body.TransformPoint(new Vector3(0, .2f, 1)));
                headlight.enabled = false; headlight.Refresh();
                YunCityStreetlightVerifier.Capture(camera, "yuncity-motorcycle-off.png");
                headlight.enabled = true; headlight.Refresh();
                YunCityStreetlightVerifier.Capture(camera, "yuncity-motorcycle-on.png");
                UnityEngine.Object.DestroyImmediate(go);
                Debug.Log("MOTORCYCLE_LIGHT_RENDERED");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
#endif
