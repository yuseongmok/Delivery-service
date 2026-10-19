using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 체포 시 잡은 경찰을 보여주고 경찰서 도착 후 시점 조작을 복원한다.
public sealed class PoliceArrestPresentation : MonoBehaviour {
 Transform player,officer,view,head;CameraMove[] looks;bool[] lookEnabled;
 PhoneController phone;bool phoneEnabled;bool active,showMessage;
 public void Begin(Transform target,Transform police){
  player=target;officer=police;if(player==null)return;
  phone=FindFirstObjectByType<PhoneController>();if(phone!=null){phoneEnabled=phone.enabled;phone.ClosePhone();phone.enabled=false;}
  var combat=player.GetComponent<PlayerCombat>();var camera=combat!=null?combat.View:player.GetComponentInChildren<Camera>();
  view=camera!=null?camera.transform:null;
  looks=player.GetComponentsInChildren<CameraMove>(true);lookEnabled=new bool[looks.Length];
  for(int i=0;i<looks.Length;i++){lookEnabled[i]=looks[i].enabled;looks[i].enabled=false;}
  var animator=officer.GetComponentInChildren<Animator>();if(animator!=null&&animator.isHuman)head=animator.GetBoneTransform(HumanBodyBones.Head);
  var direction=Vector3.ProjectOnPlane(player.position-officer.position,Vector3.up);
  if(direction.sqrMagnitude>.001f)officer.rotation=Quaternion.LookRotation(direction);
  active=true;showMessage=true;
 }
 public IEnumerator FaceOfficer(){
  if(!active||view==null)yield break;
  Quaternion startBody=player.rotation,startView=view.rotation;
  for(float elapsed=0;elapsed<1.95f;elapsed+=Time.unscaledDeltaTime){
   var aim=head!=null?head.position:officer.position+Vector3.up*1.6f;
   var horizontal=Vector3.ProjectOnPlane(aim-player.position,Vector3.up);
   float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.45f));
   if(horizontal.sqrMagnitude>.001f)player.rotation=Quaternion.Slerp(startBody,Quaternion.LookRotation(horizontal),blend);
   var direction=aim-view.position;
   if(direction.sqrMagnitude>.001f)view.rotation=Quaternion.Slerp(startView,Quaternion.LookRotation(direction),blend);
   yield return null;
  }
  showMessage=false;
 }
 public void AlignAtStation(){if(view!=null)view.localRotation=Quaternion.identity;}
 public void Finish(){
  if(!active)return;active=false;showMessage=false;
  if(view!=null)view.localRotation=Quaternion.identity;
  for(int i=0;i<looks.Length;i++)if(looks[i]!=null){looks[i].SyncViewFromTransform();looks[i].enabled=lookEnabled[i];}
  if(phone!=null)phone.enabled=phoneEnabled;
 }
 void OnDestroy(){Finish();}
 void OnGUI(){
  if(!showMessage)return;
  var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.035f),alignment=TextAnchor.MiddleCenter};
  style.normal.textColor=Color.white;
  var rect=new Rect(0,Screen.height*.76f,Screen.width,Screen.height*.08f);
  GUI.Label(rect,"체포되었습니다",style);
 }
}}
