#if UNITY_EDITOR
using System;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class WeaponThrowTest {
 public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var w=p.GetComponent<PlayerWeapon>();
 p.GetComponent<PlayerMove>().enabled=false;p.GetComponent<PlayerMove>().isControllable=true;
 foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;
 UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();Cursor.lockState=CursorLockMode.Locked;
 foreach(var b in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))b.isDriven=false;
 MoneyManager.Instance.currentMoney=100000;
 foreach(WeaponKind kind in new[]{WeaponKind.Bat,WeaponKind.Tuna,WeaponKind.Brick,WeaponKind.Pistol}){
  w.Equip(kind);float until=Time.time+.35f;while(Time.time<until)yield return null;
  Check(w.Attack(true)&&w.Thrown!=null&&!w.HasInHand,kind+" 우클릭 투척 및 빈손 전환");
  var thrown=w.Thrown;Check(thrown.kind==kind&&thrown.GetComponent<BoxCollider>().size.sqrMagnitude>.01f,kind+" 동일 모델과 크기별 충돌체");
  Check(!w.Attack(true)&&!w.Attack(false),kind+" 투척 후 무기 중복 공격 차단");
  Check(p.TryKick(),kind+" 회수 전 발차기 가능");
  until=Time.time+1;while(Time.time<until)yield return null;
  thrown.Interact(p.GetComponent<ToppingInventory>());Check(w.HasInHand&&w.Thrown==null,kind+" 상호작용 회수");yield return null;
 }
 Check(w.Ammo==0,"탄약 없는 권총도 투척 가능");Check(w.BuyAmmo()&&w.Ammo==10,"권총 탄약 준비");
 float end=Time.time+1;while(Time.time<end)yield return null;
 Check(w.Attack(true),"탄약 있는 권총 투척");var gun=w.Thrown;w.Recover(gun);Check(w.Ammo==10,"권총 회수 후 탄약 보존");yield return null;
 end=Time.time+1;while(Time.time<end)yield return null;
 w.Attack(true);gun=w.Thrown;w.Equip(WeaponKind.Bat);yield return null;Check(gun==null&&w.Thrown==null,"새 무기 장착 시 이전 투척물 제거");
 var avatar=p.avatar;Check(avatar!=null&&avatar.animator.isHuman,"실제 플레이어 휴머노이드 확인");
 var hip=avatar.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);var knee=avatar.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);var foot=avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot);
 float upper=Vector3.Distance(hip.position,knee.position),lower=Vector3.Distance(knee.position,foot.position);
 w.Equip(WeaponKind.None);p.View.transform.localRotation=Quaternion.Euler(28,0,0);
 foreach(float t in new[]{.12f,.22f,.38f,.55f,.64f}){
  avatar.PoseKick(t);yield return null;yield return null;
  Check(Mathf.Abs(Vector3.Distance(hip.position,knee.position)-upper)<.015f&&Mathf.Abs(Vector3.Distance(knee.position,foot.position)-lower)<.015f,"발차기 관절 길이 보존 "+t);
  if(t==.22f){ScreenCapture.CaptureScreenshot("Temp/Mok-kick-extended.png");yield return null;}
 }
 avatar.StopKick();yield return null;Check(!p.IsKicking,"발차기 종료 후 입력 상태 정상");
 }
}
}
#endif
