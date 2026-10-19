#if UNITY_EDITOR
using System;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class KickVisibilityTest {
 public static IEnumerator RunCore(Action<string> log){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();p.GetComponent<PlayerMove>().enabled=false;p.GetComponent<PlayerMove>().isControllable=true;
  foreach(var look in UnityEngine.Object.FindObjectsByType<CameraMove>(FindObjectsSortMode.None))look.enabled=false;
  p.enabled=false;
  UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();Cursor.lockState=CursorLockMode.Locked;
  foreach(var bike in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))bike.isDriven=false;
  p.GetComponent<PlayerWeapon>().Equip(WeaponKind.None);var avatar=p.avatar;
  Check(avatar!=null&&avatar.animator.isHuman,"실제 플레이어 휴머노이드 사용");
  var hip=avatar.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);var knee=avatar.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);var foot=avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot);
  float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,foot.position);
  p.View.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(p.transform.forward,Vector3.up),Vector3.up);
  foreach(float t in new[]{.12f,.22f,.32f,.44f,.64f}){
   avatar.PoseKick(t);yield return null;yield return null;
   Check(Mathf.Abs(Vector3.Distance(hip.position,knee.position)-a)<.015f&&Mathf.Abs(Vector3.Distance(knee.position,foot.position)-b)<.015f,"관절 길이 보존 "+t);
   if(t==.22f||t==.32f){var screen=p.View.WorldToViewportPoint(foot.position);log("Foot viewport "+screen+"; camera="+p.View.transform.position+" hip="+hip.position+" foot="+foot.position);Check(screen.z>.2f&&screen.x>.05f&&screen.x<.95f&&screen.y>.08f&&screen.y<.65f,"정면 시야 하단에 발 표시·카메라 관통 방지 "+t);}
   if(t==.22f){ScreenCapture.CaptureScreenshot("Temp/Mok-kick-forward-preview.png",2);yield return null;yield return null;}
  }
  avatar.StopKick();yield return null;Check(PlayerAvatar.HitTime==.22f&&PlayerAvatar.KickDuration==.65f,"기존 타격 시점과 전체 동작 시간 유지");
  p.enabled=true;Check(p.TryKick(),"실제 입력 경로로 발차기 시작");float end=Time.time+1;while(Time.time<end)yield return null;Check(!p.IsKicking,"발차기 종료 및 상태 복구");
 }
}
}
#endif
