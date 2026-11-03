using UnityEngine;
namespace DeliveryService.Mok {
// 사람 대신 돌파하는 가벼운 도로 장애물. 파편은 차량을 다시 막지 않는다.
public sealed class PrologueBreakableBarrier : MonoBehaviour {
 public bool Broken{get;private set;}
 public float minimumSpeed=3;
 public bool TryBreak(Vector3 velocity,Transform car){
  if(Broken)return true;
  if(velocity.magnitude<minimumSpeed)return false;
  Broken=true;
  foreach(var piece in GetComponentsInChildren<Rigidbody>()){
   foreach(var hit in piece.GetComponents<Collider>())foreach(var vehicle in car.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(hit,vehicle);
   piece.isKinematic=false;piece.linearVelocity=velocity.normalized*Mathf.Clamp(velocity.magnitude*.35f,2,7)+Vector3.up*2;
   piece.angularVelocity=new Vector3(1,2,3);Destroy(piece.gameObject,4);
  }
  var feedback=car.GetComponent<PrologueDriveFeedback>();if(feedback)feedback.Collision(velocity.magnitude*.5f);
  return true;
 }
}
}
