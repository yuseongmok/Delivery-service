#if UNITY_EDITOR
using System;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class GunReturnUpgrade {
 static Light Muzzle(Transform parent,Vector3 at){var go=new GameObject("총구 섬광");go.transform.SetParent(parent,false);go.transform.localPosition=at;var light=go.AddComponent<Light>();light.color=new Color(1,.65f,.2f);light.range=3;light.intensity=4;light.enabled=false;return light;}
 static PrologueGunFeedback Feedback(Light light,bool player){const string path="Assets/Scripts/Mok/Prologue/Art/CombatFeedback/";return (PrologueGunFeedback)typeof(BankPrologueBuilder).Assembly.GetType("DeliveryService.Mok.Editor.BankPrologueBuilder").GetMethod("InstallGunFeedback",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{light,player,AssetDatabase.LoadAssetAtPath<Mesh>(path+"MuzzleFlame.asset"),AssetDatabase.LoadAssetAtPath<Material>(path+"Flame orange.mat"),AssetDatabase.LoadAssetAtPath<Material>(path+"Flame core.mat"),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Art/SuppliedAudio/Gunshot.wav")});}
 public static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);EditorSceneManager.SaveScene(scene,"Logs/GunReturn-bank-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
  d.player.muzzle=Muzzle(d.player.weapon.transform,Vector3.zero);BankPrologueBuilder.InstallRifle(d.player);d.player.gunFeedback=Feedback(d.player.muzzle,true);
  foreach(var e in d.officers.Concat(d.roadPolice)){
   var old=e.gun;var gun=BankPrologueBuilder.CreateGun(e.transform);e.gun=gun.transform;e.muzzle=Muzzle(gun.transform,new Vector3(0,.05f,.23f));e.gunFeedback=Feedback(e.muzzle,false);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);ArmedHandPose.Apply(e.animator,e.gun,e.transform.position+e.transform.forward*10,false,false);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var main=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);if(!main.isLoaded)main=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);EditorSceneManager.SaveScene(main,"Logs/GunReturn-city-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  foreach(var e in main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CitizenCombat>(true)).Where(e=>e.usesFirearm)){
   var weapon=e.GetComponent<GangFirearm>()??e.gameObject.AddComponent<GangFirearm>();if(!weapon.gun)weapon.gun=BankPrologueBuilder.CreateGun(e.transform).transform;ArmedHandPose.Apply(e.GetComponentInChildren<Animator>(),weapon.gun,e.transform.position+e.transform.forward*10,false,false);
  }
  EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);AssetDatabase.SaveAssets();System.IO.File.WriteAllText("Logs/GunReturn-apply.txt","PASS saved rifle, gang pistols and city mission gun holders");
 }
}
}
#endif
