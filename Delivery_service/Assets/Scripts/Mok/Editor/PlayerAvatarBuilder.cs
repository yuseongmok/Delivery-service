#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.AI;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PlayerAvatarBuilder {
 const string Dir="Assets/Scripts/Mok/Art/Player/";
 static PlayerAvatarBuilder(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-player-build"))return;File.Delete("Temp/Mok-player-build");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-player-build.txt",e.ToString());Debug.LogException(e);}}
 static Material Mat(string name,Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.color=c;m.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(m,Dir+name+".mat");return m;}
 static void Curve(AnimationClip clip,string property,params float[] values){float[] times={0,.12f,.22f,.30f,.48f,.65f};var curve=new AnimationCurve();for(int i=0;i<values.Length;i++)curve.AddKey(times[i],values[i]);for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curve);}
 static AnimationClip Pose(AnimationClip idle,string name){var c=UnityEngine.Object.Instantiate(idle);c.name=name;foreach(var b in AnimationUtility.GetCurveBindings(c)){var source=AnimationUtility.GetEditorCurve(c,b);float value=source.Evaluate(0);AnimationUtility.SetEditorCurve(c,b,AnimationCurve.Constant(0,.65f,value));}var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=false;settings.stopTime=.65f;AnimationUtility.SetAnimationClipSettings(c,settings);return c;}
 static void Build(){
 if(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"DeliveryPlayer.prefab")!=null)throw new Exception("Player prefab already exists; inspect before rebuilding.");
 string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
 for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isLoaded&&!EditorSceneManager.SaveScene(s,"Temp/Player-before-"+stamp+"-"+i+".unity",true))throw new Exception("Scene backup failed");}
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;
 if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-player-"+stamp+".unity",true))throw new Exception("Game scene backup failed");
 var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerCombat>(true)).Single();
 Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
 var orange=Mat("Delivery jacket",new Color(.86f,.34f,.12f));var navy=Mat("Delivery trousers",new Color(.065f,.10f,.14f));var skin=Mat("Skin",new Color(.68f,.46f,.31f));var boots=Mat("Boots",new Color(.035f,.045f,.05f));var cream=Mat("Reflective trim",new Color(.91f,.86f,.68f));
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
 var model=UnityEngine.Object.Instantiate(source);model.name="Delivery Player";
 // 시민 AI와 관절 물리는 복사하지 않고 기존 플레이어 충돌 시스템을 유지한다.
 for(int pass=0;pass<3;pass++)foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
 foreach(var joint in model.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
 foreach(var rb in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
 foreach(var col in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
 foreach(var agent in model.GetComponentsInChildren<NavMeshAgent>(true))UnityEngine.Object.DestroyImmediate(agent);
 foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
 var animator=model.GetComponentInChildren<Animator>();if(!animator.isHuman)throw new Exception("Humanoid required");
 var renderer=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=renderer.sharedMesh;var triangles=mesh.triangles;var weights=mesh.boneWeights;
 int Slot(int vertex){var w=weights[vertex];int b=w.boneIndex0;float best=w.weight0;if(w.weight1>best){best=w.weight1;b=w.boneIndex1;}if(w.weight2>best){best=w.weight2;b=w.boneIndex2;}if(w.weight3>best)b=w.boneIndex3;string n=renderer.bones[b].name;return n.Contains("Foot")||n.Contains("Toe")?3:n.Contains("Leg")||n=="Hips"?1:n.Contains("Head")||n.Contains("Neck")||n.Contains("Hand")||n.Contains("Finger")?2:0;}
 var upper=new List<int>[4];var lower=new List<int>[4];for(int i=0;i<4;i++){upper[i]=new List<int>();lower[i]=new List<int>();}
 for(int i=0;i<triangles.Length;i+=3){int a=Slot(triangles[i]),b=Slot(triangles[i+1]),c=Slot(triangles[i+2]);int slot=a==b||a==c?a:b;var list=(slot==1||slot==3?lower:upper)[slot];list.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
 SkinnedMeshRenderer Part(string name,List<int>[] lists){var part=UnityEngine.Object.Instantiate(renderer.gameObject,renderer.transform.parent).GetComponent<SkinnedMeshRenderer>();part.name=name;var copy=UnityEngine.Object.Instantiate(mesh);copy.name=name;copy.subMeshCount=4;for(int i=0;i<4;i++)copy.SetTriangles(lists[i],i);AssetDatabase.CreateAsset(copy,Dir+name+".asset");part.sharedMesh=copy;part.sharedMaterials=new[]{orange,navy,skin,boots};part.updateWhenOffscreen=true;return part;}
 var torso=Part("Jacket and head",upper);var legs=Part("Trousers and boots",lower);UnityEngine.Object.DestroyImmediate(renderer.gameObject);
 var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
 var walk=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Walking.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
 var kick=Pose(idle,"Delivery front kick");
 Curve(kick,"Right Upper Leg Front-Back",0,.65f,1f,1f,.3f,0);
 Curve(kick,"Right Lower Leg Stretch",.7f,-.7f,1f,1f,.1f,.7f);
 Curve(kick,"Right Foot Up-Down",0,-.25f,.3f,.3f,0,0);
 Curve(kick,"Right Arm Front-Back",0,.3f,.4f,.4f,.1f,0);
 Curve(kick,"Left Arm Front-Back",0,.25f,.4f,.4f,.1f,0);
 Curve(kick,"Spine Front-Back",0,-.08f,-.15f,-.15f,0,0);
 AssetDatabase.CreateAsset(kick,Dir+"FrontKick.anim");
 var tumble=Pose(idle,"Tumble pose");Curve(tumble,"Right Upper Leg Front-Back",.45f,.45f,.45f,.45f,.45f,.45f);Curve(tumble,"Left Upper Leg Front-Back",.25f,.25f,.25f,.25f,.25f,.25f);Curve(tumble,"Right Lower Leg Stretch",-.3f,-.3f,-.3f,-.3f,-.3f,-.3f);Curve(tumble,"Left Lower Leg Stretch",-.2f,-.2f,-.2f,-.2f,-.2f,-.2f);AssetDatabase.CreateAsset(tumble,Dir+"Tumble.anim");
 var controller=AnimatorController.CreateAnimatorControllerAtPath(Dir+"Player.controller");controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
 var sm=controller.layers[0].stateMachine;var stand=sm.AddState("Idle");stand.motion=idle;sm.defaultState=stand;var walking=sm.AddState("Walk");walking.motion=walk;sm.AddState("Kick").motion=kick;sm.AddState("Tumble").motion=tumble;
 var go=stand.AddTransition(walking);go.hasExitTime=false;go.duration=.12f;go.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
 var stop=walking.AddTransition(stand);stop.hasExitTime=false;stop.duration=.12f;stop.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
 animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 idle.SampleAnimation(model,0);
 var upperRenderers=new List<Renderer>{torso};
 void Accessory(string name,HumanBodyBones bone,Vector3 position,Vector3 size,Material mat,PrimitiveType type){var o=GameObject.CreatePrimitive(type);o.name=name;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.transform.SetParent(model.transform,false);o.transform.localPosition=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;o.transform.SetParent(animator.GetBoneTransform(bone),true);upperRenderers.Add(o.GetComponent<Renderer>());}
 Accessory("Delivery helmet",HumanBodyBones.Head,new Vector3(0,1.70f,0),new Vector3(.31f,.12f,.30f),orange,PrimitiveType.Sphere);
 Accessory("Helmet brim",HumanBodyBones.Head,new Vector3(0,1.69f,.16f),new Vector3(.28f,.025f,.13f),navy,PrimitiveType.Cube);
 Accessory("Reflective chest stripe",HumanBodyBones.Chest,new Vector3(0,1.29f,.18f),new Vector3(.31f,.035f,.02f),cream,PrimitiveType.Cube);
 var avatar=model.AddComponent<PlayerAvatar>();avatar.animator=animator;avatar.upperBody=upperRenderers.ToArray();
 var prefab=PrefabUtility.SaveAsPrefabAsset(model,Dir+"DeliveryPlayer.prefab");UnityEngine.Object.DestroyImmediate(model);
 var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);placed.transform.SetParent(player.transform,false);
 var cc=player.GetComponent<CharacterController>();float height=cc!=null?cc.height:1.8f;placed.transform.localScale=Vector3.one*(height/1.8f);placed.transform.localPosition=cc!=null?cc.center-Vector3.up*height*.5f:Vector3.zero;
 avatar=placed.GetComponent<PlayerAvatar>();avatar.combat=player;player.avatar=avatar;
 foreach(var r in player.GetComponents<Renderer>())r.enabled=false;
 if(player.kickVisual!=null)player.kickVisual.gameObject.SetActive(false);
 EditorUtility.SetDirty(player);PrefabUtility.RecordPrefabInstancePropertyModifications(player);PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText("Temp/Mok-player-build.txt","PASS: Humanoid citizen rig reused. Scene and prefab saved.\nPlayer="+player.name+" height="+height+" bodyOffset="+placed.transform.localPosition+"\nBackup stamp="+stamp);
 if(opened)EditorSceneManager.CloseScene(scene,true);
 }
}
}
#endif
