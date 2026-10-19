#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class DayTenMinutesSetup {
 static DayTenMinutesSetup(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/Mok-day-ten-install-request"))return;
  File.Delete("Temp/Mok-day-ten-install-request");
  try{
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("JJinCity 씬이 필요합니다.");
   if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-ten-minute-day.unity",true))throw new Exception("씬 백업 실패");
   var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();if(cycle==null)throw new Exception("시계 없음");float before=cycle.timeMultiplier;
   cycle.timeMultiplier=108;PrefabUtility.RecordPrefabInstancePropertyModifications(cycle);EditorUtility.SetDirty(cycle);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllText("Temp/Mok-day-ten-install.txt",$"PASS: scene saved; clock multiplier {before} -> 108; 18 hours = 600 real seconds; delivery multiplier stays 54.\n");
  }catch(Exception e){File.WriteAllText("Temp/Mok-day-ten-install.txt",e.ToString());Debug.LogException(e);}
 }
}
}
#endif
