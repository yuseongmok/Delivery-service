#if UNITY_EDITOR
using System.IO;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class FuelSignFix{
static FuelSignFix(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-fuel-sign"))return;File.Delete("Temp/Mok-fuel-sign");const string path="Assets/Scripts/Mok/Art/FuelStation/FuelStation.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var t in root.GetComponentsInChildren<TextMesh>())t.transform.localRotation=Quaternion.identity;PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-fuel-sign.txt","PASS text orientation fixed");}finally{PrefabUtility.UnloadPrefabContents(root);}}
}}
#endif
