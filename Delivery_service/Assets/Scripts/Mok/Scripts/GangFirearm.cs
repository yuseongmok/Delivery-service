using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
[DefaultExecutionOrder(2000)]
public sealed class GangFirearm:MonoBehaviour {
 public bool ControlledAim {get;set;}
 public void LowerWeapon(){aiming=false;ControlledAim=false;}
 public Transform gun;public int Shots{get;private set;}float nextShot;bool aiming;PlayerCombat target;CitizenCombat combat;Animator animator;
 void Awake(){combat=GetComponent<CitizenCombat>();animator=GetComponentInChildren<Animator>();if(combat.usesFirearm&&!gun){var prefab=WeaponCatalog.Model(WeaponKind.Pistol);if(prefab){gun=Instantiate(prefab,transform).transform;foreach(var c in gun.GetComponentsInChildren<Collider>())Destroy(c);}}}
 public bool Tick(PlayerCombat player,NavMeshAgent agent){target=player;aiming=false;if(!combat.usesFirearm||!gun||!player||combat.Npc.IsDead||combat.Npc.IsKnockedDown)return false;
  var point=player.View.transform.position;var delta=point-(transform.position+Vector3.up*1.3f);
  bool clear=!FirearmBallistics.Cast(transform.position+Vector3.up*1.3f,delta.normalized,delta.magnitude,transform,out var obstruction)||IsTarget(obstruction.transform,player);
  if(delta.magnitude>16||!clear)return false;aiming=true;agent.isStopped=true;var flat=Vector3.ProjectOnPlane(delta,Vector3.up);if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),180*Time.deltaTime);
  if(Time.time>=nextShot&&Vector3.Angle(transform.forward,flat)<15&&player.CanReceiveHit){nextShot=Time.time+2.6f+Random.value;Shots++;GunshotSound.Play(gameObject,true);bool hit=Random.value<.35f;var end=point+(hit?Vector3.zero:transform.right*.9f);var muzzle=HandgunGeometry.Get(gun).Muzzle;var start=gun.TransformPoint(muzzle);GunShotFeedback.Fire(gun,muzzle);if(FirearmBallistics.Cast(start,(end-start).normalized,Vector3.Distance(start,end),transform,out var wall)){BulletImpactFeedback.Play(wall);if(!IsTarget(wall.transform,player)){end=wall.point;hit=false;}}PrologueTracer.Spawn(start,end,new Color(1,.65f,.2f));if(hit)player.ReceiveBullet((point-start).normalized);}
  return true;
 }
 static bool IsTarget(Transform hit,PlayerCombat player)=>hit.IsChildOf(player.transform)||(player.RiddenBike&&hit.IsChildOf(player.RiddenBike.transform));
 void LateUpdate(){if(!combat||!gun)return;gun.gameObject.SetActive(combat.usesFirearm);if(!combat.usesFirearm)return;bool down=combat.Npc.IsDead||combat.Npc.IsKnockedDown;ArmedHandPose.Apply(animator,gun,target?target.View.transform.position:transform.position+transform.forward*10,aiming&&combat.IsAggressive&&(!combat.ExternalAttackControl||ControlledAim),down);}
}
}
