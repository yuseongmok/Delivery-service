#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class PrologueDrivingHandsBuilder {
 public static void Refine(){var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var r=d.drive.GetComponent<PrologueDrivingHands>();r.arms.localPosition=new Vector3(-.46f,-.35f,.15f);foreach(var t in new[]{r.leftGrip,r.rightGrip}){float side=t==r.leftGrip?-1:1;t.localPosition=new Vector3(side*.27f,.045f,-.015f);t.localRotation=Quaternion.Euler(side>0?180:0,0,180);}
 foreach(var hand in new[]{r.leftHand,r.rightHand})foreach(var t in hand.GetComponentsInChildren<Transform>()){if(t==hand)continue;if(t.name.Contains("Finger")&&!t.name.EndsWith("04"))t.localRotation=Quaternion.Euler(0,0,-1*(t.name.EndsWith("01")?35:65));}
 r.leftHand.localScale=r.rightHand.localScale=Vector3.one*.8f;r.Pose();EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);PrologueCameraUpgrade.InspectDriving();}
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var scene=d.gameObject.scene;
 if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-driving-hands-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
 var rig=d.drive.GetComponent<PrologueDrivingHands>();if(rig)throw new Exception("Driving hands already installed");
 rig=d.drive.gameObject.AddComponent<PrologueDrivingHands>();rig.drive=d.drive;rig.wheel=d.getawayCar.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="SM_Veh_Car_Muscle_SteeringW");rig.wheelRest=rig.wheel.localRotation;
 var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_Hoodie.prefab"),d.getawayCar);
 g.name="Driver arms - Synty hoodie";rig.arms=g.transform;g.transform.localPosition=new Vector3(-.46f,-.35f,-.24f);g.transform.localRotation=Quaternion.identity;
 var anim=g.GetComponentInChildren<Animator>();anim.enabled=false;
 rig.leftUpper=anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);rig.leftLower=anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);rig.leftHand=anim.GetBoneTransform(HumanBodyBones.LeftHand);
 rig.rightUpper=anim.GetBoneTransform(HumanBodyBones.RightUpperArm);rig.rightLower=anim.GetBoneTransform(HumanBodyBones.RightLowerArm);rig.rightHand=anim.GetBoneTransform(HumanBodyBones.RightHand);
 foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>()){
 var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);var weights=mesh.boneWeights;
 bool Arm(int i){var b=r.bones[i];return b==rig.leftUpper||b.IsChildOf(rig.leftUpper)||b==rig.rightUpper||b.IsChildOf(rig.rightUpper);}
 bool Keep(int i){var w=weights[i];float sum=(Arm(w.boneIndex0)?w.weight0:0)+(Arm(w.boneIndex1)?w.weight1:0)+(Arm(w.boneIndex2)?w.weight2:0)+(Arm(w.boneIndex3)?w.weight3:0);return sum>.5f;}
 for(int sub=0;sub<mesh.subMeshCount;sub++){var src=mesh.GetTriangles(sub);var keep=new List<int>();for(int i=0;i<src.Length;i+=3)if(Keep(src[i])&&Keep(src[i+1])&&Keep(src[i+2])){keep.Add(src[i]);keep.Add(src[i+1]);keep.Add(src[i+2]);}mesh.SetTriangles(keep,sub);}
 var path="Assets/Scripts/Mok/Art/SyntyStory/DriverArms-"+r.name+".asset";AssetDatabase.CreateAsset(mesh,path);r.sharedMesh=mesh;r.updateWhenOffscreen=true;
 }
 foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
 Transform Grip(string name,float side){var t=new GameObject(name).transform;t.SetParent(rig.wheel,false);t.localPosition=new Vector3(side*.205f,-.065f,-.015f);return t;}
 rig.leftGrip=Grip("Left hand grip",-1);rig.rightGrip=Grip("Right hand grip",1);rig.Pose();
 EditorUtility.SetDirty(rig);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Driving-hands-apply.txt","Saved Synty arms and wheel grips. Left distance "+Vector3.Distance(rig.leftHand.position,rig.leftGrip.position)+" right "+Vector3.Distance(rig.rightHand.position,rig.rightGrip.position));
 }
}}
#endif
