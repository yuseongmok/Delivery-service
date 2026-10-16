#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class HospitalInstaller {
static HospitalInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-hospital-install"))return;File.Delete("Temp/Mok-hospital-install");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-hospital-install.txt",e.ToString());Debug.LogException(e);}}
static void Run(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var hospitals=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="HOSPITAL").ToArray();
if(hospitals.Length!=1)throw new Exception("Expected one HOSPITAL in active scene; found "+hospitals.Length);
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-hospital.unity",true))throw new Exception("Backup failed");
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();player.hospital=hospitals[0];EditorUtility.SetDirty(player);
foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Scripts/Min/Prefab/NPC"}).Select(AssetDatabase.GUIDToAssetPath)){
if(!Path.GetFileName(path).Contains("_NPC_"))continue;
var root=PrefabUtility.LoadPrefabContents(path);try{var c=root.GetComponent<CitizenCombat>();if(c!=null){c.punchRange=2.8f;c.attackStartRange=2.6f;c.combatSpacing=2.2f;PrefabUtility.SaveAsPrefabAsset(root,path);}}finally{PrefabUtility.UnloadPrefabContents(root);}}
foreach(var c in UnityEngine.Object.FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None)){c.punchRange=2.8f;c.attackStartRange=2.6f;c.combatSpacing=2.2f;EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
File.WriteAllText("Temp/Mok-hospital-install.txt","PASS HOSPITAL bound: "+hospitals[0].position+" colliders="+hospitals[0].GetComponentsInChildren<Collider>().Length);
}
}}
#endif
