using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(13000)]
    public sealed class MissionMilestoneHUD : MonoBehaviour
    {
        private sealed class Watch
        {
            public string title,result;
            public Func<bool> running,complete;
            public MissionMilestoneTracker tracker;
        }
        private sealed class Notice { public Watch watch; public bool complete; }
        private readonly List<Watch> watches=new List<Watch>();
        private readonly Queue<Notice> pending=new Queue<Notice>();
        private Notice current;
        private CanvasGroup group;
        private RectTransform card;
        private Text status,title,result;
        private Image accent;
        private float elapsed;
        private PhoneController phone;
        private DayNightCycle day;
        private PrisonSharedPresentation prison;
        public bool IsVisible=>group&&group.alpha>0;
        public int PendingCount=>pending.Count;
        public string DisplayedTitle=>title?title.text:"";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Install();}
        private static void Loaded(Scene scene,LoadSceneMode mode)=>Install();
        private static void Install()
        {
            if(GameModeSession.IsChallenge||FindFirstObjectByType<MissionMilestoneHUD>())return;
            if(!FindFirstObjectByType<BrokerMission>()&&!FindFirstObjectByType<PrisonMission>())return;
            new GameObject("Story mission milestones").AddComponent<MissionMilestoneHUD>();
        }
        private IEnumerator Start()
        {
            yield return null;yield return null;
            phone=FindFirstObjectByType<PhoneController>();day=FindFirstObjectByType<DayNightCycle>();
            prison=FindFirstObjectByType<PrisonSharedPresentation>();
            Build();
            var tutorial=FindFirstObjectByType<StoryTutorial>();
            if(tutorial)Add("위장 생활","첫 배달 완료",()=>StoryTutorial.Active,()=>tutorial&&tutorial.Completed);
            var broker=BrokerMission.Current;if(broker)Add("연락이 끊긴 중개인","실장 구출 · 행사장에 접근할 단서 확보",()=>broker&&broker.Running,()=>broker&&broker.Completed);
            var special=SpecialOrderMission.Current;if(special)Add("특별 주문","행사장 납품 승인 확보",()=>special&&special.Running,()=>special&&special.Completed);
            var backdoor=BackdoorMission.Current;if(backdoor)Add("도시의 뒷문","잠입 출입구와 탈출 경로 확인",()=>backdoor&&backdoor.Running,()=>backdoor&&backdoor.Completed);
            var mechanic=MechanicMission.Current;if(mechanic)Add("빚진 정비사","탈출 차량 지원 확보",()=>mechanic&&mechanic.Running,()=>mechanic&&mechanic.Completed);
            var finale=VenueFinale.Current;if(finale)Add("마지막 배달","박진웅의 계획 확인 · 행사장 탈출",()=>finale&&finale.Running,()=>finale&&(finale.Completed||finale.State==VenueFinale.Phase.Aftermath));
            if(prison)
            {
                if(!prison.chicken)prison.chicken=FindFirstObjectByType<PrisonChickenMission>();
                if(!prison.medicine)prison.medicine=FindFirstObjectByType<PrisonMedicineMission>();
                if(!prison.keys)prison.keys=FindFirstObjectByType<PrisonKeysMission>();
                if(!prison.office)prison.office=FindFirstObjectByType<PrisonOfficeSearch>();
                if(!prison.escape)prison.escape=FindFirstObjectByType<PrisonEscapeMission>();
                var intro=prison.mission.introduction;
                if(intro)Add("정보거래","최형욱과 정보 거래 성립",()=>intro&&intro.State==PrisonIntroduction.Stage.Yard,()=>intro&&intro.State==PrisonIntroduction.Stage.Complete);
                var chicken=prison.chicken;if(chicken)Add("갓 튀긴 치킨","간수장실에 관한 단서 확보",()=>chicken&&chicken.Active,()=>chicken&&chicken.State==PrisonChickenMission.Stage.Complete);
                var medicine=prison.medicine;if(medicine)Add("강탈","수면제 확보",()=>medicine&&medicine.Active,()=>medicine&&medicine.State==PrisonMedicineMission.Stage.Complete);
                var keys=prison.keys;if(keys)Add("소매치기","간수장실 진입 성공",()=>keys&&keys.Active,()=>keys&&keys.State==PrisonKeysMission.Stage.Complete);
                var office=prison.office;if(office)Add("간수장실 조사","간수복 · 차량 열쇠 · 암구호 확보",()=>office&&office.Active,()=>office&&office.Complete);
                var escape=prison.escape;if(escape)Add("탈옥","민기와 함께 탈출 성공",()=>escape&&escape.Active,()=>escape&&escape.State==PrisonEscapeMission.Stage.Complete);
            }
        }
        private void Add(string missionTitle,string outcome,Func<bool> running,Func<bool> complete)
        {
            watches.Add(new Watch{title=missionTitle,result=outcome,running=running,complete=complete,tracker=new MissionMilestoneTracker(complete())});
        }
        private bool Blocked()
        {
            if(StoryCutsceneHUD.Active || PrisonFeaturePolicy.Cooking)return true;
            if(Time.timeScale<=0||(phone&&phone.IsPhoneOpen())||(day&&day.pcUIPanel&&day.pcUIPanel.activeInHierarchy))return true;
            var f=VenueFinale.Current;if(f&&(f.Cinematic||f.State==VenueFinale.Phase.Scheduling||f.State==VenueFinale.Phase.Conversation))return true;
            var b=BrokerMission.Current;if(b&&(b.State==BrokerMission.Phase.Conversation||b.State==BrokerMission.Phase.Debrief))return true;
            if(SpecialOrderMission.Current&&SpecialOrderMission.Current.InDialogue)return true;
            if(BackdoorMission.Current&&BackdoorMission.Current.InDialogue)return true;
            if(MechanicMission.Current&&MechanicMission.Current.InDialogue)return true;
            if(StoryTutorial.Active&&StoryTutorial.Current.CurrentStep>=StoryTutorial.Step.Goodbye)return true;
            if(prison)
            {
                if(prison.mission.player && !prison.mission.player.View.enabled)return true;
                var i=prison.mission.introduction;if(i&&(i.State==PrisonIntroduction.Stage.Mugshot||i.State==PrisonIntroduction.Stage.Cell||i.State==PrisonIntroduction.Stage.Conversation))return true;
                var e=prison.escape;if(e&&e.Active&&e.State!=PrisonEscapeMission.Stage.Change&&e.State!=PrisonEscapeMission.Stage.Vehicle&&e.State!=PrisonEscapeMission.Stage.Complete)return true;
            }
            return false;
        }
        private void LateUpdate()
        {
            if(!group)return;
            if(GameModeSession.IsChallenge){group.alpha=0;pending.Clear();current=null;return;}
            foreach(var watch in watches)
            {
                var change=watch.tracker.Observe(watch.running(),watch.complete());
                if(change!=MissionMilestoneTracker.Change.None)pending.Enqueue(new Notice{watch=watch,complete=change==MissionMilestoneTracker.Change.Completed});
            }
            if(current!=null&&!current.complete&&(!current.watch.running()||current.watch.complete()))current=null;
            if(Blocked()){group.alpha=0;return;}
            while(current==null&&pending.Count>0)
            {
                var next=pending.Dequeue();
                if(!next.complete&&(!next.watch.running()||next.watch.complete()))continue;
                current=next;elapsed=0;
                status.text=next.complete?"미션 완료":"미션 시작";
                title.text=next.watch.title;result.text=next.complete?next.watch.result:"";
                accent.color=status.color=next.complete?new Color(.48f,.83f,.65f):new Color(1,.57f,.23f);
            }
            if(current==null){group.alpha=0;return;}
            elapsed+=Time.deltaTime;
            float duration=prison?(current.complete?1.8f:2.2f):(current.complete?4.2f:3.0f);
            float fade=Mathf.Min(Mathf.Clamp01(elapsed/(prison?.18f:.4f)),Mathf.Clamp01((duration-elapsed)/.5f));
            group.alpha=Mathf.SmoothStep(0,1,fade);
            card.anchoredPosition=new Vector2(0,-10*(1-group.alpha));
            if(elapsed>=duration){current=null;group.alpha=0;}
        }
        private void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=3;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
            card=Rect(transform,"Mission notice",620,146);card.anchorMin=card.anchorMax=new Vector2(.5f,.72f);card.pivot=new Vector2(.5f,.5f);

            var bg=card.gameObject.AddComponent<HudShape>();bg.Kind=-1;bg.color=new Color(.03f,.06f,.09f,.91f);
            var line=Rect(card,"Accent",70,3);line.anchoredPosition=new Vector2(0,-13);accent=line.gameObject.AddComponent<Image>();accent.raycastTarget=false;
            status=Label(card,"Status",-24,20);title=Label(card,"Title",-53,32);result=Label(card,"Result",-98,22);
        }
        private static RectTransform Rect(Transform parent,string name,float width,float height)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=(RectTransform)obj.transform;
            r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(width,height);return r;
        }
        private static Text Label(Transform parent,string name,float y,int size)
        {
            var rect=Rect(parent,name,580,42);rect.anchoredPosition=new Vector2(0,y);
            var text=rect.gameObject.AddComponent<Text>();text.font=MokUIFont.Font;text.fontSize=size;text.color=new Color(1,.96f,.86f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
    }
}



