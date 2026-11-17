#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] static class PrisonDuelUpgrade {
 static PrisonDuelUpgrade(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonDuelUpgrade"))return;File.Delete("Temp/PrisonDuelUpgrade");var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-auto-counter-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))return;
 var d=Object.FindFirstObjectByType<PrisonMedicineDuel>();d.bossSpot.position=d.playerSpot.position+d.playerSpot.forward*1.12f;var s=d.settings;
 s.challenge=new[]{"나: 김정민이지? 수면제를 좀 나눠 줬으면 하는데.","김정민: 싫은데? 신입이 감히 누구한테 내놓으라는 거야. 꺼져.","나: 말로는 안 되겠네. 그럼 주먹으로 해결하자.","민기: 운동장으로 나와. 다른 놈들은 끼어들지 말고.","죄수: 싸움이야! 김정민이 신입과 싸운대!"};
 s.arena=new[]{"죄수: 비켜, 비켜! 둘이 붙게 자리 만들어!","김정민: 겁도 없군. 어디 한번 버텨 봐.","민기: 어깨를 잘 봐! 반대로 피하고 바로 받아쳐!"};s.won="민기: 됐어! 떨어진 수면제 챙기고 가자.";s.tutorial="Q 왼쪽 · E 오른쪽 — 공격 직전에 반대로 회피\n회피에 성공하면 자동으로 반격합니다.";s.counter="빈틈이다! 자동 반격";s.slowSeconds=.5f;EditorUtility.SetDirty(s);
 var rig=d.boss.GetComponent<PrisonDuelRagdoll>();if(!rig)rig=d.boss.gameObject.AddComponent<PrisonDuelRagdoll>();rig.animator=d.presentation.bossAnimator;d.ragdoll=rig;
 var types=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg};
 var bodies=new List<Rigidbody>();var shapes=new List<Collider>();foreach(var type in types){var bone=rig.animator.GetBoneTransform(type);var body=bone.GetComponent<Rigidbody>();if(!body)body=bone.gameObject.AddComponent<Rigidbody>();body.mass=type==HumanBodyBones.Hips?8:2;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;var shape=bone.GetComponent<CapsuleCollider>();if(!shape)shape=bone.gameObject.AddComponent<CapsuleCollider>();var child=bone.childCount>0?bone.GetChild(0):null;Vector3 end=child?bone.InverseTransformPoint(child.position):Vector3.up*.2f;shape.center=end*.45f;shape.direction=Mathf.Abs(end.x)>Mathf.Abs(end.y)?0:1;shape.radius=type==HumanBodyBones.Head?.11f:type==HumanBodyBones.Hips?.14f:.075f;shape.height=Mathf.Max(shape.radius*2,end.magnitude);shape.enabled=false;bodies.Add(body);shapes.Add(shape);}
 foreach(var body in bodies){if(body==bodies[0])continue;var p=body.transform.parent;while(p&&!p.GetComponent<Rigidbody>())p=p.parent;if(!p)continue;var joint=body.GetComponent<CharacterJoint>();if(!joint)joint=body.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=p.GetComponent<Rigidbody>();joint.enableProjection=true;joint.projectionDistance=.1f;joint.lowTwistLimit=new SoftJointLimit{limit=-25};joint.highTwistLimit=new SoftJointLimit{limit=25};joint.swing1Limit=new SoftJointLimit{limit=45};joint.swing2Limit=new SoftJointLimit{limit=35};}
 rig.bodies=bodies.ToArray();rig.shapes=shapes.ToArray();EditorUtility.SetDirty(d);EditorUtility.SetDirty(rig);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-duel-upgrade.txt","Saved close duel, automatic counter dialogue, physical defeat rig. Preserved current scene edits.");}
}
}
#endif
