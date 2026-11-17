using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 창고 열쇠의 선택/삽입/회전 표현. 미션 단계와 발각 처리는 미션에 남긴다.
    public sealed class PrisonLockInteraction : MonoBehaviour
    {
        public enum Phase { Choose, Inserting, Turn, Removing }
        public float insertSeconds=.45f, turnPixels=210;
        public bool Playing { get; private set; }
        public Phase State { get; private set; }
        public float Insertion { get; private set; }
        public float Rotation { get; private set; }
        public bool GuardLooking { get; private set; }
        PrisonKeysMission mission;
        PrisonKeyLock keyLock;
        Transform hand, sleeve, ring;
        Material skin, cloth, metal;
        AudioSource sound;
        Vector3[] positions, scales; Quaternion[] rotations;
        Renderer[] playerRenderers; bool[] visibility;
        Vector2 lastMouse;
        float elapsed, messageUntil, nextTick;
        bool turning;
        string message;

        public void Begin(PrisonKeysMission owner)
        {
            mission=owner;keyLock=owner.keyLock;
            positions=new Vector3[keyLock.keyModels.Length];scales=new Vector3[positions.Length];rotations=new Quaternion[positions.Length];
            for(int i=0;i<positions.Length;i++){scales[i]=keyLock.keyModels[i].transform.localScale;keyLock.keyModels[i].transform.localScale=scales[i]*.65f;positions[i]=keyLock.keyModels[i].transform.position;rotations[i]=keyLock.keyModels[i].transform.rotation;}
            owner.SetTheftView(true);
            playerRenderers=owner.previous.prison.player.GetComponentsInChildren<Renderer>(true);visibility=new bool[playerRenderers.Length];
            for(int i=0;i<visibility.Length;i++){visibility[i]=playerRenderers[i].enabled;playerRenderers[i].enabled=false;}
            if(!hand)BuildVisuals();
            hand.gameObject.SetActive(true);sleeve.gameObject.SetActive(true);ring.gameObject.SetActive(true);
            Playing=true;State=Phase.Choose;Insertion=Rotation=elapsed=0;turning=false;message="";messageUntil=0;
            lastMouse=Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero;
        }
        void Update()
        {
            if(!Playing || Time.timeScale<=0)return;
            elapsed+=Time.deltaTime;
            // 삽입·회전·잘못된 열쇠를 빼는 중에도 즉시 중단할 수 있다.
            if(Keyboard.current!=null && Keyboard.current.fKey.wasPressedThisFrame && elapsed>.15f){mission.CancelLock();return;}
            if(mission.Failing){End();return;}
            var mouse=Mouse.current;if(mouse==null)return;
            Vector2 pos=mouse.position.ReadValue(),delta=(pos-lastMouse)*1080f/Mathf.Max(1,Screen.height);lastMouse=pos;
            Vector3 toPlayer=mission.previous.prison.player.transform.position-mission.keyGuard.transform.position;
            GuardLooking=toPlayer.magnitude<mission.keyGuard.sightRange && Vector3.Angle(mission.keyGuard.transform.forward,Vector3.ProjectOnPlane(toPlayer,Vector3.up))<65;
            if(State==Phase.Choose)
            {
                float scroll=mouse.scroll.ReadValue().y;if(Mathf.Abs(scroll)>0){keyLock.Select(scroll>0?1:-1);Play(PrisonCookingSounds.Current?.flip,.17f);}
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    int hit=HitKey(pos);
                    if(hit>=0){keyLock.Select(hit-keyLock.Selected);State=Phase.Inserting;Insertion=0;Play(SuppliedAudio.Current?.vaultLatch,.16f);}
                }
            }
            else if(State==Phase.Inserting)
            {
                Insertion=Mathf.MoveTowards(Insertion,1,Time.deltaTime/insertSeconds);
                if(Insertion>=1){State=Phase.Turn;turning=false;Play(PrisonCookingSounds.Current?.tick,.22f);}
            }
            else if(State==Phase.Turn)
            {
                if(mouse.rightButton.wasPressedThisFrame){State=Phase.Removing;Rotation=0;return;}
                if(mouse.leftButton.wasPressedThisFrame){turning=HitKey(pos)==keyLock.Selected;delta=Vector2.zero;}
                if(!mouse.leftButton.isPressed)turning=false;
                if(turning && delta.x>0)
                {
                    Rotation=Mathf.Clamp01(Rotation+Mathf.Min(delta.x,25)/turnPixels);
                    if(Time.time>nextTick){Play(PrisonCookingSounds.Current?.tick,.13f);nextTick=Time.time+.18f;}
                    if(keyLock.Selected!=keyLock.correctKey && Rotation>=.22f)
                    {message=mission.dialogue.wrongKey;messageUntil=Time.time+2;Play(SuppliedAudio.Current?.metalImpact,.2f);State=Phase.Removing;turning=false;}
                    else if(Rotation>=1){Play(SuppliedAudio.Current?.vaultLatch,.3f);End();mission.FinishLock();}
                }
            }
            else
            {
                Rotation=Mathf.MoveTowards(Rotation,0,Time.deltaTime*3);
                Insertion=Mathf.MoveTowards(Insertion,0,Time.deltaTime/insertSeconds);
                if(Insertion<=0){State=Phase.Choose;Rotation=0;}
            }
        }
        int HitKey(Vector2 mouse)
        {
            int best=-1;float distance=Screen.height*.075f;
            for(int i=0;i<keyLock.keyModels.Length;i++)
            {
                if(State!=Phase.Choose && i!=keyLock.Selected)continue;
                var key=keyLock.keyModels[i].transform;
                Vector3 p=mission.camera.WorldToScreenPoint(key.position-key.forward*.078f);
                float d=Vector2.Distance(mouse,new Vector2(p.x,p.y));if(p.z>0&&d<distance){best=i;distance=d;}
            }
            return best;
        }
        void LateUpdate()
        {
            if(!Playing || Time.timeScale<=0)return;
            var cam=mission.camera;var socket=keyLock.transform;
            // 기존 자물쇠 전방은 문 안쪽. 바깥쪽에서 바라본다.
            Vector3 destination=socket.position-socket.forward*.85f+Vector3.up*.34f;
            cam.transform.position=Vector3.Lerp(cam.transform.position,destination,Time.deltaTime*7);
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(socket.position+Vector3.up*.06f-cam.transform.position),Time.deltaTime*7);
            var basis=Quaternion.LookRotation(socket.forward,Vector3.up);
            Vector3 selectedGrip=Vector3.zero;
            for(int i=0;i<keyLock.keyModels.Length;i++)
            {
                var key=keyLock.keyModels[i].transform;key.gameObject.SetActive(true);
                Vector3 fan=socket.position-socket.forward*.25f-socket.right*.18f+socket.right*(i*.12f)+Vector3.up*.09f;
                Quaternion rest=basis*Quaternion.Euler(0,(i-1.5f)*12,0);
                if(i==keyLock.Selected)
                {
                    key.position=Vector3.Lerp(fan,socket.position-socket.forward*.062f,Mathf.SmoothStep(0,1,Insertion));
                    key.rotation=Quaternion.Slerp(rest,basis,Insertion)*Quaternion.AngleAxis(-90*Rotation,Vector3.forward);
                    selectedGrip=key.position-key.forward*.078f;
                }
                else {key.position=fan;key.rotation=rest;}
            }
            ring.position=socket.position-socket.forward*.328f+Vector3.up*.09f;ring.rotation=basis;
            hand.position=selectedGrip-socket.forward*.02f;hand.rotation=basis*Quaternion.AngleAxis(-90*Rotation,Vector3.forward);
            Vector3 start=cam.transform.position+cam.transform.right*.22f-cam.transform.up*.28f+cam.transform.forward*.30f;
            sleeve.position=(start+hand.position)*.5f;sleeve.rotation=Quaternion.FromToRotation(Vector3.up,hand.position-start);
            sleeve.localScale=new Vector3(.055f,Vector3.Distance(start,hand.position)*.5f,.055f);
            foreach(var r in playerRenderers)if(r)r.enabled=false;
        }
        public void End()
        {
            if(!Playing)return;Playing=false;
            for(int i=0;keyLock && i<positions.Length;i++){if(!keyLock.keyModels[i])continue;keyLock.keyModels[i].transform.SetPositionAndRotation(positions[i],rotations[i]);keyLock.keyModels[i].transform.localScale=scales[i];keyLock.keyModels[i].SetActive(false);}
            hand.gameObject.SetActive(false);sleeve.gameObject.SetActive(false);ring.gameObject.SetActive(false);
            for(int i=0;i<visibility.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=visibility[i];
            if(mission)mission.SetTheftView(false);if(keyLock)keyLock.Cancel();
        }
        void Play(AudioClip clip,float volume)
        {if(clip){sound.transform.position=keyLock.transform.position;sound.Stop();sound.clip=clip;sound.volume=volume*SuppliedAudio.Volume;sound.Play();}}
        void BuildVisuals()
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            skin=new Material(shader){color=new Color(.66f,.43f,.29f)};cloth=new Material(shader){color=new Color(.65f,.24f,.07f)};metal=new Material(shader){color=new Color(.55f,.5f,.33f)};
            cloth.SetFloat("_Smoothness",0);skin.SetFloat("_Smoothness",0);
            hand=new GameObject("자물쇠를 돌리는 손").transform;hand.SetParent(transform,false);
            Part(hand,Vector3.zero,new Vector3(.065f,.075f,.035f),skin);
            for(int i=0;i<4;i++)Part(hand,new Vector3(-.024f+i*.016f,.038f,.018f),new Vector3(.014f,.025f,.037f),skin);
            Part(hand,new Vector3(-.035f,0,.024f),new Vector3(.022f,.043f,.026f),skin);
            sleeve=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;sleeve.SetParent(transform,false);Destroy(sleeve.GetComponent<Collider>());sleeve.GetComponent<Renderer>().sharedMaterial=cloth;
            ring=new GameObject("손에 든 열쇠고리").transform;ring.SetParent(transform,false);
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Vector3 p=new Vector3(Mathf.Cos(a)*.20f,Mathf.Sin(a)*.055f,0),q=new Vector3(Mathf.Cos(b)*.20f,Mathf.Sin(b)*.055f,0);var link=Part(ring,(p+q)*.5f,new Vector3(.008f,(q-p).magnitude,.008f),metal);link.localRotation=Quaternion.FromToRotation(Vector3.up,q-p);}
            var audio=new GameObject("자물쇠 효과음");audio.transform.SetParent(transform,false);sound=audio.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.minDistance=1;sound.maxDistance=6;
        }
        static Transform Part(Transform parent,Vector3 p,Vector3 size,Material mat)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;}
        void OnGUI()
        {
            if(!Playing || Time.timeScale<=0)return;
            var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.025f),alignment=TextAnchor.MiddleCenter};
            string line=Time.time<messageUntil?message:State==Phase.Choose?mission.dialogue.lockChoose:State==Phase.Turn?mission.dialogue.lockTurn:mission.dialogue.lockInsert;
            if(GuardLooking)line=mission.dialogue.lockWarning;
            GUI.Label(new Rect(Screen.width*.08f,Screen.height*.79f,Screen.width*.84f,70),line+"\nF · 즉시 중단하고 숨기",style);
        }
        void OnDisable(){End();}
        void OnDestroy(){if(skin)Destroy(skin);if(cloth)Destroy(cloth);if(metal)Destroy(metal);}
    }
}
