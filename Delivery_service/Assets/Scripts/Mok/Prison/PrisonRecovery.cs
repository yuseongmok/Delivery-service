using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 씬을 다시 로드하지 않아 진행 단계, 미션 소품과 완료 상태를 보존한다.
public sealed class PrisonRecovery : MonoBehaviour {
 public PrisonMission mission;public Transform infirmarySpawn;
 public bool Recovering {get;private set;}
 public int Recoveries {get;private set;}
 float fade;bool waking;float graceUntil;
 public bool BlocksDamage {get {var p=mission.player;var move=p.GetComponent<PlayerMove>();return Recovering||Time.time<graceUntil||Time.timeScale<=0||mission.Failing||!move.isControllable;}}
 public IEnumerator Recover(Vector3 direction){
  if(Recovering)yield break;Recovering=true;waking=false;
  foreach(var citizen in FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None))citizen.EndFight();
  var cart=mission.cart;if(cart&&cart.isActiveAndEnabled&&cart.controller&&cart.controller.isActiveAndEnabled&&cart.controller.cartRigidbody&&!cart.controller.cartRigidbody.isKinematic)cart.StopPushing();
  var blackout=StartCoroutine(Darken());
  yield return PlayerKnockdown.Roll(mission.player.gameObject,3.5f,direction,infirmarySpawn);
  yield return blackout;
  foreach(var guard in FindObjectsByType<PrisonGuard>(FindObjectsInactive.Include,FindObjectsSortMode.None))guard.ResetWatch();
  var p=mission.player;var move=p.GetComponent<PlayerMove>();var look=p.GetComponentInChildren<CameraMove>(true);var cc=p.GetComponent<CharacterController>();
  cc.enabled=false;p.transform.SetPositionAndRotation(infirmarySpawn.position,infirmarySpawn.rotation);cc.enabled=true;
  p.View.transform.localRotation=Quaternion.identity;if(look){look.SyncViewFromTransform();look.enabled=true;look.isControllable=false;}move.enabled=true;move.isControllable=false;
  p.ResetAfterWaterRescue();
  var mingi=mission.introduction?mission.introduction.mingi:null;
  if(mingi&&mingi.Following&&mingi.agent.enabled&&mingi.agent.isOnNavMesh&&NavMesh.SamplePosition(infirmarySpawn.position+infirmarySpawn.right*1.3f,out var n,1,NavMesh.AllAreas))mingi.agent.Warp(n.position);
  waking=true;yield return new WaitForSeconds(.5f);
  for(float t=0;t<1.3f;t+=Time.deltaTime){fade=1-Mathf.SmoothStep(0,1,t/1.3f);yield return null;}
  fade=0;waking=false;move.isControllable=true;if(look){look.isControllable=true;look.SyncViewFromTransform();}Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  graceUntil=Time.time+3;Recoveries++;Recovering=false;
 }
 IEnumerator Darken(){yield return new WaitForSeconds(.9f);for(float t=0;t<.7f;t+=Time.deltaTime){fade=t/.7f;yield return null;}fade=1;}
 void OnGUI(){if(!Recovering||fade<=0||Time.timeScale<=0)return;var old=GUI.color;GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);if(waking){GUI.color=new Color(1,.95f,.85f,fade);GUI.Label(new Rect(0,Screen.height*.45f,Screen.width,70),"의무실에서 정신을 차렸다",new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.027f),alignment=TextAnchor.MiddleCenter});}GUI.color=old;}
}
}
