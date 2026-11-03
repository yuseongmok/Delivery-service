#if UNITY_EDITOR
using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class BatPolishUpgrade {
 public static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/BatPolish-scene-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  foreach(var root in scene.GetRootGameObjects()){
   foreach(var barrier in root.GetComponentsInChildren<PrologueBreakableBarrier>(true))UnityEngine.Object.DestroyImmediate(barrier.gameObject);
   foreach(var officer in root.GetComponentsInChildren<PrologueOfficer>(true))if(officer.gun)officer.gun.gameObject.SetActive(false);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  System.IO.File.WriteAllText("Logs/BatPolish-apply.txt","PASS saved unarmed gang and removed wooden road barriers");
 }
}}
#endif
