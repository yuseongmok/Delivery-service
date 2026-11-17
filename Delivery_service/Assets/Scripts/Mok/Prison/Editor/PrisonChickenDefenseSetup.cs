#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] static class PrisonChickenDefenseSetup
    {
        static PrisonChickenDefenseSetup(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonChickenDefenseSetup"))return;
            File.Delete("Temp/PrisonChickenDefenseSetup");
            try{
                var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Open PrisonChapter first");
                if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-chicken-defense-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
                var mission=Object.FindFirstObjectByType<PrisonChickenMission>();
                var companion=mission.prison.introduction.mingi;
                var animator=companion.animator;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
                var sourceAnimator=source.GetComponentInChildren<Animator>(true);
                var targets=animator.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
                Transform Map(Transform t){if(!targets.TryGetValue(t.name,out var mapped))throw new Exception("Missing Mingi bone "+t.name);return mapped;}
                foreach(var body in sourceAnimator.GetComponentsInChildren<Rigidbody>(true)){
                    if(body.transform==source.transform)continue;var target=Map(body.transform);var copy=target.GetComponent<Rigidbody>();if(!copy)copy=target.gameObject.AddComponent<Rigidbody>();EditorUtility.CopySerialized(body,copy);copy.isKinematic=true;copy.interpolation=RigidbodyInterpolation.Interpolate;copy.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;copy.solverIterations=12;
                }
                foreach(var collider in sourceAnimator.GetComponentsInChildren<Collider>(true)){
                    if(collider.transform==source.transform)continue;var target=Map(collider.transform);var copy=(Collider)target.GetComponent(collider.GetType());if(!copy)copy=(Collider)target.gameObject.AddComponent(collider.GetType());EditorUtility.CopySerialized(collider,copy);copy.enabled=false;
                }
                foreach(var joint in sourceAnimator.GetComponentsInChildren<CharacterJoint>(true)){
                    var target=Map(joint.transform);var copy=target.GetComponent<CharacterJoint>();if(!copy)copy=target.gameObject.AddComponent<CharacterJoint>();EditorUtility.CopySerialized(joint,copy);copy.connectedBody=joint.connectedBody?Map(joint.connectedBody.transform).GetComponent<Rigidbody>():null;copy.enableProjection=true;copy.projectionDistance=.08f;copy.projectionAngle=15;
                }
                var rig=companion.GetComponent<PrisonCompanionRagdoll>();if(!rig)rig=companion.gameObject.AddComponent<PrisonCompanionRagdoll>();rig.companion=companion;rig.bodies=animator.GetComponentsInChildren<Rigidbody>(true);rig.colliders=animator.GetComponentsInChildren<Collider>(true);rig.hips=animator.GetBoneTransform(HumanBodyBones.Hips);
                if(rig.bodies.Length<6)throw new Exception("Incomplete Mingi physical rig: "+rig.bodies.Length);
                var defense=mission.GetComponent<PrisonChickenDefense>();if(!defense)defense=mission.gameObject.AddComponent<PrisonChickenDefense>();defense.mission=mission;defense.ragdoll=rig;mission.defense=defense;
                EditorUtility.SetDirty(mission);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/Prison-chicken-defense-setup.txt","PASS saved defense and Mingi ragdoll: "+rig.bodies.Length+" bodies. Only PrisonChapter changed.");
            }catch(Exception e){File.WriteAllText("Logs/Prison-chicken-defense-setup.txt",e.ToString());}
        }
    }
}
#endif


