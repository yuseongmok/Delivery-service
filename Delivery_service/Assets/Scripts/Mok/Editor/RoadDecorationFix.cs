#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class RoadDecorationFix {
 static RoadDecorationFix(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-road-decoration-request"))return;File.Delete("Temp/Mok-road-decoration-request");Run();}
 static void Run(){var old=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
 EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-road-decoration.unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&!c.isTrigger&&(c.name.StartsWith("SM_Prop_Manhole_")||c.name.StartsWith("SM_Env_Road_Patch_"))&&c.bounds.max.y<.35f&&c.bounds.min.y>-.3f).ToArray();
 string log=string.Join("\n",all.Select(c=>$"{c.name} {c.bounds}"));foreach(var c in all){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-road-decoration-result.txt",$"PASS: {all.Length} thin road decoration colliders disabled; visuals preserved.\n"+log);
 }catch(Exception e){File.WriteAllText("Temp/Mok-road-decoration-result.txt",e.ToString());}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif
