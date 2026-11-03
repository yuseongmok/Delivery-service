using UnityEngine;
namespace DeliveryService.Mok {
public sealed class SyntyGripFingers:MonoBehaviour {
 public Transform[] joints;public Quaternion[] rests;public int[] kinds;
 public void Apply(Transform gun,bool gripping){
  if(joints==null)return;
  for(int i=0;i<joints.Length;i++)if(joints[i])joints[i].localRotation=rests[i];
  if(!gripping)return;
  // 손가락 길이를 유지하며 손잡이 둘레로 각 마디를 접는다.
  for(int i=0;i<joints.Length;i++){
   var joint=joints[i];if(!joint)continue;int segment=i%3;var child=segment<2?joints[i+1]:null;
   if(!child)continue;
   bool thumb=kinds[i]==0;float height=kinds[i]==1?-.035f:-.085f;
   Vector3 point=thumb?new Vector3(-.025f,segment==0?-.015f:-.035f,-.07f):new Vector3(segment==0?-.008f:-.035f,height,segment==0?-.035f:-.065f);
   joint.rotation=Quaternion.FromToRotation(child.position-joint.position,gun.TransformPoint(point)-joint.position)*joint.rotation;
  }
 }
}
}
