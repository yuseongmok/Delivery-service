using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 기존 감옥 플레이어와 공용 UI는 그대로 두고 새 도입 순서만 관리한다.
    public sealed class PrisonIntroduction : MonoBehaviour
    {
        public enum Stage { Mugshot, Cell, Yard, Conversation, Complete }
        public PrisonMission mission;
        public PrisonIntroductionText dialogue;
        public PrisonCompanion mingi;
        public Transform informant, cellSpawn, cellMingi, mugshotActor, mugshotCamera, cellCamera, portraitCamera;
        public Camera filmCamera;
        public PrisonMeetingPresentation meetingPresentation;
        public Stage State { get; private set; }
        public bool Cinematic { get; private set; }
        public string Subtitle { get; private set; } = "";
        public string Card { get; private set; } = "";
        public float Fade { get; private set; } = 1;
        public float Flash { get; private set; }
        public Transform Target => State == Stage.Yard ? informant : null;
        public string Objective => State == Stage.Yard ? dialogue.walkObjective : State == Stage.Conversation ? dialogue.talkObjective : State == Stage.Complete ? dialogue.completeObjective : "";
        PlayerMove move;
        CameraMove look;
        PlayerInteraction interaction;
        PhoneController phone;
        bool moveControl, lookControl, interactionEnabled, phoneEnabled;
        float originalFov;
        bool Skip => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        IEnumerator Start()
        {
            move = mission.player.GetComponent<PlayerMove>();
            look = mission.player.GetComponentInChildren<CameraMove>();
            interaction = mission.player.GetComponent<PlayerInteraction>();
            phone = mission.player.GetComponentInChildren<PhoneController>(true);
            yield return null;
            SetCinematic(true);
            filmCamera.transform.SetPositionAndRotation(mugshotCamera.position, mugshotCamera.rotation);
            mugshotActor.gameObject.SetActive(true);
            Card = dialogue.arrival;
            yield return FadeTo(0);
            Subtitle = dialogue.photograph;
            yield return Wait(1.8f);
            yield return Photograph();
            Quaternion start = mugshotActor.rotation;
            for (float t=0;t<.8f;t+=Time.deltaTime) { mugshotActor.rotation=Quaternion.Slerp(start,start*Quaternion.Euler(0,90,0),t/.8f); yield return null; }
            yield return Wait(.7f);
            yield return Photograph();
            yield return FadeTo(1);
            mugshotActor.gameObject.SetActive(false);
            State = Stage.Cell; Subtitle = ""; Card = dialogue.elapsed;
            PlacePlayer();
            mingi.agent.Warp(cellMingi.position); mingi.transform.rotation=cellMingi.rotation;
            filmCamera.transform.SetPositionAndRotation(cellCamera.position,cellCamera.rotation);
            yield return Wait(1.3f); yield return FadeTo(0);
            foreach (string line in dialogue.cell) { Subtitle=line; yield return Wait(dialogue.lineSeconds); }
            yield return FadeTo(1);
            Subtitle=""; Card=dialogue.morning;
            yield return Wait(1.5f);
            SetCinematic(false); State=Stage.Yard; mingi.Following=true;
            yield return FadeTo(0); Card="";
        }
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
            State=Stage.Conversation;mingi.Following=false;SetCinematic(true);
            if(meetingPresentation)meetingPresentation.Begin(filmCamera);
            else { filmCamera.transform.position=informant.position+informant.forward*2+Vector3.up*1.55f;filmCamera.transform.LookAt(informant.position+Vector3.up*1.4f); }
            yield return Wait(1.5f);
            foreach(string line in dialogue.meeting){Subtitle=line;if(meetingPresentation)meetingPresentation.ShowLine(line);yield return Wait(dialogue.lineSeconds);}
            if(meetingPresentation)meetingPresentation.End();
            Subtitle="";State=Stage.Complete;SetCinematic(false);mingi.Following=true;
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
            if(!value){mission.player.transform.rotation=Quaternion.Euler(0f,mission.player.transform.eulerAngles.y,0f);mission.player.View.fieldOfView=originalFov;if(look)look.SyncViewFromTransform();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
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

