using System.Collections;
using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok {
// 도주 연출용 사격. 벽을 관통하지 않으며 운전 조작이나 체력을 빼앗지 않는다.
public sealed class VenueGuardShooter:MonoBehaviour {
 public VenueRoadInterception owner;
 NPCControl npc;Animator animator;Transform gun,hand,arm;AudioSource shot,impact;float next;Light flash;
 void Start(){npc=GetComponent<NPCControl>();animator=GetComponentInChildren<Animator>();if(!owner||!owner.guardGun||!animator||!animator.isHuman){enabled=false;return;}
 hand=animator.GetBoneTransform(HumanBodyBones.RightHand);arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);gun=Instantiate(owner.guardGun,hand).transform;gun.localPosition=Vector3.zero;gun.localRotation=Quaternion.identity;
 foreach(var c in gun.GetComponentsInChildren<Collider>())c.enabled=false;
 shot=gameObject.AddComponent<AudioSource>();shot.playOnAwake=false;shot.spatialBlend=1;shot.minDistance=5;shot.maxDistance=65;shot.volume=.45f;
 var lightObject=new GameObject("경비 총구 섬광");lightObject.transform.SetParent(gun,false);lightObject.transform.localPosition=Vector3.forward*.5f;flash=lightObject.AddComponent<Light>();flash.color=new Color(1,.65f,.2f);flash.range=3;flash.intensity=2;flash.enabled=false;
 var hitObject=new GameObject("차체 피탄음");hitObject.transform.SetParent(transform,false);impact=hitObject.AddComponent<AudioSource>();impact.playOnAwake=false;impact.spatialBlend=1;impact.minDistance=5;impact.maxDistance=40;next=Time.time+.5f+Random.Range(0,.6f);
 }
 void LateUpdate(){if(!owner||!owner.drive.Driving||Time.timeScale<=0)return;if(npc.IsDead||npc.IsKnockedDown){if(flash)flash.enabled=false;return;}if(!gun)return;
 var target=owner.drive.transform.position+Vector3.up;var aim=target-arm.position;if(aim.magnitude>45||aim.magnitude<3)return;
 arm.rotation=Quaternion.FromToRotation(hand.position-arm.position,aim)*arm.rotation;gun.rotation=Quaternion.LookRotation(target-hand.position);
 if(Time.time>=next&&Time.time>=owner.NextShot){next=Time.time+Random.Range(1.1f,1.9f);owner.NextShot=Time.time+.22f;Fire();}}
 void Fire(){var car=owner.drive.transform;bool hitCar=owner.Shots%3==2;float side=Vector3.Dot(transform.position-car.position,car.right)>=0?1:-1;
 var end=car.TransformPoint(new Vector3(side*(hitCar?.82f:2.6f),hitCar?.8f:1.15f,hitCar?0:-3));var start=hand.position+(end-hand.position).normalized*.55f;
 var direction=end-start;foreach(var h in Physics.RaycastAll(start,direction.normalized,direction.magnitude,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(h.transform.IsChildOf(transform))continue;if(h.transform.IsChildOf(car)){end=h.point;hitCar=true;break;}end=h.point;hitCar=false;break;}
 // 가까운 벽에 막힌 경비는 발사하지 않는다.
 if(Vector3.Distance(start,end)<2)return;
 owner.Shots++;if(owner.rifleShot){shot.pitch=Random.Range(.95f,1.05f);shot.PlayOneShot(owner.rifleShot);}flash.enabled=true;
 var tracer=PrologueTracer.Spawn(start,end,new Color(1,.65f,.16f));tracer.transform.SetParent(owner.transform,true);
 StartCoroutine(Impact(end,hitCar,Vector3.Distance(start,end)/PrologueTracer.Speed));}
 IEnumerator Impact(Vector3 point,bool hit,float delay){yield return new WaitForSeconds(.05f);if(flash)flash.enabled=false;yield return new WaitForSeconds(Mathf.Max(0,delay-.05f));if(!owner||!owner.drive.Driving||!hit)yield break;
 owner.CarHits++;impact.transform.position=point;if(owner.metalHit)impact.PlayOneShot(owner.metalHit,.65f);
 var spark=PrologueTracer.Spawn(point,point+Vector3.up*.35f+owner.drive.transform.right*.2f,new Color(1,.85f,.4f));spark.transform.SetParent(owner.transform,true);
 }
 void OnDisable(){StopAllCoroutines();if(flash)flash.enabled=false;if(shot)shot.Stop();if(impact)impact.Stop();}
}}
