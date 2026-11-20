#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class RaceCityPolish
    {
        static RaceCityPolish() { EditorApplication.update += Run; }
        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists("Temp/RaceCityPolish")) return;
            File.Delete("Temp/RaceCityPolish");
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");
            bool added = !scene.isLoaded;
            if (added) scene = EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity", OpenSceneMode.Additive);
            try
            {
                if (!EditorSceneManager.SaveScene(scene, "Logs/City-before-race-polish-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true)) throw new Exception("Backup failed");
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var race = all.Select(t => t.GetComponent<RaceManager>()).First(r => r);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/RacerNPC.prefab");
                var home = all.FirstOrDefault(t => t.name == "신도시 레이서 만남 위치");
                if (!home)
                {
                    Vector3 spot = default; bool found = false;
                    // 신도시 02 배달함 옆 보행 공간. 건물 입구와 통행로는 비워 둔다.
                    foreach (var wish in new[] { new Vector3(239, .15f, -12), new Vector3(237, .15f, -16), new Vector3(237, .15f, -20), new Vector3(236, .15f, -25) })
                    {
                        if (!NavMesh.SamplePosition(wish, out var hit, 2, NavMesh.AllAreas)) continue;
                        bool clear = !Physics.OverlapCapsule(hit.position + Vector3.up * .45f, hit.position + Vector3.up * 1.8f, .4f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.gameObject.scene == scene);
                        if (!clear) continue;
                        spot = hit.position; found = true; break;
                    }
                    if (!found) throw new Exception("No clear racer standing space");
                    home = new GameObject("신도시 레이서 만남 위치").transform;
                    SceneManager.MoveGameObjectToScene(home.gameObject, scene);
                    home.SetPositionAndRotation(spot, Quaternion.Euler(0, 90, 0));
                    var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    actor.name = "레이서 · 신도시"; actor.transform.SetParent(home, false); actor.transform.localPosition = Vector3.zero; actor.transform.localRotation = Quaternion.identity;
                    var meeting = home.gameObject.AddComponent<RacerMeetingPoint>();
                    meeting.prefab = prefab; meeting.actor = actor.GetComponent<RacerNPCInteraction>();
                    foreach (var mission in all.Select(t => t.GetComponent<MechanicMission>()).Where(m => m)) { mission.racerNpc = actor.transform; mission.racerSpawnPoint = home; EditorUtility.SetDirty(mission); }
                    // Only this scene stops random racer spawning; ordinary citizens are unchanged.
                    foreach (var manager in all.Select(t => t.GetComponent<NPCManager>()).Where(n => n))
                    {
                        var serialized = new SerializedObject(manager);
                        serialized.FindProperty("racerNPCPrefab").objectReferenceValue = null;
                        serialized.ApplyModifiedProperties();
                    }
                }

                var placed = home.GetComponent<RacerMeetingPoint>().actor;
                placed.transform.SetPositionAndRotation(home.position, home.rotation);
                placed.GetComponent<Animator>().applyRootMotion = false;
                var barrier = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_Barrier_01.prefab");
                int count = 0;
                foreach (var box in race.objects.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Cube")).ToArray())
                {
                    var root = box.transform;
                    Vector3 size = Vector3.Scale(box.size, root.lossyScale);
                    Vector3 center = root.TransformPoint(box.center);
                    var rotation = root.rotation;
                    root.localScale = Vector3.one;
                    root.position = center;
                    root.name = "레이스 도로 통제 " + (++count);
                    box.center = new Vector3(0, -.25f, 0); box.size = new Vector3(size.x, 1.2f, size.z);
                    var renderer = root.GetComponent<MeshRenderer>(); if (renderer) UnityEngine.Object.DestroyImmediate(renderer);
                    var filter = root.GetComponent<MeshFilter>(); if (filter) UnityEngine.Object.DestroyImmediate(filter);
                    int pieces = Mathf.CeilToInt(size.x / 2.1f);
                    for (int i = 0; i < pieces; i++)
                    {
                        var prop = (GameObject)PrefabUtility.InstantiatePrefab(barrier, scene);
                        prop.transform.SetParent(root, false);
                        prop.transform.localPosition = new Vector3(-size.x / 2 + (i + .5f) * size.x / pieces, -center.y + .02f, 0);
                        // Preserve the original continuous road closure collider, avoiding gaps.
                        foreach (var c in prop.GetComponentsInChildren<Collider>()) c.enabled = false;
                        var bounds = prop.GetComponentsInChildren<MeshFilter>().First().sharedMesh.bounds;
                        float width = Mathf.Max(bounds.size.x, bounds.size.z);
                        if (bounds.size.z > bounds.size.x) prop.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        prop.transform.localScale = Vector3.one * (size.x / pieces / width);
                    }
                }
                if (!race.GetComponent<RaceRecovery>()) race.gameObject.AddComponent<RaceRecovery>();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Race-city-polish.txt", "Saved racer at " + home.position + "; replaced barrier groups=" + count);
            }
            catch (Exception e) { File.WriteAllText("Logs/Race-city-polish.txt", e.ToString()); }
            finally { if (added) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
#endif
