#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class PoliceArtBuilder {
const string Prefab="Assets/Scripts/Min/Prefab/NPC/Police.prefab",Dir="Assets/Scripts/Mok/Art/Police/";
static PoliceArtBuilder(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-police-build"))return;File.Delete("Temp/Mok-police-build");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-police-build.txt",e.ToString());Debug.LogException(e);}}
static Material Mat(string name,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+name+".mat");if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Dir+name+".mat");}m.color=c;m.SetFloat("_Smoothness",.18f);return m;}
static void SaveAsset(UnityEngine.Object o,string name){string path=Dir+name;var old=AssetDatabase.LoadMainAssetAtPath(path);if(old!=null){EditorUtility.CopySerialized(o,old);UnityEngine.Object.DestroyImmediate(o);}else AssetDatabase.CreateAsset(o,path);}
static Transform Attach(Transform root,Transform bone,string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType shape=PrimitiveType.Cube){var go=GameObject.CreatePrimitive(shape);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;go.transform.SetParent(bone,true);return go.transform;}
static void Word(Transform root,Transform bone,Vector3 center,bool back,Material mat){
var glyphs=new[]{new[]{"1110","1001","1110","1000","1000"},new[]{"0110","1001","1001","1001","0110"},new[]{"1000","1000","1000","1000","1111"},new[]{"1110","0100","0100","0100","1110"},new[]{"0111","1000","1000","1000","0111"},new[]{"1111","1000","1110","1000","1111"}};
var vertices=new List<Vector3>();var indices=new List<int>();float pixel=.009f;
for(int g=0;g<6;g++)for(int y=0;y<5;y++)for(int x=0;x<4;x++)if(glyphs[g][y][x]=='1'){int n=vertices.Count;float px=(g*5+x-14.5f)*pixel,py=(2-y)*pixel;vertices.Add(new Vector3(px,py,0));vertices.Add(new Vector3(px+pixel*.85f,py,0));vertices.Add(new Vector3(px+pixel*.85f,py+pixel*.85f,0));vertices.Add(new Vector3(px,py+pixel*.85f,0));indices.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
var mesh=new Mesh{name=back?"Police back lettering":"Police front lettering"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();string name=back?"BackLettering.asset":"FrontLettering.asset";SaveAsset(mesh,name);
var go=new GameObject("POLICE lettering",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+name);go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.transform.SetParent(root,false);go.transform.localPosition=center;go.transform.localRotation=Quaternion.Euler(0,back?0:180,0);go.transform.SetParent(bone,true);
}
static void Run(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-police-art.unity",true))throw new Exception("Scene backup failed");if(!File.Exists("Temp/Police-before-art.prefab"))File.Copy(Prefab,"Temp/Police-before-art.prefab");
Directory.CreateDirectory(Dir);AssetDatabase.Refresh();var navy=Mat("Uniform",new Color(.065f,.115f,.20f));var pants=Mat("Trousers",new Color(.04f,.065f,.10f));var skin=Mat("Skin",new Color(.67f,.45f,.31f));var black=Mat("Leather",new Color(.025f,.03f,.035f));var hair=Mat("Hair",new Color(.10f,.065f,.04f));var gold=Mat("Badge",new Color(.78f,.61f,.23f));var white=Mat("Lettering",new Color(.8f,.85f,.86f));
var root=PrefabUtility.LoadPrefabContents(Prefab);try{
var bones=root.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();var source=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Meshes/Male_Adult.fbx").OfType<Mesh>().First(m=>m.vertexCount==renderer.sharedMesh.vertexCount);var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Police Uniform Body";var vertices=mesh.vertices;var triangles=mesh.triangles;var lists=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
for(int i=0;i<triangles.Length;i+=3){var p=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;int slot=p.y<.17f?3:p.y<.87f?1:p.y>1.62f?4:p.y>1.38f||Mathf.Abs(p.x)>.62f?2:0;lists[slot].AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
mesh.subMeshCount=5;for(int i=0;i<5;i++)mesh.SetTriangles(lists[i],i);SaveAsset(mesh,"UniformBody.asset");renderer.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"UniformBody.asset");renderer.sharedMaterials=new[]{navy,pants,skin,black,hair};
Transform Bone(string name)=>bones[name];
// Accessories are stored on the rig, with their world scale preserved through the imported 100x armature.
Attach(root.transform,Bone("Head"),"Police cap crown",new Vector3(0,1.71f,.005f),new Vector3(.31f,.055f,.28f),navy,PrimitiveType.Cylinder);
Attach(root.transform,Bone("Head"),"Cap band",new Vector3(0,1.675f,.005f),new Vector3(.315f,.025f,.285f),black,PrimitiveType.Cylinder);
Attach(root.transform,Bone("Head"),"Cap visor",new Vector3(0,1.664f,.145f),new Vector3(.28f,.022f,.16f),black);
Attach(root.transform,Bone("Head"),"Cap badge",new Vector3(0,1.711f,.147f),new Vector3(.045f,.045f,.014f),gold);
Attach(root.transform,Bone("Spine"),"Duty belt",new Vector3(0,.94f,0),new Vector3(.37f,.065f,.23f),black);
Attach(root.transform,Bone("Spine"),"Belt buckle",new Vector3(0,.94f,.124f),new Vector3(.055f,.045f,.018f),gold);
Attach(root.transform,Bone("Spine"),"Radio",new Vector3(-.22f,1.01f,0),new Vector3(.075f,.15f,.075f),black);
Attach(root.transform,Bone("Spine"),"Radio antenna",new Vector3(-.22f,1.12f,0),new Vector3(.012f,.12f,.012f),black);
Attach(root.transform,Bone("Spine"),"Equipment pouch",new Vector3(.22f,.95f,0),new Vector3(.09f,.13f,.085f),black);
Attach(root.transform,Bone("Chest"),"Chest badge",new Vector3(-.095f,1.30f,.122f),new Vector3(.055f,.075f,.018f),gold);
Attach(root.transform,Bone("Chest"),"Nameplate",new Vector3(.085f,1.30f,.122f),new Vector3(.075f,.018f,.012f),white);
Attach(root.transform,Bone("Chest"),"Tie",new Vector3(0,1.29f,.125f),new Vector3(.035f,.19f,.018f),black);
Word(root.transform,Bone("Chest"),new Vector3(0,1.29f,-.13f),true,white);
foreach(var rb in root.GetComponentsInChildren<Rigidbody>()){rb.isKinematic=true;rb.useGravity=false;}
foreach(var col in root.GetComponentsInChildren<Collider>())if(col.gameObject!=root)col.enabled=false;
var animator=root.GetComponent<Animator>();var walk=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Walking.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
var run=UnityEngine.Object.Instantiate(walk);run.name="Police Chase Run";float ratio=.6f/walk.length;
foreach(var binding in AnimationUtility.GetCurveBindings(walk)){var curve=AnimationUtility.GetEditorCurve(walk,binding);var keys=curve.keys;for(int i=0;i<keys.Length;i++){var k=keys[i];k.time*=ratio;k.inTangent/=ratio;k.outTangent/=ratio;float gain=1,offset=0;
if(binding.propertyName.Contains("Upper Leg Front-Back"))gain=1.55f;
if(binding.propertyName.Contains("Arm Front-Back"))gain=1.6f;
if(binding.propertyName.Contains("Lower Leg Stretch"))offset=-.2f;
if(binding.propertyName.Contains("Forearm Stretch")){gain=.25f;offset=-.5f;}
if(binding.propertyName=="Spine Front-Back")offset=.12f;
k.value=k.value*gain+offset;k.inTangent*=gain;k.outTangent*=gain;keys[i]=k;}curve.keys=keys;AnimationUtility.SetEditorCurve(run,binding,curve);}
var settings=AnimationUtility.GetAnimationClipSettings(run);settings.loopTime=true;settings.loopBlend=true;settings.stopTime=.6f;AnimationUtility.SetAnimationClipSettings(run,settings);SaveAsset(run,"PoliceRun.anim");
var controller=AnimatorController.CreateAnimatorControllerAtPath(Dir+"PoliceLocomotion.controller");controller.AddParameter("MoveSpeed",AnimatorControllerParameterType.Float);controller.AddParameter("RunRate",AnimatorControllerParameterType.Float);var sm=controller.layers[0].stateMachine;var stand=sm.AddState("Stand");stand.motion=idle;var chase=sm.AddState("Chase Run");chase.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"PoliceRun.anim");chase.speedParameter="RunRate";chase.speedParameterActive=true;sm.defaultState=stand;
var toRun=stand.AddTransition(chase);toRun.hasExitTime=false;toRun.duration=.14f;toRun.AddCondition(AnimatorConditionMode.Greater,.15f,"MoveSpeed");var toStand=chase.AddTransition(stand);toStand.hasExitTime=false;toStand.duration=.18f;toStand.AddCondition(AnimatorConditionMode.Less,.15f,"MoveSpeed");
animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;if(root.GetComponent<PoliceChaseAnimation>()==null)root.AddComponent<PoliceChaseAnimation>();
PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-police-build.txt","PASS: Police prefab uniform, rig accessories and humanoid run controller saved; chase logic unchanged.");
}finally{PrefabUtility.UnloadPrefabContents(root);}
}
}}
#endif
