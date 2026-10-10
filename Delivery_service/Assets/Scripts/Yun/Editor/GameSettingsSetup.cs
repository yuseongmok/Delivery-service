#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    public static class GameSettingsSetup
    {
        private static Font font;
        private static readonly Color Ink = new Color32(25, 43, 66, 255);

        public static void RunBatch()
        {
            try
            {
                foreach (string path in new[] { "Assets/Scenes/JJinCity.unity", "Assets/Scenes/YunCity.unity" })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var roots = scene.GetRootGameObjects();
                    var clock = roots.SelectMany(r => r.GetComponentsInChildren<DayNightCycle>(true)).Single();
                    var canvas = clock.summaryPanel.GetComponentInParent<Canvas>(true);
                    if (canvas == null) throw new InvalidOperationException("Existing HUD Canvas not found");
                    var pc = roots.SelectMany(r => r.GetComponentsInChildren<ShopPC>(true)).Single();
                    if (!roots.SelectMany(r => r.GetComponentsInChildren<GameSettingsPanel>(true)).Any())
                        Build(canvas.transform, pc);
                    var audio = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameAudioSettings>(true)).Single();
                    var sources = roots.SelectMany(r => r.GetComponentsInChildren<WeatherManager>(true))
                        .Select(w => w.rainAudioSource).Where(s => s != null).Distinct().ToArray();
                    var audioData = new SerializedObject(audio);
                    var effects = audioData.FindProperty("sceneEffects");
                    effects.arraySize = sources.Length;
                    for (int i = 0; i < sources.Length; i++) effects.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
                    audioData.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    scene = EditorSceneManager.OpenScene(path);
                    var saved = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSettingsPanel>(true)).Single();
                    if (saved.gameObject.activeSelf) throw new Exception("Panel must be saved inactive");
                    var sliders = saved.GetComponentsInChildren<Slider>(true);
                    var buttons = saved.GetComponentsInChildren<Button>(true);
                    if (sliders.Length != 2 || buttons.Length != 3 ||
                        sliders.Any(s => s.onValueChanged.GetPersistentEventCount() != 1) ||
                        buttons.Any(b => b.onClick.GetPersistentEventCount() != 1))
                        throw new Exception("Saved UI callback validation failed");
                    var serialized = new SerializedObject(saved);
                    foreach (string field in new[] { "audioSettings", "shopPc", "bgmSlider", "sfxSlider", "bgmValue", "sfxValue", "status" })
                        if (serialized.FindProperty(field).objectReferenceValue == null) throw new Exception("Missing " + field);
                    Debug.Log("SETTINGS_SCENE_PASSED: " + path + " / inactive panel, two sliders, three buttons, saved callbacks and references.");
                }
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Build(Transform parent, ShopPC pc)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
            var container = Rect("Yun Settings", parent);
            var canvas = container.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 400;
            container.gameObject.AddComponent<GraphicRaycaster>();
            var audio = container.gameObject.AddComponent<GameAudioSettings>();
            var panel = Image("Settings Panel", container, new Color(0, 0, 0, .72f));
            panel.gameObject.SetActive(false);
            var controller = panel.gameObject.AddComponent<GameSettingsPanel>();
            var card = Image("Window", panel, new Color32(242, 246, 249, 255));
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
            card.sizeDelta = new Vector2(600, 460);
            Label("Title", card, "설정", 32, 0, 177, 520, 46);
            var bgm = VolumeRow(card, "BGM", "배경음", 93, out Text bgmValue);
            var sfx = VolumeRow(card, "SFX", "효과음", 22, out Text sfxValue);
            var lobby = Button("Return To Lobby", card, "로비로 돌아가기", -58);
            var quit = Button("Quit Game", card, "게임 종료", -120);
            var close = Button("Close", card, "닫기", -182);
            var status = Label("Status", panel, "", 22, 0, -264, 800, 44);
            status.color = Color.white;
            var data = new SerializedObject(controller);
            Bind(data, "audioSettings", audio); Bind(data, "shopPc", pc);
            Bind(data, "bgmSlider", bgm); Bind(data, "sfxSlider", sfx);
            Bind(data, "bgmValue", bgmValue); Bind(data, "sfxValue", sfxValue);
            Bind(data, "status", status);
            data.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(bgm.onValueChanged, controller.SetBgmVolume);
            UnityEventTools.AddPersistentListener(sfx.onValueChanged, controller.SetSfxVolume);
            UnityEventTools.AddPersistentListener(lobby.onClick, controller.ReturnToLobby);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
            UnityEventTools.AddPersistentListener(close.onClick, controller.Close);
        }

        private static void Bind(SerializedObject obj, string name, UnityEngine.Object value)
        { obj.FindProperty(name).objectReferenceValue = value; }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static RectTransform Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }
        private static void Position(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
        }
        private static Text Label(string name, Transform parent, string content, int size, float x, float y, float w, float h)
        {
            var rect = Rect(name, parent); Position(rect, x, y, w, h);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = Ink;
            text.alignment = TextAnchor.MiddleCenter; text.text = content; text.raycastTarget = false;
            return text;
        }
        private static Button Button(string name, Transform parent, string caption, float y)
        {
            var rect = Image(name, parent, new Color32(21, 143, 164, 255));
            Position(rect, 0, y, 460, 48);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Label("Label", rect, caption, 24, 0, 0, 440, 44).color = Color.white;
            return button;
        }
        private static Slider VolumeRow(Transform parent, string name, string caption, float y, out Text value)
        {
            Label(name + " Label", parent, caption, 24, -204, y, 110, 40);
            value = Label(name + " Value", parent, "100%", 22, 230, y, 90, 40);
            var rect = Rect(name + " Slider", parent); Position(rect, 23, y, 305, 32);
            var background = Image("Background", rect, new Color32(200, 212, 222, 255));
            background.anchorMin = new Vector2(0, .35f); background.anchorMax = new Vector2(1, .65f);
            var fill = Image("Fill", background, new Color32(21, 143, 164, 255));
            var handleArea = Rect("Handle Area", rect);
            handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Image("Handle", handleArea, Ink);
            handle.sizeDelta = new Vector2(20, 0);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            return slider;
        }
    }
}
#endif
