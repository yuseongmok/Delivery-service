#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CityPhoneRepair {
 static CityPhoneRepair(){EditorApplication.update+=Run;}
 static void Run(){
 if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/CityPhoneRepair"))return;
 File.Delete("Temp/CityPhoneRepair");
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool added=!scene.isLoaded;
 if(added)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(scene,"Logs/City-before-phone-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 NavigationManager manager=null;NavigationPlanner planner=null;
 foreach(var root in scene.GetRootGameObjects()){if(!manager)manager=root.GetComponentInChildren<NavigationManager>(true);if(!planner)planner=root.GetComponentInChildren<NavigationPlanner>(true);}
 if(!manager)throw new System.Exception("City navigation manager missing");
 if(!planner)planner=Undo.AddComponent<NavigationPlanner>(manager.gameObject);
 Undo.RecordObject(planner,"Restore phone apps");planner.manager=manager;planner.enabled=true;
 planner.storyAppsPrefab=AssetDatabase.LoadAssetAtPath<StoryPhoneApp>(AssetDatabase.GUIDToAssetPath("4c4ce7d2ddbf1464598fd831f19e1856"));
 if(!planner.storyAppsPrefab)throw new System.Exception("Story phone prefab missing");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/City-phone-repair.txt","Restored NavigationPlanner and story app reference; city backed up before repair.");
 if(added)EditorSceneManager.CloseScene(scene,true);
 }
}}
#endif
