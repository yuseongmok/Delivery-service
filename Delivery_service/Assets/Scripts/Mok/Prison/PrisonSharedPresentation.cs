using DeliveryService.Mok.Navigation;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
    // 감옥 진행 데이터만 연결하고 표시와 경로 계산은 도시의 공용 구현을 사용한다.
    public sealed class PrisonSharedPresentation : MonoBehaviour {
        public PrisonMission mission;
        public GameObject ui;
        public Text objective,subtitle,marker;
        public NavigationManager navigation;
        public NavigationPlace place;
        readonly EscapeRouteGuide route=new EscapeRouteGuide();
        Transform previous;
        void OnGUI(){
            if(!mission||Time.timeScale==0)return;
            float suspicion=0;foreach(var guard in mission.guards)if(guard)suspicion=Mathf.Max(suspicion,guard.Suspicion);
            var tint=GUI.color;float width=200*Screen.height/900f;
            if(suspicion>0){GUI.color=new Color(.1f,.15f,.18f,.9f);GUI.DrawTexture(new Rect((Screen.width-width)/2,Screen.height*.13f,width,6),Texture2D.whiteTexture);GUI.color=new Color(1,.5f,.15f);GUI.DrawTexture(new Rect((Screen.width-width)/2,Screen.height*.13f,width*suspicion,6),Texture2D.whiteTexture);}
            if(mission.Progress>0){GUI.color=Color.white;GUI.DrawTexture(new Rect((Screen.width-width)/2,Screen.height*.78f,width*mission.Progress,5),Texture2D.whiteTexture);}
            GUI.color=tint;if(mission.Fade<=0)return;
            var old=GUI.color;GUI.color=new Color(0,0,0,mission.Fade);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
        }
        void LateUpdate(){
            if(!mission||!mission.player)return;
            bool menu=Time.timeScale==0;
            ui.SetActive(!menu);
            if(menu)return;
            objective.text=mission.dialogue.objectives[(int)mission.State]+(mission.State==PrisonMission.Phase.Deliver?$" ({mission.Delivered}/3)":"");
            subtitle.text=mission.Subtitle;
            var target=mission.Target;
            marker.gameObject.SetActive(target&&!mission.Failing);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var body=mission.player.GetComponent<CharacterController>();
            var feet=mission.player.transform.position;feet.y=body.bounds.min.y+.1f;
            var pos=route.Marker(feet,target.position,false);
            StoryTargetDiamond.Place(marker,mission.player.View,pos+Vector3.up,mission.player.transform.position,mission.dialogue.objectives[(int)mission.State]);
            if(Physics.Raycast(mission.player.View.transform.position,mission.player.View.transform.forward,out var hit,3,~0,QueryTriggerInteraction.Ignore)){
                string hint="";var point=hit.collider.GetComponentInParent<PrisonMissionPoint>();
                if(point&&point.transform==target)hint=point.prompt;
                else if(hit.collider.GetComponentInParent<PrisonDistraction>())hint="배관을 두드려 시선 돌리기";
                else if(hit.collider.GetComponentInParent<PrisonMealCart>())hint=mission.State==PrisonMission.Phase.Load?"식사 싣기":"카트 밀기 / 놓기";
                if(hint.Length>0)objective.text+="\n[F] "+hint;
            }
        }
    }
}


