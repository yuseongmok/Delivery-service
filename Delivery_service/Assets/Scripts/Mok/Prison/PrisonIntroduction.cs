using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 기존 감옥 플레이어와 공용 UI는 그대로 두고 새 도입 순서만 관리한다.
    public sealed class PrisonIntroduction : MonoBehaviour
    {
        public enum Stage { Mugshot, Cell, Yard, Conversation, Complete, Work }
        public PrisonMission mission;
        public PrisonIntroductionText dialogue;
        public PrisonCompanion mingi;
        public Transform informant, cellSpawn, cellMingi, mugshotActor, mugshotCamera, cellCamera, portraitCamera;
        public Camera filmCamera;
        public PrisonMeetingPresentation meetingPresentation;
        public PrisonFirstWork firstWork;
        public PrisonInformantIntroduction informantEntrance;
        Coroutine walkConversation;
        public void PresentLine(string line){Subtitle=line;}
        public Stage State { get; private set; }
        public bool Cinematic { get; private set; }
        public string Subtitle { get; private set; } = "";
        public string Card { get; private set; } = "";
        public float Fade { get; private set; } = 1;
        public float Flash { get; private set; }
        public Transform Target => Cinematic ? null : State == Stage.Work && firstWork ? firstWork.Target : State == Stage.Yard ? informant : null;
        public string Objective => State == Stage.Work && firstWork ? firstWork.Objective : State == Stage.Yard ? dialogue.walkObjective : State == Stage.Conversation ? dialogue.talkObjective : State == Stage.Complete ? dialogue.completeObjective : "";
        PlayerMove move;
        CameraMove look;
        PlayerInteraction interaction;
        PhoneController phone;
        bool moveControl, lookControl, interactionEnabled, phoneEnabled;
        float originalFov;
        Quaternion mugshotFront;
        PrisonEvening lighting;
        PrisonBedtimePresentation bedtimeView;
        bool Skip => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        IEnumerator Start()
        {
            move = mission.player.GetComponent<PlayerMove>();
            look = mission.player.GetComponentInChildren<CameraMove>();
            interaction = mission.player.GetComponent<PlayerInteraction>();
            phone = mission.player.GetComponentInChildren<PhoneController>(true);
            yield return null;
            lighting=GetComponent<PrisonEvening>();if(!lighting)lighting=gameObject.AddComponent<PrisonEvening>();
            mugshotFront = mugshotActor.rotation;
            SetCinematic(true);
            filmCamera.transform.SetPositionAndRotation(mugshotCamera.position, mugshotCamera.rotation);
            mugshotActor.gameObject.SetActive(true);
            Card = dialogue.arrival;
            yield return FadeTo(0);
            Subtitle = dialogue.photograph;
            yield return Wait(1.8f);
            yield return Photograph();
            yield return ProfilePhoto(mugshotActor);
            yield return FadeTo(1);
            mugshotActor.gameObject.SetActive(false);
            Subtitle = "";
            mingi.Following = false;
            mingi.agent.ResetPath();
            mingi.agent.enabled = false;
            mingi.transform.SetPositionAndRotation(mugshotActor.position, mugshotFront);
            mingi.animator.SetFloat("MoveSpeed", 0);
            Card = dialogue.secondPhotograph;
            yield return FadeTo(0);
            Subtitle = dialogue.photograph;
            yield return Wait(1.8f);
            yield return Photograph();
            yield return ProfilePhoto(mingi.transform);
            yield return FadeTo(1);
            State = Stage.Cell; Subtitle = ""; Card = dialogue.firstNight;
            PlacePlayer();
            RestoreMingiToCell();
            filmCamera.transform.SetPositionAndRotation(cellCamera.position,cellCamera.rotation);
            yield return Wait(1.3f); yield return FadeTo(0);
            Card = "";
            foreach (string line in dialogue.arrivalCell) { Subtitle=line; yield return Wait(dialogue.lineSeconds); }
            if(firstWork){
                State=Stage.Work;SetCinematic(false);mingi.Following=true;
                firstWork.Begin(this);Subtitle=dialogue.workOrder;
                while(!firstWork.Sleeping)yield return null;
                SetCinematic(true);Subtitle="";
                yield return JoinInCell();
                filmCamera.transform.SetPositionAndRotation(cellCamera.position,cellCamera.rotation);
                foreach(var line in dialogue.returnCell){Subtitle=line;yield return Wait(dialogue.lineSeconds);}
                Subtitle=dialogue.bedtime;yield return Wait(dialogue.lineSeconds);
                Subtitle="";
                bedtimeView=GetComponent<PrisonBedtimePresentation>();if(!bedtimeView)bedtimeView=gameObject.AddComponent<PrisonBedtimePresentation>();
                yield return bedtimeView.LieDown(this);
                yield return Wait(.6f);
                float darkFrom=Fade;
                for(float t=0;t<dialogue.sleepFadeSeconds;t+=Time.deltaTime){Fade=Mathf.SmoothStep(darkFrom,1,t/dialogue.sleepFadeSeconds);yield return null;}
                Fade=1;yield return Wait(1);
                bedtimeView.Restore();
            }
            yield return FadeTo(1);
            Subtitle = ""; Card = dialogue.daysLater;
            if(lighting)lighting.RestoreDay();
            State=Stage.Cell;PlacePlayer();RestoreMingiToCell();
            filmCamera.transform.SetPositionAndRotation(cellCamera.position,cellCamera.rotation);
            yield return Wait(2.5f);
            yield return FadeTo(0); Card = "";
            foreach (string line in dialogue.cell) { Subtitle=line; yield return Wait(dialogue.lineSeconds); }
            yield return FadeTo(1);
            Subtitle=""; Card=dialogue.morning;
            yield return Wait(1.5f);
            SetCinematic(false); State=Stage.Yard; mingi.Following=true;
            yield return FadeTo(0); Card="";
            walkConversation=StartCoroutine(YardConversation());
        }
        IEnumerator YardConversation(){
            yield return Wait(1.5f);
            foreach(var line in dialogue.yardWalk){if(State!=Stage.Yard)yield break;Subtitle=line;yield return Wait(dialogue.lineSeconds);}
            if(State==Stage.Yard)Subtitle="";
            walkConversation=null;
        }
        public void WorkView(bool active,Vector3 table){SetCinematic(active);if(active){filmCamera.transform.position=table+new Vector3(.38f,1.15f,-1.15f);filmCamera.transform.LookAt(table);Subtitle="";}}
        public void WorkEvening(){StartCoroutine(Evening());}
        IEnumerator Evening(){yield return FadeTo(1);lighting.SetEvening();Card=dialogue.evening;Subtitle=dialogue.workEnd;yield return Wait(2);yield return FadeTo(0);Card="";firstWork.ReadyToSleep=true;}
        void Update()
        {
            if (State != Stage.Yard || Time.timeScale<=0 || mission.Failing || !mission.player.CanAct) return;
            if (Vector3.Distance(mission.player.transform.position,informant.position)<3.2f && Keyboard.current!=null && Keyboard.current.fKey.wasPressedThisFrame)
                BeginMeeting();
        }
        public bool BeginMeeting()
        {
            if(State!=Stage.Yard || mission.Failing || Time.timeScale<=0 || Vector3.Distance(mission.player.transform.position,informant.position)>3.2f || Vector3.Distance(mingi.transform.position,mission.player.transform.position)>6) return false;
            StartCoroutine(Meeting());return true;
        }
        IEnumerator Meeting()
        {
            if(walkConversation!=null){StopCoroutine(walkConversation);walkConversation=null;}
            State=Stage.Conversation;mingi.Following=false;SetCinematic(true);
            if(informantEntrance)yield return informantEntrance.Play(filmCamera);
            if(meetingPresentation)meetingPresentation.Begin(filmCamera);
            else { filmCamera.transform.position=informant.position+informant.forward*2+Vector3.up*1.55f;filmCamera.transform.LookAt(informant.position+Vector3.up*1.4f); }
            yield return Wait(1.5f);
            foreach(string line in dialogue.meeting){Subtitle=line;if(meetingPresentation)meetingPresentation.ShowLine(line);yield return Wait(dialogue.lineSeconds);}
            foreach(var line in dialogue.kitchenPlan){Subtitle=line;if(meetingPresentation)meetingPresentation.ShowLine(line);yield return Wait(dialogue.lineSeconds);}
            if(meetingPresentation)meetingPresentation.End();
            Subtitle="";State=Stage.Complete;SetCinematic(false);mingi.Following=true;
        }
        IEnumerator ProfilePhoto(Transform actor)
        {
            Quaternion start = actor.rotation;
            for(float t=0;t<.8f;t+=Time.deltaTime){actor.rotation=Quaternion.Slerp(start,start*Quaternion.Euler(0,90,0),t/.8f);yield return null;}
            actor.rotation=start*Quaternion.Euler(0,90,0);
            yield return Wait(.7f);
            yield return Photograph();
        }
        // 감방 대사 중 NavMesh 보정이나 따라가기 때문에 복도로 밀려나지 않게 고정한다.
        void RestoreMingiToCell()
        {
            mingi.Following=false;mingi.ScriptedTravel=false;
            if(mingi.agent.enabled&&mingi.agent.isOnNavMesh)mingi.agent.ResetPath();
            mingi.agent.enabled=false;
            mingi.transform.SetPositionAndRotation(cellMingi.position,cellMingi.rotation);
            mingi.animator.SetFloat("MoveSpeed",0);
        }
        IEnumerator JoinInCell()
        {
            filmCamera.transform.SetPositionAndRotation(mission.player.View.transform.position,mission.player.View.transform.rotation);
            mingi.Following=false;mingi.ScriptedTravel=true;
            if(!mingi.agent.enabled)mingi.agent.enabled=true;
            float oldStop=mingi.agent.stoppingDistance;mingi.agent.stoppingDistance=.08f;mingi.agent.isStopped=false;mingi.agent.SetDestination(cellMingi.position);
            for(float t=0;t<20&&Vector3.Distance(mingi.transform.position,cellMingi.position)>.23f;t+=Time.deltaTime){
                Vector3 lookAt=mingi.transform.position+Vector3.up*1.35f-filmCamera.transform.position;
                if(lookAt.sqrMagnitude>.01f)filmCamera.transform.rotation=Quaternion.Slerp(filmCamera.transform.rotation,Quaternion.LookRotation(lookAt),Time.deltaTime*2.5f);
                yield return null;
            }
            // 일반 경로에서는 입장과 대화 사이에 암전하지 않는다. 막혔을 때만 숨겨서 복구한다.
            bool blocked=Vector3.Distance(mingi.transform.position,cellMingi.position)>.3f;
            if(blocked)yield return FadeTo(1);
            mingi.agent.stoppingDistance=oldStop;RestoreMingiToCell();
            yield return PrisonBedtimePresentation.Move(filmCamera,cellCamera.position,cellCamera.rotation,1.15f);
            if(blocked)yield return FadeTo(0);
        }
        IEnumerator Photograph()
        {
            PrisonAudio.Clink(mugshotActor.position,.35f);
            Flash=1;yield return Wait(.08f);Flash=0;yield return Wait(.4f);
        }
        IEnumerator Wait(float seconds)
        {
            for(float t=0;t<seconds;t+=Time.deltaTime){if(Time.timeScale>0&&Skip)break;yield return null;}
        }
        IEnumerator FadeTo(float value)
        {
            float from=Fade;for(float t=0;t<.65f;t+=Time.deltaTime){Fade=Mathf.Lerp(from,value,t/.65f);yield return null;}Fade=value;
        }
        public void DeveloperComplete()
        {
            if(informantEntrance)informantEntrance.Finish();
            if(bedtimeView)bedtimeView.Restore();
            if(firstWork)firstWork.Cancel();
            StopAllCoroutines();if(meetingPresentation)meetingPresentation.End();
            if(lighting)lighting.RestoreDay();
            PlacePlayer();RestoreMingiToCell();mugshotActor.gameObject.SetActive(false);
            Fade=Flash=0;Card=Subtitle="";State=Stage.Complete;SetCinematic(false);mingi.Following=true;
        }
        void PlacePlayer()
        {
            var body=mission.player.GetComponent<CharacterController>();body.enabled=false;
            mission.player.transform.SetPositionAndRotation(cellSpawn.position,Quaternion.Euler(0f,cellSpawn.eulerAngles.y,0f));body.enabled=true;
        }
        void SetCinematic(bool value)
        {
            if(Cinematic==value)return;
            Cinematic=value;
            if(value){moveControl=move.isControllable;lookControl=look&&look.isControllable;interactionEnabled=interaction.enabled;phoneEnabled=phone&&phone.enabled;originalFov=mission.player.View.fieldOfView;}
            move.isControllable=value?false:moveControl;
            if(look)look.isControllable=value?false:lookControl;
            interaction.enabled=value?false:interactionEnabled;
            if(phone)phone.enabled=value?false:phoneEnabled;
            mission.player.View.enabled=!value;filmCamera.enabled=value;
            filmCamera.fieldOfView=55;
            var ears=mission.player.View.GetComponent<AudioListener>();if(ears)ears.enabled=!value;
            filmCamera.GetComponent<AudioListener>().enabled=value;
            if(!value){if(!mingi.agent.enabled){mingi.agent.enabled=true;mingi.agent.Warp(mingi.transform.position);mingi.agent.isStopped=false;}mission.player.transform.rotation=Quaternion.Euler(0f,mission.player.transform.eulerAngles.y,0f);mission.player.View.fieldOfView=originalFov;if(look)look.SyncViewFromTransform();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
        void OnGUI()
        {
            if(Time.timeScale<=0)return;
            var old=GUI.color;
            if(Cinematic){GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height*.07f),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height*.93f,Screen.width,Screen.height*.07f),Texture2D.whiteTexture);}
            if(Fade>0){GUI.color=new Color(0,0,0,Fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}
            if(Flash>0){GUI.color=new Color(1,1,1,Flash);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}
            GUI.color=Color.white;
            if(!string.IsNullOrEmpty(Card))GUI.Label(new Rect(0,Screen.height*.12f,Screen.width,70),Card,new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.032f),alignment=TextAnchor.MiddleCenter});
            GUI.color=old;
        }
    }
}





