using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 작업실 삼자 대화와 운동장 전경의 배우 배치. 결투 판정은 변경하지 않는다.
public sealed class PrisonDuelIntroduction : MonoBehaviour {
 public PrisonMedicineDuel duel;
 public Transform playerActor,herald,bossMark,playerMark,mingiMark,heraldMark,groupCamera,heraldCamera;
 public Animator playerAnimator,heraldAnimator;
 bool gesturing;Renderer[] hidden;bool following;
 void Start(){playerActor.gameObject.SetActive(false);herald.gameObject.SetActive(false);}
 public IEnumerator Conversation(){var m=duel.mission;var p=m.prison.player;var mingi=m.prison.introduction.mingi;following=mingi.Following;mingi.Following=false;
 yield return m.DuelFade(1);duel.boss.SetPositionAndRotation(bossMark.position,bossMark.rotation);PlaceMingi(mingiMark.position,mingiMark.rotation);playerActor.SetPositionAndRotation(playerMark.position,playerMark.rotation);herald.SetPositionAndRotation(heraldMark.position,heraldMark.rotation);playerActor.gameObject.SetActive(true);herald.gameObject.SetActive(true);playerAnimator.Rebind();playerAnimator.Update(0);heraldAnimator.Rebind();heraldAnimator.Update(0);
 hidden=System.Array.FindAll(p.GetComponentsInChildren<Renderer>(),r=>r.enabled);foreach(var r in hidden)r.enabled=false;
 var space=FindFirstObjectByType<PrisonMissionSpace>();if(space)space.RefreshNow();m.camera.transform.SetPositionAndRotation(groupCamera.position,groupCamera.rotation);yield return m.DuelFade(0);
 foreach(string line in duel.settings.challenge){bool shout=line.StartsWith("죄수:");gesturing=shout;if(shout){yield return MoveCamera(heraldCamera,.45f);}else{Vector3 focus=line.StartsWith("김정민:")?duel.boss.position:line.StartsWith("민기:")?mingi.transform.position:playerActor.position;Quaternion from=m.camera.transform.rotation,to=Quaternion.LookRotation(focus+Vector3.up*1.35f-m.camera.transform.position);for(float t=0;t<.25f;t+=Time.deltaTime){m.camera.transform.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,t/.25f));yield return null;}}
 m.DuelSay(line);for(float t=0;t<duel.settings.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}
 gesturing=false;m.DuelSay("");
 }
 public void Arena(){herald.gameObject.SetActive(false);playerActor.SetPositionAndRotation(duel.playerSpot.position,duel.playerSpot.rotation);playerActor.gameObject.SetActive(true);playerAnimator.SetFloat("MoveSpeed",0);var pos=duel.playerSpot.position-duel.playerSpot.right*1.1f-duel.playerSpot.forward*.4f;PlaceMingi(pos,Quaternion.LookRotation(duel.bossSpot.position-pos));}
 public IEnumerator ClearRing(){var mingi=duel.mission.prison.introduction.mingi;var agent=mingi.agent;Vector3 goal=duel.center.position+Vector3.left*3.7f;if(agent.enabled&&agent.isOnNavMesh&&NavMesh.SamplePosition(goal,out var point,.6f,NavMesh.AllAreas)){mingi.ScriptedTravel=true;agent.isStopped=false;agent.speed=2.4f;agent.SetDestination(point.position);for(float t=0;t<2.5f;t+=Time.deltaTime){if(!agent.pathPending&&agent.remainingDistance<.2f)break;yield return null;}agent.ResetPath();mingi.ScriptedTravel=false;}mingi.transform.LookAt(new Vector3(duel.center.position.x,mingi.transform.position.y,duel.center.position.z));}
 void PlaceMingi(Vector3 pos,Quaternion rotation){var c=duel.mission.prison.introduction.mingi;c.ScriptedTravel=false;c.Following=false;if(c.agent.enabled&&c.agent.isOnNavMesh){c.agent.ResetPath();c.agent.Warp(pos);}else c.transform.position=pos;c.transform.rotation=rotation;c.animator.SetFloat("MoveSpeed",0);}
 IEnumerator MoveCamera(Transform mark,float duration){var cam=duel.mission.camera.transform;var p=cam.position;var r=cam.rotation;for(float t=0;t<duration;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/duration);cam.SetPositionAndRotation(Vector3.Lerp(p,mark.position,f),Quaternion.Slerp(r,mark.rotation,f));yield return null;}cam.SetPositionAndRotation(mark.position,mark.rotation);}
 void LateUpdate(){if(!gesturing||Time.timeScale<=0||!heraldAnimator.isHuman)return;var arm=heraldAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);var elbow=heraldAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);arm.rotation=Quaternion.FromToRotation(elbow.position-arm.position,Vector3.up+herald.right*.25f+herald.forward*.2f)*arm.rotation;elbow.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*6)*12,herald.forward)*elbow.rotation;var head=heraldAnimator.GetBoneTransform(HumanBodyBones.Head);head.LookAt(duel.boss.position+Vector3.up*1.5f);}
 public void Finish(){gesturing=false;if(playerActor)playerActor.gameObject.SetActive(false);if(herald)herald.gameObject.SetActive(false);if(hidden!=null){foreach(var r in hidden)if(r)r.enabled=true;hidden=null;}var mingi=duel.mission.prison.introduction.mingi;mingi.ScriptedTravel=false;mingi.Following=following;}
 void OnDisable(){if(duel)Finish();}
}
}



