#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class WaterRescueSetup {
 static WaterRescueSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/Mok-water-install-request"))return;File.Delete("Temp/Mok-water-install-request");try{Install();}catch(Exception e){File.WriteAllText("Temp/Mok-water-install.txt",e.ToString());Debug.LogException(e);}}
 static void Install(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("JJinCity만 설치할 수 있습니다.");
 if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-water-rescue.unity",true))throw new Exception("백업 실패");
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var water=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Single(x=>x.name=="Ocean_Plane");
 var rescue=UnityEngine.Object.FindFirstObjectByType<WaterRescue>();if(rescue==null)rescue=new GameObject("Water Rescue").AddComponent<WaterRescue>();
 rescue.player=p;rescue.bike=bike;rescue.water=water;rescue.hospital=p.hospital;
 if(rescue.hospital==null||!rescue.TryParking(p.hospital.position+p.hospital.right*4,out var spot))throw new Exception("병원 주변 안전 견인 위치를 찾지 못했습니다.");
 if(rescue.towPoint==null)rescue.towPoint=new GameObject("Hospital Tow Parking").transform;
 rescue.towPoint.SetParent(rescue.transform);rescue.towPoint.SetPositionAndRotation(spot,p.hospital.rotation);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Temp/Mok-water-install.txt",$"PASS: scene saved; water={water.bounds}; hospital={p.hospital.position}; parking={spot}");
 }
}
}
#endif
