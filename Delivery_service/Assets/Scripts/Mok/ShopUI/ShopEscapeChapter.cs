using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
// 첫 사건의 후일담과 감옥 도입을 연결한다. 기존 체포 벌금·병원 정산과는 분리한다.
public sealed class ShopEscapeChapter:MonoBehaviour {
 public enum Stage { None, Packing, Escape, Arrest, Transfer, Prison }
 public Stage State{get;private set;}public bool Cinematic=>State==Stage.Arrest||State==Stage.Transfer;
 public bool InPrison=>State==Stage.Prison;
 public VenueFinale mission;public Transform bag,mingi,prisonArrival,prisonMingi;
 public GameObject policeBlock,prison;public Transform[] route,ambush,doorway;
 public Transform[] police;
 public float arrestSeconds=9,transferSeconds=8;
 [TextArea(2,6)]public string packing="민기: 납품 기록에 이 가게 주소가 있어. 여긴 이제 안전하지 않아. 가방만 챙겨. 지금 떠나자.";
 [TextArea(2,6)]public string ambushLine="조직원: 납품 기록에 나온 가게다! 저 둘, 도망친다!\n민기: 벌써 왔어! 길을 뚫어. 같이 나가자!";
 [TextArea(2,6)]public string[] arrestLines={"경찰: 도심에서 무장 소란을 일으킨 두 사람! 무기 버리고 손 들어!","나: 뒤에 놈들이 우릴 쫓고 있어!","민기: 내려놔. 앞뒤가 막혔어. 여기서 더 싸우면 끝이야."};
 [TextArea(2,6)]public string[] prisonLines={"소란 신고를 받고 출동한 경찰은 두 사람을 현장에서 체포했다.","이후, 구치소.\n박진웅의 계획은 밖에서 계속되고 있었다.","민기: 살아서 나가야 해. 우리가 여기 있는 동안 그놈은 아무 일 없었던 척하겠지."};
 NavMeshPath followPath;int followCorner;public bool CompanionHasPath=>followPath!=null&&followPath.corners.Length>1;
 int step,doorStep;float entered,nextPath,nextStep;bool secondWave,policeZoneReached;NavMeshAgent guide;Animator animation;AudioSource siren;readonly HashSet<CitizenCombat> subdued=new();
 Vector3 lastDestination=new Vector3(float.PositiveInfinity,0,0);Vector3 mingiHome;Quaternion mingiRotation;bool oldPhone,phoneCaptured;PhoneController phone;
 public int RouteStep=>step;public int SubduedCount=>subdued.Count;
 public Transform Goal=>route[Mathf.Clamp(step,0,route.Length-1)];
 void Awake(){followPath=new NavMeshPath();guide=mingi.GetComponent<NavMeshAgent>();animation=mingi.GetComponentInChildren<Animator>();if(animation)animation.applyRootMotion=false;mingiHome=mingi.position;mingiRotation=mingi.rotation;bag.gameObject.SetActive(false);policeBlock.SetActive(false);prison.SetActive(false);siren=gameObject.AddComponent<AudioSource>();siren.playOnAwake=false;siren.loop=true;siren.volume=.16f;siren.spatialBlend=0;siren.clip=SuppliedAudio.Current?SuppliedAudio.Current.policeSiren:null;StorySoundPause.Attach(siren);}
 void Set(Stage stage){State=stage;entered=Time.time;}
 public void Begin(){ResetChapter();mission.Player.ResetAfterWaterRescue();mission.Day.EnterNightPreparation();Set(Stage.Packing);bag.gameObject.SetActive(true);mission.subtitle.text=packing;mission.ui.SetActive(true);}
 public bool TakeBag(){if(State!=Stage.Packing||!mission.Player.CanAct||Vector3.Distance(mission.Player.transform.position,bag.position)>3)return false;
  bag.gameObject.SetActive(false);StorySoundMix.Play(mission.sound,SuppliedAudio.Current?SuppliedAudio.Current.bag:null,.4f);Set(Stage.Escape);policeBlock.SetActive(true);step=0;subdued.Clear();secondWave=false;policeZoneReached=false;mission.subtitle.text=ambushLine;
  doorStep=0;followPath??=new NavMeshPath();followPath.ClearCorners();lastDestination=new Vector3(float.PositiveInfinity,0,0);if(guide){guide.enabled=false;guide.speed=5.5f;guide.acceleration=18;guide.stoppingDistance=1.8f;}
  mission.SpawnStreetPursuers(ambush,2,4.8f);return true;
 }
 public void Tick(){if(Time.timeScale<=0)return;
  if(State==Stage.Packing){mission.objective.text="매장을 떠날 준비 · 비상 가방 챙기기 [F]";Mark(bag.position,"비상 가방");if(PrologueKeyboard.Pressed(Key.F))TakeBag();return;}
  if(State==Stage.Escape){
   Follow();foreach(var e in mission.Enemies)if(e&&(e.Health<=0||e.Npc.IsKnockedDown))subdued.Add(e);
   if(mission.Player.RiddenBike)mission.Player.RiddenBike.ExitBike();
   var distance=Vector3.Distance(mission.Player.transform.position,Goal.position);
   if(distance<4&&step<route.Length-1){step++;if(!secondWave){secondWave=true;mission.SpawnStreetPursuers(ambush.Skip(2).ToArray(),2,5);mission.subtitle.text="민기: 앞에도 있어! 둘만 제압하고 틈이 나면 빠져나가자!";}}
   distance=Vector3.Distance(mission.Player.transform.position,Goal.position);if(step==route.Length-1&&distance<4)policeZoneReached=true;
   mission.objective.text="민기와 매장 탈출 · 조직원을 피해 골목으로"+(step==route.Length-1&&SubduedCount<2?"\n길을 막는 조직원 제압 "+SubduedCount+" / 2":"");Mark(Goal.position,"민기와 탈출");
   if(step==route.Length-1&&policeZoneReached&&distance<15&&SubduedCount>=2&&Vector3.Distance(mingi.position,mission.Player.transform.position)<7)BeginArrest();return;
  }
  if(State==Stage.Arrest){float t=Time.time-entered;mission.objective.text="경찰 포위";mission.marker.gameObject.SetActive(false);mission.subtitle.text=arrestLines[Mathf.Min(arrestLines.Length-1,(int)(t/3))];var officer=police[0];var delta=mission.Player.transform.position-officer.position;delta.y=0;if(delta.sqrMagnitude>5)officer.position+=delta.normalized*Time.deltaTime*.9f;officer.LookAt(new Vector3(mission.Player.transform.position.x,officer.position.y,mission.Player.transform.position.z));mission.cinema.transform.rotation=Quaternion.Slerp(mission.cinema.transform.rotation,Quaternion.LookRotation(officer.position+Vector3.up*1.45f-mission.cinema.transform.position),Time.deltaTime*5);mission.fade.gameObject.SetActive(t>arrestSeconds-2);mission.fade.color=new Color(0,0,0,Mathf.InverseLerp(arrestSeconds-2,arrestSeconds,t));if(t>=arrestSeconds||PrologueKeyboard.Pressed(Key.Space))EnterPrison();return;}
  if(State==Stage.Transfer){float t=Time.time-entered;mission.objective.text="챕터 2 · 철창 너머 / 1일차";mission.subtitle.text=prisonLines[Mathf.Min(prisonLines.Length-1,(int)(t/2.8f))];mission.fade.gameObject.SetActive(t<2.5f);mission.fade.color=new Color(0,0,0,1-Mathf.InverseLerp(1,2.5f,t));if(t>=transferSeconds||PrologueKeyboard.Pressed(Key.Space)){Set(Stage.Prison);mission.StoryControl(false);mission.Player.GetComponent<PlayerWeapon>()?.Equip(WeaponKind.None);mission.objective.text="챕터 2 · 철창 너머 / 1일차";mission.subtitle.text=prisonLines[prisonLines.Length-1];}return;}
  if(State==Stage.Prison){mission.objective.text="챕터 2 · 철창 너머 / 1일차";mission.marker.gameObject.SetActive(false);if(Time.time-entered>8)mission.subtitle.text="";}
 }
 void Follow(){
 if(doorway!=null&&doorStep<doorway.Length){var goal=doorway[doorStep].position;var delta=goal-mingi.position;delta.y=0;mingi.position=Vector3.MoveTowards(mingi.position,goal,3.5f*Time.deltaTime);if(delta.sqrMagnitude>.01f)mingi.rotation=Quaternion.RotateTowards(mingi.rotation,Quaternion.LookRotation(delta),240*Time.deltaTime);if(animation)animation.SetFloat("MoveSpeed",3.5f);if(Vector3.Distance(mingi.position,goal)<.15f)doorStep++;return;}
 if(guide&&guide.enabled)guide.enabled=false;
 var cc=mingi.GetComponent<CharacterController>();if(cc)cc.enabled=false;
 if(Time.time>=nextPath&&((lastDestination-mission.Player.transform.position).sqrMagnitude>9||followPath.corners.Length<2)){
  nextPath=Time.time+.6f;
  if(NavMesh.SamplePosition(mingi.position,out var from,14,NavMesh.AllAreas)&&NavMesh.SamplePosition(mission.Player.transform.position,out var to,4,NavMesh.AllAreas)&&NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,followPath)&&followPath.status==NavMeshPathStatus.PathComplete){lastDestination=mission.Player.transform.position;followCorner=1;}
 }
 float speed=0;
 if(followPath.corners.Length>1&&Vector3.Distance(mingi.position,mission.Player.transform.position)>2){
  followCorner=Mathf.Min(followCorner,followPath.corners.Length-1);var corner=followPath.corners[followCorner];var delta=corner-mingi.position;
  if(delta.magnitude<.2f&&followCorner<followPath.corners.Length-1)followCorner++;
  else {var previous=mingi.position;mingi.position=Vector3.MoveTowards(mingi.position,corner,5.5f*Time.deltaTime);speed=Vector3.Distance(previous,mingi.position)/Mathf.Max(.001f,Time.deltaTime);delta.y=0;if(delta.sqrMagnitude>.01f)mingi.rotation=Quaternion.RotateTowards(mingi.rotation,Quaternion.LookRotation(delta),240*Time.deltaTime);}
 }
 if(animation)animation.SetFloat("MoveSpeed",speed);
 if(speed>.5f&&Time.time>=nextStep){nextStep=Time.time+.4f;var clips=SuppliedAudio.Current?SuppliedAudio.Current.streetSteps:null;if(clips!=null&&clips.Length>0)AudioSource.PlayClipAtPoint(clips[Random.Range(0,clips.Length)],mingi.position,.09f*SuppliedAudio.Volume);}
 }

 void StopGuide(){if(guide&&guide.enabled){if(guide.isOnNavMesh)guide.isStopped=true;guide.enabled=false;}if(animation)animation.SetFloat("MoveSpeed",0);}
 void Mark(Vector3 position,string text){mission.marker.gameObject.SetActive(true);StoryTargetDiamond.Place(mission.marker,mission.Player.View,position+Vector3.up,mission.Player.transform.position,text);}
 public void BeginArrest(){if(State!=Stage.Escape)return;StopGuide();foreach(var enemy in mission.Enemies)enemy.ExternalAttackControl=true;Set(Stage.Arrest);policeBlock.SetActive(true);foreach(var cop in police){var delta=mission.Player.transform.position-cop.position;delta.y=0;if(delta.sqrMagnitude>.01f)cop.rotation=Quaternion.LookRotation(delta);}phone=FindFirstObjectByType<PhoneController>();if(phone){oldPhone=phone.enabled;phoneCaptured=true;phone.ClosePhone();phone.enabled=false;}mission.StoryControl(true);mission.cinema.transform.SetPositionAndRotation(mission.Player.View.transform.position,mission.Player.View.transform.rotation);if(siren.clip)siren.Play();}
 public void EnterPrison(){if(State!=Stage.Arrest)return;Set(Stage.Transfer);mission.ClearStoryEnemies();siren.Stop();mission.Player.GetComponent<PlayerWeapon>()?.Equip(WeaponKind.None);prison.SetActive(true);policeBlock.SetActive(false);mission.PlacePlayer(prisonArrival);mingi.SetPositionAndRotation(prisonMingi.position,prisonMingi.rotation);mission.cinema.transform.SetPositionAndRotation(mission.Player.View.transform.position,mission.Player.View.transform.rotation);mission.cinema.transform.LookAt(mingi.position+Vector3.up*1.4f);mission.fade.gameObject.SetActive(true);mission.fade.color=Color.black;StorySoundMix.Play(mission.sound,SuppliedAudio.Current?SuppliedAudio.Current.metalImpact:null,.35f);}
 public void ResetChapter(){if(Cinematic)mission.StoryControl(false);StopGuide();if(siren)siren.Stop();if(phoneCaptured&&phone){phone.enabled=oldPhone;phoneCaptured=false;}bag.gameObject.SetActive(false);policeBlock.SetActive(false);prison.SetActive(false);if(State!=Stage.None)mingi.SetPositionAndRotation(mingiHome,mingiRotation);var body=mingi.GetComponent<CharacterController>();if(body)body.enabled=true;State=Stage.None;}
 void OnDisable(){if(siren)siren.Stop();}
}
}
