#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DeliveryService.Mok.Editor
{
 public static class PrisonKeysFlowCheck
 {
  static PrisonKeysMission mission;static PlayerCombat player;static CharacterController controller;
  static string log;const string Output="../Documents/Chicken_Cooking_Flow/";
  static void Note(string s){log+=s+"\n";File.WriteAllText("Logs/PrisonKeysFlow.txt",log);}
  static void Check(bool ok,string s){if(!ok){Note("FAIL "+s);throw new Exception(s);}Note("PASS "+s);}
  static Vector2 ScreenPoint(Vector3 p){var v=mission.camera.WorldToScreenPoint(p);return new Vector2(v.x,v.y);}
  static void Mouse(Vector2 p,bool held){var s=new MouseState{position=p};if(held)s=s.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,s);}
  static void Key(bool pressed){InputSystem.QueueStateEvent(Keyboard.current,pressed?new KeyboardState(UnityEngine.InputSystem.Key.F):new KeyboardState());}
  static IEnumerator Wait(float seconds){for(float t=0;t<seconds;t+=Time.deltaTime)yield return null;}
  static IEnumerator Walk(Vector3 destination,float stop=.35f)
  {
   var path=new NavMeshPath();Vector3 feet=player.transform.position;feet.y=controller.bounds.min.y+.05f;
   bool found=NavMesh.SamplePosition(feet,out var start,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(destination,out var end,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
   Check(found,"walk route to "+destination);Note("corners "+string.Join(";",path.corners.Select(v=>v.ToString())));
   float began=Time.time;
   foreach(var corner in path.corners.Skip(1))
   {
    while(Vector3.ProjectOnPlane(corner-player.transform.position,Vector3.up).magnitude>stop)
    {
     if(mission.Failing)throw new Exception("Caught while walking at "+player.transform.position+" to "+destination);
     if(Time.time-began>25)throw new Exception("Walking blocked at "+player.transform.position+" toward "+corner);
     var direction=Vector3.ProjectOnPlane(corner-player.transform.position,Vector3.up).normalized;
     player.transform.rotation=Quaternion.LookRotation(direction);controller.Move(direction*Mathf.Min(4*Time.deltaTime,Vector3.ProjectOnPlane(corner-player.transform.position,Vector3.up).magnitude));
     yield return null;
    }
   }
   Note("Reached "+player.transform.position);
  }
  static IEnumerator InputF(){Key(true);yield return null;Key(false);yield return null;}
  public static IEnumerator Run()
  {
   bool dropCatch=File.Exists("Temp/TheftDropCheck"),noiseFail=File.Exists("Temp/TheftNoiseCheck");if(dropCatch)File.Delete("Temp/TheftDropCheck");if(noiseFail)File.Delete("Temp/TheftNoiseCheck");
   log="Runtime continuous check; CharacterController collision walking + InputSystem interaction/mouse; patrol remains active.\n";
   mission=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();player=mission.previous.prison.player;controller=player.GetComponent<CharacterController>();
   mission.previous.enabled=false;mission.Begin();var w=Wait(.5f);while(w.MoveNext())yield return null;
   foreach(var c in Resources.FindObjectsOfTypeAll<Camera>()) Note("CAM "+c.name+" active="+c.gameObject.activeInHierarchy+" enabled="+c.enabled+" scene="+c.gameObject.scene.name+" type="+c.cameraType+" depth="+c.depth+" pos="+c.transform.position+" rt="+c.targetTexture);
   foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None))Note("RAW "+r.name+" active="+r.isActiveAndEnabled+" texture="+r.texture+" size="+r.rectTransform.rect);
   Check(!Resources.FindObjectsOfTypeAll<Camera>().Any(c=>c.name=="Kitchen clutter review camera" && c.enabled),"no leaked review cameras");
      // 접근 전 정면 노출도 실패로 이어지는지 확인한 후 정상 잠입을 검사한다.
   var watch=mission.keyGuard;var watchAgent=watch.GetComponent<NavMeshAgent>();watch.enabled=false;watchAgent.isStopped=true;
   controller.enabled=false;player.transform.position=watch.transform.position+watch.transform.forward*2+Vector3.up*.9f;controller.enabled=true;
   float deadline=Time.time+2;while(!mission.Failing && Time.time<deadline)yield return null;
   Check(mission.Failing,"frontal approach caught before touching keys");
   deadline=Time.time+4;while(mission.Failing && Time.time<deadline)yield return null;
   Check(!mission.Failing&&mission.State==PrisonKeysMission.Stage.Steal&&player.View.enabled,"approach failure restores checkpoint and gameplay view");
   watch.enabled=true;watchAgent.isStopped=false;
Note("PLAYER "+player.name+" parent "+player.transform.parent+" VIEW "+player.View.name+" parent "+player.View.transform.parent+" local "+player.View.transform.localPosition);
   Note("guard "+mission.keyGuard.transform.position+" lock "+mission.keyLock.transform.position+" supplies "+string.Join(";",mission.supplies.Select(x=>x.transform.position.ToString())));
   foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name.Contains("숨을 수")))Note("cover "+t.name+" "+t.position);
   // 미션 시작 지점만 준비한다. 이후에는 순간이동 없이 충돌 이동한다.
   controller.enabled=false;player.transform.position=mission.keyGuard.transform.position-mission.keyGuard.transform.forward*1.25f+Vector3.up*.3f;controller.enabled=true;
   yield return null;
   Note("INITIAL CAMERA pos="+player.View.transform.position+" matrix="+player.View.worldToCameraMatrix.MultiplyPoint(player.View.transform.position)+" stack="+string.Join(";",player.View.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().cameraStack.Select(c=>c.name+":"+c.transform.position+":"+c.enabled)));
   var f=InputF();while(f.MoveNext())yield return null;
   var theft=mission.GetComponent<PrisonPickpocket>();Check(theft.Playing,"begin theft from behind patrol");w=Wait(.7f);while(w.MoveNext())yield return null;
   f=InputF();while(f.MoveNext())yield return null;w=Wait(.3f);while(w.MoveNext())yield return null;
   Check(!theft.Playing && player.View.enabled && !mission.camera.enabled,"cancel theft restores sole gameplay camera");
   f=InputF();while(f.MoveNext())yield return null;Check(theft.Playing,"resume theft after cancellation");w=Wait(.7f);while(w.MoveNext())yield return null;
   Vector2 mouse=ScreenPoint(mission.keyRing.transform.position);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;
   bool captured=false,receivedShot=false;var stages=new System.Collections.Generic.HashSet<PrisonPickpocket.Phase>();
   for(float t=0;t<18&&theft.Playing;t+=Time.deltaTime)
   {
    if(!receivedShot&&theft.State==PrisonPickpocket.Phase.Receive&&theft.PhaseTime>.3f){receivedShot=true;ScreenCapture.CaptureScreenshot(Output+"theft-received.png");}
    if(stages.Add(theft.State)){Note("THEFT STAGE "+theft.State);ScreenCapture.CaptureScreenshot(Output+"theft-stage-"+theft.State+".png");}
    if(theft.State==PrisonPickpocket.Phase.Catch&&!dropCatch){var catchInput=new MouseState{position=mouse}.WithButton(MouseButton.Right);InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,catchInput);}
    else if(theft.State==PrisonPickpocket.Phase.Receive||theft.State==PrisonPickpocket.Phase.Catch)Mouse(mouse,false);
    else if(theft.State==PrisonPickpocket.Phase.Freeze){if(noiseFail)mouse.x+=20;Mouse(mouse,true);}
    else{if(theft.State==PrisonPickpocket.Phase.Snag)mouse.x+=theft.SnagDirection*100*Time.deltaTime*Screen.height/1080f;else mouse.y+=100*Time.deltaTime*Screen.height/1080f;Mouse(mouse,true);}
    if(!captured&&theft.Progress>.45f){captured=true;ScreenCapture.CaptureScreenshot(Output+"keys-chain-grip.png");}
    yield return null;
   }
   Mouse(mouse,false);yield return null;
      if(noiseFail){Check(mission.Failing,"moving during guard shuffle causes detection");w=Wait(2);while(w.MoveNext())yield return null;Check(!mission.Failing&&mission.State==PrisonKeysMission.Stage.Steal&&player.View.enabled,"noise failure restores checkpoint");yield break;}
   Check(mission.State==PrisonKeysMission.Stage.Unlock,"mouse theft succeeds without stage override");
   Check(stages.Contains(PrisonPickpocket.Phase.Snag)&&stages.Contains(PrisonPickpocket.Phase.Freeze)&&stages.Contains(PrisonPickpocket.Phase.Catch),"snag, freeze and catch phases reached through mouse input");
   if(dropCatch){Check(mission.Suspicion>.5f&&!mission.Failing,"missed catch keeps keys and gives escape window");yield break;}
   Vector3 outside=mission.keyLock.transform.position-mission.keyLock.transform.forward*1.1f;outside.y=0;
   var walk=Walk(outside);while(walk.MoveNext())yield return null;
   f=InputF();while(f.MoveNext())yield return null;
   var action=mission.GetComponent<PrisonLockInteraction>();Check(action.Playing,"reach warehouse on foot and open key ring");
   w=Wait(.7f);while(w.MoveNext())yield return null;
   Vector2 KeyPoint(int i){var k=mission.keyLock.keyModels[i].transform;return ScreenPoint(k.position-k.forward*.078f);}
   mouse=KeyPoint(0);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;
   for(float t=0;t<.6f;t+=Time.deltaTime){mouse.y+=100*Time.deltaTime*Screen.height/1080f;Mouse(mouse,true);yield return null;}
   Check(action.State==PrisonLockInteraction.Phase.Inserting&&action.Insertion<=.56f,"wrong key stops halfway instead of turning");
   var keyAudio=mission.GetComponent<PrisonTheftAudio>();Check(keyAudio&&keyAudio.JingleCount==0,"quiet theft and lock do not jingle below threshold");
   if(File.Exists("Temp/LockNoiseCheck")){
    File.Delete("Temp/LockNoiseCheck");
    for(float t=0;t<3&&!mission.Failing;t+=Time.deltaTime){mouse.y+=25*Screen.height/1080f;Mouse(mouse,true);yield return null;}
    Check(keyAudio.JingleCount>0&&mission.Failing,"forcing wrong key crosses sound threshold and triggers detection");
    Mouse(mouse,false);w=Wait(2.5f);while(w.MoveNext())yield return null;
    Check(!mission.Failing&&mission.State==PrisonKeysMission.Stage.Steal&&player.View.enabled,"lock noise failure restores playable checkpoint");yield break;
   }
   InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState{position=mouse}.WithButton(MouseButton.Right));yield return null;Mouse(mouse,false);
   w=Wait(.8f);while(w.MoveNext())yield return null;
   Check(action.State==PrisonLockInteraction.Phase.Choose,"wrong key pulled out for next choice");   float waitStart=Time.time;
   var diversion=mission.GetComponent<PrisonWarehouseDiversion>();
   for(float t=0;t<25&&!diversion.TalkReady;t+=Time.deltaTime)yield return null;
   Note("TALK mingi="+mission.previous.prison.introduction.mingi.transform.position+" destination="+mission.previous.prison.introduction.mingi.agent.destination+" remaining="+mission.previous.prison.introduction.mingi.agent.remainingDistance+" path="+mission.previous.prison.introduction.mingi.agent.pathStatus); Check(diversion.TalkReady&&!mission.keyGuard.enabled&&mission.keyGuard.GetComponent<NavMeshAgent>().isStopped,"Mingi holds guard in conversation before door opens");   ScreenCapture.CaptureScreenshot(Output+"keys-chain-warning.png");   f=InputF();while(f.MoveNext())yield return null;
   Check(!action.Playing && player.CanAct,"F frees movement before detection");
   var cover=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(x=>x.name.Contains("숨을 수 있는"));
   Check(cover,"physical hiding cover exists");
   Vector3 hide=cover.position+(cover.position-mission.keyGuard.transform.position).normalized*1.1f;hide.y=0;
   walk=Walk(hide);while(walk.MoveNext())yield return null;
   w=Wait(1.5f);while(w.MoveNext())yield return null;
   Note("HIDE player="+player.transform.position+" VIEW="+player.View.transform.position+" child="+player.View.transform.IsChildOf(player.transform)+" cameras="+string.Join(";",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.enabled).Select(c=>c.name+":"+c.transform.position+" depth="+c.depth)));
   Check(player.View.enabled && !mission.camera.enabled,"return blend ends with gameplay camera only");
   Check(!mission.keyGuard.CanSee(player)&&mission.Suspicion<=.05f&&!mission.Failing,"cover blocks patrol sight and suspicion clears");
   ScreenCapture.CaptureScreenshot(Output+"keys-chain-hidden.png");
   Note("ALL CAMERAS "+string.Join(";",Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()).Select(c=>c.name+":"+c.enabled+":"+c.gameObject.activeInHierarchy+":"+c.transform.position+":target="+c.targetTexture+":flags="+c.hideFlags)));
   Note("VIEW MATRIX origin="+player.View.worldToCameraMatrix.MultiplyPoint(player.View.transform.position)+" stack="+string.Join(";",player.View.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().cameraStack.Select(c=>c.name+":"+c.transform.position+":"+c.enabled)));
   waitStart=Time.time;
   while(Vector3.Dot(mission.keyGuard.transform.forward,(outside-mission.keyGuard.transform.position).normalized)>.1f && Time.time-waitStart<30)yield return null;
   walk=Walk(outside);while(walk.MoveNext())yield return null;
   f=InputF();while(f.MoveNext())yield return null;Check(action.Playing,"return from cover and resume lock");
   w=Wait(.7f);while(w.MoveNext())yield return null;
   mouse=KeyPoint(mission.keyLock.correctKey);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;
   for(float t=0;t<1.15f;t+=Time.deltaTime){mouse.y+=100*Time.deltaTime*Screen.height/1080f;Mouse(mouse,true);yield return null;}
   Check(action.State==PrisonLockInteraction.Phase.Turn&&action.MatchedKey,"correct key seats through upward drag");
   for(float t=0;t<7&&action.Playing;t+=Time.deltaTime){
    if(!action.AtSeat)mouse.x+=100*Time.deltaTime*Screen.height/1080f;
    Mouse(mouse,true);if(action.AtSeat)ScreenCapture.CaptureScreenshot(Output+"keys-new-seat.png");yield return null;
   }   Mouse(mouse,false);w=Wait(1.2f);while(w.MoveNext())yield return null;
   for(float t=0;t<35&&mission.State!=PrisonKeysMission.Stage.Gather;t+=Time.deltaTime)yield return null;
   Check(mission.State==PrisonKeysMission.Stage.Gather && mission.keyLock.Opened,"turning opens warehouse in same run");
   Check(!mission.keyGuard.enabled&&Vector3.Distance(mission.keyGuard.transform.position,mission.keyLock.transform.position)>11,"guard walks away before ingredient collection");
   Vector3 inside=mission.keyLock.transform.position+mission.keyLock.transform.forward*1.3f-mission.keyLock.transform.right*.5f;inside.y=0;
   walk=Walk(inside);while(walk.MoveNext())yield return null;
   foreach(var item in mission.supplies)
   {
    Vector3 point=item.transform.position-mission.keyLock.transform.forward*1.2f;point.y=0;
    walk=Walk(point,.55f);while(walk.MoveNext())yield return null;
    f=InputF();while(f.MoveNext())yield return null;
   }
   Check(mission.Gathered==4 && mission.State==PrisonKeysMission.Stage.Rejoin,"ingredients lead to reunion objective");
   walk=Walk(mission.checkpoint.position);while(walk.MoveNext())yield return null;
   for(float t=0;t<30&&mission.State!=PrisonKeysMission.Stage.Cook;t+=Time.deltaTime)yield return null;
   var companion=mission.previous.prison.introduction.mingi;Note("REUNION ready="+mission.DiversionReady+" pos="+companion.transform.position+" remaining="+companion.agent.remainingDistance+" stop="+companion.agent.stoppingDistance+" path="+companion.agent.pathStatus);
   Check(mission.State==PrisonKeysMission.Stage.Cook&&mission.previous.prison.introduction.mingi.Following,"reunion restores companion and cooking objective");
   Check(player.CanAct && player.View.enabled,"movement and camera restored at end");
   ScreenCapture.CaptureScreenshot(Output+"keys-chain-complete.png");w=Wait(.3f);while(w.MoveNext())yield return null;
  }
 }
}
#endif


