using System.Collections;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonCctvMission:MonoBehaviour
    {
        public enum Stage{Waiting,Change,Infiltrate,Disable,Leave,Complete,Retry}
        public PrisonOfficeSearch office;
        public PrisonEscapeMission legacyEscape;
        public PrisonCctvText dialogue;
        public PrisonWardenRestraint restraint;
        public Transform entry,door,console,safePoint,checkpoint,mingiWait;
        public PrisonGuard watch;
        public PrisonCctvMonitor[] monitors;
        public PrisonCctvWirePanel wirePanel;
        public Bounds restrictedArea;
        [Min(.5f)] public float channelSeconds=2,noticeSeconds=1.3f;
        public Stage State{get;private set;}
        public bool Active=>State!=Stage.Waiting;
        public bool Busy{get;private set;}
        public bool CamerasDisabled{get;private set;}
        public int Channels{get;private set;}
        public int Attempts{get;private set;}
        public float Progress{get;private set;}
        public float Suspicion{get;private set;}
        public string Subtitle{get;private set;}="";
        public Transform Target=>Busy?null:State==Stage.Change?legacyEscape.changePoint:State==Stage.Infiltrate?entry:State==Stage.Disable?console:State==Stage.Leave?safePoint:null;
        public string Objective=>State==Stage.Change?"탈옥 준비 · 간수복으로 갈아입기":State==Stage.Infiltrate?"CCTV실 들어가기":State==Stage.Disable?(wirePanel?"CCTV 무력화 · 배전판을 열고 영상 전원 끊기":$"CCTV 무력화 · 제어반에서 [F] 길게 누르기 ({Channels}/3)"):State==Stage.Leave?"CCTV 무력화 · 민기와 보안실 밖으로 나오기":State==Stage.Complete?"탈옥 준비 완료 · 호송 출발 "+dialogue.departureTime+" / 저녁까지 대기":"CCTV실 · 들키지 않게 이동하기";
        PlayerCombat Player=>office&&office.previous&&office.previous.previous&&office.previous.previous.prison?office.previous.previous.prison.player:null;
        PrisonCompanion Friend=>office.previous.previous.prison.introduction.mingi;
        bool greeted;
        Quaternion closed;float subtitleUntil,fade;bool locked,moveControl,lookControl,interactionEnabled,combatEnabled;
        void Awake(){closed=door.localRotation;watch.gameObject.SetActive(false);}
        void Update()
        {
            if(Time.timeScale<=0)return;
            if(State==Stage.Waiting){if(office.Complete&&!office.GetComponent<PrisonOfficePickup>().Busy){State=Stage.Change;Say(dialogue.secured);}return;}
            if(Time.time>subtitleUntil)Subtitle="";
            if(Busy)return;
            if(State==Stage.Infiltrate||State==Stage.Disable||State==Stage.Leave){
                if(Player.IsDown){Progress=0;return;}
                var watchAgent=watch.GetComponent<NavMeshAgent>();
                if(watchAgent.enabled&&watchAgent.isOnNavMesh){watchAgent.ResetPath();watchAgent.isStopped=true;}
                var animator=watch.GetComponentInChildren<Animator>();if(animator)animator.SetFloat("MoveSpeed",0);
                Suspicion=0;
                if(State==Stage.Infiltrate&&!greeted&&Vector3.Distance(Player.transform.position,watch.transform.position)<5){StartCoroutine(Greet());return;}

            }
            var keyboard=Keyboard.current;if(keyboard==null||!Player.CanAct)return;
            if(keyboard.fKey.wasPressedThisFrame)Interact();
            if(State==Stage.Disable&&!wirePanel)Operate(keyboard.fKey.isPressed,Time.deltaTime);
            if(State==Stage.Leave&&Vector3.Distance(Player.transform.position,safePoint.position)<2.5f){State=Stage.Complete;Say(dialogue.complete,12);Friend.ScriptedTravel=false;Friend.Following=true;watch.gameObject.SetActive(false);}
        }
        public bool Interact()
        {
            if(Busy||!Player.CanAct||Time.timeScale<=0||!Target||Vector3.Distance(Player.transform.position,Target.position)>2.5f)return false;
            if(State==Stage.Change){StartCoroutine(Change());return true;}
            if(State==Stage.Infiltrate){StartCoroutine(Open());return true;}if(State==Stage.Disable&&wirePanel)return wirePanel.Begin();return false;
        }
        public void Operate(bool held,float deltaTime)
        {
            if(wirePanel||State!=Stage.Disable||Busy||!Player.CanAct||Time.timeScale<=0)return;
            bool clear=false;var view=Player.View.transform;
            var hits=Physics.RaycastAll(view.position,(console.position-view.position).normalized,Vector3.Distance(view.position,console.position),~0,QueryTriggerInteraction.Ignore);
            clear=Vector3.Distance(Player.transform.position,console.position)<2.3f&&Vector3.Angle(view.forward,console.position-view.position)<35;
            foreach(var hit in hits)if(!hit.transform.IsChildOf(Player.transform)&&!hit.transform.IsChildOf(console)&&hit.distance<Vector3.Distance(view.position,console.position)-.25f)clear=false;
            if(!held||!clear)return;
            Progress+=Mathf.Min(deltaTime,.1f)/channelSeconds;
            if(Progress<1)return;Progress=0;monitors[Channels].SetDisabled(true);PrisonAudio.Clink(console.position,.08f);Channels++;
            if(Channels==monitors.Length){CamerasDisabled=true;State=Stage.Leave;Say(dialogue.disabled);Friend.ScriptedTravel=false;Friend.Following=true;}
        }
        public bool BeginWireInteraction(){if(State!=Stage.Disable||Busy||!Player.CanAct||Time.timeScale<=0)return false;Busy=true;Lock();Say(wirePanel.dialogue.hint);return true;}
        public void EndWireInteraction(){Unlock();Busy=false;}
        public void ResolveWire(bool success){
            if(success){foreach(var monitor in monitors)monitor.SetDisabled(true);Channels=monitors.Length;CamerasDisabled=true;Progress=0;State=Stage.Leave;EndWireInteraction();Say(wirePanel.dialogue.success);Friend.ScriptedTravel=false;Friend.Following=true;}
            else StartCoroutine(WireRetry());
        }
        IEnumerator WireRetry(){State=Stage.Retry;Attempts++;Say(wirePanel.dialogue.failed);yield return Black(1);wirePanel.ResetPanel();Place(Player,wirePanel.checkpoint);Suspicion=Progress=0;Channels=0;CamerasDisabled=false;foreach(var monitor in monitors)monitor.SetDisabled(false);watch.ResetWatch();watch.enabled=false;yield return Black(0);State=Stage.Disable;EndWireInteraction();Say(wirePanel.dialogue.retry,10);}
        IEnumerator Change(){Busy=true;Lock();yield return Black(1);legacyEscape.ApplyDisguise();yield return Black(0);Unlock();Busy=false;State=Stage.Infiltrate;watch.gameObject.SetActive(true);var npc=watch.GetComponent<NPCControl>();npc.ExternalControl=true;watch.enabled=false;Say(dialogue.changed,9);}
        IEnumerator Greet(){
            greeted=true;Busy=true;Lock();Friend.Following=false;
            if(Friend.agent.enabled&&Friend.agent.isOnNavMesh)Friend.agent.ResetPath();
            var view=Player.View.transform;var initial=view.rotation;
            var facing=watch.transform.position+Vector3.up*1.55f;
            var desired=Quaternion.LookRotation(facing-view.position);
            var direction=Player.transform.position-watch.transform.position;direction.y=0;
            if(direction.sqrMagnitude>.01f)watch.transform.rotation=Quaternion.LookRotation(direction);
            for(float t=0;t<.55f;t+=Time.deltaTime){view.rotation=Quaternion.Slerp(initial,desired,Mathf.SmoothStep(0,1,t/.55f));yield return null;}
            foreach(var line in dialogue.disguiseGreeting){Say(line,dialogue.lineSeconds);yield return new WaitForSeconds(dialogue.lineSeconds);}
            Friend.ResumeFollowing();Unlock();Busy=false;
        }
        IEnumerator Open(){if(!greeted){yield return Greet();}Busy=true;for(float t=0;t<.9f;t+=Time.deltaTime){door.localRotation=closed*Quaternion.Euler(0,-95*Mathf.SmoothStep(0,1,t/.9f),0);yield return null;}door.localRotation=closed*Quaternion.Euler(0,-95,0);State=Stage.Disable;Busy=false;Say(dialogue.opened);}
        IEnumerator Retry(){Busy=true;State=Stage.Retry;Attempts++;Progress=0;Say(dialogue.caught);Lock();yield return new WaitForSeconds(1.4f);yield return Black(1);Place(Player,checkpoint);if(Friend.agent.enabled&&Friend.agent.isOnNavMesh)Friend.agent.Warp(mingiWait.position);Friend.ScriptedTravel=false;Friend.Following=true;watch.ResetWatch();watch.enabled=false;door.localRotation=closed;Suspicion=0;Channels=0;CamerasDisabled=false;if(wirePanel)wirePanel.ResetPanel();foreach(var monitor in monitors)monitor.SetDisabled(false);yield return Black(0);Unlock();Busy=false;State=Stage.Infiltrate;Say(dialogue.retry);}
        static void Place(PlayerCombat p,Transform at){var body=p.GetComponent<CharacterController>();body.enabled=false;p.transform.SetPositionAndRotation(at.position+Vector3.up*1.1f,at.rotation);body.enabled=true;p.View.transform.localRotation=Quaternion.identity;p.GetComponentInChildren<CameraMove>().SyncViewFromTransform();Physics.SyncTransforms();}
        void Say(string text,float seconds=7){Subtitle=dialogue.Format(text);subtitleUntil=Time.time+seconds;}
        IEnumerator Black(float to){float from=fade;for(float t=0;t<.45f;t+=Time.deltaTime){fade=Mathf.Lerp(from,to,t/.45f);yield return null;}fade=to;}
        void Lock(){if(locked)return;locked=true;var m=Player.GetComponent<PlayerMove>();var l=Player.GetComponentInChildren<CameraMove>();var i=Player.GetComponent<PlayerInteraction>();moveControl=m.isControllable;lookControl=l.isControllable;interactionEnabled=i.enabled;combatEnabled=Player.enabled;m.isControllable=l.isControllable=false;i.enabled=Player.enabled=false;}
        void Unlock(){if(!locked)return;locked=false;if(!Player)return;Player.GetComponent<PlayerMove>().isControllable=moveControl;var l=Player.GetComponentInChildren<CameraMove>();l.SyncViewFromTransform();l.isControllable=lookControl;Player.GetComponent<PlayerInteraction>().enabled=interactionEnabled;Player.enabled=combatEnabled;}
        public void DeveloperComplete(){if(wirePanel)wirePanel.Abort();StopAllCoroutines();Unlock();fade=0;Busy=false;restraint.Bind();legacyEscape.ApplyDisguise();Channels=monitors.Length;foreach(var m in monitors)m.SetDisabled(true);CamerasDisabled=true;State=Stage.Complete;watch.gameObject.SetActive(false);Friend.ScriptedTravel=false;Friend.Following=true;Say(dialogue.complete,12);}
        void OnDisable(){if(wirePanel)wirePanel.Abort();Unlock();}
        void OnGUI(){if(!Active||Time.timeScale<=0)return;var old=GUI.color;if(Suspicion>0){GUI.color=new Color(.1f,.1f,.1f,.9f);GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.2f,Screen.width*.2f,10),Texture2D.whiteTexture);GUI.color=new Color(1,.4f,.15f);GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.2f,Screen.width*.2f*Suspicion,10),Texture2D.whiteTexture);}if(!wirePanel&&State==Stage.Disable&&Vector3.Distance(Player.transform.position,console.position)<2.5f){GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.3f,Screen.height*.72f,Screen.width*.4f,55),dialogue.channels[Channels]+"  [F] 유지",new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=24,alignment=TextAnchor.MiddleCenter});GUI.color=new Color(.3f,.85f,.7f);GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.79f,Screen.width*.2f*Progress,7),Texture2D.whiteTexture);}if(fade>0){GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=old;}
    }
}
