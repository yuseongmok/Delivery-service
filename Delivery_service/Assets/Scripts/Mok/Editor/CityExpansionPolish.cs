#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class CityExpansionPolish{
static CityExpansionPolish(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-city-polish"))return;File.Delete("Temp/Mok-city-polish");var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-city-polish.unity",true);var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
foreach(var t in all.Where(t=>t.name.StartsWith("SM_Gen_Env_Skydome"))){if(t.GetComponent<Renderer>().bounds.size.x<1000){t.localScale*=2.5f;t.position+=new Vector3(140,0,0);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}}
foreach(var t in all.Where(t=>t.name=="Ocean_Plane"||t.name=="Ocean_Floor")){if(t.GetComponent<Renderer>().bounds.size.x<1000){t.localScale*=2.5f;t.position+=new Vector3(140,0,0);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}}
var skyline=all.First(t=>t.name=="SM_Env_Skyline_01");if(skyline.GetComponent<Renderer>().bounds.size.x<900){skyline.localScale=new Vector3(skyline.localScale.x*2.5f,skyline.localScale.y,skyline.localScale.z*2.5f);skyline.position+=new Vector3(140,0,0);PrefabUtility.RecordPrefabInstancePropertyModifications(skyline);}
foreach(var t in all.Where(t=>t.name=="SpawnArea")){var c=t.GetComponent<BoxCollider>();if(c!=null){c.isTrigger=true;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}}
foreach(var cam in all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null&&!c.orthographic)){cam.farClipPlane=Mathf.Max(cam.farClipPlane,1800);PrefabUtility.RecordPrefabInstancePropertyModifications(cam);}var city=all.First(t=>t.name=="Mok East City");foreach(var c in city.GetComponentsInChildren<Collider>()){if(c.name=="Building collision"||c.name.Contains("sea wall")||c.transform.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("East Block")))c.gameObject.tag="Obstacle";} var overview=all.FirstOrDefault(t=>t.name=="NavigationOverviewCamera");if(overview!=null){overview.position=new Vector3(150,80,-5);overview.GetComponent<Camera>().orthographicSize=215;}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-city-polish.txt","PASS sky, ocean, skyline expanded; spawn volumes non-solid; overview expanded");
} catch(Exception e){File.WriteAllText("Temp/Mok-city-polish.txt",e.ToString());}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif

