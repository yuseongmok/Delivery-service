#if UNITY_EDITOR
using System;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class GangSupportUpgrade {
 public static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);EditorSceneManager.SaveScene(scene,"Logs/GangSupport-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var d=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BankPrologue>(true)).Single();var p=d.GetComponent<PrologueEscapePerformance>();
  if(p.partnerGun)UnityEngine.Object.DestroyImmediate(p.partnerGun.gameObject);p.partnerGun=BankPrologueBuilder.CreateGun(d.getawayCar,"M4_8").transform;p.partnerGun.name="민기 후방 지원 소총";p.partnerGun.localPosition=new Vector3(.98f,1.17f,-.3f);p.partnerGun.localRotation=Quaternion.Euler(0,180,0);
  foreach(var c in p.partnerGun.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
  var muzzle=new GameObject("Support muzzle");muzzle.transform.SetParent(p.partnerGun,false);muzzle.transform.localPosition=new Vector3(0,.06f,.73f);p.muzzle=muzzle.transform;var flash=muzzle.AddComponent<Light>();flash.color=new Color(1,.6f,.2f);flash.range=3;flash.intensity=2;flash.enabled=false;
  var install=typeof(BankPrologueBuilder).GetMethod("InstallGunFeedback",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);const string art="Assets/Scripts/Mok/Prologue/Art/CombatFeedback/";
  p.feedback=(PrologueGunFeedback)install.Invoke(null,new object[]{flash,false,AssetDatabase.LoadAssetAtPath<Mesh>(art+"MuzzleFlame.asset"),AssetDatabase.LoadAssetAtPath<Material>(art+"Flame orange.mat"),AssetDatabase.LoadAssetAtPath<Material>(art+"Flame core.mat"),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Art/SuppliedAudio/Gunshot.wav")});
  p.pursuitCars=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ProloguePursuitCar>(true)).ToArray();foreach(var e in d.officers.Concat(d.roadPolice))ArmedHandPose.Apply(e.animator,e.gun,e.transform.position+e.transform.forward*10,false,false);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllText("Logs/GangSupport-apply.txt","PASS saved Mingi rifle, muzzle and pursuit targets");
 }
}
}
#endif
