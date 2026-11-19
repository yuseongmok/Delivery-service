#if UNITY_EDITOR
using System;using System.IO;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad]public static class PrisonNightPolishReview{
static int step;static float wait;static double deadline;const string Log="Logs/Prison-night-polish-check.txt";
static PrisonNightPolishReview(){EditorApplication.update+=Tick;}
static void Check(bool v,string t){File.AppendAllText(Log,(v?"PASS ":"FAIL ")+t+"\n");if(!v)throw new Exception(t);}
static void Tick(){
if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/NightPolishReview")){File.Delete("Temp/NightPolishReview");File.WriteAllText(Log,"Assisted checkpoints and light exposure verification\n");SessionState.SetBool("NightPolishReview",true);step=0;EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).maximized=true;EditorApplication.EnterPlaymode();return;}
if(!EditorApplication.isPlaying||!SessionState.GetBool("NightPolishReview",false))return;
try{
var n=Object.FindFirstObjectByType<PrisonNightMission>();if(!n)return;
if(step==0){deadline=EditorApplication.timeSinceStartup+180;wait=Time.time+3;step=1;return;}if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout "+step);if(Time.time<wait)return;
if(step==1){if(PrisonMissionSkip.Target!="CCTV 무력화"){PrisonMissionSkip.Complete();wait=Time.time+1;return;}n.dialogue=Object.Instantiate(n.dialogue);n.dialogue.lineSeconds=.3f;n.previous.DeveloperComplete();step=2;wait=Time.time+8;return;}
if(step==2){Check(n.State==PrisonNightMission.Stage.Sneak&&n.Player.CanAct,"intro hands control to first-person player");Check(n.route.Length==6&&n.searchlights.Length==4,"three sections and four searchlights");var from=n.checkpoint.position;foreach(var point in n.route){var path=new NavMeshPath();bool ok=NavMesh.SamplePosition(from,out var a,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(point.position,out var b,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;Check(ok,"walkable leg to "+point.name);from=point.position;}Check(n.tensionMusic&&n.engineSound,"mission music and vehicle engine assigned");typeof(PrisonNightMission).GetField("waypoint",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(n,4);typeof(PrisonNightMission).GetMethod("UpdateStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(n,null);Check(n.StageIndex==2,"last section saves checkpoint");n.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonNightMission).GetMethod("Retry",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(n,null));step=3;wait=Time.time+3.5f;return;}
if(step==3){Check(n.State==PrisonNightMission.Stage.Sneak&&Vector3.Distance(n.Player.transform.position,n.safeCheckpoints[2].position+Vector3.up*1.1f)<.5f,"retry returns to latest checkpoint");Check(n.Player.View.enabled&&n.Player.CanAct,"retry restores camera and input");Check(n.Friend.agent.enabled&&n.Friend.agent.isOnNavMesh,"Mingi remains on navigation mesh");ScreenCapture.CaptureScreenshot("Logs/Prison-night-polish.png");File.AppendAllText(Log,"FINISHED\n");Finish();}
}catch(Exception e){File.AppendAllText(Log,e.ToString());Finish();}}
static void Finish(){SessionState.SetBool("NightPolishReview",false);EditorApplication.ExitPlaymode();}
}}
#endif
