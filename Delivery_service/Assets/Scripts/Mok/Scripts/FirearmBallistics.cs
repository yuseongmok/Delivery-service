using System.Linq;using UnityEngine;
namespace DeliveryService.Mok {
public static class FirearmBallistics {
 public static Vector3 Direction(Camera camera,float aim){var disk=Random.insideUnitCircle*Mathf.Tan(Mathf.Lerp(3.5f,.12f,aim)*Mathf.Deg2Rad);return (camera.transform.forward+camera.transform.right*disk.x+camera.transform.up*disk.y).normalized;}
 public static bool Cast(Vector3 origin,Vector3 direction,float distance,Transform owner,out RaycastHit hit){foreach(var h in Physics.RaycastAll(origin,direction,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(h.transform.IsChildOf(owner)||(h.collider.isTrigger&&!h.collider.GetComponent<HeadHitbox>()))continue;hit=h;return true;}hit=default;return false;}
 public static bool Headshot(RaycastHit hit){if(hit.collider.GetComponent<HeadHitbox>())return true;var actor=hit.collider.GetComponentInParent<Animator>();if(!actor){var npc=hit.collider.GetComponentInParent<NPCControl>();var officer=hit.collider.GetComponentInParent<PrologueOfficer>();actor=npc?npc.GetComponentInChildren<Animator>():officer?officer.animator:null;}if(!actor||!actor.isHuman)return false;var head=actor.GetBoneTransform(HumanBodyBones.Head);return head&&(hit.collider.transform.IsChildOf(head)||(Mathf.Abs(hit.point.y-head.position.y)<.2f&&Vector3.ProjectOnPlane(hit.point-head.position,Vector3.up).magnitude<.45f));}
}
}
