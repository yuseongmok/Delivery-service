using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonTransportEscape : MonoBehaviour
    {
        public enum Stage { Waiting, Ready, Boarding, Riding, Falling, Retry, Relief, Complete, Gate }
        public PrisonNightMission previous;
        public PrisonTransportText dialogue;
        public Transform vehicle, start, destination, playerExit, friendExit;
        public PrisonTransportRoute road;
        public Transform gateShot;
        [Tooltip("탈옥 이후 돈 회수와 새 매장 시네마틱")]
        public string aftermathScene = "PrisonAftermath";
        bool gateOpened;
        public Transform playerHang, friendHang, eye, gateLeft, gateRight;
        public PrisonTransportPose playerActor, friendActor;
        public Camera film;
        public PrisonTransportHands hands;
        [Min(8)] public float rideSeconds = 22;
        [Range(.01f,.5f)] public float drainPerSecond = .13f, tapRecovery = .085f;
        [Range(1,6)] public float finalDrainMultiplier=3.8f;
        public float Fatigue=>Mathf.SmoothStep(0,1,Progress);
        public float CurrentDrain=>drainPerSecond*Mathf.Lerp(1,finalDrainMultiplier,Fatigue);
        public float LastTapTime{get;private set;}
        public bool GateOpened=>gateOpened;
        float gateProgress;
        public Stage State { get; private set; }
        public bool Active => State != Stage.Waiting;
        public bool Complete => State == Stage.Complete;
        public float Grip { get; private set; } = 1;
        public float Progress { get; private set; }
        public int Attempts { get; private set; }
        public string Subtitle { get; private set; } = "";
        public Transform Target => State == Stage.Ready ? previous.vehiclePoint : null;
        public string Objective => Complete ? dialogue.complete : "탈옥 · 호송차량 밑에서 버티기";
        public PlayerCombat Player => previous.Player;
        public PrisonCompanion Friend => previous.Friend;
        Quaternion leftClosed, rightClosed, filmRotation;
        Vector3 filmPosition;
        bool locked, moveEnabled, lookEnabled, combatEnabled, interactionEnabled, agentEnabled, viewEnabled, filmEnabled;
        bool playerListenerEnabled, filmListenerEnabled;
        readonly List<Renderer> hidden = new List<Renderer>();
        AudioSource engine;
        float age, nextTap, fade, subtitleUntil, shake, filmNear;
        bool warned, gatePassed;
        void Awake()
        {
            leftClosed=gateLeft.rotation;rightClosed=gateRight.rotation;
            if(road){float best=float.MaxValue;var center=(gateLeft.position+gateRight.position)*.5f;for(int i=0;i<=1000;i++){float t=i/1000f;float d=(road.Position(t)-center).sqrMagnitude;if(d<best){best=d;gateProgress=t;}}}
            if(hands)hands.mission=this;
            playerActor.gameObject.SetActive(false);friendActor.gameObject.SetActive(false);if(hands)hands.gameObject.SetActive(false);
        }
        void Update()
        {
            if(engine){engine.volume=.2f*SuppliedAudio.Volume;if(Time.timeScale<=0||State==Stage.Gate)engine.Pause();else if((State==Stage.Riding||State==Stage.Relief)&&!engine.isPlaying)engine.UnPause();}
            if(Time.timeScale<=0)return;
            if(State==Stage.Waiting){if(previous.Complete){State=Stage.Ready;Say(dialogue.ready,12);}return;}
            if(Time.time>subtitleUntil)Subtitle="";
            var keyboard=Keyboard.current;
            if(State==Stage.Ready&&keyboard!=null&&keyboard.fKey.wasPressedThisFrame)Interact();
            if(State==Stage.Relief&&Vector3.Distance(vehicle.position,destination.position)<18)vehicle.position+=vehicle.forward*2*Time.deltaTime;
            if(State!=Stage.Riding)return;
            if(keyboard!=null&&keyboard.spaceKey.wasPressedThisFrame)Tap();
            Grip=Mathf.Max(0,Grip-CurrentDrain*Time.deltaTime);
            if(Grip<=0){StartCoroutine(Fall());return;}
            if(Grip<.3f&&!warned){warned=true;Say(dialogue.warning);}
            if(Grip>.5f)warned=false;
            age+=Time.deltaTime;
            float ramp=Mathf.Min(2,rideSeconds*.1f);
            float travel=age<ramp?age*age/(2*ramp):age>rideSeconds-ramp?rideSeconds-ramp-Mathf.Pow(Mathf.Max(0,rideSeconds-age),2)/(2*ramp):age-ramp*.5f;
            Progress=Mathf.Clamp01(travel/(rideSeconds-ramp));
            if(!gateOpened&&road&&Progress>=Mathf.Max(0,gateProgress-10/road.Length)){StartCoroutine(GatePass());return;}
            if(road)vehicle.SetPositionAndRotation(road.Position(Progress),road.Rotation(Progress));
            else vehicle.position=Vector3.Lerp(start.position,destination.position,Progress);
            if(!gateOpened&&!road&&Vector3.Distance(vehicle.position,(gateLeft.position+gateRight.position)*.5f)<12){StartCoroutine(GatePass());return;}
            if(!gatePassed&&vehicle.position.z>Mathf.Max(gateLeft.position.z,gateRight.position.z)+3){gatePassed=true;Say(dialogue.clearGate);shake=.035f;}
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*.08f);
            if(Progress>=1)StartCoroutine(Escape());
        }
        void LateUpdate()
        {
            if(State!=Stage.Riding&&State!=Stage.Retry)return;
            film.transform.SetPositionAndRotation(eye.position,eye.rotation);
            float tremor=.006f+Fatigue*Fatigue*.055f+(1-Grip)*.03f+shake;
            film.transform.position+=vehicle.up*Mathf.Sin(Time.time*25)*tremor+vehicle.right*Mathf.Sin(Time.time*17)*tremor*.6f;
            film.transform.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*19)*Fatigue*1.1f,0,Mathf.Sin(Time.time*23)*Fatigue*1.4f);
        }
        public bool Interact()
        {
            if(State!=Stage.Ready||!Player.CanAct||Time.timeScale<=0||Vector3.Distance(Player.transform.position,previous.vehiclePoint.position)>3||Vector3.Distance(Friend.transform.position,vehicle.position)>6)return false;
            StartCoroutine(Board());return true;
        }
        public bool Tap()
        {
            if(State!=Stage.Riding||Time.timeScale<=0||Time.unscaledTime<nextTap)return false;
            LastTapTime=Time.time;nextTap=Time.unscaledTime+.07f;Grip=Mathf.Clamp01(Grip+tapRecovery);shake=.012f;return true;
        }
        IEnumerator Board()
        {
            State=Stage.Boarding;Lock();Say(dialogue.board);
            playerActor.gameObject.SetActive(true);friendActor.gameObject.SetActive(true);
            playerActor.HideHead=false;playerActor.Hanging=friendActor.Hanging=false;
            film.transform.position=vehicle.TransformPoint(new Vector3(-3.2f,.9f,-2));film.transform.LookAt(vehicle.position+Vector3.up*.35f);
            // 양옆에서 몸을 낮춘 뒤 바퀴 사이로 미끄러져 들어간다.
            Vector3 p=vehicle.TransformPoint(new Vector3(-1.8f,.15f,1));Vector3 f=vehicle.TransformPoint(new Vector3(1.8f,.15f,1));
            for(float t=0;t<2.2f;t+=Time.deltaTime)
            {
                float a=Mathf.SmoothStep(0,1,t/2.2f);
                playerActor.transform.SetPositionAndRotation(Vector3.Lerp(p,playerHang.position,a),Quaternion.Slerp(vehicle.rotation,playerHang.rotation,Mathf.Clamp01(a*1.8f)));
                friendActor.transform.SetPositionAndRotation(Vector3.Lerp(f,friendHang.position,a),Quaternion.Slerp(vehicle.rotation,friendHang.rotation,Mathf.Clamp01(a*1.8f)));
                yield return null;
            }
            AttachActors();yield return Fade(1);film.transform.SetPositionAndRotation(eye.position,eye.rotation);playerActor.HideHead=true;SetFirstPersonHands();
            yield return Fade(0);Say(dialogue.departure);yield return new WaitForSeconds(1);
            StartRide();
        }
        void SetFirstPersonHands()
        {
            foreach(var rr in playerActor.GetComponentsInChildren<Renderer>())rr.enabled=false;
            if(hands)hands.gameObject.SetActive(true);
        }
        void AttachActors()
        {
            playerActor.transform.SetParent(playerHang,false);playerActor.transform.localPosition=Vector3.zero;playerActor.transform.localRotation=Quaternion.identity;
            friendActor.transform.SetParent(friendHang,false);friendActor.transform.localPosition=Vector3.zero;friendActor.transform.localRotation=Quaternion.identity;
            playerActor.Hanging=friendActor.Hanging=true;
        }
        IEnumerator OpenGate()
        {
            for(float t=0;t<1.5f;t+=Time.deltaTime){float a=Mathf.SmoothStep(0,1,t/1.5f);gateLeft.rotation=leftClosed*Quaternion.Euler(0,-100*a,0);gateRight.rotation=rightClosed*Quaternion.Euler(0,100*a,0);yield return null;}
            gateLeft.rotation=leftClosed*Quaternion.Euler(0,-100,0);gateRight.rotation=rightClosed*Quaternion.Euler(0,100,0);
        }
        IEnumerator GatePass()
        {
            State=Stage.Gate;if(engine)engine.Pause();
            if(gateShot)film.transform.SetPositionAndRotation(gateShot.position,gateShot.rotation);
            if(hands)hands.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);yield return OpenGate();gateOpened=true;yield return new WaitForSeconds(.5f);
            film.transform.SetPositionAndRotation(eye.position,eye.rotation);if(hands)hands.gameObject.SetActive(true);State=Stage.Riding;if(engine)engine.UnPause();
        }
        void StartRide()
        {
            age=Progress=0;Grip=1;LastTapTime=Time.time;nextTap=0;gateOpened=gatePassed=warned=false;State=Stage.Riding;
            if(!engine){engine=vehicle.gameObject.AddComponent<AudioSource>();engine.clip=SuppliedAudio.Current?.playerEngine;engine.loop=true;engine.spatialBlend=.3f;}
            if(engine.clip)engine.Play();Say(dialogue.departure);
        }
        IEnumerator Fall()
        {
            State=Stage.Falling;if(hands)hands.gameObject.SetActive(false);Attempts++;Say(dialogue.fall);if(engine)engine.Stop();
            playerActor.Hanging=false;var from=film.transform.position;
            for(float t=0;t<.7f;t+=Time.deltaTime){film.transform.position=from+Vector3.down*(t*.18f)-vehicle.forward*t;film.transform.rotation=eye.rotation*Quaternion.Euler(0,0,18*t);yield return null;}
            yield return Fade(1);State=Stage.Retry;vehicle.SetPositionAndRotation(start.position,start.rotation);gateLeft.rotation=leftClosed;gateRight.rotation=rightClosed;
            AttachActors();playerActor.HideHead=true;SetFirstPersonHands();Grip=1;age=Progress=0;Say(dialogue.retry);
            yield return new WaitForSeconds(1);yield return Fade(0);StartRide();
        }
        IEnumerator Escape()
        {
            State=Stage.Relief;if(engine)engine.Stop();yield return Fade(1);
            playerActor.gameObject.SetActive(false);friendActor.gameObject.SetActive(false);if(hands)hands.gameObject.SetActive(false);
            // 차량에서 손을 놓은 직후 암전 상태로 기존 탈옥 후 시네마틱에 연결한다.
            if(!string.IsNullOrEmpty(aftermathScene)&&Application.CanStreamedLevelBeLoaded(aftermathScene)){
                Unlock();State=Stage.Complete;
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(aftermathScene);
                yield break;
            }
            RestoreActors();PlacePlayer(playerExit);PlaceFriend(friendExit);Unlock();State=Stage.Complete;
            Say(dialogue.complete,12);yield return Fade(0);
        }
        void PlacePlayer(Transform at)
        {
            var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.SetPositionAndRotation(at.position+Vector3.up*1.1f,at.rotation);cc.enabled=true;Player.View.transform.localRotation=Quaternion.identity;
        }
        void PlaceFriend(Transform at)
        {
            Friend.transform.SetPositionAndRotation(at.position,at.rotation);
            if(agentEnabled&&NavMesh.SamplePosition(at.position,out var hit,3,NavMesh.AllAreas)){Friend.agent.enabled=true;Friend.agent.Warp(hit.position);Friend.agent.isStopped=false;}
            Friend.ScriptedTravel=false;Friend.Following=true;
        }
        void Lock()
        {
            if(locked)return;locked=true;
            var move=Player.GetComponent<PlayerMove>();var look=Player.GetComponentInChildren<CameraMove>();
            moveEnabled=move.isControllable;lookEnabled=look.isControllable;combatEnabled=Player.enabled;interactionEnabled=Player.GetComponent<PlayerInteraction>().enabled;
            move.isControllable=look.isControllable=false;Player.enabled=false;Player.GetComponent<PlayerInteraction>().enabled=false;
            viewEnabled=Player.View.enabled;filmEnabled=film.enabled;filmPosition=film.transform.position;filmRotation=film.transform.rotation;filmNear=film.nearClipPlane;
            Player.View.enabled=false;film.enabled=true;film.nearClipPlane=.025f;
            var listener=Player.View.GetComponent<AudioListener>();playerListenerEnabled=listener&&listener.enabled;if(listener)listener.enabled=false;
            var other=film.GetComponent<AudioListener>();filmListenerEnabled=other&&other.enabled;if(!other)other=film.gameObject.AddComponent<AudioListener>();other.enabled=true;
            hidden.Clear();foreach(var r in Player.GetComponentsInChildren<Renderer>())if(r.enabled){hidden.Add(r);r.enabled=false;}
            foreach(var r in Friend.GetComponentsInChildren<Renderer>())if(r.enabled){hidden.Add(r);r.enabled=false;}
            agentEnabled=Friend.agent.enabled;if(agentEnabled&&Friend.agent.isOnNavMesh)Friend.agent.ResetPath();Friend.agent.enabled=false;Friend.Following=false;Friend.ScriptedTravel=true;
        }
        void RestoreActors(){foreach(var r in hidden)if(r)r.enabled=true;hidden.Clear();}
        void Unlock()
        {
            if(!locked)return;locked=false;RestoreActors();
            Player.View.enabled=viewEnabled;film.enabled=filmEnabled;film.nearClipPlane=filmNear;film.transform.SetPositionAndRotation(filmPosition,filmRotation);
            Player.GetComponent<PlayerMove>().isControllable=moveEnabled;var look=Player.GetComponentInChildren<CameraMove>();look.SyncViewFromTransform();look.isControllable=lookEnabled;
            Player.enabled=combatEnabled;Player.GetComponent<PlayerInteraction>().enabled=interactionEnabled;
            var listener=Player.View.GetComponent<AudioListener>();if(listener)listener.enabled=playerListenerEnabled;
            var other=film.GetComponent<AudioListener>();if(other)other.enabled=filmListenerEnabled;
        }
        void Say(string text,float seconds=8){Subtitle=text;subtitleUntil=Time.time+seconds;}
        IEnumerator Fade(float to){float from=fade;for(float t=0;t<.55f;t+=Time.deltaTime){fade=Mathf.Lerp(from,to,t/.55f);yield return null;}fade=to;}
        public void DeveloperComplete()
        {
            StopAllCoroutines();if(engine)engine.Stop();playerActor.gameObject.SetActive(false);friendActor.gameObject.SetActive(false);if(hands)hands.gameObject.SetActive(false);
            if(!locked)agentEnabled=Friend.agent.enabled;RestoreActors();PlacePlayer(playerExit);PlaceFriend(friendExit);Unlock();fade=0;State=Stage.Complete;Say(dialogue.complete,12);
        }
        void OnDisable(){if(engine)engine.Stop();Unlock();}
        void OnGUI()
        {
            if(!Active||Time.timeScale<=0)return;
            var old=GUI.color;
            if(State==Stage.Riding)
            {
                PrisonGripDisplay.Draw(Grip);

            }
            if(fade>0){GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=old;
        }
    }
}
