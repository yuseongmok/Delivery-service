using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 경로 옆에서 달려 나오는 경비. 운전 중 실제 차 위치를 기준으로 움직인다.
public sealed class VenueRoadInterception:MonoBehaviour {
 [System.Serializable]public class Wave {public Transform center;public Transform[] starts;}
 public VenueGetawayDrive drive;public Wave[] waves;public float triggerDistance=32,runSpeed=5.8f;
 public GameObject guardGun;public AudioClip rifleShot,metalHit;public int Shots{get;set;}public int CarHits{get;set;}public float NextShot{get;set;}
 public int WavesStarted{get;private set;}public int Spawned{get;private set;}public float DistanceRun{get;private set;}
 class Guard {public NPCControl npc;public NavMeshAgent agent;public Animator animator;public Vector3 goal,last;public float expires;}
 readonly List<Guard> guards=new();bool[] fired;bool active;
 public void Begin(){Stop();fired=new bool[waves.Length];WavesStarted=Spawned=Shots=CarHits=0;NextShot=Time.time;DistanceRun=0;active=true;}
 public void Stop(){active=false;foreach(var g in guards)if(g.npc)Destroy(g.npc.gameObject);guards.Clear();foreach(var tracer in GetComponentsInChildren<PrologueTracer>())Destroy(tracer.gameObject);}
 void Update(){if(!active||!drive.Driving||Time.timeScale<=0)return;
 for(int i=0;i<waves.Length;i++){var w=waves[i];var offset=w.center.position-drive.transform.position;
 if(!fired[i]&&offset.magnitude<triggerDistance&&Vector3.Dot(drive.transform.forward,offset.normalized)>.25f){fired[i]=true;WavesStarted++;foreach(var point in w.starts)Spawn(point,w.center.position);}}
 foreach(var g in guards){if(!g.npc||g.npc.IsDead)continue;g.npc.ExternalControl=true;if(Time.time>g.expires){Destroy(g.npc.gameObject);continue;}
 if(g.agent&&g.agent.enabled&&g.agent.isOnNavMesh){g.agent.SetDestination(g.goal);float speed=g.agent.velocity.magnitude;DistanceRun+=Vector3.Distance(g.last,g.npc.transform.position);g.last=g.npc.transform.position;
 if(g.animator)foreach(var param in g.animator.parameters){if(param.type==AnimatorControllerParameterType.Float&&(param.name=="MoveSpeed"||param.name=="RunRate"))g.animator.SetFloat(param.name,param.name=="RunRate"?Mathf.Clamp(speed/4.2f,.8f,1.3f):speed);if(param.type==AnimatorControllerParameterType.Bool&&param.name=="IsWalking")g.animator.SetBool(param.name,speed>.1f);}}
 }
 }
 void Spawn(Transform start,Vector3 goal){var actor=Instantiate(drive.mission.preparation.enemyTemplate,start.position,start.rotation,transform);actor.missionEnemy=true;actor.enabled=false;actor.gameObject.SetActive(true);var npc=actor.GetComponent<NPCControl>();npc.ExternalControl=true;var agent=actor.GetComponent<NavMeshAgent>();if(!agent)return;agent.enabled=true;agent.Warp(start.position);agent.speed=runSpeed;agent.acceleration=28;agent.angularSpeed=720;agent.stoppingDistance=.3f;agent.isStopped=false;agent.autoBraking=true;
 var a=actor.GetComponentInChildren<Animator>();var shooter=actor.gameObject.AddComponent<VenueGuardShooter>();shooter.owner=this;guards.Add(new Guard{npc=npc,agent=agent,animator=a,goal=goal,last=start.position,expires=Time.time+22});Spawned++;}
 void OnDisable(){Stop();}
}}
