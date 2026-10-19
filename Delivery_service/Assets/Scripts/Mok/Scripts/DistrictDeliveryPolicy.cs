using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class DistrictDeliveryPolicy : MonoBehaviour {
 [Range(0,100)] public float oldCityWeight=15, eastCityWeight=50, westQuarterWeight=35;
 public List<DeliveryPoint> oldCity=new(), eastCity=new(), westQuarter=new();
 public string Region(DeliveryPoint p)=>eastCity.Contains(p)?"신도시":westQuarter.Contains(p)?"빈민가":"기존 도시";
 public float Multiplier(DeliveryPoint p)=>eastCity.Contains(p)?1.3f:westQuarter.Contains(p)?1.6f:1f;
 public DeliveryPoint Choose(List<DeliveryPoint> registered){
  var groups=new[]{oldCity,eastCity,westQuarter};var weights=new[]{oldCityWeight,eastCityWeight,westQuarterWeight};
  var valid=groups.Select(g=>g.Where(p=>p!=null&&p.isActiveAndEnabled&&registered.Contains(p)).ToArray()).ToArray();
  float total=0;for(int i=0;i<3;i++)if(valid[i].Length>0)total+=Mathf.Max(0,weights[i]);
  if(total<=0)return registered.FirstOrDefault(p=>p!=null&&p.isActiveAndEnabled);
  float roll=Random.value*total;for(int i=0;i<3;i++){if(valid[i].Length==0||weights[i]<=0)continue;roll-=weights[i];if(roll<=0)return valid[i][Random.Range(0,valid[i].Length)];}
  return valid.Last(g=>g.Length>0).Last();
 }
}
}
