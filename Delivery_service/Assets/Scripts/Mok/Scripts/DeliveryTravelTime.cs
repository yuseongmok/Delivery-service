using System.Linq;using UnityEngine;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok {
public static class DeliveryTravelTime {
 public const float NearMeters=250f,MiddleMeters=500f;
 public static float MinutesForDistance(float meters)=>meters<=NearMeters?50:meters<=MiddleMeters?70:90;
 public static float Recommend(DeliveryPoint point,out float meters){
  meters=-1;var store=Object.FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None).FirstOrDefault(p=>p.kind==NavigationPlaceKind.Pizza);
  if(store!=null&&point!=null&&NavigationManager.TryMeasureTravelDistance(store.Destination.position,point.transform.position,out meters))return MinutesForDistance(meters);
  // 연결된 도로를 못 찾았을 때 직선거리로 짧게 판정하지 않고 가장 넉넉한 시간을 준다.
  meters=-1;Debug.LogWarning("배달 이동 경로를 찾지 못해 권장 시간을 90분으로 적용합니다.");return 90;
 }
}
}
