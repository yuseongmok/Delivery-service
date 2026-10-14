#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    public static class SettingsCanvasMigration
    {
        public static void RunBatch()
        {
            try
            {
                foreach (string name in new[] { "JJinCity", "YunCity" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    var settings = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameAudioSettings>(true)).Single();
                    var root = scene.GetRootGameObjects().SingleOrDefault(r => r.name == "SettingsCanvas");
                    if (root == null) root = new GameObject("SettingsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var canvas = root.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 1000;
                    var scaler = root.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = .5f;
                    settings.transform.SetParent(root.transform, false);
                    var rect = settings.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                    var panel = settings.GetComponentInChildren<GameSettingsPanel>(true);
                    panel.gameObject.SetActive(false);
                    if (settings.transform.root != root.transform || root.transform.parent != null)
                        throw new Exception("Settings must have an independent root Canvas");
                    foreach (var button in panel.GetComponentsInChildren<Button>(true))
                        if (button.onClick.GetPersistentEventCount() != 1) throw new Exception("Button callback lost: " + button.name);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("SETTINGS_CANVAS_PASSED: " + name + " SettingsCanvas/Yun Settings/Settings Panel");
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
