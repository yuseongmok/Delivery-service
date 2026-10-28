using System.Linq;
using UnityEngine.AI;
using UnityEngine;
namespace DeliveryService.Mok {
// 행사장 밖에서만 작동하는 유한한 추격 세 파동. 일반 도시 스폰에는 영향을 주지 않는다.
public sealed class VenueStreetPursuit:MonoBehaviour {
 public VenueFinale mission;public Transform[] firstWave,secondWave,thirdWave;
 public Transform secondTrigger,thirdTrigger;public float speed=4.7f;public int maximumActive=12;
 public int WavesStarted{get;private set;}float started;float nextRepath;bool boarding;
 public float VehicleDistanceRun{get;private set;}
 public Vector3 LastBlockGoal{get;private set;}
 public void Begin(){boarding=false;VehicleDistanceRun=0;WavesStarted=0;started=Time.time;Wave(firstWave);}
 public void ResetPursuit(){boarding=false;WavesStarted=0;}
 void Wave(Transform[] points){int free=Mathf.Max(0,maximumActive-mission.PendingSpawnCount-mission.Enemies.Count(e=>e&&e.Health>0&&!e.Npc.IsDead));mission.SpawnStreetPursuers(points,free,speed);WavesStarted++;}
 public void BoardCar(){boarding=true;nextRepath=0;foreach(var e in mission.Enemies){if(!e||e.Npc.IsDead)continue;e.EndFight();e.enabled=false;e.Npc.ExternalControl=true;}}
 void BlockCar(){
 bool repath=Time.time>=nextRepath;if(repath)nextRepath=Time.time+.2f;
 int index=0;foreach(var e in mission.Enemies){if(!e||e.Npc.IsDead||e.Npc.IsKnockedDown)continue;var agent=e.GetComponent<NavMeshAgent>();if(!agent||!agent.enabled||!agent.isOnNavMesh)continue;
 e.Npc.ExternalControl=true;agent.areaMask=NavMesh.AllAreas;agent.speed=6.2f;agent.acceleration=22;agent.angularSpeed=540;agent.stoppingDistance=.25f;agent.isStopped=false;
 if(repath){
 // 차 앞의 예상 위치로 이동하되 순간이동하거나 차량 속도에 맞춰 가속하지 않는다.
 var car=mission.car;float ahead=3.8f+Mathf.Clamp(mission.getaway.Speed*.55f,0,7);
 var goal=car.position+car.forward*ahead+car.right*((index%3-1)*1.1f);
 if(NavMesh.SamplePosition(goal,out var hit,3,NavMesh.AllAreas)){if(!agent.hasPath||Vector3.Distance(agent.destination,hit.position)>.75f){var path=new NavMeshPath();if(agent.CalculatePath(hit.position,path)&&path.status==NavMeshPathStatus.PathComplete)agent.SetPath(path);}LastBlockGoal=hit.position;}
  } 
 // 막아서는 위치에 도착하면 진행 방향 대신 실제 운전석을 바라본다.
 var towardDriver=mission.car.TransformPoint(mission.getaway.eye)-e.transform.position;towardDriver.y=0;
 bool confront=towardDriver.sqrMagnitude<196&&(Vector3.Dot(e.transform.position-mission.car.position,mission.car.forward)>1||agent.remainingDistance<1.5f);
 agent.updateRotation=!confront;
 if(confront&&towardDriver.sqrMagnitude>.01f)e.transform.rotation=Quaternion.RotateTowards(e.transform.rotation,Quaternion.LookRotation(towardDriver),540*Time.deltaTime);
 index++;float velocity=agent.velocity.magnitude;VehicleDistanceRun+=velocity*Time.deltaTime;
 var animator=e.GetComponentInChildren<Animator>();if(animator&&animator.enabled)foreach(var param in animator.parameters){if(param.type==AnimatorControllerParameterType.Float&&param.name=="MoveSpeed")animator.SetFloat(param.name,velocity);if(param.type==AnimatorControllerParameterType.Float&&param.name=="RunRate")animator.SetFloat(param.name,Mathf.Clamp(velocity/4.2f,.8f,1.3f));if(param.type==AnimatorControllerParameterType.Bool&&param.name=="IsWalking")animator.SetBool(param.name,velocity>.1f);}
 }
 }
 void Update(){if(boarding&&mission&&Time.timeScale>0&&(mission.State==VenueFinale.Phase.Boarding||mission.State==VenueFinale.Phase.Driving||mission.State==VenueFinale.Phase.Abandoning)){BlockCar();return;}if(Time.timeScale<=0||!mission||mission.State!=VenueFinale.Phase.StreetEscape)return;float elapsed=Time.time-started;
 if(WavesStarted==1&&elapsed>2&&(Vector3.Distance(mission.Player.transform.position,secondTrigger.position)<9||elapsed>10))Wave(secondWave);
 if(WavesStarted==2&&elapsed>5&&(Vector3.Distance(mission.Player.transform.position,thirdTrigger.position)<9||elapsed>18))Wave(thirdWave);
 }
}
}
