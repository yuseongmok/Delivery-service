#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        public static void DressForest()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-forest-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                film = Object.FindFirstObjectByType<AftermathFilm>(); root = film.transform;
                var set = film.shots[0].set.transform;
                var old = set.Find("숲 원경 보강"); if (old) Object.DestroyImmediate(old.gameObject);
                foreach (var t in set.GetComponentsInChildren<Transform>(true).Where(t => t.name == "SM_Gen_Env_Mountain_02").ToArray()) Object.DestroyImmediate(t.gameObject);
                var dressing = At("숲 원경 보강", set, Vector3.zero);
                var ground = set.GetComponentsInChildren<Transform>(true).First(t => t.name == "숲길");
                ground.position = new Vector3(310, -.16f, 45); ground.localScale = new Vector3(180, .3f, 170);
                string environment = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";
                for (int i = 0; i < 7; i++)
                {
                    var mountain = Prop(environment + "SM_Gen_Env_Background_Mountain_0" + (1 + i % 3) + ".prefab", dressing,
                        new Vector3(240 + i * 23, -7, 68 + i % 2 * 17), 32 + i % 3 * 5, i * 43);
                    mountain.transform.localScale = Vector3.Scale(mountain.transform.localScale, new Vector3(1.5f, 1, 1.3f));
                }
                // Staggered depth layers close the horizon without creating a straight wall of trees.
                for (int row = 0; row < 3; row++)
                    for (int i = 0; i < 22; i++)
                    {
                        float x = 254 + i * 4.4f + row * 1.7f;
                        float z = 20 + row * 12 + Mathf.Sin(i * 1.8f + row) * 2;
                        if (x > 317 && x < 340 && z < 35) continue;
                        Prop(environment + "SM_Gen_Env_Tree_Pine_0" + (1 + (i + row) % 3) + ".prefab", dressing,
                            new Vector3(x, 0, z), 7 + (i * 3 + row) % 6, i * 71 + row * 23);
                    }
                var road = set.GetComponentsInChildren<Transform>(true).First(t => t.name == "호송 도로");
                road.position = new Vector3(288, -.015f, 9); road.localScale = new Vector3(26, .035f, 6);
                var van = film.shots[0].motions.First(m => !m.actor);
                van.from.position = new Vector3(301, 0, 9); van.to.position = new Vector3(337, 0, 35);
                if (!van.bend) van.bend = At("숲길 차량 회전", root, new Vector3(324, 0, 9));
                van.bend.position = new Vector3(324, 0, 9); van.facePath = true;
                Vector3 previous = van.from.position;
                for (int i = 1; i <= 32; i++)
                {
                    float t = i / 32f;
                    Vector3 next = Vector3.Lerp(Vector3.Lerp(van.from.position, van.bend.position, t), Vector3.Lerp(van.bend.position, van.to.position, t), t);
                    var segment = Box("굽은 호송 도로 " + i, dressing, (previous + next) * .5f + Vector3.down * .015f,
                        new Vector3(6, .035f, Vector3.Distance(previous, next) + .12f), road.GetComponent<Renderer>().sharedMaterial);
                    segment.rotation = Quaternion.LookRotation(next - previous); previous = next;
                }
                for (int i = 0; i < 9; i++)
                {
                    var p = new Vector3(315 + i * 2.3f, 0, 4 + i * .65f);
                    Prop(environment + "SM_Gen_Env_Tree_Pine_0" + (1 + i % 3) + ".prefab", dressing, p, 8 + i % 4, i * 53);
                    Prop(environment + "SM_Gen_Env_Bush_Large_0" + (1 + i % 3) + ".prefab", dressing, p + new Vector3(-.7f, 0, -.3f), 1.8f + i % 2 * .4f, i * 37);
                }
                // Inside shoulder of the bend, outside the six-metre carriageway.
                for (int i = 0; i < 5; i++)
                {
                    var p = new Vector3(322 + i * 1.2f, 0, 23 + i * 1.3f);
                    Prop(environment + "SM_Gen_Env_Tree_Pine_0" + (1 + i % 3) + ".prefab", dressing, p, 9 + i % 3, i * 67);
                    Prop(environment + "SM_Gen_Env_Bush_Large_0" + (1 + i % 3) + ".prefab", dressing, p, 3.2f, i * 47);
                }
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
