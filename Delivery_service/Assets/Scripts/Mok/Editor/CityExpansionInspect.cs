#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad]public static class CityExpansionInspect{
static CityExpansionInspect(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-city-inspect"))return;File.Delete("Temp/Mok-city-inspect");var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
var sb=new StringBuilder();string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
void Item(Transform t){var render=t.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)&&r.enabled).ToArray();var b=new Bounds(t.position,Vector3.zero);if(render.Length>0){b=render[0].bounds;foreach(var r in render)b.Encapsulate(r.bounds);}sb.AppendLine(P(t)+" pos="+t.position+" bounds="+b+" children="+t.childCount+" renderers="+render.Length);}
foreach(var root in scene.GetRootGameObjects()){Item(root.transform);if(root.name=="Demo")foreach(Transform t in root.transform){Item(t);foreach(Transform c in t)if(c.childCount>5)Item(c);}}
foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.name.Contains("Bridge")))sb.AppendLine("BRIDGE "+P(r.transform)+" pos="+r.transform.position+" rotation="+r.transform.eulerAngles+" bounds="+r.bounds);
foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.parent!=null && t.parent.name=="NopeNavMesh").Take(35))Item(t); foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.bounds.Intersects(new Bounds(new Vector3(195,2,-5),new Vector3(20,4,18)))))sb.AppendLine("BLOCK "+P(c.transform)+" "+c.bounds+" trigger="+c.isTrigger); foreach(var s in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true)))sb.AppendLine("NAV "+P(s.transform)+" "+EditorJsonUtility.ToJson(s)); foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.bounds.size.x>200||r.name.Contains("Sky")||r.name.Contains("Background")||r.name.Contains("SpawnArea")))sb.AppendLine("ENV "+P(r.transform)+" "+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m==null?"null":m.name+":"+m.shader.name))); foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="SpawnArea"))sb.AppendLine("SPAWN "+P(t)+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name))); File.WriteAllText("Temp/Mok-city-inspect.txt",sb.ToString());
}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif


