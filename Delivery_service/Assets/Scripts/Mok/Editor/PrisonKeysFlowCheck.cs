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
   log="Runtime continuous check; CharacterController collision walking + InputSystem interaction/mouse; patrol remains active.\n";
   mission=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();player=mission.previous.prison.player;controller=player.GetComponent<CharacterController>();
   mission.previous.enabled=false;mission.Begin();var w=Wait(.5f);while(w.MoveNext())yield return null;
   foreach(var c in Resources.FindObjectsOfTypeAll<Camera>()) Note("CAM "+c.name+" active="+c.gameObject.activeInHierarchy+" enabled="+c.enabled+" scene="+c.gameObject.scene.name+" type="+c.cameraType+" depth="+c.depth+" pos="+c.transform.position+" rt="+c.targetTexture);
   foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None))Note("RAW "+r.name+" active="+r.isActiveAndEnabled+" texture="+r.texture+" size="+r.rectTransform.rect);
   Check(!Resources.FindObjectsOfTypeAll<Camera>().Any(c=>c.name=="Kitchen clutter review camera" && c.enabled),"no leaked review cameras");
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
   bool captured=false;
   for(float t=0;t<8&&theft.Playing;t+=Time.deltaTime)
   {
    mouse.y+=100*Time.deltaTime*Screen.height/1080f;Mouse(mouse,true);
    if(!captured&&theft.Progress>.45f){captured=true;ScreenCapture.CaptureScreenshot(Output+"keys-chain-grip.png");}
    yield return null;
   }
   Mouse(mouse,false);yield return null;
   Check(mission.State==PrisonKeysMission.Stage.Unlock,"mouse theft succeeds without stage override");
   Vector3 outside=mission.keyLock.transform.position-mission.keyLock.transform.forward*1.1f;outside.y=0;
   var walk=Walk(outside);while(walk.MoveNext())yield return null;
   f=InputF();while(f.MoveNext())yield return null;
   var action=mission.GetComponent<PrisonLockInteraction>();Check(action.Playing,"reach warehouse on foot and open key ring");
   w=Wait(.7f);while(w.MoveNext())yield return null;
   Vector2 KeyPoint(int i){var k=mission.keyLock.keyModels[i].transform;return ScreenPoint(k.position-k.forward*.078f);}
   mouse=KeyPoint(0);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;Mouse(mouse,false);
   w=Wait(.8f);while(w.MoveNext())yield return null;
   Check(action.State==PrisonLockInteraction.Phase.Turn,"wrong key physically inserted");
   mouse=KeyPoint(0);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;
   for(int i=0;i<25;i++){mouse.x+=4*Screen.height/1080f;Mouse(mouse,true);yield return null;}
   Mouse(mouse,false);w=Wait(.8f);while(w.MoveNext())yield return null;
   Check(action.State==PrisonLockInteraction.Phase.Choose,"wrong key released for next choice state="+action.State+" playing="+action.Playing+" failed="+mission.Failing+" mission="+mission.State);
   float waitStart=Time.time;
   while(!action.GuardLooking && Time.time-waitStart<15 && !mission.Failing){if(Time.frameCount%120==0){var agent=mission.keyGuard.GetComponent<NavMeshAgent>();Note("PATROL pos="+agent.transform.position+" forward="+agent.transform.forward+" enabled="+mission.keyGuard.enabled+" stop="+agent.isStopped+" path="+agent.hasPath+" pending="+agent.pathPending+" destination="+agent.destination+" velocity="+agent.velocity+" hold="+mission.keyGuard.holdPost+" meal="+mission.keyGuard.receivingMeal+" missionTalk="+mission.previous.prison.InConversation);}yield return null;}
   Check(action.GuardLooking&&!mission.Failing,"live patrol looks toward lock and warns");
   ScreenCapture.CaptureScreenshot(Output+"keys-chain-warning.png");
   f=InputF();while(f.MoveNext())yield return null;
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
   mouse=KeyPoint(mission.keyLock.correctKey);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;Mouse(mouse,false);
   w=Wait(.8f);while(w.MoveNext())yield return null;
   Check(action.State==PrisonLockInteraction.Phase.Turn,"correct key seated after returning");
   mouse=KeyPoint(mission.keyLock.correctKey);Mouse(mouse,false);yield return null;Mouse(mouse,true);yield return null;
   for(int i=0;i<60&&action.Playing;i++){mouse.x+=4*Screen.height/1080f;Mouse(mouse,true);if(i==25)ScreenCapture.CaptureScreenshot(Output+"keys-chain-turn.png");yield return null;}
   Mouse(mouse,false);w=Wait(1.2f);while(w.MoveNext())yield return null;
   Check(mission.State==PrisonKeysMission.Stage.Gather && mission.keyLock.Opened,"turning opens warehouse in same run");
   Vector3 inside=mission.keyLock.transform.position+mission.keyLock.transform.forward*1.3f-mission.keyLock.transform.right*.5f;inside.y=0;
   walk=Walk(inside);while(walk.MoveNext())yield return null;
   foreach(var item in mission.supplies)
   {
    Vector3 point=item.transform.position-mission.keyLock.transform.forward*1.2f;point.y=0;
    walk=Walk(point,.55f);while(walk.MoveNext())yield return null;
    f=InputF();while(f.MoveNext())yield return null;
   }
   Check(mission.Gathered==4 && mission.State==PrisonKeysMission.Stage.Cook,"all four ingredients picked up and pizza cooking objective follows");
   Check(player.CanAct && player.View.enabled,"movement and camera restored at end");
   ScreenCapture.CaptureScreenshot(Output+"keys-chain-complete.png");w=Wait(.3f);while(w.MoveNext())yield return null;
  }
 }
}
#endif

