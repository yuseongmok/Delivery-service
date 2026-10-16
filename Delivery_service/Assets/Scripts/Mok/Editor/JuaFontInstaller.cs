#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;using TMPro;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class JuaFontInstaller {
static JuaFontInstaller(){EditorApplication.update+=Poll;}
static void Poll(){
if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-jua-install"))return;
File.Delete("Temp/Mok-jua-install");
try{
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity first");
var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
var tmp=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Jua_Navigation_SDF.asset");
if(font==null||tmp==null)throw new Exception("Jua assets missing");
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-jua.unity",true))throw new Exception("Backup failed");
const string dir="Assets/Scripts/Mok/Resources";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
var settings=AssetDatabase.LoadAssetAtPath<MokUIFont>(dir+"/MokUIFont.asset");
if(settings==null){settings=ScriptableObject.CreateInstance<MokUIFont>();AssetDatabase.CreateAsset(settings,dir+"/MokUIFont.asset");}
settings.jua=font;EditorUtility.SetDirty(settings);
int texts=0,tmps=0,refs=0;
foreach(var root in scene.GetRootGameObjects()){
foreach(var t in root.GetComponentsInChildren<Text>(true)){t.font=font;EditorUtility.SetDirty(t);texts++;}
foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){t.font=tmp;EditorUtility.SetDirty(t);tmps++;}
foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true)){
if(b==null)continue;var script=MonoScript.FromMonoBehaviour(b);if(!AssetDatabase.GetAssetPath(script).Contains("/Mok/"))continue;
var so=new SerializedObject(b);var prop=so.GetIterator();bool changed=false;
while(prop.NextVisible(true))if(prop.propertyType==SerializedPropertyType.ObjectReference&&prop.objectReferenceValue is Font){prop.objectReferenceValue=font;changed=true;refs++;}
if(changed)so.ApplyModifiedProperties();
}
}
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
int wrong=0;foreach(var root in scene.GetRootGameObjects()){
foreach(var t in root.GetComponentsInChildren<Text>(true))if(t.font!=font)wrong++;
foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))if(t.font!=tmp)wrong++;
}
File.WriteAllText("Temp/Mok-jua-install.txt",$"Text: {texts}; TMP: {tmps}; Mok font references: {refs}; wrong fonts: {wrong}; runtime Jua: {MokUIFont.Font==font}; scene saved; backup preserved.");
}catch(Exception e){File.WriteAllText("Temp/Mok-jua-install.txt",e.ToString());}
}
}}
#endif
