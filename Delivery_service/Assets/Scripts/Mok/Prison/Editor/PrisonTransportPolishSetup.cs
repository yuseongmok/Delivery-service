#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad]public static class PrisonTransportPolishSetup{
static PrisonTransportPolishSetup(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/TransportPolish"))return;File.Delete("Temp/TransportPolish");try{Apply();}catch(Exception e){File.WriteAllText("Logs/Transport-polish.txt",e.ToString());}};}
static void Apply(){
var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Prison only");if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-transport-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
var m=Object.FindFirstObjectByType<PrisonTransportEscape>();var sb=new StringBuilder();
foreach(Transform box in m.previous.coverProps.transform)if(box.position.x< -47&&box.position.x> -49){box.position=new Vector3(-51.5f,box.position.y,box.position.z);sb.AppendLine("Moved cover out of lane: "+box.name+" "+box.position);}
if(m.start.position.y>.1f){
 float drop=m.start.position.y-.025f;
 foreach(var point in m.road.points)point.position-=Vector3.up*drop;
 m.road.Rebuild();m.start.position=m.road.Position(0);m.destination.position=m.road.Position(1);m.vehicle.position=m.start.position;
 m.eye.localPosition+=Vector3.up*drop;
 sb.AppendLine("Aligned wheel clearance to road: 0.025m; retained eye height");
}
m.finalDrainMultiplier=3.8f;m.drainPerSecond=.13f;m.tapRecovery=.085f;if(m.hands)m.hands.mission=m;
var p=m.previous.previous.wirePanel;
const string path="Assets/DeliveryPrisonChapter/CctvWire/Black cable.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.025f,.03f,.035f);AssetDatabase.CreateAsset(mat,path);}foreach(var wire in p.wholeWires.Concat(p.cutWires))foreach(var r in wire.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
p.dialogue.hint="민기: 검정 선을 끝까지 따라가. CCTV와 연결된 절단 지점을 클릭하면 돼.";p.dialogue.controls="절단 지점 클릭 · 니퍼로 자르기   |   F 돌아가기";p.dialogue.retry="민기: 교차하는 부분을 잘 봐. CCTV 단자에서 다시 따라가.";EditorUtility.SetDirty(p.dialogue);
sb.AppendLine("Gate rotations "+m.gateLeft.eulerAngles+" "+m.gateRight.eulerAngles);sb.AppendLine("Eye "+m.eye.position+" Hang "+m.playerHang.position);
foreach(var r in m.vehicle.GetComponentsInChildren<Renderer>(true))if(r.name.ToLower().Contains("wheel")||r.name.ToLower().Contains("body"))sb.AppendLine(r.name+" "+r.bounds);
Physics.SyncTransforms();var closedL=m.gateLeft.rotation;var closedR=m.gateRight.rotation;m.gateLeft.rotation=closedL*Quaternion.Euler(0,-100,0);m.gateRight.rotation=closedR*Quaternion.Euler(0,100,0);Physics.SyncTransforms();
var hits=new System.Collections.Generic.HashSet<string>();for(int i=0;i<=700;i++){float t=i/700f;var pos=m.road.Position(t);foreach(var c in Physics.OverlapBox(pos+Vector3.up*1.25f,new Vector3(1.15f,.85f,2.7f),m.road.Rotation(t),~0,QueryTriggerInteraction.Ignore)){if(c.transform.IsChildOf(m.vehicle)||c.GetComponentInParent<NPCControl>()||c.GetComponentInParent<PlayerCombat>())continue;hits.Add(c.name+" "+c.bounds+" at "+pos);}}
sb.AppendLine("Remaining swept-volume hits: "+hits.Count);foreach(var hit in hits)sb.AppendLine(hit);
m.gateLeft.rotation=closedL;m.gateRight.rotation=closedR;Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Transport-polish.txt",sb.ToString());
}
}}
#endif
