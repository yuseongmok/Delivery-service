using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 소매치기 입력과 연출만 담당한다. 성공/실패 이후 진행은 기존 미션이 맡는다.
    public sealed class PrisonPickpocket : MonoBehaviour
    {
        public Mesh handMesh;
        public float pullDistance = 360, noisySpeed = 430, noiseRecovery = .18f;
        public bool Playing { get; private set; }
        public float Progress { get; private set; }
        public float Noise { get; private set; }
        PrisonKeysMission mission;
        Transform ring, hand, wrist;
        Vector3 ringRest, lastMouse;
        Quaternion ringRotation;
        NavMeshAgent agent;
        bool guardEnabled, stopped, grabbing;
        float age, warning, nextSound;
        AudioSource audioSource;
        Material skin, sleeve;
        Renderer[] playerRenderers; bool[] rendererEnabled;

        public void Begin(PrisonKeysMission owner)
        {
            mission = owner; ring = owner.keyRing.transform;
            ringRest = ring.localPosition; ringRotation = ring.localRotation;
            agent = owner.keyGuard.GetComponent<NavMeshAgent>();
            guardEnabled = owner.keyGuard.enabled;
            stopped = agent && agent.isOnNavMesh && agent.isStopped;
            owner.keyGuard.enabled = false;
            if (agent && agent.isOnNavMesh) agent.isStopped = true;
            var animator = owner.keyGuard.GetComponentInChildren<Animator>();
            if (animator) animator.SetFloat("MoveSpeed", 0);
            owner.SetTheftView(true);
            playerRenderers=owner.previous.prison.player.GetComponentsInChildren<Renderer>(true);
            rendererEnabled=new bool[playerRenderers.Length];
            for(int i=0;i<playerRenderers.Length;i++){rendererEnabled[i]=playerRenderers[i].enabled;playerRenderers[i].enabled=false;}
            lastMouse = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
            Progress = Noise = age = warning = 0; grabbing = false; Playing = true;
            if (!hand) BuildHand();
            hand.gameObject.SetActive(true); wrist.gameObject.SetActive(true);
            if(!audioSource){var sound=new GameObject("열쇠 소리");sound.transform.SetParent(transform,false);audioSource=sound.AddComponent<AudioSource>();}
            audioSource.playOnAwake = false; audioSource.spatialBlend = 1;
            audioSource.minDistance = 1; audioSource.maxDistance = 8;
        }

        void Update()
        {
            if (!Playing || Time.timeScale <= 0) return;
            age += Time.deltaTime;
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && age > .2f)
            { End(false); return; }
            if (Mouse.current == null) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
            var screen = mission.camera.WorldToScreenPoint(ring.position);
            if (Mouse.current.leftButton.wasPressedThisFrame)
                grabbing = Vector2.Distance(mouse, new Vector2(screen.x,screen.y)) < Screen.height * .10f;
            if (!Mouse.current.leftButton.isPressed) grabbing = false;
            Vector2 delta = mouse - (Vector2)lastMouse;
            lastMouse = mouse;
            // 화면 해상도가 달라도 당기는 길이와 소음 기준은 동일하다.
            delta *= 1080f / Mathf.Max(1, Screen.height);
            if (grabbing && age > .3f)
            {
                float speed = delta.magnitude / Mathf.Max(.001f,Time.deltaTime);
                if (speed > noisySpeed)
                {
                    Noise = Mathf.Clamp01(Noise + delta.magnitude / 230f);
                    if (Time.time > nextSound) { Sound(PrisonCookingSounds.Current?.flip,.32f); nextSound=Time.time+.25f; }
                }
                Progress = Mathf.Clamp01(Progress + Mathf.Clamp(delta.y,-18,18) / pullDistance);
            }
            else Noise = Mathf.MoveTowards(Noise,0,Time.deltaTime*noiseRecovery);
            if (Noise >= .6f && warning <= 0) warning = 1.5f;
            if (warning > 0)
            {
                warning -= Time.deltaTime;
                if (warning <= 0 && Noise >= .35f) { End(false); mission.TheftCaught(); return; }
            }
            if (Progress >= 1) { Sound(PrisonCookingSounds.Current?.pickup,.3f); End(true); }
        }

        void LateUpdate()
        {
            if (!Playing) return;
            ring.localPosition = ringRest + Vector3.up * (Progress*.22f);
            ring.localRotation = ringRotation * Quaternion.Euler(0,0,Mathf.Sin(Time.time*35)*Noise*12);
            var cam = mission.camera;
            Vector3 focus = ring.parent.TransformPoint(ringRest);
            Vector3 desired = focus - mission.keyGuard.transform.forward*.8f + Vector3.up*.34f;
            // 카메라는 간수 뒤 실제 플레이어 위치에서 가까운 허리 접점으로 천천히 이동한다.
            cam.transform.position = Vector3.Lerp(cam.transform.position,desired,Time.deltaTime*5);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(focus+Vector3.up*.12f-cam.transform.position),Time.deltaTime*6);
            Vector3 target = ring.position + cam.transform.right*.035f - cam.transform.forward*.025f;
            if (!grabbing) target += cam.transform.right*.10f-Vector3.up*.09f;
            hand.position = Vector3.Lerp(hand.position,target,Time.deltaTime*12);
            hand.rotation = cam.transform.rotation;
            Vector3 start=cam.transform.position+cam.transform.right*.20f-cam.transform.up*.28f+cam.transform.forward*.30f;
            wrist.position=(start+hand.position)*.5f;
            wrist.rotation=Quaternion.FromToRotation(Vector3.up,hand.position-start);
            wrist.localScale=new Vector3(.055f,Vector3.Distance(start,hand.position)*.5f,.055f);
            var animator=mission.keyGuard.GetComponentInChildren<Animator>();
            if(animator && animator.isHuman && warning>0)
            {
                var head=animator.GetBoneTransform(HumanBodyBones.Head);
                if(head) head.rotation=Quaternion.AngleAxis(Mathf.Sin((1.5f-warning)/1.5f*Mathf.PI)*65,Vector3.up)*head.rotation;
            }
        }

        public void End(bool success)
        {
            if (!Playing) return;
            Playing=false;
            ring.localPosition=ringRest; ring.localRotation=ringRotation;
            hand.gameObject.SetActive(false); wrist.gameObject.SetActive(false);
            mission.keyGuard.enabled=guardEnabled;
            if(agent && agent.isOnNavMesh) agent.isStopped=stopped;
            for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=rendererEnabled[i];
            mission.SetTheftView(false);
            if(success) mission.FinishTheft();
        }
        void Sound(AudioClip clip,float volume)
        { if(clip) { audioSource.transform.position=ring.position; audioSource.PlayOneShot(clip,volume*SuppliedAudio.Volume); } }
        void BuildHand()
        {
            skin=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); skin.color=new Color(.66f,.43f,.29f);
            sleeve=new Material(skin); sleeve.color=new Color(.65f,.24f,.07f);sleeve.SetFloat("_Smoothness",0);skin.SetFloat("_Smoothness",0);
            hand=new GameObject("열쇠를 잡는 죄수의 손").transform; hand.SetParent(transform,false);
            Part("손바닥",hand,new Vector3(0,0,0),new Vector3(.065f,.08f,.045f),skin);
            for(int i=0;i<4;i++) Part("굽힌 손가락",hand,new Vector3(-.024f+i*.016f,.044f,.012f),new Vector3(.013f,.035f,.028f),skin);
            Part("엄지",hand,new Vector3(-.036f,.008f,.02f),new Vector3(.02f,.045f,.024f),skin);
            wrist=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform; wrist.name="주황 죄수복 소매"; wrist.SetParent(transform,false);
            Destroy(wrist.GetComponent<Collider>()); wrist.GetComponent<Renderer>().sharedMaterial=sleeve;
            
            hand.position=mission.camera.transform.position-mission.camera.transform.up*.3f;
        }
        void Part(string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;Destroy(go.GetComponent<Collider>());if(handMesh)go.GetComponent<MeshFilter>().sharedMesh=handMesh;go.GetComponent<Renderer>().sharedMaterial=material; }
        void OnGUI()
        {
            if(!Playing || Time.timeScale<=0)return;
            var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.026f),alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(0,Screen.height*.79f,Screen.width,Screen.height*.12f),warning>0?mission.dialogue.theftWarning:grabbing?mission.dialogue.theftPull:mission.dialogue.theftGrab,style);
            GUI.Label(new Rect(0,Screen.height*.91f,Screen.width,40),"F · 손을 빼고 물러나기",style);
            var old=GUI.color;GUI.color=Color.Lerp(new Color(.5f,.75f,.65f),new Color(1,.3f,.1f),Noise);
            GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.77f,Screen.width*.2f*Noise,4),Texture2D.whiteTexture);GUI.color=old;
        }
        void OnDisable(){End(false);}
        void OnDestroy(){if(skin)Destroy(skin);if(sleeve)Destroy(sleeve);}
    }
}
