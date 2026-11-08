using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 차도 경로와 문·좌석·하차 위치는 씬에서 조정할 수 있다.
[DefaultExecutionOrder(2200)]
public sealed class ParkPoliceArrivalBehaviour : MonoBehaviour {
 public Transform car;public Transform[] route,doors,actors,seats,exits,stands;
 public Transform mingi;public Transform[] guns;public bool SpeakerBusy;public bool AimWeapons;
 NavMeshPath[] paths;int[] corners;Vector3[] goals;
 public bool Ready{get;private set;}
 public float driveSeconds=6,exitSeconds=2.2f;public AudioClip openSound,closeSound;
 public float Duration=>driveSeconds+exitSeconds+35;
 float elapsed;bool opened,closed;bool[] clearedCar;Quaternion[] doorRest;Animator[] animators;
 public void Begin(){if(doorRest==null){doorRest=new Quaternion[doors.Length];animators=new Animator[actors.Length];clearedCar=new bool[actors.Length];for(int i=0;i<doors.Length;i++)doorRest[i]=doors[i].localRotation;for(int i=0;i<actors.Length;i++)animators[i]=actors[i].GetComponentInChildren<Animator>();}ResetArrival();paths=new NavMeshPath[actors.Length];corners=new int[actors.Length];goals=new Vector3[actors.Length];
  var from=Vector3.ProjectOnPlane(route[route.Length-1].position-mingi.position,Vector3.up).normalized;var side=Vector3.Cross(Vector3.up,from);
  for(int i=0;i<actors.Length;i++){var v=mingi.position+from*(2.5f+(i>1?1:0))+side*(i==0?0:i==1?1.5f:i==2?-1.5f:3);goals[i]=NavMesh.SamplePosition(v,out var h,2,NavMesh.AllAreas)?h.position:v;paths[i]=new NavMeshPath();if(animators[i]){animators[i].enabled=true;animators[i].speed=1;animators[i].applyRootMotion=false;animators[i].cullingMode=AnimatorCullingMode.AlwaysAnimate;}}
  AimWeapons=true;car.gameObject.SetActive(true);}
 public void Tick(float time){elapsed=time;
  float distance=0;for(int i=1;i<route.Length;i++)distance+=Vector3.Distance(route[i-1].position,route[i].position);
  float travel=distance*Mathf.SmoothStep(0,1,Mathf.Clamp01(time/driveSeconds));
  for(int i=1;i<route.Length;i++){float segment=Vector3.Distance(route[i-1].position,route[i].position);if(travel<=segment||i==route.Length-1){car.position=Vector3.Lerp(route[i-1].position,route[i].position,segment>0?travel/segment:1);car.rotation=Quaternion.RotateTowards(car.rotation,Quaternion.LookRotation(route[i].position-route[i-1].position),Time.deltaTime*90);break;}travel-=segment;}
  if(time>=driveSeconds){car.SetPositionAndRotation(route[route.Length-1].position,route[route.Length-1].rotation);}
  if(time>=driveSeconds&&!opened){opened=true;Play(openSound);}if(time>=driveSeconds+exitSeconds+1&&!closed){closed=true;Play(closeSound);}
  for(int i=0;i<doors.Length;i++){float t=time-driveSeconds-i*.12f;float angle=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.65f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-exitSeconds-.6f)/.65f)));doors[i].localRotation=doorRest[i]*Quaternion.Euler(0,(i%2==0?68:-68)*angle,0);}
  for(int i=0;i<actors.Length;i++){float t=time-driveSeconds-.45f-i*.12f;actors[i].gameObject.SetActive(t>=0);if(t<0)continue;
   if(t<exitSeconds){float f=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/exitSeconds));actors[i].position=Vector3.Lerp(seats[i].position,exits[i].position,f);actors[i].rotation=Quaternion.Slerp(seats[i].rotation,exits[i].rotation,f);Speed(i,0);}
   else{var goal=goals[i];if(i%2==1&&!clearedCar[i]){goal=car.TransformPoint(new Vector3(1.8f,0,i<2?3.5f:-3.5f));if(Vector3.Distance(actors[i].position,goal)<.2f)clearedCar[i]=true;}if(i%2==0||clearedCar[i]){if(paths[i].corners.Length==0){NavMesh.CalculatePath(actors[i].position,goals[i],NavMesh.AllAreas,paths[i]);corners[i]=1;}if(paths[i].status==NavMeshPathStatus.PathComplete&&paths[i].corners.Length>1){while(corners[i]<paths[i].corners.Length-1&&Vector3.Distance(actors[i].position,paths[i].corners[corners[i]])<.2f)corners[i]++;goal=paths[i].corners[Mathf.Min(corners[i],paths[i].corners.Length-1)];}else{Speed(i,0);continue;}}
   var delta=goal-actors[i].position;delta.y=0;if(delta.sqrMagnitude>.02f)actors[i].rotation=Quaternion.RotateTowards(actors[i].rotation,Quaternion.LookRotation(delta),Time.deltaTime*180);actors[i].position=Vector3.MoveTowards(actors[i].position,goal,Time.deltaTime*1.8f);Speed(i,delta.magnitude>.15f?1.8f:0);}
  }
  Ready=time>driveSeconds+exitSeconds;for(int i=0;i<actors.Length;i++)if(Vector3.Distance(actors[i].position,goals[i])>.3f)Ready=false;
 }
 void LateUpdate(){if(guns!=null&&animators!=null)for(int i=0;i<guns.Length;i++){if(!guns[i])continue;bool showing=AimWeapons&&elapsed>driveSeconds+exitSeconds;guns[i].gameObject.SetActive(showing&&!(i==0&&SpeakerBusy));if(showing&&!(i==0&&SpeakerBusy)){var target=i%2==0?mingi.position+Vector3.up:VenueFinale.Current.Player.View.transform.position;ArmedHandPose.Apply(animators[i],guns[i],target,true,false);
}}
  if(!opened||closed)return;for(int i=0;i<actors.Length;i++){var a=animators[i];float t=elapsed-driveSeconds-.45f-i*.12f;if(!a||!a.isHuman||t<0||t>=exitSeconds)continue;float bend=1-Mathf.SmoothStep(0,1,t/exitSeconds);var hips=a.GetBoneTransform(HumanBodyBones.Hips);hips.position-=Vector3.up*(.38f*bend);var spine=a.GetBoneTransform(HumanBodyBones.Spine);spine.localRotation*=Quaternion.Euler(25*bend,0,0);foreach(var side in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})a.GetBoneTransform(side).localRotation*=Quaternion.Euler(-65*bend,0,0);foreach(var side in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})a.GetBoneTransform(side).localRotation*=Quaternion.Euler(75*bend,0,0);}}
 void Speed(int i,float v){if(!animators[i])return;foreach(var p in animators[i].parameters){if(p.name=="MoveSpeed")animators[i].SetFloat(p.name,v);if(p.name=="IsWalking")animators[i].SetBool(p.name,v>.1f);}}
 void Play(AudioClip clip){if(clip)AudioSource.PlayClipAtPoint(clip,car.position,.45f*SuppliedAudio.Volume);}
 public void Complete(){elapsed=Duration;car.SetPositionAndRotation(route[route.Length-1].position,route[route.Length-1].rotation);for(int i=0;i<actors.Length;i++){actors[i].gameObject.SetActive(true);actors[i].position=goals[i];var facing=Vector3.ProjectOnPlane(mingi.position-actors[i].position,Vector3.up);if(facing.sqrMagnitude>.01f)actors[i].rotation=Quaternion.LookRotation(facing);Speed(i,0);}for(int i=0;i<doors.Length;i++)doors[i].localRotation=doorRest[i];closed=true;}
 public void ResetArrival(){opened=closed=false;elapsed=0;Ready=false;AimWeapons=false;SpeakerBusy=false;if(guns!=null)foreach(var g in guns)if(g)g.gameObject.SetActive(false);if(clearedCar!=null)System.Array.Clear(clearedCar,0,clearedCar.Length);if(doorRest!=null)for(int i=0;i<doors.Length;i++)doors[i].localRotation=doorRest[i];if(route.Length>0)car.SetPositionAndRotation(route[0].position,route[0].rotation);}
}
}
// 경찰차 하차 연출
