#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DeliveryService.Yun.Streetlights;
using DeliveryService.Yun.ShopUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Yun.Editor
{
    public static class JJinCityLightingMigration
    {
        public static void RunBatch()
        {
            try { Migrate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static string Key(Transform transform) => (transform.parent == null ? "" : Key(transform.parent) + "/")
            + transform.name + "[" + transform.GetSiblingIndex() + "]";

        private static void Migrate()
        {
            var target = EditorSceneManager.OpenScene(YunCityStreetlightSetup.SourceScene);
            if (All<StreetlightTimeController>(target).Any())
                throw new InvalidOperationException("JJinCity already has lighting controllers; refusing duplicate migration.");
            var source = EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene, OpenSceneMode.Additive);
            var clock = All<DayNightCycle>(target).Single();
            var destination = All<Transform>(target).ToDictionary(Key);
            var controllers = All<StreetlightTimeController>(source).ToArray();
            var street = controllers.Single(c => c.LampCount == 96);
            var bike = controllers.Single(c => c.LampCount == 1);
            var serialized = new SerializedObject(street);
            var sourceLights = serialized.FindProperty("lamps");
            var sourceSurfaces = serialized.FindProperty("glowingSurfaces");
            var objects = new List<GameObject>();
            for (int i = 0; i < sourceLights.arraySize; i++)
                objects.Add(((Light)sourceLights.GetArrayElementAtIndex(i).objectReferenceValue).gameObject);
            for (int i = 0; i < sourceSurfaces.arraySize; i++)
                objects.Add((GameObject)sourceSurfaces.GetArrayElementAtIndex(i).objectReferenceValue);
            objects.Add(bike.gameObject);
            // Validate every attachment before changing any target object.
            foreach (var item in objects)
            {
                if (item == null || item.transform.parent == null || !destination.ContainsKey(Key(item.transform.parent)))
                    throw new InvalidOperationException("Cannot match lighting parent: " + (item == null ? "missing object" : Key(item.transform)));
                var a = item.transform.parent.GetComponent<MeshFilter>();
                var b = destination[Key(item.transform.parent)].GetComponent<MeshFilter>();
                if (a == null || b == null || a.sharedMesh != b.sharedMesh)
                    throw new InvalidOperationException("Parent model differs: " + Key(item.transform.parent));
            }
            var clones = new Dictionary<GameObject, GameObject>();
            foreach (var item in objects.Distinct())
            {
                var clone = UnityEngine.Object.Instantiate(item, destination[Key(item.transform.parent)], false);
                clone.name = item.name;
                clones.Add(item, clone);
            }
            var lamps = new List<Light>();
            var surfaces = new List<GameObject>();
            for (int i = 0; i < sourceLights.arraySize; i++)
                lamps.Add(clones[((Light)sourceLights.GetArrayElementAtIndex(i).objectReferenceValue).gameObject].GetComponent<Light>());
            for (int i = 0; i < sourceSurfaces.arraySize; i++)
                surfaces.Add(clones[(GameObject)sourceSurfaces.GetArrayElementAtIndex(i).objectReferenceValue]);
            var root = new GameObject(street.name);
            SceneManager.MoveGameObjectToScene(root, target);
            var migratedStreet = root.AddComponent<StreetlightTimeController>();
            EditorUtility.CopySerialized(street, migratedStreet);
            migratedStreet.Configure(clock, lamps.ToArray(), surfaces.ToArray());
            var migratedBike = clones[bike.gameObject].GetComponent<StreetlightTimeController>();
            migratedBike.Configure(clock, clones[bike.gameObject].GetComponentsInChildren<Light>(true),
                clones[bike.gameObject].GetComponentsInChildren<MeshRenderer>(true).Select(r => r.gameObject).ToArray());
            foreach (var controller in All<StreetlightTimeController>(target))
            {
                var properties = new SerializedObject(controller);
                if (((DayNightCycle)properties.FindProperty("clock").objectReferenceValue).gameObject.scene != target)
                    throw new InvalidOperationException("Cross-scene clock reference");
            }
            EditorSceneManager.MarkSceneDirty(target);
            if (!EditorSceneManager.SaveScene(target)) throw new InvalidOperationException("JJinCity save failed");
            EditorSceneManager.CloseScene(source, true);
            // The UI fixes install at scene load. Validate their existing Canvas references without saving runtime bindings.
            PendingStockText.Install(target);
            int labels = All<PendingStockText>(target).Count();
            if (labels < 4) throw new InvalidOperationException("Expected at least four ingredient stock labels");
            Debug.Log($"JJINCITY_MIGRATION_PASSED: {lamps.Count} streetlights + 1 headlight; {labels} existing stock labels resolved; target clock references rebound.");
        }
    }
}
#endif
