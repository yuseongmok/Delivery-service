using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 조리 미션과 독립적으로 작업실 수색, 전투, 회수, 귀환을 관리한다.
    public sealed class PrisonMedicineMission : MonoBehaviour
    {
        public enum Stage { Waiting, Search, Conversation, Fight, Collect, Return, Complete }
        public PrisonMission prison;
        public PrisonChickenMission chicken;
        public PrisonMedicineText dialogue;
        public Transform workshop, checkpoint, cell, medicine;
        public MedicineBrawler[] enemies;
        public Transform[] decoys;
        public Camera camera;
        public Stage State { get; private set; }
        public bool Active=>State!=Stage.Waiting;
        public bool Failing { get; private set; }
        public string Subtitle { get; private set; }="";
        public float Fade { get; private set; }
        public int Hits { get; private set; }
        public bool HasMedicine { get; private set; }
        public Transform Target=>State==Stage.Search?workshop:State==Stage.Collect?medicine:State==Stage.Return?cell:null;
        public string Objective=>Failing?"작업실 앞에서 다시 준비합니다":State switch {
            Stage.Search=>"강탈 · 작업실에서 파란 완장의 죄수 찾기",
            Stage.Conversation=>"강탈 · 대화 중",Stage.Fight=>"강탈 · 죄수 무리 제압하기",
            Stage.Collect=>"강탈 · 떨어진 약 봉투 챙기기",Stage.Return=>"강탈 · 민기와 자기 방으로 돌아가기",
            Stage.Complete=>dialogue.complete,_=>""};
        PlayerCombat Player=>prison.player;
        float nextHit,noticeUntil;
        bool locked,moveControl,lookControl,interactionEnabled,phoneEnabled;
        void Start(){medicine.gameObject.SetActive(false);foreach(var e in enemies)e.gameObject.SetActive(false);foreach(var d in decoys)d.gameObject.SetActive(false);}
        void Update()
        {
            if(Time.timeScale<=0)return;
            if(State==Stage.Waiting){if(chicken.State==PrisonChickenMission.Stage.Complete)BeginSearch();return;}
            if(State!=Stage.Conversation&&!Failing&&Time.time>noticeUntil)Subtitle="";
            if(Failing)return;
            if(State==Stage.Search&&enemies.Any(e=>e.combat&&e.combat.Health<e.combat.maximumHealth))
            {State=Stage.Fight;foreach(var e in enemies)if(!e.Defeated)e.Begin();}
            if(State==Stage.Fight&&enemies.All(e=>e.Defeated)){State=Stage.Collect;medicine.gameObject.SetActive(true);Say(dialogue.won);}
            if(Player.CanAct&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)Interact();
        }
        public void BeginSearch()
        {
            State=Stage.Search;Hits=0;HasMedicine=false;nextHit=0;medicine.gameObject.SetActive(false);
            foreach(var e in enemies){e.gameObject.SetActive(true);e.Hold=true;}
            foreach(var d in decoys)d.gameObject.SetActive(true);
            prison.introduction.mingi.Following=true;Say(dialogue.clue,9);
        }
        public bool Interact()
        {
            if(!Player.CanAct||Failing)return false;
            if(State==Stage.Search)
            {
                if(!Physics.Raycast(Player.View.transform.position,Player.View.transform.forward,out var hit,3.2f,~0,QueryTriggerInteraction.Ignore))return false;
                if(enemies[0].combat&&hit.transform.IsChildOf(enemies[0].combat.transform)){StartCoroutine(Demand());return true;}
                if(decoys.Any(d=>hit.transform.IsChildOf(d))){Say(dialogue.wrong);return true;}
            }
            if(State==Stage.Collect&&Vector3.Distance(Player.transform.position,medicine.position)<2.5f)
            {HasMedicine=true;medicine.gameObject.SetActive(false);State=Stage.Return;PrisonAudio.Clink(Player.transform.position,.2f);Say(dialogue.won);return true;}
            if(State==Stage.Return&&HasMedicine&&Vector3.Distance(Player.transform.position,cell.position)<2.5f)
            {StartCoroutine(Home());return true;}
            return false;
        }
        IEnumerator Demand()
        {
            State=Stage.Conversation;Lock(true);camera.transform.LookAt(enemies[0].combat.transform.position+Vector3.up*1.45f);
            yield return Lines(dialogue.demand);Lock(false);State=Stage.Fight;Hits=0;
            foreach(var e in enemies)e.Begin();
        }
        IEnumerator Home()
        {
            State=Stage.Conversation;Lock(true);
            var mingi=prison.introduction.mingi;mingi.Following=false;
            Vector3 target=cell.position+Vector3.right*1.3f;
            if(UnityEngine.AI.NavMesh.SamplePosition(target,out var point,2,UnityEngine.AI.NavMesh.AllAreas))mingi.agent.Warp(point.position);
            Vector3 facing=Player.transform.position-mingi.transform.position;facing.y=0;
            if(facing.sqrMagnitude>.01f)mingi.transform.rotation=Quaternion.LookRotation(facing);
            camera.transform.LookAt(mingi.transform.position+Vector3.up*1.4f);
            yield return Lines(dialogue.home);Lock(false);mingi.Following=true;State=Stage.Complete;
        }
        IEnumerator Lines(string[] lines)
        {
            foreach(var line in lines){Subtitle=line;for(float t=0;t<dialogue.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}
            Subtitle="";
        }
        public void Hit(Vector3 direction)
        {
            if(State!=Stage.Fight||Failing||DeveloperCheats.Invincible||Time.time<nextHit)return;
            var ragdoll=Player.GetComponentInChildren<PlayerRagdoll>();if(ragdoll&&ragdoll.IsActive)return;
            nextHit=Time.time+3.5f;Hits++;if(ragdoll)ragdoll.Launch(direction*2+Vector3.up,2.2f);
            if(Hits>=3)StartCoroutine(Retry());
        }
        IEnumerator Retry()
        {
            Failing=true;foreach(var e in enemies)e.Hold=true;Say(dialogue.failed,10);
            var ragdoll=Player.GetComponentInChildren<PlayerRagdoll>();while(ragdoll&&ragdoll.IsActive)yield return null;
            Lock(true);yield return Blackout(1);
            var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.SetPositionAndRotation(checkpoint.position,checkpoint.rotation);cc.enabled=true;
            foreach(var e in enemies)e.ResetActor();BeginSearch();Lock(false);yield return Blackout(0);Failing=false;
        }
        IEnumerator Blackout(float value){float from=Fade;for(float t=0;t<.5f;t+=Time.deltaTime){Fade=Mathf.Lerp(from,value,t/.5f);yield return null;}Fade=value;}
        void Say(string text,float seconds=5){Subtitle=text;noticeUntil=Time.time+seconds;}
        void Lock(bool value)
        {
            var move=Player.GetComponent<PlayerMove>();var look=Player.GetComponentInChildren<CameraMove>();var interaction=Player.GetComponent<PlayerInteraction>();var phone=Player.GetComponentInChildren<PhoneController>(true);
            if(value&&!locked){moveControl=move.isControllable;lookControl=look.isControllable;interactionEnabled=interaction.enabled;phoneEnabled=phone&&phone.enabled;camera.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);}
            locked=value;move.isControllable=value?false:moveControl;look.isControllable=value?false:lookControl;interaction.enabled=value?false:interactionEnabled;if(phone)phone.enabled=value?false:phoneEnabled;
            Player.View.enabled=!value;camera.enabled=value;Player.View.GetComponent<AudioListener>().enabled=!value;camera.GetComponent<AudioListener>().enabled=value;
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
        }
        void OnGUI(){if(Fade<=0||Time.timeScale<=0)return;var tint=GUI.color;GUI.color=new Color(0,0,0,Fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=tint;}
    }
}
