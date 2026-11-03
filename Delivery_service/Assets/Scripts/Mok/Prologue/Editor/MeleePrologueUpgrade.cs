#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class MeleePrologueUpgrade {
 static GameObject Bat(Transform parent){var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MokWeapons/Bat.prefab"),parent);o.name="야구배트";foreach(var c in o.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);return o;}
 public static void Cleanup(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/Melee-before-display-cleanup.unity",true);
  foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray())if(t&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)=="Assets/Resources/MokWeapons/Pistol.prefab"){
   var root=PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);if(root)UnityEngine.Object.DestroyImmediate(root);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  const string path="Assets/Resources/MokWeapons/Pistol.prefab";if(File.Exists(path)){File.Copy(path,"Logs/Melee-removed-Pistol.prefab",true);File.Copy(path+".meta","Logs/Melee-removed-Pistol.prefab.meta",true);AssetDatabase.DeleteAsset(path);}
  AssetDatabase.SaveAssets();File.WriteAllText("Logs/Melee-cleanup.txt","PASS removed shop gun display and Resources pistol without missing prefab references");
 }
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");
  var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);
  if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/Melee-before-prologue-"+stamp+".unity",true);
  var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
  var player=d.player;var old=player.weapon;
  var weapon=Bat(old.transform.parent);weapon.transform.SetLocalPositionAndRotation(new Vector3(.32f,-.35f,.65f),Quaternion.Euler(15,0,-18));
  player.weapon=weapon;player.muzzle=null;player.magazine=null;player.bolt=null;player.gunFeedback=null;
  UnityEngine.Object.DestroyImmediate(old);
  foreach(var o in d.officers){var oldGun=o.gun;var bat=Bat(o.transform);o.gun=bat.transform;o.gunFeedback=null;o.muzzle=null;
   if(o.animator&&o.animator.isHuman){var hand=o.animator.GetBoneTransform(HumanBodyBones.RightHand);bat.transform.SetPositionAndRotation(hand.position,o.transform.rotation*Quaternion.Euler(-20,0,15));}
   if(oldGun)UnityEngine.Object.DestroyImmediate(oldGun.gameObject);
   if(!o.GetComponent<StunStars>())o.gameObject.AddComponent<StunStars>();
  }
  var performance=d.GetComponent<PrologueEscapePerformance>();if(performance){if(performance.partnerGun)UnityEngine.Object.DestroyImmediate(performance.partnerGun.gameObject);performance.partnerGun=null;performance.muzzle=null;performance.feedback=null;}
  foreach(var feedback in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrologueGunFeedback>(true)).ToArray()){
   if(feedback.flame)feedback.flame.gameObject.SetActive(false);if(feedback.flash)feedback.flash.enabled=false;UnityEngine.Object.DestroyImmediate(feedback);
  }
  // 역할을 유지하면서 사격 관련 대사만 근접 위협으로 바꾼다.
  var so=new SerializedObject(d.story);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.String){it.stringValue=it.stringValue.Replace("총 든 놈들","무장한 놈들").Replace("총성이","소란이").Replace("총성","싸우는 소리").Replace("사격","공격");}so.ApplyModifiedPropertiesWithoutUndo();
  EditorUtility.SetDirty(d.story);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var main=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);if(!main.isLoaded)main=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(main,"Logs/Melee-before-city-"+stamp+".unity",true);
  foreach(var c in main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VenueRoadInterception>(true)))c.guardGun=null;
  int removed=0;
  foreach(var m in main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).ToArray()){
   var path=AssetDatabase.GetAssetPath(m.sharedMesh);if(path.Contains("Low Poly Weapons")||path.Contains("MokWeapons/Pistol")){m.gameObject.SetActive(false);removed++;}
  }
  EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/Melee-apply.txt","PASS saved scene bats, no prologue firearms, no ammo shop or police shooting; hidden city firearm meshes: "+removed);
 }
}
}
#endif
