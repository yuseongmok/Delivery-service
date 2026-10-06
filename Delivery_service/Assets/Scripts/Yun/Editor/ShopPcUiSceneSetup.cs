#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.ShopUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    public static class ShopPcUiSceneSetup
    {
        public const string PrefabPath = "Assets/Scripts/Yun/ShopUI/Prefabs/ShopPcCanvas.prefab";

        public static GameObject InstantiateForVerification()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (asset == null) throw new InvalidOperationException("Bake the PC UI prefab first.");
            var instance = UnityEngine.Object.Instantiate(asset);
            instance.GetComponent<ShopPcUiView>().Initialize(null);
            return instance;
        }

        public static void RunBatch()
        {
            try
            {
                BakeOnce();
                foreach (string path in new[] { "Assets/Scenes/JJinCity.unity", "Assets/Scenes/YunCity.unity" })
                    Install(path);
                ShopPcUiVerifier.Run();
                Debug.Log("SHOP_PC_SCENE_CANVAS_PASSED: both scenes contain a saved inactive prefab, matching PC/day-cycle references and 20 buttons.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void BakeOnce()
        {
            // Re-running setup must never replace an artist's edited prefab.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            const string folder = "Assets/Scripts/Yun/ShopUI/Prefabs";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Scripts/Yun/ShopUI", "Prefabs");
            var root = new GameObject("Yun Shop PC UI", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                root.AddComponent<ShopPcUiView>().Build(null);
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
                if (font == null) throw new InvalidOperationException("Project Korean font is missing.");
                foreach (var label in root.GetComponentsInChildren<Text>(true)) label.font = font;
                var source = root.GetComponentsInChildren<Image>(true).First(i => i.sprite != null).sprite;
                var texture = UnityEngine.Object.Instantiate(source.texture);
                texture.name = "Shop PC Rounded Mask";
                AssetDatabase.CreateAsset(texture, folder + "/ShopPcRounded.asset");
                var sprite = Sprite.Create(texture, source.rect, new Vector2(.5f, .5f), 100, 0,
                    SpriteMeshType.FullRect, source.border);
                sprite.name = "Shop PC Rounded Sprite";
                AssetDatabase.AddObjectToAsset(sprite, texture);
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                    if (image.sprite == source) image.sprite = sprite;
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Install(string path)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var pc = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShopPC>(true)).Single();
            var oldPanel = pc.pcUIPanel;
            if (oldPanel != null && oldPanel.GetComponent<ShopPcUiView>() != null) return;
            var clocks = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DayNightCycle>(true))
                .Where(c => c.pcUIPanel == oldPanel).ToArray();
            if (clocks.Length != 1) throw new InvalidOperationException("Expected one matching PC/day cycle: " + path);
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            canvas.name = "Yun Shop PC UI";
            canvas.SetActive(false);
            if (oldPanel != null) oldPanel.SetActive(false);
            pc.pcUIPanel = canvas;
            clocks[0].pcUIPanel = canvas;
            EditorUtility.SetDirty(pc);
            EditorUtility.SetDirty(clocks[0]);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene(path);
            var saved = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShopPcUiView>(true)).Single();
            if (saved.gameObject.activeSelf || saved.GetComponentsInChildren<Button>(true).Length != 20)
                throw new InvalidOperationException("Saved canvas validation failed: " + path);
        }
    }
}
#endif
