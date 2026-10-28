#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class VenueEntryGuardRepair {
 public static void Apply(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();var root=m.entryGuard;EditorSceneManager.SaveScene(root.gameObject.scene,"Logs/Venue-before-entry-guard-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var oldAnimator=root.GetComponentInChildren<Animator>(true);var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);string report=string.Join("\n",renderers.Select(r=>r.name+" mesh="+r.sharedMesh.name+" bounds="+r.bounds+" head="+string.Join(",",r.bones.Where(b=>b&&b.name.ToLower().Contains("head")).Select(b=>b.name+":"+b.lossyScale))));
 // 변환된 메시 대신 원본 Synty 메시와 뼈대를 한 세트로 사용한다.
 foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>(true))a.enabled=false;
 var previous=root.Find("Original Synty entry guard");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
 var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab"));g.name="Original Synty entry guard";g.transform.SetPositionAndRotation(root.position,root.rotation);var rs=g.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);g.transform.localScale*=1.8f/bounds.size.y;bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);g.transform.position+=Vector3.up*(root.position.y-bounds.min.y);g.transform.SetParent(root,true);
 foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);var animator=g.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
 foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
 EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);File.WriteAllText("Logs/Venue-entry-guard-repair.txt",report+"\nSaved original Synty skin and native rig, height 1.8m.");
 }
}
}
#endif
