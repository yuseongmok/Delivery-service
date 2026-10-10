#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.Audio;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    public static class GameSettingsVerifier
    {
        private const string Key = "Yun.SettingsVerification";
        private static int phase;
        private static double deadline;
        private static int frame = -1;

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().enabled = false;
            SessionState.SetString(Key + ".company", PlayerSettings.companyName);
            SessionState.SetString(Key + ".product", PlayerSettings.productName);
            PlayerSettings.companyName = "YunVerification";
            PlayerSettings.productName = "SettingsIsolatedTest";
            SessionState.SetInt(Key + ".phase", 0);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            phase = SessionState.GetInt(Key + ".phase", 0);
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, "Timeout");
                if (phase == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Debug.Log("SETTINGS_PLAY_PASSED: saved panel activation, sliders/category volumes, saved preferences, close/reopen, missing-lobby guard, editor quit.");
                    Finish(0); return;
                }
                if (!EditorApplication.isPlaying || Time.frameCount < 5 || frame == Time.frameCount) return;
                frame = Time.frameCount;
                var panel = UnityEngine.Object.FindObjectsByType<GameSettingsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
                var sliders = panel.GetComponentsInChildren<Slider>(true);
                var bgm = sliders.Single(s => s.name == "BGM Slider");
                var sfx = sliders.Single(s => s.name == "SFX Slider");
                if (phase == 0)
                {
                    Check(!panel.gameObject.activeSelf, "Saved panel starts hidden");
                    VerifyEscape(panel);
                    if (AudioManager.Instance == null) new GameObject("Verification Audio Manager").AddComponent<AudioManager>();
                    panel.gameObject.SetActive(true);
                    bgm.value = .25f; sfx.value = .4f;
                    phase = 1;
                    return;
                }
                if (phase != 1) return;
                var manager = AudioManager.Instance;
                Check(Mathf.Approximately(manager.GetCategoryVolume(SoundCategory.Bgm), .25f), "BGM category");
                foreach (var category in new[] { SoundCategory.Sfx, SoundCategory.Ui, SoundCategory.Ambience })
                    Check(Mathf.Approximately(manager.GetCategoryVolume(category), .4f), "Effect category " + category);
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Settings cursor");
                panel.GetComponentsInChildren<Button>(true).Single(b => b.name == "Close").onClick.Invoke();
                Check(!panel.gameObject.activeSelf, "Close hides saved panel");
                panel.gameObject.SetActive(true);
                Check(Mathf.Approximately(bgm.value, .25f) && Mathf.Approximately(sfx.value, .4f), "Reopen values");
                Check(Mathf.Approximately(PlayerPrefs.GetFloat("Yun.Settings.BgmVolume"), .25f)
                    && Mathf.Approximately(PlayerPrefs.GetFloat("Yun.Settings.SfxVolume"), .4f), "Saved preferences");
                panel.GetComponentsInChildren<Button>(true).Single(b => b.name == "Return To Lobby").onClick.Invoke();
                Check(panel.gameObject.scene.name == "JJinCity" && panel.GetComponentsInChildren<Text>(true)
                    .Any(t => t.text.Contains("아직 연결되지")), "Unassigned lobby remains in scene with explanation");
                phase = 2;
                SessionState.SetInt(Key + ".phase", phase);
                panel.GetComponentsInChildren<Button>(true).Single(b => b.name == "Quit Game").onClick.Invoke();
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }

        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }

        private static void VerifyEscape(GameSettingsPanel panel)
        {
            var gate = new SettingsEscapeGate();
            Check(!SettingsEscapeGate.HasOtherWindow(panel, null), "Normal HUD must allow settings");
            gate.Capture(panel, null); gate.Apply(panel, null);
            Check(panel.gameObject.activeInHierarchy, "ESC opens existing settings panel");
            gate.Capture(panel, null); gate.Apply(panel, null);
            Check(!panel.gameObject.activeSelf, "ESC closes settings");
            var pc = UnityEngine.Object.FindFirstObjectByType<ShopPC>();
            pc.pcUIPanel.SetActive(true);
            gate.Capture(panel, null);
            pc.pcUIPanel.SetActive(false); // Simulate ShopPC consuming the same ESC during Update.
            gate.Apply(panel, null);
            Check(!panel.gameObject.activeSelf, "Closing PC with ESC must not open settings on the same frame");
            var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            clock.summaryPanel.SetActive(true);
            gate.Capture(panel, null); gate.Apply(panel, null);
            Check(!panel.gameObject.activeSelf, "Summary blocks settings");
            clock.summaryPanel.SetActive(false);
            var fixture = new GameObject("ESC test popup", typeof(RectTransform), typeof(Canvas));
            try
            {
                var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Button));
                buttonObject.transform.SetParent(fixture.transform, false);
                buttonObject.GetComponent<Button>().interactable = false;
                gate.Capture(panel, null); gate.Apply(panel, null);
                Check(!panel.gameObject.activeSelf, "Other visible UI blocks even when its button is disabled");
                fixture.SetActive(false);
                gate.Capture(panel, null);
                fixture.SetActive(true);
                gate.Apply(panel, null);
                Check(!panel.gameObject.activeSelf, "UI appearing later in the frame blocks settings too");
                var group = fixture.AddComponent<CanvasGroup>(); group.alpha = 0;
                gate.Capture(panel, null); gate.Apply(panel, null);
                Check(panel.gameObject.activeSelf, "Invisible UI does not block settings");
                panel.Close();
                buttonObject.SetActive(false); group.alpha = 1;
                gate.Capture(panel, new[] { fixture }); gate.Apply(panel, new[] { fixture });
                Check(!panel.gameObject.activeSelf, "Explicit text-only popup blocks settings");
            }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
            Debug.Log("SETTINGS_ESCAPE_PASSED: open/close, normal HUD, PC same-frame close, summary, other windows, new popup, hidden UI and explicit blockers.");
        }
        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            PlayerSettings.companyName = SessionState.GetString(Key + ".company", PlayerSettings.companyName);
            PlayerSettings.productName = SessionState.GetString(Key + ".product", PlayerSettings.productName);
            EditorApplication.Exit(code);
        }
    }
}
#endif
