#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using DeliveryService.Yun.Streetlights;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryService.Yun.Editor
{
    public static class YunCityStreetlightSetup
    {
        public const string SourceScene = "Assets/Scenes/JJinCity.unity";
        public const string TestScene = "Assets/Scenes/YunCity.unity";

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Build()
        {
            if (File.Exists(TestScene)) throw new InvalidOperationException("YunCity already exists; refusing to overwrite it.");
            if (!AssetDatabase.CopyAsset(SourceScene, TestScene)) throw new InvalidOperationException("Scene copy failed");
            var scene = EditorSceneManager.OpenScene(TestScene);
            const string folder = "Assets/Scripts/Yun/Streetlights/Visuals";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP unlit shader missing");
            var glow = new Material(shader) { name = "Streetlight Warm Glow" };
            glow.SetColor("_BaseColor", new Color(3.2f, 2.35f, 1.15f, 1));
            AssetDatabase.CreateAsset(glow, folder + "/StreetlightGlow.mat");
            var lanternMesh = MakeLanternBand();
            AssetDatabase.CreateAsset(lanternMesh, folder + "/LanternGlow.asset");
            var poles = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(m => m.name.StartsWith("SM_Prop_LightPole_Base_", StringComparison.Ordinal)).ToArray();
            var lamps = new List<Light>();
            var surfaces = new List<GameObject>();
            foreach (var pole in poles)
            {
                if (pole.sharedMesh.name == "SM_Prop_LightPole_Base_01")
                {
                    var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    surface.name = "Yun Lamp Lens";
                    UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
                    surface.transform.SetParent(pole.transform, false);
                    surface.transform.localPosition = new Vector3(0, 6.222f, 1.96f);
                    surface.transform.localScale = new Vector3(.16f, .014f, .50f);
                    SetupGlow(surface, glow);
                    surfaces.Add(surface);
                    lamps.Add(AddLight(pole.transform, new Vector3(0, 6.18f, 1.96f), LightType.Spot, 65f, 13f));
                }
                else if (pole.sharedMesh.name == "SM_Prop_LightPole_Base_02")
                {
                    foreach (float x in new[] { -.35f, .35f })
                    {
                        var surface = new GameObject("Yun Lantern Lens", typeof(MeshFilter), typeof(MeshRenderer));
                        surface.transform.SetParent(pole.transform, false);
                        surface.transform.localPosition = new Vector3(x, 4.635f, 0);
                        surface.GetComponent<MeshFilter>().sharedMesh = lanternMesh;
                        SetupGlow(surface, glow);
                        surfaces.Add(surface);
                        lamps.Add(AddLight(pole.transform, new Vector3(x, 4.63f, 0), LightType.Point, 12f, 9f));
                    }
                }
                else throw new InvalidOperationException("Unrecognized pole mesh: " + pole.sharedMesh.name);
            }
            var clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            if (clock == null || lamps.Count == 0) throw new InvalidOperationException("Clock or lamps missing");
            var root = new GameObject("Yun Streetlights (16h)");
            var controller = root.AddComponent<StreetlightTimeController>();
            controller.Configure(clock, lamps.ToArray(), surfaces.ToArray());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"YUNCITY_CREATED: {poles.Length} poles, {lamps.Count} lights, clock={clock.name}, original scene unchanged.");
        }

        private static void SetupGlow(GameObject surface, Material glow)
        {
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = glow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            surface.SetActive(false);
        }

        private static Light AddLight(Transform pole, Vector3 position, LightType type, float intensity, float range)
        {
            var go = new GameObject("Yun Streetlight");
            go.transform.SetParent(pole, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = new Color(1f, .79f, .52f);
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = 105;
            light.innerSpotAngle = 65;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.enabled = false;
            return light;
        }

        private static Mesh MakeLanternBand()
        {
            var vertices = new Vector3[16];
            var triangles = new int[48];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                vertices[i * 2] = new Vector3(Mathf.Sin(angle) * .204f, -.06f, Mathf.Cos(angle) * .204f);
                vertices[i * 2 + 1] = vertices[i * 2] + Vector3.up * .12f;
                int a = i * 2, b = ((i + 1) % 8) * 2;
                int t = i * 6;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = b; triangles[t + 5] = b + 1;
            }
            var mesh = new Mesh { name = "Lantern glow band", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public static void TuneBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(TestScene);
                foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.name == "Yun Streetlight"))
                    light.intensity = light.type == LightType.Spot ? 65f : 12f;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void AuditBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(SourceScene);
                var poles = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                    .Where(m => m.name.StartsWith("SM_Prop_LightPole_Base_", StringComparison.Ordinal)).ToArray();
                Debug.Log("STREETLIGHT_POLES " + poles.Length);
                foreach (var group in poles.GroupBy(p => p.sharedMesh))
                {
                    var mesh = group.Key;
                    Debug.Log($"STREETLIGHT_MESH {mesh.name} count={group.Count()} bounds={mesh.bounds} material={group.First().GetComponent<Renderer>().sharedMaterial.name}");
                    var points = mesh.vertices.Where(v => v.y > mesh.bounds.max.y - 1.5f)
                        .Select(v => $"{v.x:F2},{v.y:F2},{v.z:F2}").Distinct().ToArray();
                    Debug.Log("STREETLIGHT_TOP " + string.Join(";", points));
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
