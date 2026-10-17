#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RobberInstaller {
const string Prefab="Assets/Scripts/Min/Prefab/NPC/Robbery.prefab",Dir="Assets/Scripts/Mok/Art/Robber/";
static RobberInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-robber-install"))return;File.Delete("Temp/Mok-robber-install");try{Install();}catch(Exception e){File.WriteAllText("Temp/Mok-robber-install.txt",e.ToString());Debug.LogException(e);}}
static Material Mat(string name,Color color){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+name+".mat");if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Dir+name+".mat");}m.color=color;m.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(m);return m;}
static void Part(Transform root,Transform bone,string name,Vector3 position,Vector3 size,Material mat){
var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;go.transform.SetParent(bone,true);
}
static void Hood(Transform root,Transform head,Material mat,Bounds bounds){
var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
Vector3 Point(int ring,int i){float a=i*Mathf.PI*2/12;float x=(bounds.extents.x+.025f)*(ring==1?.77f:1f),y=(bounds.extents.y+.02f)*(ring==1?.73f:1f);return new Vector3(bounds.center.x+Mathf.Cos(a)*x,bounds.center.y+Mathf.Sin(a)*y,ring==2?bounds.min.z-.02f:bounds.max.z+.015f);}
void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.AddRange(new[]{n,n+1,n+2});}
for(int i=0;i<12;i++){int j=(i+1)%12;Tri(Point(0,i),Point(0,j),Point(1,j));Tri(Point(0,i),Point(1,j),Point(1,i));Tri(Point(0,i),Point(2,i),Point(2,j));Tri(Point(0,i),Point(2,j),Point(0,j));Tri(new Vector3(bounds.center.x,bounds.center.y,bounds.min.z-.02f),Point(2,j),Point(2,i));}
var mesh=new Mesh{name="Robber Hood"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
string path=Dir+"Hood.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old!=null){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
var go=new GameObject("Robber hood",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.transform.SetParent(root,false);go.transform.SetParent(head,true);
}
static void Install(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("JJinCity를 열어주세요");
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-robber.unity",true))throw new Exception("씬 백업 실패");
if(!File.Exists("Temp/Robbery-before-upgrade.prefab"))File.Copy(Prefab,"Temp/Robbery-before-upgrade.prefab");
Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
var cloth=Mat("Hoodie",new Color(.16f,.19f,.17f));var pants=Mat("Trousers",new Color(.09f,.11f,.14f));var skin=Mat("Skin",new Color(.69f,.49f,.35f));var black=Mat("GlovesMaskBoots",new Color(.045f,.05f,.06f));var bag=Mat("CanvasBag",new Color(.32f,.24f,.15f));var zip=Mat("Zipper",new Color(.43f,.45f,.4f));
var root=PrefabUtility.LoadPrefabContents(Prefab);try{
var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();
// 경찰에서 검증한 부위별 메시를 복사해 강도 전용 재질을 사용한다.
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"RobberBody.asset");if(mesh==null){mesh=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scripts/Mok/Art/Police/UniformBody.asset"));mesh.name="Robber Body";AssetDatabase.CreateAsset(mesh,Dir+"RobberBody.asset");}
renderer.sharedMesh=mesh;renderer.sharedMaterials=new[]{cloth,pants,skin,black,black};
var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
var position=root.transform.position;idle.SampleAnimation(root,0);root.transform.position=position;
var bones=root.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);var head=bones["Head"];var chest=bones["Chest"];
// 재실행해도 장비가 중복되지 않게 이전 제작분만 정리한다.
foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Robber ")).ToArray())if(t!=root.transform)UnityEngine.Object.DestroyImmediate(t.gameObject);
var baked=new Mesh();renderer.BakeMesh(baked);var weights=renderer.sharedMesh.boneWeights;var points=baked.vertices;bool first=true;Bounds headBounds=default;
for(int i=0;i<points.Length;i++)if(renderer.bones[weights[i].boneIndex0].name.Contains("Head")){var v=root.transform.InverseTransformPoint(renderer.transform.TransformPoint(points[i]));if(first){headBounds=new Bounds(v,Vector3.zero);first=false;}else headBounds.Encapsulate(v);}
UnityEngine.Object.DestroyImmediate(baked);if(first)throw new Exception("머리 메시 범위 확인 실패");
Hood(root.transform,head,cloth,headBounds);
Part(root.transform,head,"Robber mask",new Vector3(0,1.555f,.115f),new Vector3(.20f,.105f,.04f),black);
Part(root.transform,chest,"Robber zipper",new Vector3(0,1.24f,.14f),new Vector3(.015f,.28f,.02f),zip);
Part(root.transform,chest,"Robber bag",new Vector3(0,1.25f,-.20f),new Vector3(.30f,.36f,.16f),bag);
Part(root.transform,chest,"Robber strap left",new Vector3(-.12f,1.27f,.14f),new Vector3(.035f,.30f,.025f),black);
Part(root.transform,chest,"Robber strap right",new Vector3(.12f,1.27f,.14f),new Vector3(.035f,.30f,.025f),black);
// 손에 들고 도망갈 박스도 프리팹에 저장한다.
var old=root.transform.Find("Stolen pizza visual");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
var box=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/Box.prefab")) as GameObject;
if(box==null)throw new Exception("포장 박스 프리팹 없음");box.name="Stolen pizza visual";box.transform.SetParent(root.transform,false);box.transform.localPosition=new Vector3(0,1.08f,.39f);box.transform.localScale=Vector3.one*.6f;box.SetActive(false);
var npc=root.GetComponent<NPCControl>();if(npc==null)npc=root.AddComponent<NPCControl>();var nso=new SerializedObject(npc);nso.FindProperty("mainBone").objectReferenceValue=bones["Hips"];nso.ApplyModifiedPropertiesWithoutUndo();
var ai=root.GetComponent<RobberAI>();var so=new SerializedObject(ai);so.FindProperty("moveSpeed").floatValue=5f;so.FindProperty("attackDistance").floatValue=1.5f;so.FindProperty("escapeSpeed").floatValue=6f;so.FindProperty("windupSeconds").floatValue=1f;so.ApplyModifiedPropertiesWithoutUndo();
foreach(var rb in root.GetComponentsInChildren<Rigidbody>()){rb.isKinematic=true;rb.useGravity=true;}
foreach(var col in root.GetComponentsInChildren<Collider>(true))if(col.gameObject!=root)col.enabled=false;
var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Dir+"Robber.controller");if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(Dir+"Robber.controller");controller.AddParameter("MoveSpeed",AnimatorControllerParameterType.Float);controller.AddParameter("RunRate",AnimatorControllerParameterType.Float);controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);var sm=controller.layers[0].stateMachine;var stand=sm.AddState("Stand");stand.motion=idle;var run=sm.AddState("Run");run.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Scripts/Mok/Art/Police/PoliceRun.anim");run.speedParameter="RunRate";run.speedParameterActive=true;sm.defaultState=stand;var a=stand.AddTransition(run);a.hasExitTime=false;a.duration=.15f;a.AddCondition(AnimatorConditionMode.Greater,.15f,"MoveSpeed");a=run.AddTransition(stand);a.hasExitTime=false;a.duration=.15f;a.AddCondition(AnimatorConditionMode.Less,.15f,"MoveSpeed");}
var animator=root.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;if(root.GetComponent<RobberPresentation>()==null)root.AddComponent<RobberPresentation>();
PrefabUtility.SaveAsPrefabAsset(root,Prefab);
}finally{PrefabUtility.UnloadPrefabContents(root);}
var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>(FindObjectsInactive.Include);if(manager==null)throw new Exception("강도 매니저 없음");var managerSo=new SerializedObject(manager);managerSo.FindProperty("lowChance").floatValue=15;managerSo.FindProperty("mediumChance").floatValue=25;managerSo.FindProperty("highChance").floatValue=35;managerSo.ApplyModifiedProperties();
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-robber-install.txt","PASS: 강도 프리팹 외형, 레그돌, 달리기와 강탈 연출 연결. 씬 백업 및 저장 완료.");
}
}}
#endif
