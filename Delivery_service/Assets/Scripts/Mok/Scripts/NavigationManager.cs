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
 // Keep the delivery-only API for existing delivery highlighting and order scripts.
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
 public void ResumeDelivery(){selectedPlace=null;stopped=false;RefreshRoute();}
 public void StopGuidance(){selectedPlace=null;stopped=true;ClearNavigationPath();RouteStatus="안내를 종료했습니다";RouteRevision++;}
 // Called both on day changes and completed/expired orders. Preserve an independent POI route within the same day.
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
 void CalculateNavigationPath(){
 var destination=RouteDestination;ClearNavigationPath();
 if(playerTransform==null||destination==null){RouteStatus="목적지를 선택하세요";return;}
 lastDestination=destination;lastPosition=playerTransform.position;lastDestinationPosition=destination.position;
 if(Vector2.Distance(new Vector2(lastPosition.x,lastPosition.z),new Vector2(lastDestinationPosition.x,lastDestinationPosition.z))<4){RouteStatus="목적지에 도착했습니다";return;}
 int road=NavMesh.GetAreaFromName("Road"),sidewalk=NavMesh.GetAreaFromName("SideWalk");
 if(road<0||sidewalk<0||NavMesh.GetSettingsCount()==0){RouteStatus="도로 정보를 찾을 수 없습니다";return;}
 int mask=(1<<road)|(1<<sidewalk);
 var filter=new NavMeshQueryFilter{agentTypeID=NavMesh.GetSettingsByIndex(0).agentTypeID,areaMask=mask};filter.SetAreaCost(road,1);filter.SetAreaCost(sidewalk,20);
 if(!NavMesh.SamplePosition(lastPosition,out var start,30,filter)||!NavMesh.SamplePosition(lastDestinationPosition,out var end,20,filter)){RouteStatus="이 위치에서는 경로를 찾을 수 없습니다";return;}
 var path=new NavMeshPath();
 if(!NavMesh.CalculatePath(start.position,end.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete){RouteStatus="목적지까지 연결된 도로가 없습니다";return;}
 if(path.corners.Length<2){RouteStatus="목적지 근처입니다";return;}
 // Preserve actual navigable corners: adding artificial right-angle bends can cut through buildings.
 for(int i=0;i<path.corners.Length;i++){displayPath.Add(path.corners[i]);if(i>0)RemainingDistance+=Vector3.Distance(path.corners[i-1],path.corners[i]);}
 if(pathLineRenderer!=null){pathLineRenderer.positionCount=displayPath.Count;for(int i=0;i<displayPath.Count;i++){var point=displayPath[i];point.y=pathLineHeight;pathLineRenderer.SetPosition(i,point);}}
 RouteStatus="안내 중";
 }
 void ClearNavigationPath(){displayPath.Clear();RemainingDistance=0;if(pathLineRenderer!=null)pathLineRenderer.positionCount=0;}
}
