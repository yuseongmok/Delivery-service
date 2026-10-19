using System.Collections;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 수면의 실제 범위와 높이로 검사하며 다리 위에서는 입수 처리하지 않는다.
public sealed class WaterRescue:MonoBehaviour {
 public Renderer water;public PlayerCombat player;public MotorcycleController bike;public Transform hospital,towPoint;
 public bool Busy {get;private set;}public int HospitalCharges {get;private set;}public int TowCharges {get;private set;}
 float checkAt,graceUntil;bool bikeQueued;
 public bool Submerged(Vector3 point){if(water==null)return false;var b=water.bounds;return point.x>=b.min.x&&point.x<=b.max.x&&point.z>=b.min.z&&point.z<=b.max.z&&point.y<water.transform.position.y-.2f;}
 Vector3 PlayerPoint(){var rag=player.GetComponentInChildren<PlayerRagdoll>();if(rag!=null&&rag.IsActive)return rag.hips.position;return player.transform.position;}
 void Update(){
  if(Busy||Time.time<graceUntil||Time.time<checkAt||Time.timeScale<=0||player==null||hospital==null||PoliceControl.IsArrestInProgress)return;checkAt=Time.time+.1f;
  bool wetBike=bike!=null&&Submerged(bike.sphereRB!=null?bike.sphereRB.position:bike.transform.position);
  bool wetPlayer=Submerged(PlayerPoint())||(wetBike&&bike.isDriven);
  if(wetPlayer||wetBike)Begin(wetPlayer,wetBike);
 }
 public bool Begin(bool rescuePlayer,bool towBike){if(Busy||Time.time<graceUntil||(!rescuePlayer&&!towBike)||hospital==null||(towBike&&(bike==null||towPoint==null)))return false;Busy=true;bikeQueued=towBike;StartCoroutine(Recover(rescuePlayer));return true;}
 IEnumerator Recover(bool rescuePlayer){
  bool alreadyHospital=player.IsDown&&player.HitsTaken>=3;
  if(bikeQueued&&bike.isDriven)bike.ExitBike();
  if(rescuePlayer){
   player.WaterRescueActive=true;FindFirstObjectByType<PhoneController>()?.ClosePhone();
   FindFirstObjectByType<PoliceManager>()?.CancelReplacements();foreach(var cop in SceneActors.Police)Destroy(cop.gameObject);
   foreach(var citizen in FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None))citizen.EndFight();
   if(player.hospitalFade!=null)yield return player.hospitalFade.DarkenAfter(0);
   // 기존 사고·전투 복구가 끝난 뒤 이동해야 원래 위치로 되돌아가지 않는다.
   while(player.IsDown||(player.avatar!=null&&player.avatar.IsPhysical))yield return null;
   if(player.hospitalFade!=null)yield return player.hospitalFade.DarkenAfter(0);
  }
  if(bikeQueued){
   if(TryParking(towPoint.position,out var spot)){
    var anchor=new GameObject("견인 임시 위치").transform;anchor.SetPositionAndRotation(spot,towPoint.rotation);
    bike.StopAllCoroutines();bike.RespawnBike(anchor);Destroy(anchor.gameObject);
    MoneyManager.Instance?.AccidentMoney(10000);TowCharges++;DeliveryRequestNotice.Show("오토바이 견인비 10,000원");
   }else{ // 이동 가능한 자리가 없으면 비용 없이 다시 시도한다.
    Busy=false;player.WaterRescueActive=false;graceUntil=Time.time+2;
    if(rescuePlayer&&player.hospitalFade!=null)yield return player.hospitalFade.WakeUp();yield break;
   }
  }
  if(rescuePlayer){
   var move=player.GetComponent<PlayerMove>();var cc=player.GetComponent<CharacterController>();var look=player.View.GetComponent<CameraMove>();
   bool moving=move!=null&&move.enabled,looking=look!=null&&look.enabled,colliding=cc!=null&&cc.enabled;
   if(move!=null)move.enabled=false;if(look!=null)look.enabled=false;if(cc!=null)cc.enabled=false;
   var body=player.GetComponent<Rigidbody>();if(body!=null&&!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
   player.transform.SetPositionAndRotation(hospital.position,hospital.rotation);player.View.transform.localRotation=Quaternion.identity;if(look!=null)look.SyncViewFromTransform();
   player.ResetAfterWaterRescue();TrafficRecovery.Get(player.gameObject).Protect(3);
   if(!alreadyHospital){MoneyManager.Instance?.AccidentMoney(20000);HospitalCharges++;}
   if(player.hospitalFade!=null)yield return player.hospitalFade.WakeUp();
   if(move!=null)move.enabled=moving;if(look!=null)look.enabled=looking;if(cc!=null)cc.enabled=colliding;
   player.WaterRescueActive=false;
  }
  bikeQueued=false;graceUntil=Time.time+3;Busy=false;
 }
 public bool TryParking(Vector3 center,out Vector3 result){
  for(int i=0;i<33;i++){
   var desired=center+(i==0?Vector3.zero:Quaternion.Euler(0,i*45,0)*Vector3.forward*(2+(i-1)/8*2));
   if(!NavMesh.SamplePosition(desired,out var nav,2,NavMesh.AllAreas))continue;
   if(water!=null&&nav.position.y<water.transform.position.y+.3f)continue;
   if(!Physics.Raycast(nav.position+Vector3.up*2,Vector3.down,out var ground,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||ground.normal.y<.85f)continue;
   bool blocked=false;foreach(var col in Physics.OverlapCapsule(ground.point+Vector3.up*.6f,ground.point+Vector3.up*1.7f,.55f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
    if((bike!=null&&col.transform.IsChildOf(bike.transform))||(player!=null&&col.transform.IsChildOf(player.transform)))continue;blocked=true;break;
   }
   if(blocked)continue;result=ground.point+Vector3.up*.1f;return true;
  }result=default;return false;
 }
}
}
