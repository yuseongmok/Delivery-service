#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad]public static class PrisonFinalePolishSetup{
static PrisonFinalePolishSetup(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonFinalePolish"))return;File.Delete("Temp/PrisonFinalePolish");try{Apply();}catch(Exception e){File.WriteAllText("Logs/Prison-finale-polish.txt",e.ToString());}};}
static void Apply(){
var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("PrisonChapter only");if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-finale-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
var p=Object.FindFirstObjectByType<PrisonCctvWirePanel>();
if(p.wholeWires.Length==3){
var wire=Object.Instantiate(p.wholeWires[2],p.wholeWires[2].transform.parent);wire.name="노랑 영상/경보 배선";wire.transform.localPosition+=Vector3.right*.07f;
var cut=Object.Instantiate(p.cutWires[2],p.cutWires[2].transform.parent);cut.name="노랑 절단 배선";cut.transform.localPosition+=Vector3.right*.07f;cut.SetActive(false);
var target=Object.Instantiate(p.wireTargets[2],p.wireTargets[2].transform.parent);target.name="노랑 절단 지점";target.transform.localPosition+=Vector3.right*.07f;
var mat=new Material(p.wholeWires[2].GetComponentInChildren<Renderer>().sharedMaterial);mat.color=new Color(.95f,.72f,.12f);AssetDatabase.CreateAsset(mat,"Assets/DeliveryPrisonChapter/CctvWire/Yellow wire.mat");foreach(var r in wire.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;foreach(var r in cut.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
p.wholeWires=p.wholeWires.Concat(new[]{wire}).ToArray();p.cutWires=p.cutWires.Concat(new[]{cut}).ToArray();p.wireTargets=p.wireTargets.Concat(new[]{target}).ToArray();}
p.dialogue.title="CCTV 배선 추적";p.dialogue.hint="민기: CCTV 단자에서 선을 따라가. 아래에서 연결된 선을 골라 잘라.";p.dialogue.controls="선 클릭 · 선택   |   자르기 버튼 · 절단   |   F 돌아가기";p.dialogue.retry="민기: 색만 보지 말고 CCTV 단자부터 연결을 따라가.";p.RefreshDiagram();EditorUtility.SetDirty(p.dialogue);
var n=Object.FindFirstObjectByType<PrisonNightMission>();
if(!n.transform.Find("피날레 확장 동선")){
 var root=new GameObject("피날레 확장 동선").transform;root.SetParent(n.transform);
 var points=new[]{new Vector3(-26,0,22),new Vector3(-32,0,23),new Vector3(-42,0,22),new Vector3(-46,0,16),new Vector3(-46,0,3),new Vector3(-46,0,-10)};
 n.route=points.Select((v,i)=>{var t=new GameObject((i<2?"수감동 그늘":i<4?"운동장 교차 구간":"창고 마지막 접근")+" · "+(i+1)).transform;t.SetParent(root);t.position=v;return t;}).ToArray();
 n.safeCheckpoints=new Transform[]{n.checkpoint,n.route[1],n.route[3]};n.stageStarts=new[]{0,2,4};
 // 기존 엄폐 프리팹을 복제하여 이동 경로 옆에 실제 가림을 만든다.
 var template=n.coverProps.transform.GetChild(0);
 foreach(var v in new[]{new Vector3(-33.8f,0,23),new Vector3(-47.8f,0,16),new Vector3(-47.8f,0,3),new Vector3(-47.8f,0,-10)}){
  var box=Object.Instantiate(template,n.coverProps.transform);box.name="피날레 잠입 엄폐 상자";box.position=new Vector3(v.x,template.position.y,v.z);
 }
 var extra=Object.Instantiate(n.searchlights[0],n.searchlights[0].transform.parent);extra.name="운동장 교차 탐조등";extra.transform.position+=Vector3.right*1.3f;extra.sweepFrom=new Vector3(-48,0,16);extra.sweepTo=new Vector3(-34,0,23);extra.period=11;extra.phase=.65f;
 var last=Object.Instantiate(n.searchlights[1],n.searchlights[1].transform.parent);last.name="호송차량 접근 탐조등";last.transform.position+=Vector3.right*1.3f;last.sweepFrom=new Vector3(-46,0,-18);last.sweepTo=new Vector3(-46,0,6);last.period=14;last.phase=.35f;last.endPause=.2f;
 n.searchlights=n.searchlights.Concat(new[]{extra,last}).ToArray();
}
n.searchlights[0].period=14;n.searchlights[1].period=12;
foreach(var light in n.searchlights){light.motorSound=SuppliedAudio.Current?SuppliedAudio.Current.vaultLatch:null;light.lamp.spotAngle=8;light.lamp.innerSpotAngle=6;light.ResetSweep();light.SetRunning(false);}
n.detectionSeconds=.8f;
n.tensionMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/미션 브금.mp3");
var audio=SuppliedAudio.Current;if(audio)n.engineSound=audio.playerEngine;
var gesture=n.Friend.GetComponent<PrisonNightCompanionPose>();if(!gesture)gesture=n.Friend.gameObject.AddComponent<PrisonNightCompanionPose>();gesture.mission=n;
n.dialogue.introduction=new[]{"방송: 야간 점호를 시작한다. 작업 인원은 즉시 복귀하라.","민기: 이제 간수복도 소용없어. 저 불빛에 걸리면 끝이야.","민기: 벽에 붙어서 가자. 불빛이 지나가면 다음 상자 뒤로 움직여."};EditorUtility.SetDirty(n.dialogue);
var report="Start "+n.checkpoint.position+" Vehicle "+n.vehiclePoint.position+"\n";foreach(var t in n.route)report+=t.name+" "+t.position+"\n";foreach(var t in n.searchlights)report+=t.name+" "+t.transform.position+" from "+t.sweepFrom+" to "+t.sweepTo+"\n";
File.WriteAllText("Logs/Prison-finale-polish.txt",report);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
}
}}
#endif

