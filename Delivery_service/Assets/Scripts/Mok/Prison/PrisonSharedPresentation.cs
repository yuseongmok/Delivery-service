using DeliveryService.Mok.Navigation;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
    // 감옥 진행 데이터만 연결하고 표시와 경로 계산은 도시의 공용 구현을 사용한다.
    public sealed class PrisonSharedPresentation : MonoBehaviour {
        public PrisonMission mission;
        public PrisonChickenMission chicken;
        public PrisonMedicineMission medicine;
        public PrisonKeysMission keys;
        public PrisonOfficeSearch office;
        public PrisonEscapeMission escape;
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
        void PresentIntroduction(){
            var intro=mission.introduction;objective.text=intro.Objective;subtitle.text=intro.Subtitle;subtitle.color=intro.Subtitle.StartsWith("민기:")?new Color(1,.74f,.4f):intro.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):new Color(1,.94f,.8f);
            var target=intro.Target;marker.gameObject.SetActive(target&&!mission.Failing);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;feet.y=mission.player.GetComponent<CharacterController>().bounds.min.y+.1f;
            StoryTargetDiamond.Place(marker,mission.player.View,route.Marker(feet,target.position,false)+Vector3.up,feet,"최형욱");
            if(Vector3.Distance(feet,target.position)<3.2f)objective.text+=Vector3.Distance(intro.mingi.transform.position,mission.player.transform.position)>6?"\n민기를 기다리세요":"\n[F] 최형욱과 대화";
        }
        void PresentChicken(){
            objective.text=chicken.Objective;subtitle.text=chicken.Subtitle;subtitle.color=chicken.Subtitle.StartsWith("민기:")?new Color(1,.74f,.4f):chicken.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):new Color(1,.94f,.8f);var target=chicken.Target;marker.gameObject.SetActive(target&&!chicken.Failing&&!chicken.cooking.Open);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;feet.y=mission.player.GetComponent<CharacterController>().bounds.min.y+.1f;
            StoryTargetDiamond.Place(marker,mission.player.View,route.Marker(feet,target.position,false)+Vector3.up,feet,target.name);
            if(!chicken.cooking.Open&&Vector3.Distance(feet,target.position)<3.2f)objective.text+="\n[F] 상호작용";
        }
        void LateUpdate(){
            if(!mission||!mission.player)return;
            bool menu=Time.timeScale==0;
            ui.SetActive(!menu);
            if(menu)return;
            if(escape&&escape.Active){PresentEscape();return;} if(office&&office.Active){PresentOffice();return;} if(keys&&keys.Active){PresentKeys();return;}
            if(medicine&&medicine.Active){PresentMedicine();return;}
            if(chicken&&chicken.Active){PresentChicken();return;}
            if(mission.introduction){PresentIntroduction();return;}
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
        void PresentMedicine(){
            objective.text=medicine.Objective;subtitle.text=medicine.Subtitle;
            subtitle.color=medicine.Subtitle.StartsWith("민기:")?new Color(1,.74f,.4f):medicine.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):new Color(1,.94f,.8f);
            var target=medicine.Target;marker.gameObject.SetActive(target&&!medicine.Failing);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;feet.y=mission.player.GetComponent<CharacterController>().bounds.min.y+.1f;
            StoryTargetDiamond.Place(marker,mission.player.View,route.Marker(feet,target.position,false)+Vector3.up,feet,target.name);
            if(Vector3.Distance(feet,target.position)<3.2f)objective.text+=medicine.State==PrisonMedicineMission.Stage.Search?"\n죄수를 바라보고 [F] 대화":"\n[F] 상호작용";
        }
                void PresentOffice(){
            objective.text=office.Objective;subtitle.text=office.Subtitle;subtitle.color=office.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):new Color(1,.74f,.4f);
            var target=office.Target;marker.gameObject.SetActive(target);if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;StoryTargetDiamond.Place(marker,mission.player.View,target.position+Vector3.up*.3f,feet,target.name);
            if(Vector3.Distance(feet,target.position)<3)objective.text+="\n바라보고 [F] 조사 / 열린 보관함에서 [F] 확보";
        }
                void PresentEscape(){
            objective.text=escape.Objective;subtitle.text=escape.Subtitle;subtitle.color=escape.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):escape.Subtitle.StartsWith("민기:")?new Color(1,.74f,.4f):new Color(1,.94f,.8f);
            var target=escape.Target;marker.gameObject.SetActive(target);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;
            StoryTargetDiamond.Place(marker,mission.player.View,route.Marker(feet,target.position,false)+Vector3.up,feet,target.name);
            if(Vector3.Distance(feet,target.position)<3)objective.text+="\n[F] 상호작용";
        }
        void PresentKeys(){
            objective.text=keys.Objective;subtitle.text=keys.Subtitle;subtitle.color=keys.Subtitle.StartsWith("민기:")?new Color(1,.74f,.4f):keys.Subtitle.StartsWith("나:")?new Color(.55f,.82f,1):new Color(1,.94f,.8f);
            var target=keys.Target;marker.gameObject.SetActive(target&&!keys.Failing);
            if(!target){if(previous)navigation.StopGuidance();previous=null;return;}
            if(previous!=target){previous=target;route.Reset();place.entrance=target;place.mapAnchor=target;navigation.SelectPlace(place);}
            var feet=mission.player.transform.position;feet.y=mission.player.GetComponent<CharacterController>().bounds.min.y+.1f;
            var guide=route.Marker(feet,target.position,false);if(keys.State==PrisonKeysMission.Stage.Serve)guide=PrisonMealCart.ClearancePoint(guide);StoryTargetDiamond.Place(marker,mission.player.View,guide+Vector3.up,feet,target.name);
            if(Vector3.Distance(feet,target.position)<3&&keys.State!=PrisonKeysMission.Stage.Steal)objective.text+="\n[F] 상호작용";
        }
    }
}
