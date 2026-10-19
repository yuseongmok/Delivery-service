#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class SlumWaveApply {
 static SlumWaveApply(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-slum-wave-apply"))return;
  File.Delete("Temp/Mok-slum-wave-apply");
  try {
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");
   if(!scene.isLoaded)throw new Exception("JJinCity 씬을 먼저 열어 주세요.");
   var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RobberyManager>(true)).Single();
   if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-slum-wave.unity",true))throw new Exception("백업 실패");
   var so=new SerializedObject(manager);
   void Set(string name,float value){so.FindProperty(name).floatValue=value;}
   Set("districtInterval",8);Set("districtChance",100);Set("districtCooldown",10);
   so.FindProperty("districtWaveMin").intValue=2;so.FindProperty("districtWaveMax").intValue=3;so.FindProperty("districtMaxActive").intValue=3;
   so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
   File.WriteAllText("Temp/Mok-slum-wave-apply.txt","PASS: scene backed up and frequency settings saved.");
  }catch(Exception ex){File.WriteAllText("Temp/Mok-slum-wave-apply.txt",ex.ToString());Debug.LogException(ex);}
 }
}}
#endif
