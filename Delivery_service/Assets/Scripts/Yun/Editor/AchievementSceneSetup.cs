#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.Achievements;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    // 편집기에서 한 번 배치하고 저장합니다. 플레이 중에는 UI를 생성하지 않습니다.
    public static class AchievementSceneSetup
    {
        private static Font font;
        private static readonly Color Ink = new Color32(25, 43, 66, 255);
        private static readonly Color Teal = new Color32(21, 143, 164, 255);

        [MenuItem("Tools/Delivery Service/Yun/Preview Achievement Notification")]
        public static void PreviewNotification()
        {
            if (!EditorApplication.isPlaying) return;
            var notification = UnityEngine.Object.FindFirstObjectByType<AchievementNotification>();
            if (notification != null) notification.Show(1);
        }

        [MenuItem("Tools/Delivery Service/Yun/Setup Achievements %#u")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your scene edits before running achievement setup.");
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { "Assets/Scenes/JJinCity.unity", "Assets/Scenes/YunCity.unity" })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    if (Build(scene))
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                    Validate(EditorSceneManager.OpenScene(path));
                }
                System.IO.File.WriteAllText("Temp/YunAchievementSetup.txt", "PASSED: JJinCity and YunCity saved and reloaded with achievement references and callbacks.");
                Debug.Log("YUN_ACHIEVEMENT_SETUP_PASSED");
            }
            catch (Exception error)
            {
                System.IO.File.WriteAllText("Temp/YunAchievementSetup.txt", error.ToString());
                throw;
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
        }

        private static T[] Find<T>(Scene scene) where T : Component
        { return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray(); }

        private static bool Build(Scene scene)
        {
            if (Find<AchievementManager>(scene).Length > 0) return false; // 편집한 UI를 다시 만들지 않습니다.
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
            var settings = Find<GameSettingsPanel>(scene).Single();
            var clock = Find<DayNightCycle>(scene).Single();
            var canvas = clock.timeText.GetComponentInParent<Canvas>();
            var host = Rect("Yun Achievements", canvas.transform);
            var manager = host.gameObject.AddComponent<AchievementManager>();
            var notification = host.gameObject.AddComponent<AchievementNotification>();
            var bridge = host.gameObject.AddComponent<PizzaAchievementBridge>();

            var popup = Box("Yun Achievement Popup", clock.timeText.transform, Ink);
            popup.anchorMin = popup.anchorMax = new Vector2(1, 0);
            popup.pivot = new Vector2(1, 1);
            popup.sizeDelta = new Vector2(380, 76);
            popup.anchoredPosition = new Vector2(0, -14);
            popup.GetComponent<Image>().raycastTarget = false;
            var popupCanvas = popup.gameObject.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true; popupCanvas.sortingOrder = 450;
            var message = Label("Message", popup, "업적 1개 달성됨!", 27, 0, 0, 360, 64);
            message.color = Color.white;
            popup.gameObject.SetActive(false);
            Bind(notification, "popup", popup.gameObject); Bind(notification, "message", message);
            Bind(manager, "notification", notification);
            Bind(bridge, "achievements", manager); Bind(bridge, "dayNightCycle", clock);
            BindArray(bridge, "pickupZones", Find<PizzaPickUpZone>(scene));
            BindArray(bridge, "inventories", Find<ToppingInventory>(scene));
            BindArray(bridge, "bikeStorages", Find<BikePizzaStorage>(scene));

            var window = (RectTransform)settings.transform.Find("Window");
            window.sizeDelta = new Vector2(600, 550);
            foreach (string name in new[] { "Title", "BGM Label", "BGM Value", "BGM Slider", "SFX Label", "SFX Value", "SFX Slider" })
            {
                var rect = (RectTransform)window.Find(name);
                rect.anchoredPosition += new Vector2(0, 45);
            }
            var open = Button("Achievements", window, "업적", 4, 460);
            UnityEventTools.AddPersistentListener(open.onClick, settings.OpenAchievements);

            var page = Box("Achievements Page", settings.transform, new Color32(242, 246, 249, 255));
            Position(page, 0, 0, 700, 460);
            var view = page.gameObject.AddComponent<AchievementPanelView>();
            Label("Title", page, "업적", 34, 0, 176, 620, 48);
            var progress = Label("Progress", page, "달성한 업적  0 / 1", 23, 0, 116, 620, 38);
            var card = Box("First Pizza", page, Color.white); Position(card, 0, 0, 620, 158);
            Label("Name", card, "첫 피자 완성", 28, -75, 46, 420, 42);
            Label("Description", card, "영업 시작 후 구운 피자를 1개 포장하세요.", 22, 0, 0, 580, 44);
            Label("Reward", card, "보상: 미정", 20, -160, -47, 240, 34);
            var state = Label("State", card, "미달성", 23, 197, 46, 150, 42); state.color = Teal;
            var back = Button("Back To Settings", page, "설정으로 돌아가기", -160, 460);
            UnityEventTools.AddPersistentListener(back.onClick, settings.BackToSettings);
            Bind(view, "achievements", manager); Bind(view, "progress", progress); Bind(view, "state", state);
            Bind(settings, "settingsWindow", window.gameObject); Bind(settings, "achievementsPage", page.gameObject);
            page.gameObject.SetActive(false);
            return true;
        }

        public static void Validate(Scene scene)
        {
            if (Find<AchievementManager>(scene).Length != 1) throw new Exception("Expected one achievement manager");
            var bridge = new SerializedObject(Find<PizzaAchievementBridge>(scene).Single());
            foreach (string name in new[] { "pickupZones", "inventories", "bikeStorages" })
                if (bridge.FindProperty(name).arraySize == 0) throw new Exception("Missing bridge sources: " + name);
            var settings = Find<GameSettingsPanel>(scene).Single();
            var data = new SerializedObject(settings);
            if (data.FindProperty("settingsWindow").objectReferenceValue == null || data.FindProperty("achievementsPage").objectReferenceValue == null)
                throw new Exception("Missing settings navigation references");
            var view = Find<AchievementPanelView>(scene).Single();
            if (view.gameObject.activeSelf) throw new Exception("Achievement page must start hidden");
            foreach (var button in new[] { settings.transform.Find("Window/Achievements").GetComponent<Button>(), view.GetComponentInChildren<Button>(true) })
                if (button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentTarget(0) != settings)
                    throw new Exception("Missing saved navigation callback");
            foreach (var component in new Component[] { Find<AchievementManager>(scene).Single(), Find<AchievementNotification>(scene).Single(), view })
            {
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        throw new Exception("Missing reference: " + component.name + "/" + property.name);
            }
        }

        private static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void BindArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
        {
            var data = new SerializedObject(target); var array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static RectTransform Box(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent); rect.gameObject.AddComponent<Image>().color = color; return rect;
        }
        private static void Position(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
        }
        private static Text Label(string name, Transform parent, string text, int size, float x, float y, float w, float h)
        {
            var rect = Rect(name, parent); Position(rect, x, y, w, h);
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.fontSize = size;
            label.text = text; label.color = Ink; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        private static Button Button(string name, Transform parent, string caption, float y, float width)
        {
            var rect = Box(name, parent, Teal); Position(rect, 0, y, width, 48);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            Label("Label", rect, caption, 24, 0, 0, width - 20, 44).color = Color.white; return button;
        }
    }
}
#endif
