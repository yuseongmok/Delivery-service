#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonTheftHandsSetup {
static PrisonTheftHandsSetup(){EditorApplication.update+=Poll;}
static void Poll(){const string f="Temp/TheftHandsSetup";if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(f))return;var mode=File.ReadAllText(f).Trim();File.Delete(f);try{if(mode=="checkpoint"){var m=Object.FindFirstObjectByType<PrisonKeysMission>();EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Prison-before-safe-checkpoint.unity",true);m.checkpoint.position=new Vector3(-52.25f,1.1f,-8.6f);m.checkpoint.rotation=Quaternion.Euler(0,180,0);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);}else if(mode=="polish")Polish();else Build();}catch(Exception e){File.WriteAllText("Logs/TheftHandsSetup.txt",e.ToString());}}
static void Polish(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Prison-before-theft-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
var h=m.GetComponent<PrisonPickpocket>().hands;h.fingers=h.rightHand.GetComponentsInChildren<Transform>().Concat(h.leftHand.GetComponentsInChildren<Transform>()).Where(t=>(t.name.Contains("Finger")||t.name.Contains("Thumb"))&&!t.name.Contains("04")).ToArray();h.fingerRest=h.fingers.Select(t=>t.localRotation).ToArray();
var root=m.transform.Find("골목 끝 식료품 창고 마감");
foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("골목 접근 엄폐 상자")||t.name.StartsWith("골목 팔레트"))){var rr=t.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);bool crate=t.name.StartsWith("골목 접근");var center=t.position;center.y=crate?.91f:.12f;t.position+=center-b.center;if(crate){var col=t.GetComponentInChildren<BoxCollider>();col.transform.position=center;}}
EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/TheftHandsSetup.txt","PASS thumb joints and crate grounding refined");}
static void Build(){
var mission=Object.FindFirstObjectByType<PrisonKeysMission>();if(!mission||mission.gameObject.scene.name!="PrisonChapter")throw new Exception("Prison only");
EditorSceneManager.SaveScene(mission.gameObject.scene,"Logs/Prison-before-theft-hands-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
var theft=mission.GetComponent<PrisonPickpocket>();if(theft.hands)throw new Exception("Already installed");
const string folder="Assets/DeliveryPrisonChapter/KeysMission";
var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_Hoodie.prefab"),mission.transform);
g.name="소매치기 전용 플레이어 손과 죄수복";var a=g.GetComponentInChildren<Animator>();a.enabled=false;
var hands=g.AddComponent<PrisonTheftHands>();
hands.rightUpper=a.GetBoneTransform(HumanBodyBones.RightUpperArm);hands.rightLower=a.GetBoneTransform(HumanBodyBones.RightLowerArm);hands.rightHand=a.GetBoneTransform(HumanBodyBones.RightHand);
hands.leftUpper=a.GetBoneTransform(HumanBodyBones.LeftUpperArm);hands.leftLower=a.GetBoneTransform(HumanBodyBones.LeftLowerArm);hands.leftHand=a.GetBoneTransform(HumanBodyBones.LeftHand);
hands.fingers=hands.rightHand.GetComponentsInChildren<Transform>().Concat(hands.leftHand.GetComponentsInChildren<Transform>()).Where(t=>t.name.Contains("Finger")&&!t.name.EndsWith("04")).ToArray();hands.fingerRest=hands.fingers.Select(t=>t.localRotation).ToArray();
var cloth=new Material(Shader.Find("Universal Render Pipeline/Lit"));cloth.color=new Color(.65f,.24f,.07f);cloth.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(cloth,folder+"/소매치기 죄수 소매.mat");
int index=0;foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>()){
var mesh=Object.Instantiate(r.sharedMesh);var weights=mesh.boneWeights;
bool Within(int bone,Transform root){return r.bones[bone]&&(r.bones[bone]==root||r.bones[bone].IsChildOf(root));}
bool Arm(int v){var b=weights[v].boneIndex0;return Within(b,hands.rightUpper)||Within(b,hands.leftUpper);}
bool Hand(int v){var b=weights[v].boneIndex0;return Within(b,hands.rightHand)||Within(b,hands.leftHand);}
int count=mesh.subMeshCount;var skin=new List<int>[count];var sleeves=new List<int>();
for(int s=0;s<count;s++){skin[s]=new List<int>();var triangles=mesh.GetTriangles(s);for(int j=0;j<triangles.Length;j+=3){int x=triangles[j],y=triangles[j+1],z=triangles[j+2];if(!Arm(x)||!Arm(y)||!Arm(z))continue;var list=(Hand(x)?1:0)+(Hand(y)?1:0)+(Hand(z)?1:0)>=2?skin[s]:sleeves;list.AddRange(new[]{x,y,z});}}
mesh.subMeshCount=count+1;for(int s=0;s<count;s++)mesh.SetTriangles(skin[s],s);mesh.SetTriangles(sleeves,count);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/소매치기 플레이어 팔 "+index+++".asset");r.sharedMesh=mesh;r.sharedMaterials=r.sharedMaterials.Concat(new[]{cloth}).ToArray();r.updateWhenOffscreen=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
}
foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
g.transform.position=mission.keyGuard.transform.position;g.SetActive(false);theft.hands=hands;theft.pullDistance=650;theft.noisySpeed=210;theft.noiseRecovery=.45f;
EditorUtility.SetDirty(theft);EditorSceneManager.MarkSceneDirty(mission.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(mission.gameObject.scene);File.WriteAllText("Logs/TheftHandsSetup.txt","PASS saved Synty hoodie arm geometry and prison sleeves; finger joints="+hands.fingers.Length);
}
}}
#endif
