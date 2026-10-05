#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DeliveryService.Yun.Streetlights;
using System.IO;

namespace DeliveryService.Yun.Editor
{
    public static class YunMotorcycleLightSetup
    {
        public static void BuildBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
                var bike = UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None).Single();
                if (bike.GetComponentsInChildren<StreetlightTimeController>(true).Length > 0)
                    throw new InvalidOperationException("Headlight already installed; refusing duplicate installation.");
                var body = bike.GetComponentsInChildren<MeshFilter>().Single(m => m.name == "Bike");
                var materials = body.GetComponent<Renderer>().sharedMaterials;
                int index = Array.FindIndex(materials, m => m.name == "Headlight");
                if (index < 0) throw new InvalidOperationException("Headlight surface missing");
                const string folder = "Assets/Scripts/Yun/Streetlights/Visuals";
                var source = body.sharedMesh;
                var vertices = source.vertices;
                var lensVertices = source.GetTriangles(index).Select(i => vertices[i] + Vector3.forward * .003f).ToArray();
                var mesh = new Mesh { name = "Motorcycle headlight lens", vertices = lensVertices,
                    triangles = Enumerable.Range(0, lensVertices.Length).ToArray() };
                mesh.RecalculateBounds(); mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, folder + "/MotorcycleHeadlightLens.asset");
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Motorcycle headlight glow" };
                material.SetColor("_BaseColor", new Color(4, 3.7f, 3.1f, 1));
                AssetDatabase.CreateAsset(material, folder + "/MotorcycleHeadlightGlow.mat");

                var root = new GameObject("Yun Motorcycle Headlight");
                root.transform.SetParent(body.transform, false);
                var lens = new GameObject("Yun Headlight Lens", typeof(MeshFilter), typeof(MeshRenderer));
                lens.transform.SetParent(root.transform, false);
                lens.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = lens.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                lens.SetActive(false);
                var beam = new GameObject("Yun Motorcycle Beam", typeof(Light));
                beam.transform.SetParent(root.transform, false);
                beam.transform.localPosition = new Vector3(mesh.bounds.center.x, mesh.bounds.center.y, mesh.bounds.max.z + .04f);
                beam.transform.localRotation = Quaternion.Euler(8, 0, 0);
                var light = beam.GetComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(1, .91f, .76f);
                light.intensity = 35;
                light.range = 24;
                light.spotAngle = 65;
                light.innerSpotAngle = 35;
                light.shadows = LightShadows.Hard;
                light.shadowBias = .05f;
                light.shadowNormalBias = .1f;
                light.lightmapBakeType = LightmapBakeType.Realtime;
                light.enabled = false;
                var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
                root.AddComponent<StreetlightTimeController>().Configure(clock, new[] { light }, new[] { lens });
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("YUN_MOTORCYCLE_HEADLIGHT_INSTALLED: original lens submesh, following bike body, shared 16h/business schedule.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void AuditBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(YunCityStreetlightSetup.TestScene);
                foreach (var bike in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))
                foreach (var filter in bike.GetComponentsInChildren<MeshFilter>())
                {
                    Debug.Log($"BIKE_MESH {filter.name} bounds={filter.sharedMesh.bounds} world={filter.transform.position}");
                    var mesh = filter.sharedMesh;
                    var materials = filter.GetComponent<Renderer>().sharedMaterials;
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        var points = mesh.GetTriangles(i).Select(n => mesh.vertices[n]).ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero);
                        foreach (var p in points) bounds.Encapsulate(p);
                        Debug.Log($"BIKE_PART {filter.name} index={i} material={materials[i].name} bounds={bounds}");
                    }
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
