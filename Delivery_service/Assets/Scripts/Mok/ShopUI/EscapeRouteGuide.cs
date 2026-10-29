using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 경유지 통과 기록 대신 현재 위치에서 목적지까지 경로를 다시 계산한다.
public sealed class EscapeRouteGuide {
 Vector3[] corners;Vector3 lastStart,lastEnd;float next;bool ready;
 public bool HasPath{get;private set;}public int Revision{get;private set;}
 public Vector3 Marker(Vector3 start,Vector3 end,bool driving){
  if(!ready||(end-lastEnd).sqrMagnitude>.25f||(Time.time>=next&&(!HasPath||(start-lastStart).sqrMagnitude>.25f))){
   ready=true;next=Time.time+.75f;lastStart=start;lastEnd=end;Revision++;corners=null;
   if(driving)corners=NavigationManager.FindTravelRoute(start,end);
   else if(NavMesh.SamplePosition(start,out var a,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(end,out var b,3,NavMesh.AllAreas)){
    var path=new NavMeshPath();if(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)corners=path.corners;
   }
   HasPath=corners!=null&&corners.Length>1;
  }
  if(!HasPath)return end; // 연결되지 않은 공간을 통과하라고 안내하지 않고 최종 목적지만 표시한다.
  float radius=driving?3:2;
  for(int i=1;i<corners.Length;i++)if(Vector3.Distance(start,corners[i])>radius)return corners[i];
  return end;
 }
 public void Reset(){ready=false;corners=null;HasPath=false;}
}
}
