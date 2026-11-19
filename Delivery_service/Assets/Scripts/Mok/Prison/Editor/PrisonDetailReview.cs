#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonDetailReview
 {
  const string Log="Logs/Prison-detail-check.txt";static int step,index;static float wait;static double deadline;
  static PrisonDetailReview(){EditorApplication.update+=Tick;}
  public static void Run(){File.WriteAllText(Log,"Assisted stage setup; actual evidence reading, disguise and evening conversation.\n");step=0;index=0;SessionState.SetBool("PrisonDetailReview",true);var game=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));SessionState.SetBool("DetailWasMax",game.maximized);game.maximized=true;game.Show();game.Focus();EditorApplication.EnterPlaymode();}
  static void Check(bool pass,string message){File.AppendAllText(Log,(pass?"PASS ":"FAIL ")+message+"\n");if(!pass)throw new Exception(message);}
  static void Place(PlayerCombat player,Vector3 point){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=point;c.enabled=true;Cursor.lockState=CursorLockMode.Locked;Physics.SyncTransforms();}
  static void Tick(){
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("PrisonDetailFinished",false)){SessionState.SetBool("PrisonDetailFinished",false);EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).maximized=SessionState.GetBool("DetailWasMax",false);return;}
   if(!EditorApplication.isPlaying||!SessionState.GetBool("PrisonDetailReview",false))return;
   try{
    if(step==0){deadline=EditorApplication.timeSinceStartup+180;step=1;wait=Time.time+3;return;}if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step "+step);if(Time.time<wait)return;
    var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();if(!office)return;var evidence=office.evidence;var player=office.previous.previous.prison.player;
    if(step==1){if(PrisonMissionSkip.Target!="간수장실 조사"){PrisonMissionSkip.Complete();wait=Time.time+1;return;}Check(!evidence.dialogue.documents.Any(x=>x.Contains("이현우")),"documents do not name player as Lee Hyunwoo");Check(office.cctv.entry.name=="CCTV실 들어가기","CCTV destination label updated");Check(office.cctv.dialogue.interrogation.Any(x=>x.Contains("옷 벗")),"editable hesitation dialogue installed");foreach(var cue in Enum.GetNames(typeof(PrisonDetailAudio.Cue)))Check(Resources.Load<AudioClip>("PrisonDetailAudio/"+cue),"dedicated audio loaded "+cue);evidence.dialogue=Object.Instantiate(evidence.dialogue);evidence.dialogue.lineSeconds=1.5f;step=2;wait=Time.time+.5f;return;}
    if(step==2){Place(player,evidence.evidencePoints[index].position+Vector3.back*.9f);Check(evidence.Inspect(index),"read evidence "+index);step=3;wait=Time.time+.65f;return;}
    if(step==3){ScreenCapture.CaptureScreenshot("Logs/Prison-evidence-"+index+".png");File.AppendAllText(Log,"Capture "+Screen.width+"x"+Screen.height+"\n");step=4;wait=Time.time+4;return;}
    if(step==4){if(evidence.Busy)return;Check(evidence.Discovered==index+1&&player.View.enabled,"reading restores player view "+index);index++;if(index<3){step=2;return;}var guard=office.previous.warden;guard.gameObject.SetActive(true);guard.GetComponent<NPCControl>().RecoverFromKnockDown();guard.transform.position=evidence.wardenStand.position;Place(player,guard.transform.position+Vector3.forward*2+Vector3.up);step=5;wait=Time.time+1.5f;return;}
    if(step==5){var guard=office.previous.warden;Vector3 to=Vector3.ProjectOnPlane(player.transform.position-guard.transform.position,Vector3.up);Check(Vector3.Angle(guard.transform.forward,to)<8&&!guard.enabled,"warden faces player instead of patrol scanning");foreach(var g in office.previous.diners){g.gameObject.SetActive(true);var eating=g.GetComponent<PrisonPizzaEating>();if(eating)typeof(PrisonPizzaEating).GetProperty("Finished").SetValue(eating,true);}office.cctv.legacyEscape.ApplyDisguise();Check(office.previous.diners.All(x=>!x.gameObject.activeSelf),"sleeping guards hidden after disguise");var night=Object.FindFirstObjectByType<PrisonNightMission>();night.dialogue=Object.Instantiate(night.dialogue);night.dialogue.lineSeconds=2;Place(player,office.cctv.safePoint.position+Vector3.up*1.1f);var friend=night.Friend;if(friend.agent.enabled&&friend.agent.isOnNavMesh)friend.agent.Warp(office.cctv.safePoint.position+Vector3.right*2);typeof(PrisonCctvMission).GetProperty("State").SetValue(office.cctv,PrisonCctvMission.Stage.Complete);step=6;wait=Time.time+1.2f;return;}
    if(step==6){var night=Object.FindFirstObjectByType<PrisonNightMission>();var head=night.Friend.animator.GetBoneTransform(HumanBodyBones.Head);File.AppendAllText(Log,"Night "+night.State+" angle "+Vector3.Angle(night.film.transform.forward,head.position-night.film.transform.position)+"\n");Check(night.State==PrisonNightMission.Stage.Evening&&Vector3.Angle(night.film.transform.forward,head.position-night.film.transform.position)<8,"waiting dialogue looks at Mingi");ScreenCapture.CaptureScreenshot("Logs/Prison-waiting-mingi.png");step=7;wait=Time.time+16;return;}
    var done=Object.FindFirstObjectByType<PrisonNightMission>();Check(done.State==PrisonNightMission.Stage.Sneak&&player.View.enabled,"evening transition restores gameplay");File.AppendAllText(Log,"FINISHED\n");Finish();
   }catch(Exception e){File.AppendAllText(Log,e.ToString());Finish();}
  }
  static void Finish(){SessionState.SetBool("PrisonDetailReview",false);SessionState.SetBool("PrisonDetailFinished",true);Time.timeScale=1;EditorApplication.ExitPlaymode();}
 }
}
#endif


