#if UNITY_EDITOR
using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class PoliceArtInspector {
static PoliceArtInspector(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-police-inspect"))return;File.Delete("Temp/Mok-police-inspect");var root=PrefabUtility.LoadPrefabContents("Assets/Scripts/Min/Prefab/NPC/Police.prefab");try{var s=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Transform>())s.AppendLine(t.name+" position="+t.position+" local="+t.localPosition+" scale="+t.lossyScale+" rotation="+t.eulerAngles);foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())s.AppendLine("MESH "+r.name+" bounds="+r.bounds+" local="+r.sharedMesh.bounds+" vertices="+r.sharedMesh.vertexCount+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m.name)));var a=root.GetComponent<Animator>();s.AppendLine("ANIM humanoid="+a.isHuman+" controller="+a.runtimeAnimatorController.name);foreach(var clip in a.runtimeAnimatorController.animationClips)s.AppendLine("CLIP "+AssetDatabase.GetAssetPath(clip)+" "+clip.name);foreach(var clip in a.runtimeAnimatorController.animationClips.Where(c=>c.name.Contains("Walking")))foreach(var b in AnimationUtility.GetCurveBindings(clip))s.AppendLine("CURVE "+b.type+" "+b.path+" "+b.propertyName);File.WriteAllText("Temp/Mok-police-inspect.txt",s.ToString());}finally{PrefabUtility.UnloadPrefabContents(root);}}
}}
#endif
