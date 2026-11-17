using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
namespace DeliveryService.Mok {
    // 입력/판정은 이 컴포넌트, 실제 팔 자세는 PrisonTheftHands, 미션 진행은 PrisonKeysMission.
    public sealed class PrisonPickpocket : MonoBehaviour {
        public enum Phase { Pull, Snag, Freeze, Catch, Receive }
        [HideInInspector] public Mesh handMesh;
        public PrisonTheftHands hands;
        public float pullDistance=650,noisySpeed=210,noiseRecovery=.45f,snagDistance=65,catchSeconds=1.25f;
        [Header("간수 뒤척임")]
        public Vector2 stirInterval=new Vector2(1.8f,2.7f);
        public float stirDuration=1.1f, reactionGrace=.22f;
        public bool Playing{get;private set;} public float Progress{get;private set;} public float Noise{get;private set;}
        public Phase State{get;private set;} public int SnagDirection{get;private set;} public float SnagProgress{get;private set;}
        public float PhaseTime{get;private set;}
        PrisonKeysMission mission;Transform ring;Vector3 ringRest;Quaternion ringRotation;
        NavMeshAgent agent;bool guardEnabled,stopped,grabbing,caught;float age,warning,nextSound,firstSnag,secondSnag,nextStir;int snags;
        Phase resumePhase;
        PrisonTheftAudio audioFeedback;
        Vector2 lastMouse;AudioSource sound;Renderer[] renderers;bool[] visibility;
        public void Begin(PrisonKeysMission owner){
            if(Playing)return;mission=owner;ring=owner.keyRing.transform;ringRest=ring.localPosition;ringRotation=ring.localRotation;
            agent=owner.keyGuard.GetComponent<NavMeshAgent>();guardEnabled=owner.keyGuard.enabled;stopped=agent&&agent.isOnNavMesh&&agent.isStopped;
            owner.keyGuard.enabled=false;if(agent&&agent.isOnNavMesh)agent.isStopped=true;
            var animator=owner.keyGuard.GetComponentInChildren<Animator>();if(animator)animator.SetFloat("MoveSpeed",0);
            owner.SetTheftView(true);renderers=owner.previous.prison.player.GetComponentsInChildren<Renderer>(true);visibility=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++){visibility[i]=renderers[i].enabled;renderers[i].enabled=false;}
            lastMouse=Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero;
            Progress=Noise=age=warning=PhaseTime=SnagProgress=0;snags=0;caught=grabbing=false;State=Phase.Pull;
            nextStir=Random.Range(stirInterval.x,stirInterval.y);nextSound=0;
            firstSnag=Random.Range(.27f,.34f);secondSnag=Random.Range(.64f,.71f);SnagDirection=Random.value>.5f?1:-1;
            Playing=true;if(hands)hands.gameObject.SetActive(true);
            if(!audioFeedback)audioFeedback=gameObject.AddComponent<PrisonTheftAudio>();audioFeedback.Begin(this);
            if(!sound){sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;}
        }
        void Update(){
            if(!Playing||Time.timeScale<=0)return;age+=Time.deltaTime;PhaseTime+=Time.deltaTime;
            if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame&&age>.2f){End(false);return;}
            var mouse=Mouse.current;if(mouse==null)return;var pos=mouse.position.ReadValue();var delta=(pos-lastMouse)*1080f/Mathf.Max(1,Screen.height);lastMouse=pos;
            if(State==Phase.Catch){
                if(mouse.rightButton.wasPressedThisFrame){caught=true;State=Phase.Receive;PhaseTime=0;Sound(PrisonCookingSounds.Current?.pickup,.25f);}
                else if(PhaseTime>catchSeconds){Noise=1;audioFeedback.Jingle(.65f);End(true);mission.ReportDroppedKeys();}return;
            }
            if(State==Phase.Receive){if(PhaseTime>.5f)End(true);return;}
            if(State!=Phase.Freeze&&age>=nextStir){resumePhase=State;State=Phase.Freeze;PhaseTime=0;}
            var p=mission.camera.WorldToScreenPoint(ring.position);
            if(mouse.leftButton.wasPressedThisFrame)grabbing=Vector2.Distance(pos,new Vector2(p.x,p.y))<Screen.height*.10f;
            if(!mouse.leftButton.isPressed)grabbing=false;
            bool moving=grabbing&&delta.magnitude>.5f;
            if(State==Phase.Freeze){
                if(moving&&PhaseTime>reactionGrace)AddNoise(delta.magnitude/90f);
                if(PhaseTime>=stirDuration){State=resumePhase;PhaseTime=0;nextStir=age+Random.Range(stirInterval.x,stirInterval.y);}
            }else if(grabbing&&age>.3f){
                float speed=delta.magnitude/Mathf.Max(.001f,Time.deltaTime);
                if(speed>noisySpeed)AddNoise(delta.magnitude/240f);
                if(State==Phase.Snag){
                    if(delta.x*SnagDirection>0)SnagProgress=Mathf.Clamp01(SnagProgress+Mathf.Min(12,delta.x*SnagDirection)/snagDistance);
                    else if(Mathf.Abs(delta.x)>1)AddNoise(Mathf.Abs(delta.x)/200f);
                    if(delta.y>1)AddNoise(delta.y/160f);
                    if(SnagProgress>=1){snags++;State=Phase.Pull;SnagProgress=0;PhaseTime=0;Sound(PrisonCookingSounds.Current?.tick,.17f);}
                }else{
                    Progress=Mathf.Clamp01(Progress+Mathf.Clamp(delta.y,-12,12)/pullDistance);
                    if((snags==0&&Progress>=firstSnag)||(snags==1&&Progress>=secondSnag)){Progress=snags==0?firstSnag:secondSnag;State=Phase.Snag;if(snags==1)SnagDirection=-SnagDirection;PhaseTime=0;Sound(PrisonCookingSounds.Current?.tick,.18f);}
                    else if(Progress>=1){State=Phase.Catch;PhaseTime=0;grabbing=false;}
                }
            }
            if(!moving)Noise=Mathf.MoveTowards(Noise,0,Time.deltaTime*noiseRecovery);
            if(Noise>=.6f&&warning<=0)warning=1.5f;
            if(warning>0){warning-=Time.deltaTime;if(warning<=0&&Noise>=.35f){End(false);mission.TheftCaught();}}
        }
        void AddNoise(float amount){Noise=Mathf.Clamp01(Noise+amount);if(Noise>=.6f&&Time.time>nextSound){audioFeedback.Jingle(.25f+Noise*.15f);nextSound=Time.time+.3f;}}
        void LateUpdate(){
            if(!Playing||Time.timeScale<=0)return;
            float drop=State==Phase.Catch?Mathf.Clamp01(PhaseTime/catchSeconds)*.12f:0;
            ring.position=ring.parent.TransformPoint(ringRest)+Vector3.up*(Progress*.22f-drop);
            float snag=State==Phase.Snag?SnagDirection*(1-SnagProgress):0;
            ring.localRotation=ringRotation*Quaternion.Euler(0,0,snag*22+Mathf.Sin(Time.time*30)*Noise*9);
            var cam=mission.camera;var focus=ring.parent.TransformPoint(ringRest);
            var target=focus-mission.keyGuard.transform.forward*.8f+Vector3.up*.34f;
            cam.transform.position=Vector3.Lerp(cam.transform.position,target,Time.deltaTime*5);
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(focus+Vector3.up*.12f-cam.transform.position),Time.deltaTime*6);
            if(hands)hands.Pose(cam,ring.position,grabbing||State==Phase.Snag||State==Phase.Freeze||State==Phase.Catch||State==Phase.Receive,snag,State==Phase.Catch?.35f:caught?1:0);
            var a=mission.keyGuard.GetComponentInChildren<Animator>();if(a&&a.isHuman&&(State==Phase.Freeze||warning>0)){
                var head=a.GetBoneTransform(HumanBodyBones.Head);if(head)head.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*4)*22,Vector3.up)*head.rotation;
                var shoulder=a.GetBoneTransform(HumanBodyBones.RightUpperArm);if(shoulder)shoulder.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*5)*7,mission.keyGuard.transform.forward)*shoulder.rotation;
            }
        }
        public void End(bool success){
            if(!Playing)return;Playing=false;if(ring){ring.localPosition=ringRest;ring.localRotation=ringRotation;}
            if(hands)hands.gameObject.SetActive(false);if(mission.keyGuard)mission.keyGuard.enabled=guardEnabled;
            if(agent&&agent.isOnNavMesh)agent.isStopped=stopped;
            for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=visibility[i];
            mission.SetTheftView(false);if(success)mission.FinishTheft();
        }
        void Sound(AudioClip clip,float volume){if(clip&&sound)sound.PlayOneShot(clip,volume*SuppliedAudio.Volume);}
        void OnGUI(){
            if(!Playing||Time.timeScale<=0)return;
            var d=mission.dialogue;string line=warning>0?d.theftWarning:State==Phase.Snag?(SnagDirection>0?d.theftSnagRight:d.theftSnagLeft):State==Phase.Freeze?d.theftFreeze:State==Phase.Catch?d.theftCatch:State==Phase.Receive?d.theftReceive:grabbing?d.theftPull:d.theftGrab;
            var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.025f),alignment=TextAnchor.MiddleCenter};
            string action=warning>0?"소리가 났다 · 멈춰!":State==Phase.Freeze?"멈춰!":State==Phase.Catch?"우클릭 · 열쇠 받기":State==Phase.Receive?"열쇠 확보":State==Phase.Snag?(SnagDirection>0?"오른쪽으로 풀기":"왼쪽으로 풀기"):grabbing?"천천히 위로 당기기":"좌클릭 유지 · 열쇠 잡기";
            PrisonTheftDisplay.Instruction(action,State==Phase.Freeze?"간수가 뒤척입니다 · 움직이지 마세요":"F · 손을 빼고 물러나기",warning>0||State==Phase.Freeze);
            PrisonTheftDisplay.Draw(Noise,State,SnagDirection,warning>0,Progress);
        }
        void OnDisable(){End(false);}
    }
}
