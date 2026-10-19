#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class RegularRobberPairSetup {
 static RegularRobberPairSetup(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/Mok-regular-pair-install-request"))return;
  File.Delete("Temp/Mok-regular-pair-install-request");
  try{
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("JJinCity 씬이 필요합니다.");
   if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-regular-robber-pair.unity",true))throw new Exception("씬 백업 실패");
   var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();if(manager==null)throw new Exception("강도 매니저 없음");
   var so=new SerializedObject(manager);so.FindProperty("lowChance").floatValue=45;so.FindProperty("mediumChance").floatValue=55;so.FindProperty("highChance").floatValue=65;so.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-regular-pair-install.txt","PASS: scene backed up and saved; regular chances 45/55/65; foot bonus unchanged.\n");
  }catch(Exception e){File.WriteAllText("Temp/Mok-regular-pair-install.txt",e.ToString());Debug.LogException(e);}
 }
}
}
#endif
