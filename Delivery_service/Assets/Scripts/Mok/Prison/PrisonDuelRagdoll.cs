using UnityEngine;
namespace DeliveryService.Mok {
// 결투 패배 전용 물리. 일반 시민의 사망/복귀 로직과 분리한다.
public sealed class PrisonDuelRagdoll : MonoBehaviour {
 public Animator animator;
 public Rigidbody[] bodies;
 public Collider[] shapes;
 Vector3[] positions; Quaternion[] rotations; Transform[] bones;
 public bool Down {get;private set;}
 void Awake(){bones=animator.GetComponentsInChildren<Transform>(true);positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}foreach(var a in shapes)foreach(var b in shapes)if(a!=b)Physics.IgnoreCollision(a,b);ResetBody();}
 public void ResetBody(){foreach(var b in bodies){b.isKinematic=true;}foreach(var c in shapes)c.enabled=false;if(Down)for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}Down=false;animator.enabled=true;var root=GetComponent<Collider>();if(root)root.enabled=true;}
 public void Launch(Vector3 direction){if(Down)return;Down=true;animator.enabled=false;var root=GetComponent<Collider>();if(root)root.enabled=false;foreach(var j in GetComponentsInChildren<CharacterJoint>()){j.autoConfigureConnectedAnchor=false;j.connectedAnchor=j.connectedBody.transform.InverseTransformPoint(j.transform.position);}foreach(var c in shapes)c.enabled=true;Physics.SyncTransforms();foreach(var b in bodies){b.isKinematic=false;b.useGravity=true;b.linearVelocity=direction.normalized*4+Vector3.up*2;b.angularVelocity=Vector3.right*1.5f;}}
}
}
