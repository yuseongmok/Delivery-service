using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok {
[RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
public sealed class SlumRock : MonoBehaviour {
 public Transform landingWarning;
 Rigidbody body;Transform owner;bool launched,finished;float expires;
 public bool InFlight=>launched&&!finished;
 public static Vector3 LaunchVelocity(Vector3 from,Vector3 to,float seconds)=>(to-from)/seconds-Physics.gravity*(seconds*.5f);
 public void Launch(Transform source,Vector3 target,float flightSeconds){
  owner=source;body=GetComponent<Rigidbody>();body.isKinematic=false;body.useGravity=true;
  body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  body.linearVelocity=LaunchVelocity(transform.position,target,flightSeconds);body.angularVelocity=new Vector3(3,5,2);
  foreach(var own in source.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(GetComponent<Collider>(),own);
  if(landingWarning!=null){landingWarning.SetParent(null);landingWarning.position=target+Vector3.up*.03f;landingWarning.rotation=Quaternion.identity;landingWarning.gameObject.SetActive(true);}
  launched=true;expires=Time.time+flightSeconds+3;
 }
 void FixedUpdate(){
  if(!InFlight)return;
  if(Time.time>=expires){Expire();return;}
  // 고속 돌이 캐릭터 컨트롤러나 얇은 벽을 통과하지 않도록 다음 물리 구간도 검사한다.
  Vector3 step=body.linearVelocity*Time.fixedDeltaTime+Physics.gravity*(.5f*Time.fixedDeltaTime*Time.fixedDeltaTime);
  if(step.sqrMagnitude<.000001f)return;
  foreach(var hit in Physics.SphereCastAll(transform.position,.20f,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.transform.IsChildOf(transform)||(owner!=null&&hit.transform.IsChildOf(owner)))continue;
   Impact(hit.collider);break;
  }
 }
 void OnCollisionEnter(Collision collision){if(InFlight&&(owner==null||!collision.transform.IsChildOf(owner)))Impact(collision.collider);}
 void Impact(Collider hit){if(finished)return;var player=hit.GetComponentInParent<PlayerCombat>();if(player!=null&&player.CanAct)player.ReceivePunch(10,body.linearVelocity.normalized,true);Expire();}
 public void Expire(){if(finished)return;finished=true;if(landingWarning!=null)Destroy(landingWarning.gameObject);gameObject.SetActive(false);Destroy(gameObject);}
 void OnDestroy(){if(Application.isPlaying&&landingWarning!=null)Destroy(landingWarning.gameObject);}
}
}
