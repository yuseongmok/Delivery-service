using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 시민의 관절 물리를 재사용하되 경찰 추격과 체포는 기존 컨트롤러가 맡는다.
public sealed class PoliceCombat:MonoBehaviour {
 public int Health {get;private set;}=100;public bool Armed {get;private set;}
 public bool IsDead=>Health<=0;public bool IsDown=>npc!=null&&(npc.IsKnockedDown||npc.IsDead);
 NPCControl npc;PoliceControl police;NavMeshAgent agent;Animator animator;PlayerCombat target;Transform gun;
 float nextShot;bool aiming;Light flash;
 void Awake(){police=GetComponent<PoliceControl>();agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();}
 void EnsureRagdoll(){if(npc!=null)return;npc=GetComponent<NPCControl>()??gameObject.AddComponent<NPCControl>();npc.ExternalControl=true;npc.enabled=false;foreach(var body in GetComponentsInChildren<Rigidbody>())if(body.gameObject!=gameObject)body.useGravity=true;if(animator!=null&&animator.isHuman)npc.SetCombatRagdollCenter(animator.GetBoneTransform(HumanBodyBones.Hips));}
 public bool ReceiveHit(PlayerCombat attacker,Vector3 direction,int damage){
  if(attacker==null||damage<=0||IsDead||IsDown||PoliceControl.IsArrestInProgress)return false;
  target=attacker;FindFirstObjectByType<PoliceManager>()?.SpawnArmedSupport(attacker.transform);
  Health=Mathf.Max(0,Health-damage);EnsureRagdoll();aiming=false;
  if(IsDead){FindFirstObjectByType<PoliceManager>()?.ReportDeath(police,Armed,attacker.transform);npc.Die(direction,8);return true;}npc.KnockDown(direction,5);StartCoroutine(Recover());return true;
 }
 IEnumerator Recover(){yield return new WaitForSeconds(3);while(!IsDead&&!npc.RecoverFromKnockDown())yield return new WaitForSeconds(.5f);nextShot=Time.time+1.5f;}
 public void Arm(){Armed=true;target=FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);nextShot=Time.time+2;
  var model=WeaponCatalog.Model(WeaponKind.Pistol);if(model!=null){gun=Instantiate(model,transform).transform;gun.name="Police pistol";foreach(var c in gun.GetComponentsInChildren<Collider>())Destroy(c);}
 }
 public bool HoldForShot(Transform activeTarget){
  if(!Armed||IsDown||agent==null||!agent.enabled||!agent.isOnNavMesh)return false;
  bool hold=target!=null&&target.CanReceiveBullet&&Vector3.Distance(transform.position,activeTarget.position)<13&&ClearShot();agent.isStopped=hold;return hold;
 }
 public bool ClearShot(){
  if(target==null||target.View==null)return false;Vector3 from=transform.position+Vector3.up*1.35f,to=target.View.transform.position;
  if(Vector3.Distance(from,to)>18)return false;
  foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.transform.IsChildOf(transform))continue;return hit.transform.IsChildOf(target.transform);
  }return true;
 }
 void Update(){
  if(!Armed||IsDown||IsDead||target==null||PoliceControl.IsArrestInProgress){aiming=false;return;}
  aiming=target.CanReceiveBullet&&ClearShot();if(!aiming)return;
  Vector3 flat=target.transform.position-transform.position;flat.y=0;if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),360*Time.deltaTime);
  if(Time.time<nextShot||Vector3.Angle(transform.forward,flat)>12)return;
  nextShot=Time.time+2.2f;GunshotSound.Play(gameObject,true);target.ReceiveBullet((target.transform.position-transform.position).normalized);StartCoroutine(Muzzle());
 }
 IEnumerator Muzzle(){if(gun==null)yield break;var go=new GameObject("Police muzzle",typeof(Light));go.transform.position=gun.position+transform.forward*.15f;flash=go.GetComponent<Light>();flash.color=new Color(1,.65f,.25f);flash.intensity=3;flash.range=2;yield return new WaitForSeconds(.06f);if(go!=null)Destroy(go);}
 void LateUpdate(){
  if(gun==null||animator==null||!animator.isHuman)return;
  var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
  if(aiming&&!IsDown){var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var aim=(target.View.transform.position-upper.position).normalized;upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,aim)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,aim)*lower.rotation;}
  gun.SetPositionAndRotation(hand.position,aiming?Quaternion.LookRotation(target.View.transform.position-hand.position):transform.rotation);
 }
 void OnDestroy(){if(flash!=null)Destroy(flash.gameObject);}
}
}
