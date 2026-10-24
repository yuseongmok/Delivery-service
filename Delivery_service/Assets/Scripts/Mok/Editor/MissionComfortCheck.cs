#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class MissionComfortCheck {
 public static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity first");
  EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-comfort-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var m=UnityEngine.Object.FindFirstObjectByType<MechanicMission>();var apron=m.mechanic.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Mechanic tool apron");var parent=apron.parent;
  apron.position=parent.position+m.mechanic.forward*.13f-Vector3.up*.12f;apron.rotation=m.mechanic.rotation;
  var scale=parent.lossyScale;apron.localScale=new Vector3(.27f/Mathf.Abs(scale.x),.32f/Mathf.Abs(scale.y),.04f/Mathf.Abs(scale.z));
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Logs/Mission-comfort-apply.txt","PASS apron world bounds "+apron.GetComponent<Renderer>().bounds+"; scene saved after backup");
 }
 public static void Audit(){
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity first");var lines=new List<string>();var garage=GameObject.Find("repair_shop");
  lines.Add("GARAGE "+garage.transform.position);
  foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>Vector3.Distance(r.bounds.center,garage.transform.position)<25)){
   string path=r.name;for(var t=r.transform.parent;t;t=t.parent)path=t.name+"/"+path;
   lines.Add(path+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" bounds="+r.bounds+" collider="+(r.GetComponent<Collider>()!=null));
  }
  File.WriteAllLines("Logs/Mission-comfort-audit.txt",lines);
 }
}}
#endif
