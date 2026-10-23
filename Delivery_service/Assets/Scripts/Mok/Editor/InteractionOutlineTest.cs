#if UNITY_EDITOR
using System;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class InteractionOutlineTest {
 public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var input=p.GetComponent<PlayerInteraction>();var hover=p.GetComponent<InteractHoverManager>();var w=p.GetComponent<PlayerWeapon>();
 p.GetComponent<PlayerMove>().enabled=false;p.GetComponent<PlayerMove>().isControllable=true;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;
 UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();Cursor.lockState=CursorLockMode.Locked;
 var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(0,80,0);p.View.transform.rotation=Quaternion.identity;
 Check(hover!=null,"씬의 기존 아웃라인 관리자 연결");
 foreach(WeaponKind kind in new[]{WeaponKind.Bat,WeaponKind.Tuna,WeaponKind.Brick,WeaponKind.Pistol}){
  w.Equip(kind);float until=Time.time+.35f;while(Time.time<until)yield return null;Check(w.Attack(true),kind+" 투척");var drop=w.Thrown;drop.GetComponent<Rigidbody>().isKinematic=true;
  var col=drop.GetComponent<BoxCollider>();drop.transform.rotation=Quaternion.identity;drop.transform.position=p.View.transform.position+Vector3.forward*2-drop.transform.TransformVector(col.center);
  foreach(var renderer in drop.GetComponentsInChildren<Renderer>())if(renderer.gameObject!=drop.gameObject)renderer.gameObject.layer=0;
  Physics.SyncTransforms();until=Time.time+.6f;while(Time.time<until)yield return null;
  Check(InteractionFocus.Find(input)==drop.transform&&hover.CurrentTarget==drop.transform,kind+" 회수 가능한 대상과 아웃라인 일치");
  Check(drop.GetComponent<Outline>()!=null&&drop.GetComponent<Outline>().enabled,kind+" 자식 모델 레이어와 무관하게 아웃라인 표시");
  Check(hover.interactText.text=="F를 눌러 줍기"&&hover.interactText.gameObject.activeInHierarchy,kind+" 줍기 안내");
  if(kind==WeaponKind.Bat){ScreenCapture.CaptureScreenshot("Temp/Mok-weapon-marker.png");yield return null;}
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=p.View.transform.position+Vector3.forward;wall.transform.localScale=new Vector3(2,2,.1f);Physics.SyncTransforms();
  until=Time.time+.12f;while(Time.time<until)yield return null;Check(hover.CurrentTarget==null,kind+" 벽 너머 강조 차단");UnityEngine.Object.Destroy(wall);yield return null;
  Cursor.lockState=CursorLockMode.None;yield return null;Check(hover.CurrentTarget==null,"메뉴 입력 상태에서 강조 해제");Cursor.lockState=CursorLockMode.Locked;
  drop.Interact(p.GetComponent<ToppingInventory>());yield return null;until=Time.time+.12f;while(Time.time<until)yield return null;
  Check(w.HasInHand&&drop==null&&hover.CurrentTarget==null,kind+" 회수 후 표시 제거");
 }
 }
}
}
#endif
