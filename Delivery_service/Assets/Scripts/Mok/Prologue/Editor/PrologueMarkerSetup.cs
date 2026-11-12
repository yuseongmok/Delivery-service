#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
 [InitializeOnLoad] public static class PrologueMarkerSetup {
  static PrologueMarkerSetup(){EditorApplication.update+=Poll;}
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static void Poll(){
   if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrologueMarker-request"))return;
   File.Delete("Temp/PrologueMarker-request");
   try {
    var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
    EditorSceneManager.SaveScene(scene,"Logs/Before-prologue-marker-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
    var director=All<BankPrologue>(scene).Single();var display=director.GetComponent<PrologueObjectiveMarker>();if(!display)display=director.gameObject.AddComponent<PrologueObjectiveMarker>();display.director=director;
    if(!display.marker){
     var city=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!city.isLoaded;if(opened)city=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
     var source=All<BrokerMission>(city).Single().marker;
     display.marker=UnityEngine.Object.Instantiate(source,director.objective.canvas.transform);display.marker.name="Prologue destination";display.marker.raycastTarget=false;display.marker.text="";display.marker.gameObject.SetActive(false);
     if(opened)EditorSceneManager.CloseScene(city,true);
    }
    if(!display.marker.GetComponentInChildren<StoryTargetDiamond>(true))throw new Exception("City diamond missing");
    EditorUtility.SetDirty(display);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    File.WriteAllText("Logs/Prologue-marker-check.txt","PASS saved city marker with diamond and label\nPASS escape waypoints="+director.escapeRoute.Length+"; car="+(director.exitPoint!=null)+"; drive finish="+(director.drive&&director.drive.finish)+"\n");
   }catch(Exception e){File.WriteAllText("Logs/Prologue-marker-check.txt",e.ToString());}
  }
 }
}
#endif
