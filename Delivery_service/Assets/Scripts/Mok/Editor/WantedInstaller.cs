#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class WantedInstaller {
static WantedInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-wanted-install"))return;
File.Delete("Temp/Mok-wanted-install");try{
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity");
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-wanted.unity",true))throw new Exception("Backup failed");
var inventory=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();var so=new SerializedObject(inventory);var field=so.FindProperty("currentTopping");var text=field.objectReferenceValue as Text;
if(text==null)throw new Exception("Topping HUD missing");
var hud=text.GetComponent<WantedHUD>();if(hud==null)hud=text.gameObject.AddComponent<WantedHUD>();hud.label=text;text.text="";text.color=new Color(1,.25f,.2f);text.raycastTarget=false;
field.objectReferenceValue=null;so.ApplyModifiedProperties();EditorUtility.SetDirty(text);EditorUtility.SetDirty(hud);
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-wanted-install.txt","PASS: Existing topping HUD replaced by wanted warning; inventory reference cleared; scene backed up and saved.");
}catch(Exception e){File.WriteAllText("Temp/Mok-wanted-install.txt",e.ToString());}}
}}
#endif
