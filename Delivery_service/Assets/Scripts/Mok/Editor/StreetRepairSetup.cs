#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class StreetRepairSetup {
 static bool Sidewalk(Vector3 p)=>Physics.RaycastAll(p+Vector3.up*.3f,Vector3.down,.7f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name.Contains("Sidewalk")&&Mathf.Abs(h.point.y-p.y)<.15f);
 public static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=StoryTutorialBuilder.Main||EditorApplication.isPlaying)throw new Exception("JJinCity edit mode required");
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-street-repair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var s=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();var m=UnityEngine.Object.FindFirstObjectByType<BackdoorMission>();var report=s.returnPoint.position;
  s.transform.position=new Vector3(7.55f,.04f,115.5f);s.returnPoint.position=report;s.transform.Find("Reception canopy").localScale=new Vector3(2.6f,.16f,2.2f);
  var footprint=s.transform.Find("Reception navigation footprint").GetComponent<NavMeshObstacle>();footprint.size=new Vector3(2.6f,2,2.2f);
  var cover=m.transform.Find("Loading crates cover");cover.SetPositionAndRotation(new Vector3(8,.94f,128),Quaternion.identity);
  cover.GetComponent<NavMeshObstacle>().size=new Vector3(1.55f,1,1.4f);
  m.transform.Find("Loading bay label").SetPositionAndRotation(new Vector3(7.27f,1.35f,128),Quaternion.Euler(0,90,0));
  m.surveyTargets[1].position=new Vector3(8,1.14f,129.4f);m.standPoints[1].position=new Vector3(8,.04f,129.65f);
  m.standPoints[0].position=new Vector3(7,.04f,121);m.guards[0].posts[0].position=new Vector3(6.6f,.04f,120);m.guards[0].posts[1].position=new Vector3(6.6f,.04f,125);m.guards[0].transform.position=m.guards[0].posts[0].position;
  Physics.SyncTransforms();var evidence=new List<string>();foreach(var root in new[]{s.transform,m.transform})foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
   if(r.name!="Reception canopy"&&r.name!="Catering counter"&&r.name!="Loading crates cover")continue;var b=r.bounds;foreach(float x in new[]{b.min.x,b.max.x})foreach(float z in new[]{b.min.z,b.max.z}){var p=new Vector3(x,0,z);if(!Sidewalk(p))throw new Exception("Prop crosses sidewalk: "+r.name+" "+p);}evidence.Add("PASS sidewalk footprint "+r.name);
  }
  const string path="Assets/Resources/MokWeapons/Pistol.prefab";File.Copy(path,"Logs/Pistol-before-asset-replacement-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".prefab",true);
  var pistol=PrefabUtility.LoadPrefabContents(path);try{
   foreach(var t in pistol.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab"),pistol.transform);model.transform.localPosition=new Vector3(0,.27f,.07f);model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=Vector3.one*1.2f;
   var materialPath="Assets/Scripts/Mok/Art/Weapons/M1911 URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){var source=model.GetComponentInChildren<Renderer>().sharedMaterial;mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetTexture("_BaseMap",source.mainTexture);mat.SetColor("_BaseColor",source.color);mat.SetFloat("_Metallic",.3f);mat.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(mat,materialPath);}
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();PrefabUtility.SaveAsPrefabAsset(pistol,path);
  }finally{PrefabUtility.UnloadPrefabContents(pistol);}
  var water=UnityEngine.Object.FindFirstObjectByType<WaterRescue>();var recall=water.player.GetComponent<BikeRecall>();if(!recall)recall=water.player.gameObject.AddComponent<BikeRecall>();recall.player=water.player;recall.bike=water.bike;recall.water=water;
  EditorUtility.SetDirty(s);EditorUtility.SetDirty(m);EditorUtility.SetDirty(recall);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();evidence.Add("PASS M1911 prefab and owned-bike recall saved");File.WriteAllLines("Logs/Street-repair-apply.txt",evidence);SpecialOrderBuilder.Preview();BackdoorBuilder.Preview();
 }
 public static void Inspect(){
  var lines=new List<string>();var m=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();lines.Add("Reception "+m.transform.position);
  for(int x=-8;x<=20;x+=2)for(int z=106;z<=140;z+=2){var p=new Vector3(x,8,z);var hits=Physics.RaycastAll(p,Vector3.down,12,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();if(hits.Length>0)lines.Add(x+","+z+" "+string.Join(" / ",hits.Take(3).Select(h=>h.collider.name+" y="+h.point.y.ToString("F2"))));}
  var gun=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab"));foreach(var r in gun.GetComponentsInChildren<Renderer>())lines.Add("GUN "+r.name+" "+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(a=>a.name+":"+a.shader.name)));UnityEngine.Object.DestroyImmediate(gun);File.WriteAllLines("Logs/Street-repair-inspect.txt",lines);
 }
}
}
#endif
