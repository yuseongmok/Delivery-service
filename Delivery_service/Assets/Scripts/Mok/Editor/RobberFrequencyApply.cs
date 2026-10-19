#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RobberFrequencyApply {
 static RobberFrequencyApply(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-robber-frequency-apply"))return;
  File.Delete("Temp/Mok-robber-frequency-apply");
  try {
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");
   if(!scene.isLoaded)throw new Exception("JJinCity 씬을 먼저 열어 주세요.");
   var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RobberyManager>(true)).Single();
   if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-robber-frequency.unity",true))throw new Exception("백업 실패");
   var so=new SerializedObject(manager);
   void Set(string name,float value){so.FindProperty(name).floatValue=value;}
   Set("lowInterval",40);Set("mediumInterval",30);Set("highInterval",20);
   Set("lowChance",30);Set("mediumChance",40);Set("highChance",50);
   Set("encounterCooldown",45);Set("footIntervalMultiplier",.75f);Set("footChanceBonus",15);
   so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
   File.WriteAllText("Temp/Mok-robber-frequency-apply.txt","PASS: scene backed up and frequency settings saved.");
  }catch(Exception ex){File.WriteAllText("Temp/Mok-robber-frequency-apply.txt",ex.ToString());Debug.LogException(ex);}
 }
}}
#endif
