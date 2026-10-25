using UnityEngine;
namespace DeliveryService.Mok {
// 저녁에만 약한 환경광을 더해 도로와 인물의 윤곽을 살린다.
[DefaultExecutionOrder(200)]
public sealed class CityEveningVisibility:MonoBehaviour {
 public DayNightCycle cycle;public Color eveningFill=new Color(.09f,.115f,.15f);Color original;bool captured;
 void OnEnable(){original=RenderSettings.ambientLight;captured=true;}
 void LateUpdate(){Refresh();}
 public void Refresh(){if(!cycle||!captured)return;float hour=cycle.currentTime;float weight=hour>=17?Mathf.InverseLerp(17,18,hour):0;RenderSettings.ambientLight=Color.Lerp(original,new Color(Mathf.Max(original.r,eveningFill.r),Mathf.Max(original.g,eveningFill.g),Mathf.Max(original.b,eveningFill.b)),weight);}
 void OnDisable(){if(captured)RenderSettings.ambientLight=original;captured=false;}
}}
