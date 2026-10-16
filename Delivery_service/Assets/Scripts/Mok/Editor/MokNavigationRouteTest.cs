#if UNITY_EDITOR
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok.Navigation;
public static class MokNavigationRouteTest {
public static string RunCore(){
 var watch=System.Diagnostics.Stopwatch.StartNew();int deliveryFailures=0,returnFailures=0;
 var log=new StringBuilder();var nav=Object.FindFirstObjectByType<NavigationManager>();var orders=Object.FindFirstObjectByType<OrderManager>();var originalTarget=nav.NavigationTarget;
 var points=Object.FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None).OrderBy(p=>p.DeliveryPointID).ToArray();
 var places=Object.FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None);var shop=places.First(p=>p.kind==NavigationPlaceKind.Pizza);
 var filter=new NavMeshQueryFilter{agentTypeID=NavMesh.GetSettingsByIndex(0).agentTypeID,areaMask=(1<<NavMesh.GetAreaFromName("Road"))|(1<<NavMesh.GetAreaFromName("SideWalk"))};
 var origin=new GameObject("Route diagnostic origin");origin.transform.position=nav.NavigationTarget.position;nav.SetNavigationTarget(origin.transform);
 var flags=BindingFlags.Instance|BindingFlags.NonPublic;var id=typeof(OrderManager).GetField("currentTargetDeliveryID",flags);
 log.AppendLine("SHOP "+shop.Destination.position+" origin="+origin.transform.position+" mask="+filter.areaMask);
 foreach(var surface in Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None))log.AppendLine("SURFACE "+surface.name+" agent="+surface.agentTypeID+" data="+surface.navMeshData+" pos="+surface.transform.position);
 foreach(var link in Object.FindObjectsByType<Unity.AI.Navigation.NavMeshLink>(FindObjectsSortMode.None))log.AppendLine("LINK "+link.name+" bidirectional="+link.bidirectional+" area="+link.area);
 foreach(var cost in new[]{1f,2f,20f}){ int mask=filter.areaMask;
  var f=filter;f.areaMask=mask;f.SetAreaCost(NavMesh.GetAreaFromName("SideWalk"),cost);
  foreach(var start in new[]{origin.transform.position,new Vector3(-12.5f,1,5),shop.Destination.position})foreach(var end in new[]{origin.transform.position,new Vector3(-12.5f,1,5),shop.Destination.position}){
   NavMesh.SamplePosition(start,out var a,30,f);NavMesh.SamplePosition(end,out var b,20,f);var path=new NavMeshPath();NavMesh.CalculatePath(a.position,b.position,f,path);
   log.AppendLine("DIRECT sidewalkCost="+cost+" "+a.position+" -> "+b.position+" "+path.status);
  }
 }
 foreach(var p in points){
  id.SetValue(orders,p.DeliveryPointID);nav.ResumeDelivery();if(nav.DisplayPath.Count==0)deliveryFailures++;
  bool found=NavMesh.SamplePosition(p.transform.position,out var hit,20,filter);
  log.AppendLine("OUT "+p.DeliveryPointID+" target="+p.transform.position+" sample="+(found?hit.position.ToString():"NONE")+" nav="+nav.RouteStatus+" corners="+nav.DisplayPath.Count);
 }
 foreach(var p in points){origin.transform.position=p.transform.position;nav.SelectPlace(shop);if(nav.DisplayPath.Count==0)returnFailures++;log.AppendLine("RETURN "+p.DeliveryPointID+" nav="+nav.RouteStatus+" corners="+nav.DisplayPath.Count);}
 // Sample road positions throughout the city to detect disconnected starting islands.
 int failures=0,total=0;for(int x=-180;x<=180;x+=20)for(int z=-180;z<=180;z+=20){var v=new Vector3(x,0,z);if(!NavMesh.SamplePosition(v,out var h,3,filter))continue;origin.transform.position=h.position;nav.SelectPlace(shop);total++;if(nav.DisplayPath.Count==0&&Vector3.Distance(h.position,shop.Destination.position)>5){failures++;if(failures<35)log.AppendLine("GRID_FAIL "+h.position+" "+nav.RouteStatus);}}
 log.AppendLine("GRID total="+total+" failures="+failures);
 log.AppendLine("Delivery failures="+deliveryFailures+" return failures="+returnFailures+" total elapsed ms="+watch.ElapsedMilliseconds);
 if(deliveryFailures+returnFailures+failures>0)throw new System.Exception(log.ToString());
 origin.transform.position=shop.Destination.position;nav.SelectPlace(shop);if(nav.DisplayPath.Count!=0||!nav.RouteStatus.Contains("도착"))throw new System.Exception("Arrival regression");
 origin.transform.position=new Vector3(10000,0,10000);nav.RefreshRoute();if(nav.DisplayPath.Count!=0||nav.RouteStatus.Contains("안내 중"))throw new System.Exception("Off-map route must not be fabricated");
 nav.StopGuidance();if(nav.HasRouteDestination||nav.DisplayPath.Count!=0)throw new System.Exception("Stop guidance regression");
 log.AppendLine("PASS arrival, off-map rejection and stop guidance");
 id.SetValue(orders,"");nav.SetNavigationTarget(originalTarget);nav.ResumeDelivery();Object.DestroyImmediate(origin);
 log.AppendLine(MokNavigationPhoneTest.RunCore());
 return log.ToString();
}}
#endif

