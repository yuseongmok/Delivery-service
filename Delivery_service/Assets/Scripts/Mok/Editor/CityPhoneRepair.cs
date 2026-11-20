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
 foreach(var root in scene.GetRootGameObjects())foreach(var place in root.GetComponentsInChildren<NavigationPlace>(true))
 if(place.name=="Fake_Shop"&&place.placeId=="repair")Undo.DestroyObjectImmediate(place);
 // Restore the saved map artwork, without rebuilding or changing city geometry.
 var mapRoot=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Mok Navigation Map");
 if(!mapRoot){mapRoot=new GameObject("Mok Navigation Map");SceneManager.MoveGameObjectToScene(mapRoot,scene);Undo.RegisterCreatedObjectUndo(mapRoot,"Restore map artwork");}
 mapRoot.layer=NavigationMapStyle.MapLayer;
 var filter=mapRoot.GetComponent<MeshFilter>();if(!filter)filter=mapRoot.AddComponent<MeshFilter>();
 const string folder="Assets/Scripts/Mok/Art/NavigationMap/";
 filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"CityNavigationMap.asset");
 var renderer=mapRoot.GetComponent<MeshRenderer>();if(!renderer)renderer=mapRoot.AddComponent<MeshRenderer>();
 renderer.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(folder+"Ground.mat"),AssetDatabase.LoadAssetAtPath<Material>(folder+"Roads.mat"),AssetDatabase.LoadAssetAtPath<Material>(folder+"Buildings.mat")};
 var style=mapRoot.GetComponent<NavigationMapStyle>();if(!style)style=mapRoot.AddComponent<NavigationMapStyle>();
 foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true)){
 if(camera.name=="NavigationCamera"){style.trackingCamera=camera;camera.enabled=true;camera.orthographicSize=40;NavigationMapStyle.Configure(camera);}
 else if(camera.name=="NavigationOverviewCamera"){style.overviewCamera=camera;camera.enabled=false;NavigationMapStyle.Configure(camera);}
 else camera.cullingMask&=~(1<<NavigationMapStyle.MapLayer);
 }
 if(!filter.sharedMesh||!style.trackingCamera)throw new System.Exception("Map artwork or tracking camera missing");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/City-phone-repair.txt","Restored NavigationPlanner and story app reference; city backed up before repair.");
 if(added)EditorSceneManager.CloseScene(scene,true);
 }
}}
#endif
