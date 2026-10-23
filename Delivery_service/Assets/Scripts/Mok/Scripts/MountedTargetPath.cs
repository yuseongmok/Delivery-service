using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok {
public static class MountedTargetPath {
 // 도로 가장자리의 분리된 작은 NavMesh 대신 실제 연결된 보행 지점을 고른다.
 public static bool Set(NavMeshAgent agent,Vector3 target){
  if(!agent||!agent.isOnNavMesh)return false;var path=new NavMeshPath();
  for(int ring=0;ring<=3;ring++){int count=ring==0?1:8;for(int i=0;i<count;i++){float a=i*Mathf.PI*.25f;var wish=target+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*ring*3;
   if(NavMesh.SamplePosition(wish,out var hit,2.5f,agent.areaMask)&&agent.CalculatePath(hit.position,path)&&path.status==NavMeshPathStatus.PathComplete){agent.SetPath(path);return true;}
  }}return false;
 }
}
}
