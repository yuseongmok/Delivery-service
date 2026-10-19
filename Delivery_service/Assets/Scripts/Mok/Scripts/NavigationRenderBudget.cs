using UnityEngine;
namespace DeliveryService.Mok.Navigation {
// 숨긴 휴대폰은 그리지 않고, 열려 있는 화면은 최대 초당 30회 갱신한다.
[DefaultExecutionOrder(10000)]
public sealed class NavigationRenderBudget:MonoBehaviour {
 Camera map,ui;PhoneController phone;NavigationPlanner planner;MotorcycleController[] bikes;bool mapEnabled,uiEnabled;float nextFrame,visibleUntil;bool previousVisible,previousMap;NavigationPlanner.Screen previousScreen;
 void Start(){var style=GetComponent<NavigationMapStyle>();map=style!=null?style.trackingCamera:null;foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c.name=="NavigationUICamera")ui=c;phone=FindFirstObjectByType<PhoneController>();planner=FindFirstObjectByType<NavigationPlanner>();bikes=FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);mapEnabled=map!=null&&map.enabled;uiEnabled=ui!=null&&ui.enabled;}
 void LateUpdate(){if(map==null||ui==null||planner==null)return;bool riding=false;foreach(var bike in bikes)if(bike!=null&&bike.isDriven){riding=true;break;}bool visible=riding||(phone!=null&&phone.IsPhoneOpen());if(visible)visibleUntil=Time.unscaledTime+.35f;visible|=Time.unscaledTime<visibleUntil;
 bool mapScreen=planner.CurrentScreen==NavigationPlanner.Screen.Map||planner.CurrentScreen==NavigationPlanner.Screen.Destinations;
 bool changed=visible!=previousVisible||mapScreen!=previousMap||previousScreen!=planner.CurrentScreen;bool draw=visible&&(changed||Time.unscaledTime>=nextFrame);if(draw)nextFrame=Time.unscaledTime+1f/30f;
 ui.enabled=uiEnabled&&draw;map.enabled=mapEnabled&&draw&&mapScreen;previousVisible=visible;previousMap=mapScreen;previousScreen=planner.CurrentScreen;}
 void OnDisable(){if(map!=null)map.enabled=mapEnabled;if(ui!=null)ui.enabled=uiEnabled;}
}
}
