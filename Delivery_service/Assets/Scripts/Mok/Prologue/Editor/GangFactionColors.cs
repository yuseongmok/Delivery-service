#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class GangFactionColors {
 const string Folder="Assets/Scripts/Mok/Prologue/Art/Yamazaki";
 static GangFactionColors(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/GangColors-request"))return;File.Delete("Temp/GangColors-request");try{Apply();}catch(Exception e){File.WriteAllText("Logs/Gang-colors.txt",e.ToString());}}
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/Before-gang-colors-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var director=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
  var officers=director.officers.Concat(director.roadPolice).Where(o=>o).Distinct().ToArray();
  var map=new Dictionary<Material,Material>();string report="Yamazaki clothing palettes\n";int renderers=0;
  foreach(var officer in officers)foreach(var renderer in officer.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
   var mats=renderer.sharedMaterials;
   for(int i=0;i<mats.Length;i++){
    var source=mats[i];if(!source||AssetDatabase.GetAssetPath(source).StartsWith(Folder))continue;
    if(!map.TryGetValue(source,out var changed)){
     string property=source.HasProperty("_Albedo_Map")?"_Albedo_Map":source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";var texture=source.GetTexture(property);if(!texture)continue;
     string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
     var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;Graphics.Blit(texture,rt);RenderTexture.active=rt;
     var copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);copy.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
     var pixels=copy.GetPixels();int count=0;
     // 원본 팔레트의 차가운 셔츠 색만 바꿔 피부·머리·장비는 보존한다.
     for(int p=0;p<pixels.Length;p++){var color=pixels[p];Color.RGBToHSV(color,out float h,out float s,out float v);if(h>=.45f&&h<=.73f&&s>.10f&&v>.12f){var tint=Color.HSVToRGB(.97f,Mathf.Max(.48f,s),v*.76f);tint.a=color.a;pixels[p]=tint;count++;}}
     copy.SetPixels(pixels);copy.Apply();string texPath=Folder+"/"+key+".png";File.WriteAllBytes(texPath,copy.EncodeToPNG());UnityEngine.Object.DestroyImmediate(copy);AssetDatabase.ImportAsset(texPath);
     var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
     string matPath=Folder+"/"+key+".mat";changed=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!changed){changed=new Material(source);AssetDatabase.CreateAsset(changed,matPath);}changed.CopyPropertiesFromMaterial(source);changed.name="Yamazaki burgundy clothing";
     var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);changed.SetTexture(property,tex);EditorUtility.SetDirty(changed);map[source]=changed;report+=source.name+" recolored pixels="+count+"\n";
    }
    mats[i]=changed;
   }
   renderer.sharedMaterials=mats;EditorUtility.SetDirty(renderer);renderers++;
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  report+="officers="+officers.Length+" skin renderers="+renderers+"\nOriginal materials unchanged; city scene unchanged.\n";
  File.WriteAllText("Logs/Gang-colors.txt",report);
  if(opened)EditorSceneManager.CloseScene(scene,true);
 }
}
}
#endif

