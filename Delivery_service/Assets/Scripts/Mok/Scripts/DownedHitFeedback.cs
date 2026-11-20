using UnityEngine;
namespace DeliveryService.Mok {
// 추가 타격은 기존 회복 시간을 다시 시작하지 않고 현재 레그돌에 작은 충격만 준다.
public static class DownedHitFeedback {
 public static void Apply(NPCControl npc, Vector3 direction){
  var body=npc.RagdollCenter.GetComponent<Rigidbody>();
  if(body&&!body.isKinematic)body.AddForce(direction.normalized*.8f,ForceMode.VelocityChange);
 }
}
}
