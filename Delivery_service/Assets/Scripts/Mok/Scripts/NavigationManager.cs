using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using DeliveryService.Mok.Navigation;
public class NavigationManager : MonoBehaviour {
 [SerializeField] OrderManager orderManager;
 [SerializeField] Transform playerTransform;
 [SerializeField] LineRenderer pathLineRenderer;
 [SerializeField] float pathLineHeight=.6f;
 [SerializeField] float pathUpdateInterval=.5f;
 [SerializeField] float pathUpdateDistance=1.5f;
 readonly List<Vector3> displayPath=new List<Vector3>();
 NavigationPlace selectedPlace;
 Transform lastDestination;
 DeliveryPoint lastOrder;
 Vector3 lastPosition,lastDestinationPosition;
 float nextUpdate;
 bool stopped;
 int knownDay;
 DayNightCycle cycle;
 public float RemainingDistance {get;private set;}
 public IReadOnlyList<Vector3> DisplayPath=>displayPath;
 // 기존 배달 강조 표시와 주문 코드가 쓰는 배달 전용 API를 유지한다.
 public DeliveryPoint CurrentDestination=>orderManager==null?null:orderManager.CurrentTargetDeliveryPoint;
 public Transform NavigationTarget=>playerTransform;
 public NavigationPlace SelectedPlace=>selectedPlace;
 public Transform RouteDestination=>stopped?null:selectedPlace!=null?selectedPlace.Destination:CurrentDestination!=null?CurrentDestination.transform:null;
 public bool HasRouteDestination=>RouteDestination!=null;
 public string DestinationName=>selectedPlace!=null?selectedPlace.displayName:CurrentDestination!=null?CurrentDestination.DeliveryPointID:"목적지 없음";
 public string RouteStatus {get;private set;}="목적지를 선택하세요";
 public int RouteRevision {get;private set;}
 void Awake(){cycle=FindFirstObjectByType<DayNightCycle>();knownDay=cycle==null?1:cycle.currentDay;}
 public void SetNavigationTarget(Transform target){if(target==null)return;playerTransform=target;RefreshRoute();}
 public void SelectPlace(NavigationPlace place){if(place==null||!place.IsAvailable)return;selectedPlace=place;stopped=false;RefreshRoute();}
 // 주문 시스템 밖의 스토리 배달 목적지. 휴대폰 목적지 목록의 '배달지' 줄이 이 장소를 가리킨다.
 public NavigationPlace StoryDelivery {get;private set;}
 NavigationPlace temporaryStoryPlace;
 public bool HasDelivery=>CurrentDestination!=null||StoryDelivery!=null;
 public void SetStoryDelivery(NavigationPlace place){if(place==null)return;if(StoryDelivery!=place&&temporaryStoryPlace!=place)DropTemporaryPlace();StoryDelivery=place;SelectPlace(place);}
 // 배달 대상에 NavigationPlace가 없으면 안내하는 동안만 임시로 붙였다가 끝나면 지운다.
 public void SetStoryDelivery(Transform target,string displayName){
  if(target==null)return;var place=target.GetComponent<NavigationPlace>();
  if(place==null){DropTemporaryPlace();place=target.gameObject.AddComponent<NavigationPlace>();place.placeId="story-delivery";place.displayName=displayName;place.kind=NavigationPlaceKind.Other;temporaryStoryPlace=place;}
  SetStoryDelivery(place);
 }
 public void ClearStoryDelivery(){if(StoryDelivery==null)return;bool guiding=selectedPlace==StoryDelivery;StoryDelivery=null;DropTemporaryPlace();if(guiding)StopGuidance();}
 void DropTemporaryPlace(){if(temporaryStoryPlace==null)return;if(selectedPlace==temporaryStoryPlace)selectedPlace=null;Destroy(temporaryStoryPlace);temporaryStoryPlace=null;}
 public void ResumeDelivery(){if(CurrentDestination==null&&StoryDelivery!=null){SelectPlace(StoryDelivery);return;}selectedPlace=null;stopped=false;RefreshRoute();}
 public void StopGuidance(){selectedPlace=null;stopped=true;ClearNavigationPath();RouteStatus="안내를 종료했습니다";RouteRevision++;}
 // 일차 변경·주문 완료·시간 초과 시 호출하며 같은 날의 시설 안내 경로는 유지한다.
 public void ResetNavigationForNewDay(){int day=cycle==null?knownDay:cycle.currentDay;if(day!=knownDay){selectedPlace=null;stopped=false;knownDay=day;}ClearNavigationPath();lastDestination=null;nextUpdate=0;}
 void Update(){
 if(CurrentDestination!=lastOrder){lastOrder=CurrentDestination;if(lastOrder!=null)stopped=false;RefreshRoute();}
 if(selectedPlace!=null&&!selectedPlace.IsAvailable){selectedPlace=null;RefreshRoute();}
 var destination=RouteDestination;
 if(destination==null){if(displayPath.Count>0)ClearNavigationPath();return;}
 if(playerTransform==null)return;
 if(Time.unscaledTime<nextUpdate)return;
 if(destination!=lastDestination||Vector3.Distance(playerTransform.position,lastPosition)>=pathUpdateDistance||Vector3.Distance(destination.position,lastDestinationPosition)>.5f||displayPath.Count==0)CalculateNavigationPath();
 nextUpdate=Time.unscaledTime+pathUpdateInterval;
 }
 public void RefreshRoute(){RouteRevision++;CalculateNavigationPath();nextUpdate=0;}
 public void DebugCurrentDestination(){Debug.Log("[Navigation] "+DestinationName+" / "+RouteStatus);}
 void CalculateNavigationPath(){using(DeliveryService.Mok.PerformanceMarkers.Route.Auto()){
 var destination=RouteDestination;ClearNavigationPath();
 if(playerTransform==null||destination==null){RouteStatus="목적지를 선택하세요";return;}
 lastDestination=destination;lastPosition=playerTransform.position;lastDestinationPosition=destination.position;
 if(Vector2.Distance(new Vector2(lastPosition.x,lastPosition.z),new Vector2(lastDestinationPosition.x,lastDestinationPosition.z))<4){RouteStatus="목적지에 도착했습니다";return;}
 var routeCorners=FindTravelRoute(lastPosition,lastDestinationPosition);
 if(routeCorners==null){RouteStatus="목적지까지 연결된 도로가 없습니다";return;}
 if(routeCorners.Length<2){RouteStatus="목적지 근처입니다";return;}
 // 건물을 가로지르지 않도록 실제 통행 가능한 경로 모서리를 유지한다.
 for(int i=0;i<routeCorners.Length;i++){displayPath.Add(routeCorners[i]);if(i>0)RemainingDistance+=Vector3.Distance(routeCorners[i-1],routeCorners[i]);}
 if(pathLineRenderer!=null){pathLineRenderer.positionCount=displayPath.Count;for(int i=0;i<displayPath.Count;i++){var point=displayPath[i];point.y=pathLineHeight;pathLineRenderer.SetPosition(i,point);}}
 RouteStatus="안내 중";
 }}
 // 배달 시간과 화면 안내가 동일한 실제 도로 경로를 사용한다.
 public static bool TryMeasureTravelDistance(Vector3 start,Vector3 end,out float distance){
  distance=0;var corners=FindTravelRoute(start,end);if(corners==null||corners.Length==0)return false;
  distance=Vector3.Distance(start,corners[0])+Vector3.Distance(corners[corners.Length-1],end);
  for(int i=1;i<corners.Length;i++)distance+=Vector3.Distance(corners[i-1],corners[i]);return true;
 }
 public static Vector3[] FindTravelRoute(Vector3 startPosition,Vector3 endPosition){
  // 차도가 'Road' 영역으로 구워지지 않은 지역(신도시·야마자키)은 도로+인도만으로 이어지지 않는다.
  // 기존 탐색이 실패할 때만 일반 바닥(비용 높게)을 더하고, 지붕처럼 높이가 크게 다른 기준점은 쓰지 않는다.
  return FindTravelRoute(startPosition,endPosition,false)??FindTravelRoute(startPosition,endPosition,true);
 }
 static Vector3[] FindTravelRoute(Vector3 startPosition,Vector3 endPosition,bool anyWalkable){
 int road=NavMesh.GetAreaFromName("Road"),sidewalk=NavMesh.GetAreaFromName("SideWalk");
 if(road<0||sidewalk<0||NavMesh.GetSettingsCount()==0){return null;}
 int mask=(1<<road)|(1<<sidewalk);
 var filter=new NavMeshQueryFilter{agentTypeID=NavMesh.GetSettingsByIndex(0).agentTypeID,areaMask=mask};filter.SetAreaCost(road,1);filter.SetAreaCost(sidewalk,2);
 int walkable=NavMesh.GetAreaFromName("Walkable");
 if(anyWalkable){if(walkable<0)return null;filter.areaMask|=1<<walkable;filter.SetAreaCost(walkable,3);}
 anchorHeightLimit=anyWalkable?2.5f:float.PositiveInfinity;
 // 가까운 보도 영역이 끊어진 입구나 위층일 수 있다.
 // 완전한 기존 경로를 유지하고 없으면 주변 도로 기준점을 탐색한다.
 // 건물을 가로지르는 직선이나 목적지에 닿지 않는 경로는 사용하지 않는다.
 var starts = new List<Vector3>(); var ends = new List<Vector3>();
 AddAnchor(startPosition, 30, filter, starts);
 AddAnchor(endPosition, 20, filter, ends);
 var roadFilter = filter; roadFilter.areaMask = anyWalkable ? filter.areaMask : 1 << road; // 2차 탐색은 고립된 입구 주변의 연결된 바닥까지 기준점으로 쓴다.
 AddAnchor(startPosition, 30, roadFilter, starts);
 AddAnchor(endPosition, 20, roadFilter, ends);
 // 2차 탐색: 가장 가까운 면이 지붕처럼 높이가 달라 빠졌으면 주변 바닥에서 기준점을 찾는다.
 if(anyWalkable){if(starts.Count==0)AddNearbyAnchors(startPosition,roadFilter,starts);if(ends.Count==0)AddNearbyAnchors(endPosition,roadFilter,ends);}
 if(starts.Count==0||ends.Count==0){return null;}
 var path=new NavMeshPath(); bool complete=false;
 for(int i=0;i<starts.Count&&!complete;i++)for(int j=0;j<ends.Count&&!complete;j++)
  complete=NavMesh.CalculatePath(starts[i],ends[j],filter,path)&&path.status==NavMeshPathStatus.PathComplete;
 if(!complete){
  // 보도 턱·입구·겹친 표면 때문에 가장 가까운 도로 기준점도 끊길 수 있다.
  // 가까운 기준점 탐색에 실패한 경우에만 제한된 주변 범위를 검사한다.
  AddNearbyAnchors(endPosition,roadFilter,ends);
  for(int i=0;i<starts.Count&&!complete;i++)for(int j=0;j<ends.Count&&!complete;j++)
   complete=NavMesh.CalculatePath(starts[i],ends[j],filter,path)&&path.status==NavMeshPathStatus.PathComplete;
  if(!complete){
   AddNearbyAnchors(startPosition,roadFilter,starts);
   for(int i=0;i<starts.Count&&!complete;i++)for(int j=0;j<ends.Count&&!complete;j++)
    complete=NavMesh.CalculatePath(starts[i],ends[j],filter,path)&&path.status==NavMeshPathStatus.PathComplete;
  }
 }
 Vector3[] routeCorners = complete ? path.corners : TryTransitRoute(starts, ends, filter);
 return routeCorners;
 }
 // 확장 구역의 긴 경로가 한 번에 계산되지 않으면 등록된 입구를 경유한다.
 // 양쪽 구간이 모두 연결된 경우에만 표시하며 직선으로 빈 구간을 잇지 않는다.
 static Vector3[] TryTransitRoute(List<Vector3> starts,List<Vector3> ends,NavMeshQueryFilter filter){
  foreach(var place in Object.FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None)){
   if(place.kind!=NavigationPlaceKind.Other||!place.IsAvailable)continue;
   if(!NavMesh.SamplePosition(place.Destination.position,out var hit,3,filter))continue;
   foreach(var start in starts)foreach(var end in ends){
    var first=new NavMeshPath();var second=new NavMeshPath();
    if(!NavMesh.CalculatePath(start,hit.position,filter,first)||first.status!=NavMeshPathStatus.PathComplete)continue;
    if(!NavMesh.CalculatePath(hit.position,end,filter,second)||second.status!=NavMeshPathStatus.PathComplete)continue;
    var result=new List<Vector3>(first.corners);
    foreach(var corner in second.corners)if(result.Count==0||(result[result.Count-1]-corner).sqrMagnitude>.001f)result.Add(corner);
    return result.ToArray();
   }
  }
  return null;
 }
 static void AddNearbyAnchors(Vector3 position,NavMeshQueryFilter filter,List<Vector3> anchors){
  for(int radius=4;radius<=12;radius+=4)for(int direction=0;direction<8;direction++){
   float angle=direction*Mathf.PI/4;
   var probe=position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
   AddAnchor(probe,4,filter,anchors);
  }
 }
 static float anchorHeightLimit=float.PositiveInfinity;
 static void AddAnchor(Vector3 position,float radius,NavMeshQueryFilter filter,List<Vector3> anchors){
  if(!NavMesh.SamplePosition(position,out var hit,radius,filter))return;
  if(Mathf.Abs(hit.position.y-position.y)>anchorHeightLimit)return;
  foreach(var anchor in anchors)if((anchor-hit.position).sqrMagnitude<.01f)return;
  anchors.Add(hit.position);
 }
 void ClearNavigationPath(){displayPath.Clear();RemainingDistance=0;if(pathLineRenderer!=null)pathLineRenderer.positionCount=0;}
}


