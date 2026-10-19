#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PlayerRagdollSetup {
 static PlayerRagdollSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-ragdoll-setup"))return;File.Delete("Temp/Mok-ragdoll-setup");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-ragdoll-setup.txt",e.ToString());}}
 static void Run(){
 string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
 for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isLoaded&&!EditorSceneManager.SaveScene(s,"Temp/Before-ragdoll-"+stamp+"-"+i+".unity",true))throw new Exception("Scene backup failed");}
 const string path="Assets/Scripts/Mok/Art/Player/DeliveryPlayer.prefab";
 File.Copy(path,"Temp/Before-ragdoll-"+stamp+".prefab");
 var root=PrefabUtility.LoadPrefabContents(path);
 try{
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
 var transforms=root.GetComponentsInChildren<Transform>().ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,root.transform));
 Transform Map(Transform t)=>transforms[AnimationUtility.CalculateTransformPath(t,source.transform)];
 foreach(var rb in source.GetComponentsInChildren<Rigidbody>()){
 if(rb.transform==source.transform)continue;
 var target=Map(rb.transform);var copy=target.GetComponent<Rigidbody>();if(copy==null)copy=target.gameObject.AddComponent<Rigidbody>();EditorUtility.CopySerialized(rb,copy);
 copy.isKinematic=true;copy.interpolation=RigidbodyInterpolation.Interpolate;copy.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;copy.solverIterations=12;copy.solverVelocityIterations=4;
 }
 foreach(var col in source.GetComponentsInChildren<Collider>()){
 if(col.transform==source.transform)continue;
 var target=Map(col.transform);var copy=(Collider)target.GetComponent(col.GetType());if(copy==null)copy=(Collider)target.gameObject.AddComponent(col.GetType());EditorUtility.CopySerialized(col,copy);copy.enabled=false;
 }
 foreach(var joint in source.GetComponentsInChildren<CharacterJoint>()){
 var target=Map(joint.transform);var copy=target.GetComponent<CharacterJoint>();if(copy==null)copy=target.gameObject.AddComponent<CharacterJoint>();EditorUtility.CopySerialized(joint,copy);
 copy.connectedBody=joint.connectedBody!=null?Map(joint.connectedBody.transform).GetComponent<Rigidbody>():null;
 copy.autoConfigureConnectedAnchor=false;if(copy.connectedBody!=null)copy.connectedAnchor=copy.connectedBody.transform.InverseTransformPoint(copy.transform.TransformPoint(copy.anchor));
 copy.enableProjection=true;copy.projectionDistance=.08f;copy.projectionAngle=15;
 }
 var avatar=root.GetComponent<PlayerAvatar>();var ragdoll=root.GetComponent<PlayerRagdoll>();if(ragdoll==null)ragdoll=root.AddComponent<PlayerRagdoll>();
 ragdoll.avatar=avatar;avatar.ragdoll=ragdoll;ragdoll.bodies=root.GetComponentsInChildren<Rigidbody>();ragdoll.colliders=root.GetComponentsInChildren<Collider>();ragdoll.hips=avatar.animator.GetBoneTransform(HumanBodyBones.Hips);
 if(!ragdoll.IsReady)throw new Exception("Missing ragdoll bodies");
 PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
 File.WriteAllText("Temp/Mok-ragdoll-setup.txt","PASS bodies="+ragdoll.bodies.Length+" colliders="+ragdoll.colliders.Length+" joints="+root.GetComponentsInChildren<CharacterJoint>().Length);
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}}
#endif
