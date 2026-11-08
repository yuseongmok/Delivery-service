using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
// 매장 탈출 후 공원 총격전. 동선·소품은 씬에 저장하고 이 컴포넌트는 연출 순서만 제어한다.
[DefaultExecutionOrder(2100)]
public sealed class ParkEscapeSequence : MonoBehaviour {
 public enum Beat { None, Pickup, Run, LookBack, Fight, Wounded, Defend, Arrival, Call, Release, Custody, Done }
 public Beat Current{get;private set;}
 public bool Cinematic=>Current==Beat.LookBack||Current==Beat.Wounded||Current>=Beat.Arrival&&Current<=Beat.Custody;
 public ShopEscapeChapter chapter;
 public Transform pistol,park,mingiStand,threat,officer,officerStand,policeCar,carStart,carStop,arrivalCamera,gangExit,phoneProp,mingiGun;
 public Transform[] approach,enemyPosts,backupPosts;
 public Transform[] enemyCover;public Transform partnerCover;public ParkPoliceArrivalBehaviour policeArrival;
 ParkCoverActor partnerCoverActor;bool engagementReady;float combatStarted;int coverIndex;
 public float fightSeconds=18,defendSeconds=22,turnSeconds=2.5f,arrivalSeconds=7,lineSeconds=4.5f;
 [TextArea(2,6)]public string pickupLine="민기: 놈들이 곧 올 거야. 벽에 걸린 권총 챙겨. 공원 쪽으로 빠지자!";
 [TextArea(2,6)]public string runLine="민기: 이쪽이야! 공원까지 가서 뒤를 막자!";
 [TextArea(2,6)]public string lookLine="민기: 뒤에서 온다! 여기서 막아!";
 [TextArea(2,6)]public string hurtLine="민기: 윽! 다리를 맞았어…!\n나: 숙이고 있어! 내가 막을게!";
 [TextArea(2,6)]public string arrivalLine="경찰: 전부 사격 중지! 무기 내려!";
 [TextArea(2,6)]public string[] callLines={"경찰: 이거 박진웅 부하들이잖아…", "경찰: 박진웅. 도시에서 이런 소란을 일으키면 어떡해! 봐주는 데도 한계가 있지!", "경찰: …", "경찰: 하… 이번만이다."};
 [TextArea(2,6)]public string[] releaseLines={"경찰: 너네들은 가 봐.","나: 저놈들이 먼저 쐈는데… 그냥 보내준다고?"};
 [TextArea(2,6)]public string[] custodyLines={"경찰: 너희 둘은 같이 간다. 무기 내려놓고 손 보여.","나: 내려놓을게. 민기부터 치료해 줘.","민기: 경찰들도 한패였어… 그렇다면 교도소도 안전하지 않을 거야…!"};
 public int SupportShots{get;private set;}public bool GangReleased{get;private set;}
 VenueFinale M=>chapter.mission;PlayerCombat Player=>M.Player;
 bool ownsTraffic;float nextTraffic;readonly EscapeRouteGuide parkRoute=new EscapeRouteGuide();
 public bool ProtectedFirefight=>Current>=Beat.LookBack&&Current<=Beat.Defend;
 public static bool Protects(PlayerCombat p)=>VenueFinale.Active&&VenueFinale.Current.Player==p&&VenueFinale.Current.aftermath&&VenueFinale.Current.aftermath.parkSequence&&VenueFinale.Current.aftermath.parkSequence.ProtectedFirefight;
 public static bool Protects(CitizenCombat e)=>VenueFinale.Active&&VenueFinale.Current.aftermath&&VenueFinale.Current.aftermath.parkSequence&&VenueFinale.Current.aftermath.parkSequence.ProtectedFirefight&&e.GetComponent<ParkCoverActor>();
 float entered,nextSupport,nextWave,nextAmmo;int waypoint,waves;bool injured;
 Vector3 viewStart,aimPoint;Quaternion viewRotation;Animator partnerAnimator,officerAnimator;
 AudioSource siren;Light red,blue;Quaternion[] officerRest;Transform[] officerBones;
 Vector3 officerOrigin;Quaternion officerRotation;
 float Elapsed=>Time.time-entered;
 float Duration(string[] lines)=>Mathf.Max(1,lines==null?0:lines.Length)*Mathf.Max(2,lineSeconds);
 string Line(string[] lines)=>lines==null||lines.Length==0?"":lines[Mathf.Clamp((int)(Elapsed/Mathf.Max(2,lineSeconds)),0,lines.Length-1)];
 void Awake(){partnerAnimator=chapter.mingi.GetComponentInChildren<Animator>();officerAnimator=officer.GetComponentInChildren<Animator>();officerOrigin=officer.position;officerRotation=officer.rotation;
  siren=policeCar.gameObject.AddComponent<AudioSource>();siren.playOnAwake=false;siren.loop=true;siren.spatialBlend=.65f;siren.volume=.25f;siren.clip=SuppliedAudio.Current?SuppliedAudio.Current.policeSiren:null;StorySoundPause.Attach(siren);
  var lights=policeCar.GetComponentsInChildren<Light>(true);if(lights.Length>1){red=lights[0];blue=lights[1];}
  ResetSequence();
 }
 void Set(Beat beat){Current=beat;entered=Time.time;}
 void Mark(Transform at,string text){M.marker.gameObject.SetActive(true);StoryTargetDiamond.Place(M.marker,Player.View,at.position+Vector3.up*(at==pistol?.2f:1),Player.transform.position,text);}
 public void BeginPreparation(){ResetSequence();M.getaway.SuppressTraffic();ownsTraffic=true;nextTraffic=0;chapter.bag.gameObject.SetActive(false);pistol.gameObject.SetActive(true);Set(Beat.Pickup);M.subtitle.text=pickupLine;}
 public bool TakePistol(){if(Current!=Beat.Pickup||!Player.CanAct||Vector3.Distance(Player.View.transform.position,pistol.position)>3)return false;
  var delta=pistol.position-Player.View.transform.position;if(Vector3.Dot(Player.View.transform.forward,delta.normalized)<.65f)return false;
  if(Physics.RaycastAll(Player.View.transform.position,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(Player.transform)&&!h.transform.IsChildOf(pistol)&&h.distance<delta.magnitude-.12f))return false;
  var weapon=Player.GetComponent<PlayerWeapon>();weapon.Equip(WeaponKind.Pistol);weapon.GrantMissionAmmo(90);pistol.gameObject.SetActive(false);chapter.StartParkEscape();Set(Beat.Run);waypoint=0;M.subtitle.text=runLine;return true;
 }
 public bool Tick(){if(Current==Beat.None||Current==Beat.Done)return false;
  if(ownsTraffic&&Time.time>=nextTraffic){M.getaway.SuppressTraffic();nextTraffic=Time.time+1;}
  if(Current==Beat.Pickup){M.objective.text="매장 탈출 · 벽의 권총 챙기기 [F]";Mark(pistol,"권총과 탄약");if(PrologueKeyboard.Pressed(Key.F))TakePistol();return true;}
  if(Current==Beat.Run){chapter.Follow();if(Player.RiddenBike)Player.RiddenBike.ExitBike();var next=parkRoute.Marker(Player.transform.position,park.position,false);M.marker.gameObject.SetActive(true);StoryTargetDiamond.Place(M.marker,Player.View,next+Vector3.up,Player.transform.position,"민기와 공원으로");M.objective.text="민기와 공원으로 도망치기";
   if(Vector3.Distance(Player.transform.position,park.position)<4&&Vector3.Distance(chapter.mingi.position,Player.transform.position)<4)BeginLookBack();if(Elapsed>7)M.subtitle.text="";return true;}
  if(Current==Beat.LookBack){UpdateCover(false);AimCamera(threat.position+Vector3.up);if(Elapsed>=turnSeconds||PrologueKeyboard.Pressed(Key.Space)){ReturnControl();Set(Beat.Fight);M.subtitle.text=lookLine;nextWave=Time.time+8;}return true;}
  if(Current==Beat.Fight||Current==Beat.Defend){ArmEnemies();
   if(!engagementReady){UpdateCover(false);M.objective.text="공원 안으로 진입하는 조직원에 대비";
    if(M.Enemies.Count(e=>e&&e.Health>0&&e.GetComponent<ParkCoverActor>()&&e.GetComponent<ParkCoverActor>().AtCover)>=Mathf.Min(2,M.Enemies.Count(e=>e&&e.Health>0))&&M.Enemies.Any(e=>e&&e.Health>0)){engagementReady=true;combatStarted=Time.time;nextWave=Time.time+8;}
    else if(!M.Enemies.Any(e=>e&&e.Health>0)&&M.PendingSpawnCount==0){SpawnWave();}
    return true;
   }
   UpdateCover(true);M.objective.text=Current==Beat.Fight?"민기와 함께 조직원 저지":"다친 민기를 지키며 조직원 저지";M.marker.gameObject.SetActive(false);
   if(Vector3.Distance(Player.transform.position,park.position)>25){M.objective.text="민기를 두고 갈 수 없습니다 · 공원으로 돌아가기";Mark(park,"민기에게 돌아가기");}
   if(Time.time>=nextWave&&waves<3){nextWave=Time.time+10;SpawnWave();}
   if(Current==Beat.Fight){SupportFire();if(Time.time-combatStarted>=fightSeconds){if(M.Enemies.Any(e=>e&&e.Health>0&&!e.Npc.IsKnockedDown))WoundMingi();else if(M.PendingSpawnCount==0&&Time.time>=nextWave){SpawnWave();nextWave=Time.time+5;}}}
   else if(Elapsed>=defendSeconds&&Vector3.Distance(Player.transform.position,chapter.mingi.position)<20&&!Player.IsDown)BeginArrival();
   // 전투 중 탄약이 떨어져 진행이 막히지 않도록 민기 옆에서 예비 탄창을 받는다.
   var weapon=Player.GetComponent<PlayerWeapon>();if(weapon.Equipped==WeaponKind.Pistol&&weapon.Ammo<12&&Vector3.Distance(Player.transform.position,chapter.mingi.position)<4&&Time.time>=nextAmmo){weapon.GrantMissionAmmo(30);nextAmmo=Time.time+10;}
   return true;}
  if(Current==Beat.Wounded){AimCamera(chapter.mingi.position+Vector3.up);if(Elapsed>=2.3f||PrologueKeyboard.Pressed(Key.Space)){ReturnControl();UnfreezeEnemies();Set(Beat.Defend);SpawnWave();nextWave=Time.time+10;}return true;}
  if(Current==Beat.Arrival){
   if(policeArrival){chapter.policeBlock.SetActive(true);policeArrival.Tick(Elapsed);}else {policeCar.position=Vector3.Lerp(carStart.position,carStop.position,Mathf.SmoothStep(0,1,Mathf.Clamp01(Elapsed/4)));if(Elapsed>4){chapter.policeBlock.SetActive(true);MoveOfficer(officerStand.position,1.8f);}}
   FlashLights();M.cinema.transform.SetPositionAndRotation(arrivalCamera.position,arrivalCamera.rotation);M.cinema.transform.LookAt((Elapsed>7?officer.position:policeCar.position)+Vector3.up);
   if((policeArrival?policeArrival.Ready:Elapsed>=arrivalSeconds)||PrologueKeyboard.Pressed(Key.Space)){if(policeArrival)policeArrival.Complete();else{policeCar.SetPositionAndRotation(carStop.position,carStop.rotation);officer.position=officerStand.position;}chapter.policeBlock.SetActive(true);SetOfficerSpeed(0);siren.volume=.08f;Set(Beat.Call);M.subtitle.text=Line(callLines);}return true;}
  if(Current==Beat.Call){M.objective.text="무기를 내려놓고 기다리기";M.subtitle.text=Line(callLines);CloseOfficerCamera();FlashLights();if(Elapsed>=Duration(callLines)||PrologueKeyboard.Pressed(Key.Space))BeginRelease();return true;}
  if(Current==Beat.Release){M.subtitle.text=Line(releaseLines);FlashLights();RetreatGang();var lead=M.Enemies.FirstOrDefault(e=>e&&e.Health>0&&!e.Npc.IsKnockedDown);if(lead){Face(officer,lead.transform.position);AimCamera(lead.transform.position+Vector3.up);}else CloseOfficerCamera();
   if(Elapsed>=Duration(releaseLines)||PrologueKeyboard.Pressed(Key.Space)){Set(Beat.Custody);Player.GetComponent<PlayerWeapon>().Equip(WeaponKind.None);M.subtitle.text=Line(custodyLines);}return true;}
  if(Current==Beat.Custody){M.objective.text="민기와 함께 체포";M.subtitle.text=Line(custodyLines);RetreatGang();Face(officer,Player.transform.position);if(Elapsed<Duration(custodyLines)-lineSeconds)CloseOfficerCamera();else{M.cinema.transform.position=viewStart;AimCamera(chapter.mingi.position+Vector3.up);}
   float duration=Duration(custodyLines);M.fade.gameObject.SetActive(Elapsed>duration-1.5f);M.fade.color=new Color(0,0,0,Mathf.InverseLerp(duration-1.5f,duration,Elapsed));if(Elapsed>=duration||PrologueKeyboard.Pressed(Key.Space))chapter.EnterPrison();return true;}
  return false;
 }
 void ReturnControl(){var rotation=M.cinema.transform.rotation;Player.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);Player.View.transform.rotation=rotation;M.StoryControl(false);}
 void BeginLookBack(){chapter.StopGuide();M.marker.gameObject.SetActive(false);viewStart=Player.View.transform.position;viewRotation=Player.View.transform.rotation;M.StoryControl(true);M.cinema.transform.SetPositionAndRotation(viewStart,viewRotation);Set(Beat.LookBack);engagementReady=false;coverIndex=0;if(partnerCover){partnerCoverActor=chapter.mingi.GetComponent<ParkCoverActor>();if(!partnerCoverActor)partnerCoverActor=chapter.mingi.gameObject.AddComponent<ParkCoverActor>();partnerCoverActor.cover=partnerCover;partnerCoverActor.phase=2;}M.subtitle.text=lookLine;SpawnWave();mingiGun.gameObject.SetActive(true);}
 void SpawnWave(){var points=waves==0?enemyPosts:backupPosts;M.SpawnStreetPursuers(points,Mathf.Min(3,points.Length),4);waves++;}
 public void PrepareParkEnemy(CitizenCombat e){if(!e)return;e.ExternalAttackControl=true;e.usesFirearm=true;e.Provoke(Player);if(enemyCover==null||enemyCover.Length==0)return;var c=e.GetComponent<ParkCoverActor>();if(!c){c=e.gameObject.AddComponent<ParkCoverActor>();c.cover=enemyCover[coverIndex%enemyCover.Length];c.phase=coverIndex++*1.6f;}}
 void UpdateCover(bool allow){foreach(var e in M.Enemies){if(!e)continue;PrepareParkEnemy(e);e.GetComponent<ParkCoverActor>()?.Tick(allow,Player);}if(!injured&&partnerCoverActor)partnerCoverActor.Tick(allow,Player);}
 void StopCover(){foreach(var e in M.Enemies)if(e)e.GetComponent<ParkCoverActor>()?.Stop();if(partnerCoverActor)partnerCoverActor.Stop();}
 void ArmEnemies(){foreach(var e in M.Enemies){if(!e||e.Health<=0)continue;if(!e.usesFirearm){e.usesFirearm=true;e.Provoke(Player);}}}
 void SupportFire(){if(partnerCoverActor&&!partnerCoverActor.CanFire)return;var target=M.Enemies.Where(e=>e&&e.Health>0&&!e.Npc.IsKnockedDown&&!e.Npc.IsDead).OrderBy(e=>(e.transform.position-chapter.mingi.position).sqrMagnitude).FirstOrDefault();if(!target)return;aimPoint=target.transform.position+Vector3.up*1.25f;Face(chapter.mingi,aimPoint);
  if(Time.time<nextSupport)return;var origin=chapter.mingi.position+Vector3.up*1.35f;var d=aimPoint-origin;if(d.magnitude>35)return;
  // 엄폐물에 가려져도 제압 사격은 유지하되 탄환은 벽을 관통하지 않는다.
  bool struck=FirearmBallistics.Cast(origin,d.normalized,d.magnitude,chapter.mingi,out var hit);bool blocked=struck&&!hit.transform.IsChildOf(target.transform);var impact=blocked?hit.point:aimPoint;
  nextSupport=Time.time+1.5f;if(struck)BulletImpactFeedback.Play(hit);SupportShots++;GunshotSound.Play(chapter.mingi.gameObject,true);GunShotFeedback.Fire(mingiGun,HandgunGeometry.Get(mingiGun).Muzzle);PrologueTracer.Spawn(mingiGun.TransformPoint(HandgunGeometry.Get(mingiGun).Muzzle),impact,new Color(1,.8f,.3f));if(!blocked&&SupportShots%3==0)target.ReceiveBullet(Player,d.normalized,18);
 }
 void WoundMingi(){if(Player.IsDown)return;FreezeEnemies();var shooter=M.Enemies.FirstOrDefault(e=>e&&e.Health>0&&!e.Npc.IsKnockedDown);if(shooter){GunshotSound.Play(shooter.gameObject,true);PrologueTracer.Spawn(shooter.transform.position+Vector3.up*1.35f,chapter.mingi.position+Vector3.up*.45f,new Color(1,.55f,.2f));}
  injured=true;if(partnerCoverActor)partnerCoverActor.Stop();chapter.StartInjury();mingiGun.gameObject.SetActive(false);viewStart=Player.View.transform.position;M.StoryControl(true);M.cinema.transform.SetPositionAndRotation(viewStart,Player.View.transform.rotation);Set(Beat.Wounded);M.objective.text="민기가 다리를 맞았다";M.subtitle.text=hurtLine;
 }
 void FreezeEnemies(){StopCover();M.StopStorySpawning();foreach(var e in M.Enemies){if(!e)continue;e.ExternalAttackControl=true;var agent=e.GetComponent<NavMeshAgent>();if(agent&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;}}
 void UnfreezeEnemies(){foreach(var e in M.Enemies)if(e)e.ExternalAttackControl=true;}
 void BeginArrival(){FreezeEnemies();viewStart=Player.View.transform.position;chapter.BeginArrest();Set(Beat.Arrival);M.objective.text="경찰 출동 · 사격 중지";M.subtitle.text=arrivalLine;policeCar.gameObject.SetActive(true);policeCar.SetPositionAndRotation(carStart.position,carStart.rotation);chapter.policeBlock.SetActive(false);if(policeArrival)policeArrival.Begin();if(siren.clip)siren.Play();foreach(var e in M.Enemies)if(e)e.usesFirearm=false;}
 void BeginRelease(){phoneProp.gameObject.SetActive(false);RestoreOfficer();Set(Beat.Release);GangReleased=true;foreach(var e in M.Enemies){if(!e)continue;e.EndFight();e.ExternalAttackControl=true;e.enabled=false;e.Npc.ExternalControl=true;}M.subtitle.text=Line(releaseLines);}
 void RetreatGang(){foreach(var e in M.Enemies){if(!e||e.Health<=0||e.Npc.IsDead||e.Npc.IsKnockedDown)continue;var agent=e.GetComponent<NavMeshAgent>();if(!agent||!agent.enabled||!agent.isOnNavMesh)continue;agent.isStopped=false;agent.speed=3;agent.updateRotation=true;if(!agent.hasPath)agent.SetDestination(gangExit.position);var anim=e.GetComponentInChildren<Animator>();SetSpeed(anim,agent.velocity.magnitude);}}
 void AimCamera(Vector3 at){var d=at-M.cinema.transform.position;if(d.sqrMagnitude>.01f)M.cinema.transform.rotation=Quaternion.Slerp(M.cinema.transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*3);}
 void CloseOfficerCamera(){M.cinema.transform.position=Vector3.Lerp(M.cinema.transform.position,officer.position+officer.forward*2.7f+officer.right*.45f+Vector3.up*1.55f,Time.deltaTime*2);AimCamera(officer.position+Vector3.up*1.5f);}
 void Face(Transform t,Vector3 at){var d=Vector3.ProjectOnPlane(at-t.position,Vector3.up);if(d.sqrMagnitude>.01f)t.rotation=Quaternion.RotateTowards(t.rotation,Quaternion.LookRotation(d),180*Time.deltaTime);}
 void MoveOfficer(Vector3 goal,float speed){Face(officer,goal);officer.position=Vector3.MoveTowards(officer.position,goal,speed*Time.deltaTime);SetOfficerSpeed(Vector3.Distance(officer.position,goal)>.1f?speed:0);}
 void SetOfficerSpeed(float speed)=>SetSpeed(officerAnimator,speed);
 static void SetSpeed(Animator a,float value){if(!a)return;foreach(var p in a.parameters){if(p.name=="MoveSpeed"&&p.type==AnimatorControllerParameterType.Float)a.SetFloat(p.name,value);if(p.name=="IsWalking"&&p.type==AnimatorControllerParameterType.Bool)a.SetBool(p.name,value>.1f);}}
 void FlashLights(){if(red)red.enabled=Mathf.Repeat(Time.time, .6f)<.3f;if(blue)blue.enabled=!red.enabled;}
 void LateUpdate(){if(Time.timeScale<=0)return;if(policeArrival){policeArrival.AimWeapons=Current>=Beat.Arrival&&Current<=Beat.Custody;policeArrival.SpeakerBusy=Current==Beat.Call;}if(Current==Beat.Fight&&!injured&&mingiGun.gameObject.activeSelf)ArmedHandPose.Apply(partnerAnimator,mingiGun,aimPoint,partnerCoverActor?partnerCoverActor.CanFire:true,false);
  if(Current!=Beat.Call){phoneProp.gameObject.SetActive(false);return;}bool calling=Elapsed>=lineSeconds*.8f;
  if(!officerAnimator||!officerAnimator.isHuman)return;var hand=officerAnimator.GetBoneTransform(HumanBodyBones.RightHand);var head=officerAnimator.GetBoneTransform(HumanBodyBones.Head);var upper=officerAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=officerAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
  if(officerBones==null){officerBones=new[]{upper,lower,hand};officerRest=officerBones.Select(b=>b.localRotation).ToArray();}
  float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((Elapsed-lineSeconds*.8f)/.8f));for(int i=0;i<officerBones.Length;i++)officerBones[i].localRotation=officerRest[i];
  var target=head.position+officer.right*.13f-officer.forward*.02f;var elbow=upper.position+officer.forward*.28f-Vector3.up*.2f;upper.rotation=Quaternion.Slerp(upper.rotation,Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation,blend);lower.rotation=Quaternion.Slerp(lower.rotation,Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation,blend);
  phoneProp.gameObject.SetActive(calling);phoneProp.SetPositionAndRotation(hand.position,officer.rotation*Quaternion.Euler(0,0,90));
 }
 void RestoreOfficer(){if(officerBones!=null){for(int i=0;i<officerBones.Length;i++)if(officerBones[i])officerBones[i].localRotation=officerRest[i];officerBones=null;}}
 void RestoreMissionTraffic(){if(ownsTraffic&&M&&M.getaway)M.getaway.RestoreTraffic();ownsTraffic=false;}
 public void Finish(){RestoreMissionTraffic();RestoreOfficer();siren.Stop();phoneProp.gameObject.SetActive(false);mingiGun.gameObject.SetActive(false);policeCar.gameObject.SetActive(false);Set(Beat.Done);}
 public void ResetSequence(){RestoreMissionTraffic();StopCover();engagementReady=false;coverIndex=0;if(policeArrival)policeArrival.ResetArrival();RestoreOfficer();if(siren){siren.Stop();siren.volume=.25f;}if(pistol)pistol.gameObject.SetActive(false);if(phoneProp)phoneProp.gameObject.SetActive(false);if(mingiGun)mingiGun.gameObject.SetActive(false);if(policeCar)policeCar.gameObject.SetActive(false);if(officer){officer.SetPositionAndRotation(officerOrigin,officerRotation);SetOfficerSpeed(0);}injured=false;SupportShots=0;GangReleased=false;waves=0;nextSupport=nextAmmo=0;parkRoute.Reset();Current=Beat.None;}
 void OnDisable(){RestoreMissionTraffic();if(siren)siren.Stop();}
}
}
