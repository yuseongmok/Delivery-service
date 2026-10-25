using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace DeliveryService.Mok {
// 플레이어가 실제로 달린 경로를 따라 추격한다. 벽을 통과하거나 앞으로 순간이동하지 않는다.
public sealed class VenueVehiclePursuit:MonoBehaviour {
 [Serializable]public class Car {public Transform model;public Transform[] wheels;public AudioSource engine;[NonSerialized]public int node;[NonSerialized]public bool spawned;[NonSerialized]public float travelled,nextRam,ramStarted,windup;[NonSerialized]public int ramPhase;}
 public VenueGetawayDrive drive;public Car[] cars;public float maximumSpeed=9.5f,escapeDistance=16,loseSeconds=2.5f;
 public int RamHits{get;private set;}public int RamAttempts{get;private set;}float nextAttack;
 public int TotalSpawned{get;private set;}public float Travelled=>cars.Sum(c=>c.travelled);
 public bool Escaped{get;private set;}public float LostSeconds{get;private set;}public bool Active{get;private set;}
 public int SpawnedCount=>cars.Count(c=>c.spawned);readonly List<Vector3> trail=new();float travelled;Quaternion startRotation;
 public void Begin(){Stop();Escaped=false;TotalSpawned=0;RamHits=RamAttempts=0;nextAttack=Time.time+3;foreach(var c in cars){c.ramPhase=0;c.nextRam=Time.time+3;}foreach(var c in cars)c.travelled=0;Active=true;travelled=0;LostSeconds=0;trail.Clear();trail.Add(drive.transform.position);startRotation=drive.transform.rotation;}
 public void Stop(){Active=false;foreach(var c in cars){c.model.gameObject.SetActive(false);if(c.engine)c.engine.Stop();c.spawned=false;c.node=1;c.ramPhase=0;}}
 void Awake(){Stop();}
 void Update(){if(!Active)return;if(Time.timeScale<=0){foreach(var c in cars)if(c.engine&&c.engine.isPlaying)c.engine.Pause();return;}foreach(var c in cars)if(c.spawned&&c.engine&&!c.engine.isPlaying)c.engine.UnPause();}
 void FixedUpdate(){if(!Active||!drive.Driving||Time.timeScale<=0)return;var player=drive.transform.position;
  if(Vector3.Distance(trail[trail.Count-1],player)>1.5f){travelled+=Vector3.Distance(trail[trail.Count-1],player);trail.Add(player);}
  bool seen=false;int index=0;foreach(var c in cars){index++;
   if(!c.spawned&&travelled>index*16){c.spawned=true;TotalSpawned++;c.node=1;c.model.SetPositionAndRotation(trail[0],startRotation);c.model.gameObject.SetActive(true);if(c.engine)c.engine.Play();}
   if(!c.spawned)continue;
   float gap=Vector3.Distance(player,c.model.position);bool obscured=Physics.RaycastAll(c.model.position+Vector3.up*1.2f,(player-c.model.position).normalized,gap,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(c.model)&&!h.transform.IsChildOf(drive.transform)&&!h.transform.IsChildOf(drive.mission.Player.transform)&&!h.collider.attachedRigidbody&&h.collider.bounds.size.y>2);
   if(gap<escapeDistance||(!obscured&&gap<32))seen=true;
   if(TryRam(c,index,gap,obscured))continue;
   while(c.node<trail.Count-1&&Vector3.Distance(c.model.position,trail[c.node])<1)c.node++;
   if(c.node>=trail.Count||gap<7+index*3)continue;
   var delta=trail[c.node]-c.model.position;delta.y=0;if(delta.sqrMagnitude<.001f)continue;
   float corner=Vector3.Angle(c.model.forward,delta);float speed=Mathf.Min(maximumSpeed,Mathf.Max(4,drive.Speed+1))*Mathf.Lerp(1,.4f,Mathf.InverseLerp(15,75,corner));var step=delta.normalized*Mathf.Min(delta.magnitude,speed*Time.fixedDeltaTime);
   bool blocked=Physics.BoxCastAll(c.model.position+Vector3.up*.85f,new Vector3(.7f,.5f,1.4f),step.normalized,Quaternion.LookRotation(delta),step.magnitude+.1f,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(c.model)&&!h.transform.IsChildOf(drive.transform)&&!h.transform.IsChildOf(drive.mission.Player.transform)&&h.normal.y<.6f);
   if(!blocked){c.model.position+=step;c.model.rotation=Quaternion.RotateTowards(c.model.rotation,Quaternion.LookRotation(delta),160*Time.fixedDeltaTime);c.travelled+=step.magnitude;foreach(var w in c.wheels)w.Rotate(Vector3.right,speed*Time.fixedDeltaTime*130,Space.Self);}
   if(c.engine)c.engine.pitch=blocked?.75f:Mathf.Lerp(.8f,1.5f,speed/maximumSpeed);
  }
  LostSeconds=SpawnedCount==cars.Length&&!seen?LostSeconds+Time.fixedDeltaTime:0;
  if(LostSeconds>=loseSeconds){Escaped=true;Stop();drive.mission.SecurityWarning("나: 따돌렸어. 표시된 곳에 차를 버리고 돌아가자.");}
 }
 bool Road(Vector3 at){return Physics.RaycastAll(at+Vector3.up*2,Vector3.down,3,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name.Contains("Road")&&Mathf.Abs(h.point.y-at.y)<.4f);}
 bool Clear(Car c,Vector3 step){return !Physics.BoxCastAll(c.model.position+Vector3.up*.85f,new Vector3(.75f,.5f,1.5f),step.normalized,c.model.rotation,step.magnitude+.1f,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(c.model)&&!h.transform.IsChildOf(drive.transform)&&!h.transform.IsChildOf(drive.mission.Player.transform)&&h.normal.y<.6f);}
 void EndRam(Car c){c.ramPhase=0;c.nextRam=Time.time+16;nextAttack=Time.time+8;float best=float.PositiveInfinity;for(int n=0;n<trail.Count;n++){float d=(trail[n]-c.model.position).sqrMagnitude;if(d<best){best=d;c.node=n;}}}
 // 빈 차선으로 접근한 뒤 짧게 예고하고 측면을 공격한다. 두 대가 동시에 밀어붙이지 않는다.
 bool TryRam(Car c,int index,float gap,bool obscured){
  float side=index%2==1?1:-1;
  if(c.ramPhase==0){if(Time.time<nextAttack||Time.time<c.nextRam||gap>28||obscured||drive.Speed<3||Mathf.Abs(drive.SteeringInput)>.35f||cars.Any(other=>other.ramPhase!=0))return false;
   var approach=drive.transform.position+drive.transform.right*side*3.2f-drive.transform.forward*1.5f;if(!Road(approach)||!Clear(c,approach-c.model.position)){c.nextRam=Time.time+2;return false;}c.ramPhase=1;c.ramStarted=Time.time;RamAttempts++;}
  if(Time.time-c.ramStarted>6||gap>40||Mathf.Abs(drive.SteeringInput)>.65f){EndRam(c);return false;}
  Vector3 local=drive.transform.InverseTransformPoint(c.model.position);
  if(c.ramPhase==1&&Mathf.Abs(local.z)<3&&Mathf.Abs(local.x)<3.7f){c.ramPhase=2;c.windup=Time.time+.65f;}
  bool striking=c.ramPhase==2&&Time.time>=c.windup;
  var goal=drive.transform.position+drive.transform.right*side*(striking?1.7f:3.2f)+drive.transform.forward*(striking?0:-.7f);
  if(!Road(goal)){EndRam(c);return false;}
  var delta=goal-c.model.position;delta.y=0;float speed=Mathf.Clamp(drive.Speed+6,6,26);var step=Vector3.ClampMagnitude(delta,speed*Time.fixedDeltaTime);
  if(!Clear(c,step)){EndRam(c);return false;}
  if(striking&&Mathf.Abs(local.x)<2.05f&&Mathf.Abs(local.z)<2.5f){if(drive.ReceiveSideImpact(-drive.transform.right*side))RamHits++;EndRam(c);return true;}
  c.model.position+=step;c.travelled+=step.magnitude;c.model.rotation=Quaternion.RotateTowards(c.model.rotation,drive.transform.rotation*Quaternion.Euler(0,striking?-side*15:0,0),100*Time.fixedDeltaTime);foreach(var w in c.wheels)w.Rotate(Vector3.right,speed*Time.fixedDeltaTime*130,Space.Self);if(c.engine)c.engine.pitch=1.6f;return true;
 }
 void OnDisable(){Stop();}
}
}
