using System.Linq;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
// 새 탈것을 만들지 않고 기존 오토바이와 적재물을 함께 옮긴다.
public sealed class BikeRecall:MonoBehaviour {
 public MotorcycleController bike;public PlayerCombat player;public WaterRescue water;
 public string Message {get;private set;}="앞쪽의 빈 공간으로 오토바이를 부릅니다.";
 float availableAt;
 bool Restricted(Vector3 p)=>FindObjectsByType<RobberyDistrict>(FindObjectsSortMode.None).Any(d=>d.Contains(p));
 public bool FindSpot(out Vector3 point,out Quaternion rotation){
  point=default;var forward=Vector3.ProjectOnPlane(player.View.transform.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=player.transform.forward;rotation=Quaternion.LookRotation(forward);
  // 정면 부채꼴 안에서 평평한 지면과 오토바이 전체 공간을 함께 확인한다.
  foreach(float distance in new[]{3.5f,5f,6.5f})foreach(float angle in new[]{0f,25f,-25f,50f,-50f}){
   var wish=player.transform.position+Quaternion.Euler(0,angle,0)*forward*distance;
   if(!NavMesh.SamplePosition(wish,out var nav,1.5f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-player.transform.position.y)>2||Restricted(nav.position))continue;
   if(!Physics.Raycast(nav.position+Vector3.up*1.5f,Vector3.down,out var hit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||hit.normal.y<.94f)continue;
   var p=hit.point;if(water&&water.water&&p.y<water.water.transform.position.y+.25f)continue;
   bool valid=true;foreach(float x in new[]{-.6f,.6f})foreach(float z in new[]{-1.2f,1.2f}){var q=p+rotation*new Vector3(x,0,z);if(!Physics.Raycast(q+Vector3.up*.8f,Vector3.down,out var edge,1.2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||Mathf.Abs(edge.point.y-p.y)>.15f)valid=false;}
   if(!valid)continue;
   if(Physics.OverlapBox(p+Vector3.up*1.1f,new Vector3(.7f,.95f,1.35f),rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(bike.transform)&&c!=bike.sphereCollider))continue;
   point=p;return true;
  }return false;
 }
 public bool Call(){
  if(!bike||!player){Message="등록된 오토바이가 없습니다.";return false;}
  if(Time.timeScale<=0||Time.time<availableAt){Message="잠시 후 다시 호출해 주세요.";return false;}
  if(bike.isDriven||player.RiddenBike||player.IsDown||player.WaterRescueActive||PoliceControl.IsArrestInProgress||(water&&water.Busy)){Message="지금은 호출할 수 없습니다.";return false;}
  if(bike.isRefueling||!bike.IsControlAvailable||(bike.sphereRB&&bike.sphereRB.linearVelocity.magnitude>2)){Message="주유나 이동이 끝나면 호출할 수 있습니다.";return false;}
  if(Restricted(player.transform.position)){Message="빈민가 밖의 주차 공간에서 호출해 주세요.";return false;}
  if(!FindSpot(out var ground,out var rotation)){Message="앞쪽에 빈 공간이 없습니다. 넓은 곳에서 다시 호출해 주세요.";return false;}
  var offset=Quaternion.Inverse(bike.transform.rotation)*(bike.transform.position-bike.sphereRB.position);
  float radius=bike.sphereCollider.bounds.extents.y;var sphere=ground+Vector3.up*(radius+.12f);
  var anchor=new GameObject("Bike recall anchor").transform;anchor.SetPositionAndRotation(sphere+rotation*offset,rotation);
  bike.RespawnBike(anchor);bike.sphereRB.position=sphere;bike.sphereRB.transform.position=sphere;bike.transform.position=anchor.position;bike.sphereRB.linearVelocity=Vector3.zero;bike.sphereRB.angularVelocity=Vector3.zero;Physics.SyncTransforms();Destroy(anchor.gameObject);
  availableAt=Time.time+3;Message="오토바이가 도착했습니다.";DeliveryRequestNotice.Show(Message);return true;
 }
}
}
