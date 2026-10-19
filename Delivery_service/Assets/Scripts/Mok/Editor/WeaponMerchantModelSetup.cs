#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class WeaponMerchantModelSetup {
 const string Dir="Assets/Scripts/Mok/Art/Weapons/";const string Prefab=Dir+"WeaponMerchantVisual.prefab";
 static WeaponMerchantModelSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-merchant-model"))return;File.Delete("Temp/Mok-merchant-model");Run();}
 public static void ApplyVisual(Transform merchant){foreach(Transform child in merchant.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));model.name="시민 모델 상인";model.transform.SetParent(merchant,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;var animator=model.GetComponentInChildren<Animator>();var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));idle.SampleAnimation(model,0);}
 static void Run(){var old=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
 if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-merchant-model.unity",true))throw new Exception("Backup failed");var merchant=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WeaponMerchant>(true)).Single();
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");var model=UnityEngine.Object.Instantiate(source);model.name="Weapon Merchant Visual";
 // 시민 외형과 뼈대만 사용하고 배회·소매치기·전투는 상인에게 붙이지 않는다.
 foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true).OrderBy(c=>c is NPCControl?1:0).ToArray())UnityEngine.Object.DestroyImmediate(script);
 foreach(var joint in model.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
 foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
 foreach(var col in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
 foreach(var agent in model.GetComponentsInChildren<NavMeshAgent>(true))UnityEngine.Object.DestroyImmediate(agent);
 foreach(var t in model.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=13;t.gameObject.tag="Untagged";}
 var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var path=Dir+"MerchantIdle.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(path);var state=controller.layers[0].stateMachine.AddState("상인 대기");state.motion=idle;controller.layers[0].stateMachine.defaultState=state;}var anim=model.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;idle.SampleAnimation(model,0);
 PrefabUtility.SaveAsPrefabAsset(model,Prefab);UnityEngine.Object.DestroyImmediate(model);ApplyVisual(merchant.transform);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Temp/Mok-merchant-model.txt","PASS: Existing Male_Adult_NPC_1 visual and idle animation saved. Merchant component, collider and transform preserved; civilian AI removed from visual clone only.");
 }catch(Exception e){File.WriteAllText("Temp/Mok-merchant-model.txt",e.ToString());Debug.LogException(e);}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif
