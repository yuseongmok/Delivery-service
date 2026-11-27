using UnityEngine;
namespace DeliveryService.Mok.Navigation {
// 컷신 동안 Player가 꺼진 채 시작하는 챕터 씬에서도 휴대폰·오토바이를 찾는다.
// 숨긴 휴대폰은 그리지 않고, 열려 있는 화면은 최대 초당 30회 갱신한다.
[DefaultExecutionOrder(10000)]
public sealed class NavigationRenderBudget:MonoBehaviour {
 Camera map,ui;PhoneController phone;NavigationPlanner planner;MotorcycleController[] bikes;bool mapEnabled,uiEnabled;float nextFrame,visibleUntil,nextBikeSearch;bool previousVisible,previousMap;NavigationPlanner.Screen previousScreen;
 void Start(){var style=GetComponent<NavigationMapStyle>();map=style!=null?style.trackingCamera:null;foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c.name=="NavigationUICamera")ui=c;phone=FindFirstObjectByType<PhoneController>(FindObjectsInactive.Include);planner=FindFirstObjectByType<NavigationPlanner>(FindObjectsInactive.Include);bikes=FindObjectsByType<MotorcycleController>(FindObjectsInactive.Include,FindObjectsSortMode.None);mapEnabled=map!=null&&map.enabled;uiEnabled=ui!=null&&ui.enabled;}
 void LateUpdate(){if(map==null||ui==null||planner==null)return;
 // 터널로 넘어온 씬은 오토바이를 나중에 생성하므로 주기적으로 다시 찾는다.
 if(Time.unscaledTime>=nextBikeSearch){nextBikeSearch=Time.unscaledTime+1f;bikes=FindObjectsByType<MotorcycleController>(FindObjectsInactive.Include,FindObjectsSortMode.None);if(phone==null)phone=FindFirstObjectByType<PhoneController>(FindObjectsInactive.Include);}
 bool riding=false;foreach(var bike in bikes)if(bike!=null&&bike.isDriven){riding=true;break;}bool visible=riding||(phone!=null&&phone.IsPhoneOpen());if(visible)visibleUntil=Time.unscaledTime+.35f;visible|=Time.unscaledTime<visibleUntil;
 bool mapScreen=planner.CurrentScreen==NavigationPlanner.Screen.Map||planner.CurrentScreen==NavigationPlanner.Screen.Destinations;
 bool changed=visible!=previousVisible||mapScreen!=previousMap||previousScreen!=planner.CurrentScreen;bool draw=visible&&(changed||Time.unscaledTime>=nextFrame);if(draw)nextFrame=Time.unscaledTime+1f/30f;
 ui.enabled=uiEnabled&&draw;map.enabled=mapEnabled&&draw&&mapScreen;previousVisible=visible;previousMap=mapScreen;previousScreen=planner.CurrentScreen;}
 void OnDisable(){if(map!=null)map.enabled=mapEnabled;if(ui!=null)ui.enabled=uiEnabled;}
}
}
