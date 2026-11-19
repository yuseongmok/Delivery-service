#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonCircuitReview
 {
  static int step;static float wait;static double deadline;const string Log="Logs/Circuit-play-check.txt";
  static PrisonCircuitReview(){EditorApplication.update+=Tick;}
  public static void Run(){File.WriteAllText(Log,"Assisted entry; real conversation, panel, retry and success routines.\n");SessionState.SetBool("CircuitReview",true);EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).maximized=true;EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.EnterPlaymode();}
  static void Assert(bool b,string text){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+text+"\n");if(!b)throw new Exception(text);}
  static void Place(PlayerCombat p,Transform t){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=t.position+Vector3.up*1.1f;c.enabled=true;Physics.SyncTransforms();}
  static void Tick(){
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/CircuitReview")){File.Delete("Temp/CircuitReview");step=0;Run();return;}
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("CircuitReviewFinished",false)){SessionState.SetBool("CircuitReviewFinished",false);return;}
   if(!EditorApplication.isPlaying||!SessionState.GetBool("CircuitReview",false))return;
   try{
    var m=Object.FindFirstObjectByType<PrisonCctvMission>();if(!m)return;var p=m.office.previous.previous.prison.player;var w=m.wirePanel;var c=w.circuit;var friend=m.office.previous.previous.prison.introduction.mingi;
    if(step==0){deadline=EditorApplication.timeSinceStartup+150;wait=Time.time+3;step++;return;}
    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout "+step);if(Time.time<wait)return;
    if(step==1){if(PrisonMissionSkip.Target!="CCTV 무력화"){PrisonMissionSkip.Complete();wait=Time.time+1;return;}m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.2f;Place(p,m.legacyEscape.changePoint);step=11;wait=Time.time+1;return;}
    if(step==11){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;File.AppendAllText(Log,"Change state="+m.State+" CanAct="+p.CanAct+" enabled="+p.enabled+" move="+p.GetComponent<PlayerMove>().isControllable+" cursor="+Cursor.lockState+" Busy="+m.Busy+" distance="+Vector3.Distance(p.transform.position,m.legacyEscape.changePoint.position)+"\n");Assert(m.Interact(),"change clothes begins");step=2;wait=Time.time+2;return;}

    if(step==2){Assert(m.legacyEscape.Disguised&&!friend.ScriptedTravel&&friend.Following,"disguise clears scripted movement and resumes follow");Place(p,m.entry);step++;wait=Time.time+2;return;}
    if(step==3){Cursor.lockState=CursorLockMode.Locked;Assert(!m.Busy&&p.CanAct&&m.Suspicion==0,"guard accepts disguise and restores controls");Assert(m.Interact(),"authorized CCTV door opens");step++;wait=Time.time+1.5f;return;}
    if(step==4){Cursor.lockState=CursorLockMode.Locked;Place(p,w.checkpoint);Assert(m.Interact(),"wire panel begins");step++;wait=Time.time+1;return;}
    if(step==5){Assert(w.Ready&&w.camera.enabled&&!p.View.enabled,"close-up camera active");Assert(w.wholeWires.Length==4,"four colored circuits installed");c.chosen=0;Time.timeScale=0;Assert(!c.Confirm(),"pause blocks cutting");Time.timeScale=1;ScreenCapture.CaptureScreenshot("Logs/Circuit-minigame.png");step=51;wait=Time.time+1;return;}
    if(step==51){w.Cancel();Assert(p.CanAct&&p.View.enabled&&!w.Playing,"cancel restores player view");Assert(m.Interact(),"panel can reopen");step=6;wait=Time.time+1;return;}
    if(step==6){c.chosen=(w.correctWire+1)%4;Assert(c.Confirm(),"wrong wire can be cut");step=81;wait=Time.time+5;return;}
    if(step==81){Assert(m.Attempts==1&&!m.CamerasDisabled&&p.CanAct,"wrong circuit triggers alarm and retries");Assert(c.chosen==-1,"retry clears selection");Assert(m.Interact(),"reopen after wrong circuit");step=82;wait=Time.time+1;return;}
    if(step==82){c.chosen=w.correctWire;Assert(c.Confirm(),"connected CCTV wire cuts");step=9;wait=Time.time+2;return;}
    if(step==9){Assert(m.CamerasDisabled&&m.monitors.All(x=>x.Disabled)&&p.CanAct&&p.View.enabled,"CCTV disabled; input and view restored");Assert(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled&&x.gameObject.activeInHierarchy)==1,"one active audio listener");friend.Following=false;if(friend.agent.enabled&&friend.agent.isOnNavMesh)friend.agent.ResetPath();step=91;wait=Time.time+1;return;}
    if(step==91){Assert(friend.animator.GetFloat("MoveSpeed")<.15f,"stationary companion stops running animation");friend.ResumeFollowing();Place(p,m.safePoint);step=10;wait=Time.time+.2f;return;}
    Assert(m.State==PrisonCctvMission.Stage.Complete,"existing evening mission handoff");File.AppendAllText(Log,"FINISHED\n");Finish(0);
   }catch(Exception ex){File.AppendAllText(Log,ex.ToString());Finish(1);}
  }
  static void Finish(int code){Time.timeScale=1;SessionState.SetBool("CircuitReview",false);SessionState.SetBool("CircuitReviewFinished",true);SessionState.SetInt("CircuitReviewExit",code);EditorApplication.ExitPlaymode();}
 }
}
#endif

