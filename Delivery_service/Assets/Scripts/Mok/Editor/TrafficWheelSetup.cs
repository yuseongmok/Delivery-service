#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class TrafficWheelSetup {
 static TrafficWheelSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-wheels-setup"))return;File.Delete("Temp/Mok-wheels-setup");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-wheels-result.txt",e.ToString());}}
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static void Run(){
 var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
 for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isLoaded)Check(EditorSceneManager.SaveScene(s,"Temp/Before-wheels-"+stamp+"-"+i+".unity",true),"Backup open scene");}
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;
 if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 Check(EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-wheels-"+stamp+".unity",true),"Backup JJinCity");
 string log="";
 try{
 foreach(var car in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarMov>(true))){
 var wheels=car.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("_Wheel_")).ToArray();Check(wheels.Length==4,car.name+" wheel count");
 var visual=car.GetComponent<TrafficWheelVisual>();if(visual==null)visual=car.gameObject.AddComponent<TrafficWheelVisual>();
 visual.wheels=wheels;visual.radii=wheels.Select(w=>w.GetComponent<Renderer>().bounds.extents.y).ToArray();Check(visual.radii.All(r=>r>.1f&&r<2),"Wheel radii");
 // 별도 시험 오브젝트에서 전진·정지·후진을 검사하고 실제 차량은 움직이지 않는다.
 var test=new GameObject("Wheel visual check");try{
 var component=test.AddComponent<TrafficWheelVisual>();component.wheels=new Transform[4];component.radii=visual.radii.ToArray();
 for(int i=0;i<4;i++){var w=new GameObject("wheel");w.transform.SetParent(test.transform);component.wheels[i]=w.transform;}
 var flags=BindingFlags.Instance|BindingFlags.NonPublic;var enable=typeof(TrafficWheelVisual).GetMethod("OnEnable",flags);var tick=typeof(TrafficWheelVisual).GetMethod("LateUpdate",flags);
 enable.Invoke(component,null);test.transform.position=Vector3.forward;tick.Invoke(component,null);
 var moved=component.wheels.Select(w=>w.localRotation).ToArray();Check(moved.All(q=>Quaternion.Angle(q,Quaternion.identity)>1),"All four wheels roll");
 for(int i=0;i<4;i++)Check(Quaternion.Angle(moved[i],Quaternion.AngleAxis(Mathf.Rad2Deg/component.radii[i],Vector3.right))<.01f,"Distance to angle");
 tick.Invoke(component,null);for(int i=0;i<4;i++)Check(Quaternion.Angle(moved[i],component.wheels[i].localRotation)<.01f,"Stopped wheels");
 test.transform.position=Vector3.zero;tick.Invoke(component,null);Check(component.wheels.All(w=>Quaternion.Angle(w.localRotation,Quaternion.identity)<.01f),"Reverse rolls back");
 }finally{UnityEngine.Object.DestroyImmediate(test);}
 EditorUtility.SetDirty(visual);PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
 log+="PASS "+car.name+": four wheels, distance/radius, stop, reverse. Radius="+string.Join(",",visual.radii)+"\n";
 }
 EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Save scene");File.WriteAllText("Temp/Mok-wheels-result.txt",log);
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
}}
#endif
