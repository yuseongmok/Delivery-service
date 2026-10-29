using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 기존 휴머노이드에 손·팔 IK와 소품 동작을 얹어 장면 전환을 연결한다.
[DefaultExecutionOrder(100)]
public sealed class ProloguePartnerPerformance : MonoBehaviour {
    public PrologueAccomplice partner;
    public Transform wheel, bagLid, bagMouth, handCash, shoulderMount;
    public Transform[] vaultBolts;
    public Transform passengerDoor, doorHandle, passengerSeat, passengerStand;
    public GameObject shoulderStraps;
    public GameObject[] bagContents;
    public AudioClip vaultLatch, vaultSwing, paperRustle, zipper, cloth, doorOpen, doorClose;
    public bool Busy {get;private set;}
    public bool BagCarried {get;private set;}
    public bool EnteredCar {get;private set;}
    public int DepositedBundles {get;private set;}
    public int SoundEvents {get;private set;}
    BankPrologue D=>partner.director;
    Animator Anim=>partner.animator;
    AudioSource audioSource;
    Vector3 rightTarget,leftTarget;
    float rightWeight,leftWeight,lean;
    int station=-1,lastPickup=-1,lastDeposit=-1;
    bool prepared,audioPaused;
    float duckUntil,nextStartle,duck,lookUntil,nextLook,beckonUntil,nextBeckon;
    public int StartleCount {get;private set;}
    public int BeckonCount {get;private set;}
    void OnEnable(){PrologueGunFeedback.ShotHeard+=HearShot;}
    void OnDisable(){PrologueGunFeedback.ShotHeard-=HearShot;}
    void HearShot(Vector3 position){
        if(!partner||!D||!D.CanPlay||partner.Boarded||Busy||Time.time<nextStartle)return;
        if((position-transform.position).sqrMagnitude>100)return;
        duckUntil=Time.time+.65f;nextStartle=Time.time+2.8f;StartleCount++;
    }
    bool grippingWheel,handlingCash;
    Vector3 cashPosition;
    Vector3 WheelGrip(float side)=>wheel.TransformPoint(new Vector3(side*.365f,0,-.065f));
    void Awake(){
        var bank=SuppliedAudio.Current;
        if(bank){vaultLatch=bank.vaultLatch;vaultSwing=bank.vaultSwing;paperRustle=bank.bag;zipper=bank.bag;doorOpen=bank.carOpen;doorClose=bank.carClose;}
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
        audioSource.spatialBlend=1;audioSource.minDistance=1.5f;audioSource.maxDistance=22;audioSource.rolloffMode=AudioRolloffMode.Linear;
        handCash.gameObject.SetActive(false);
    }
    void Update(){
        if(partner&&D&&!D.Paused){
            bool reacting=D.CanPlay&&!partner.Boarded&&!Busy;
            duck=Mathf.MoveTowards(duck,reacting&&Time.time<duckUntil?1:0,Time.deltaTime*5);
            // 돈을 내려놓은 틈에만 동료를 확인한다.
            if(reacting&&D.Current==BankPrologue.Phase.Robbery&&handlingCash&&Time.time>=nextLook&&
                (partner.CollectionProgress*5)%1>.84f){lookUntil=Time.time+.8f;nextLook=Time.time+5;}
            if(reacting&&D.Current==BankPrologue.Phase.Escape&&Time.time>=nextBeckon&&duck<.1f){
                float distance=Vector3.Distance(transform.position,D.player.transform.position);
                var agent=GetComponent<NavMeshAgent>();
                if(distance>1.3f&&distance<8&&agent.enabled&&agent.velocity.sqrMagnitude<.25f){
                    beckonUntil=Time.time+1.1f;lookUntil=beckonUntil;nextBeckon=Time.time+7;BeckonCount++;
                }
            }
        }
        if(D.Paused!=audioPaused){audioPaused=D.Paused;if(audioPaused)audioSource.Pause();else audioSource.UnPause();}
        if(D.Current==BankPrologue.Phase.Failed){StopAllCoroutines();Busy=false;rightWeight=leftWeight=lean=0;audioSource.Stop();handCash.gameObject.SetActive(false);}
    }
    AudioClip lastSound;float lastSoundAt;
    void Sound(AudioClip clip){if(!clip)return;
        // 같은 소품 접촉이 한 프레임 안에 중복 호출되는 경우만 걸러낸다.
        if(clip==lastSound&&Time.time-lastSoundAt<.12f)return;
        lastSound=clip;lastSoundAt=Time.time;
        float gain=clip==paperRustle?.28f:clip==doorOpen||clip==doorClose?.32f:.45f;
        StorySoundMix.Play(audioSource,clip,gain);SoundEvents++;
    }
    static float Smooth(float p)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(p));
    IEnumerator Motion(float seconds,System.Action<float> step){
        for(float t=0;t<seconds;t+=Time.deltaTime){
            if(D.Current==BankPrologue.Phase.Failed)yield break;
            if(D.IsCinematic&&D.CinematicSkipRequested)break;
            step(Smooth(t/seconds));yield return null;
        }
        step(1);
    }
    public IEnumerator OpenVault(){
        Busy=true;var agent=GetComponent<NavMeshAgent>();agent.enabled=false;
        Anim.SetFloat("MoveSpeed",0);
        Vector3 grip=wheel.position;var start=transform.position;
        Vector3 stand=new Vector3(grip.x,0,grip.z-.43f);
        grippingWheel=true;
        yield return Motion(.7f,p=>{transform.position=Vector3.Lerp(start,stand,p);transform.rotation=Quaternion.identity;
            rightTarget=WheelGrip(1);leftTarget=WheelGrip(-1);rightWeight=leftWeight=p;lean=5*p;});
        Sound(vaultLatch);
        yield return Motion(1.4f,p=>{
            wheel.localRotation=Quaternion.Euler(0,0,-105*p);
            rightTarget=WheelGrip(1);leftTarget=WheelGrip(-1);
        });
        // 손잡이 회전이 끝나면 잠금봉을 문 안쪽으로 회수한다.
        if(vaultBolts!=null&&vaultBolts.Length>0){
            var starts=new Vector3[vaultBolts.Length];
            for(int i=0;i<starts.Length;i++)starts[i]=vaultBolts[i].localPosition;
            yield return Motion(.45f,p=>{for(int i=0;i<starts.Length;i++)
                vaultBolts[i].localPosition=starts[i]-vaultBolts[i].localRotation*Vector3.up*(.22f*p);});
        }
        // 잠금만 해제하고 손을 뗀 뒤 물러난다. 문 회전에는 동료를 연결하지 않는다.
        yield return Motion(.3f,p=>{rightWeight=leftWeight=1-p;lean=5*(1-p);});
        grippingWheel=false;
        Vector3 release=transform.position;
        Vector3 retreat=release-transform.forward*.85f;
            Anim.SetFloat("RunRate", -0.7f);
            Anim.SetFloat("MoveSpeed",1f);
        yield return Motion(.65f,p=>transform.position=Vector3.Lerp(release,retreat,p));
            Anim.SetFloat("RunRate", 1.25f);
            Anim.SetFloat("MoveSpeed",0);
        Sound(vaultSwing);
        yield return Motion(2.4f,p=>D.vaultDoor.localRotation=Quaternion.Euler(0,-105*p,0));
        if(NavMesh.SamplePosition(transform.position,out var hit,2,NavMesh.AllAreas))transform.position=hit.position;
        agent.enabled=true;agent.Warp(transform.position);Busy=false;
    }
    public bool PrepareCash(int index){
        if(prepared&&station==index)return true;
        if(!Busy)StartCoroutine(PlaceBag(index));
        return false;
    }
    public IEnumerator ReactToAlarm(){
        Busy=true;var agent=GetComponent<NavMeshAgent>();
        if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;
        Anim.SetFloat("MoveSpeed",0);var start=transform.rotation;
        var direction=D.shots[2].position-transform.position;direction.y=0;
        var look=direction.sqrMagnitude>.01f?Quaternion.LookRotation(direction):start;
        yield return Motion(.25f,p=>lean=-3*p);
        yield return Motion(.55f,p=>{transform.rotation=Quaternion.Slerp(start,look,p);lean=-3*(1-p);});
        yield return Motion(.35f,p=>{});
        Busy=false;
    }
    IEnumerator PlaceBag(int index){
        Busy=true;prepared=false;station=index;
        Anim.SetFloat("MoveSpeed",0);
        var bag=partner.bag;bag.SetParent(null,true);
        Vector3 from=bag.position, to=D.cash[index].transform.position+new Vector3(-.6f,-.07f,0);
        Quaternion rotation=bag.rotation;
        Sound(cloth);
        yield return Motion(.9f,p=>{
            bag.position=Vector3.Lerp(from,to,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.15f);
            bag.rotation=Quaternion.Slerp(rotation,Quaternion.identity,p);
            rightTarget=bag.position+Vector3.up*.15f;leftTarget=bag.position+Vector3.left*.2f;
            rightWeight=leftWeight=1;lean=15*p;
        });
        Sound(zipper);yield return Motion(.45f,p=>bagLid.localRotation=Quaternion.Euler(-105*p,0,0));
        prepared=true;Busy=false;handlingCash=true;
    }
    public void WorkCash(int index,float progress){
        float cycles=Mathf.Min(4.999f,progress*5);int bundle=Mathf.FloorToInt(cycles);float p=cycles-bundle;
        int key=index*5+bundle;
        Transform pile=D.cash[index].transform;
        int child=(4-bundle)*2;
        Vector3 source=pile.GetChild(child).position+Vector3.up*.025f;
        Vector3 above=bagMouth.position+Vector3.up*.17f;
        Vector3 inside=bagMouth.position-Vector3.up*.08f;
        // 접근 → 집기 → 들어 올리기 → 가방 안으로 내리기 → 손 빼기.
        if(p<.25f)cashPosition=Vector3.Lerp(above,source,Smooth(p/.25f));
        else if(p<.34f)cashPosition=source;
        else if(p<.65f){float t=Smooth((p-.34f)/.31f);cashPosition=Vector3.Lerp(source,above,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.12f);}
        else if(p<.83f)cashPosition=Vector3.Lerp(above,inside,Smooth((p-.65f)/.18f));
        else cashPosition=Vector3.Lerp(inside,above,Smooth((p-.83f)/.17f));
        rightTarget=cashPosition+Vector3.up*.055f;
        leftTarget=bagMouth.position-transform.right*.22f;rightWeight=leftWeight=1;
        lean=9+5*Mathf.Sin(p*Mathf.PI);
        if(p>=.34f&&lastPickup!=key){
            lastPickup=key;pile.GetChild(child).gameObject.SetActive(false);pile.GetChild(child+1).gameObject.SetActive(false);Sound(paperRustle);
        }
        handCash.gameObject.SetActive(p>=.34f&&p<.83f);
        if(p>=.83f&&lastDeposit!=key){
            lastDeposit=key;DepositedBundles++;Sound(paperRustle);
            if(bagContents.Length>0)bagContents[Mathf.Min(bagContents.Length-1,DepositedBundles-1)].SetActive(true);
        }
    }
    public IEnumerator PackAndCarry(){
        Busy=true;handlingCash=false;handCash.gameObject.SetActive(false);Sound(zipper);
        yield return Motion(.65f,p=>{bagLid.localRotation=Quaternion.Euler(-105*(1-p),0,0);
            rightTarget=bagMouth.position+Vector3.Lerp(Vector3.left*.2f,Vector3.right*.2f,p);leftTarget=bagMouth.position;});
        var bag=partner.bag;Vector3 start=bag.position;Quaternion rotation=bag.rotation;Sound(cloth);
        yield return Motion(1.1f,p=>{
            bag.position=Vector3.Lerp(start,shoulderMount.position,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.2f);
            bag.rotation=Quaternion.Slerp(rotation,shoulderMount.rotation,p);
            rightTarget=bag.position+Vector3.up*.12f;leftWeight=1-p;lean=12*(1-p);
        });
        bag.SetParent(shoulderMount,false);bag.localPosition=Vector3.zero;bag.localRotation=Quaternion.identity;
        yield return Motion(.3f,p=>rightWeight=1-p);
        BagCarried=true;Busy=false;
    }
    public void CarryToNextPile(){StartCoroutine(CarryBriefly());}
    IEnumerator CarryBriefly(){
        Busy=true;handlingCash=false;handCash.gameObject.SetActive(false);Sound(cloth);
        var bag=partner.bag;var start=bag.position;
        var hand=Anim.GetBoneTransform(HumanBodyBones.LeftHand);
        yield return Motion(.6f,p=>{bag.position=Vector3.Lerp(start,transform.position-transform.right*.35f+Vector3.up*.65f,p);rightWeight=1-p;leftWeight=1;leftTarget=bag.position+Vector3.up*.25f;lean=12*(1-p);});
        bag.SetParent(hand,true);rightWeight=leftWeight=0;Busy=false;
    }
    public IEnumerator EnterVehicle(){
        Busy=true;var agent=GetComponent<NavMeshAgent>();agent.enabled=false;
        Anim.SetFloat("MoveSpeed",0);
        Vector3 start=transform.position;Quaternion startRotation=transform.rotation;
        yield return Motion(.6f,p=>{transform.position=Vector3.Lerp(start,passengerStand.position,p);
            transform.rotation=Quaternion.Slerp(startRotation,passengerStand.rotation,p);
            rightTarget=doorHandle.position;rightWeight=p;});
        Sound(doorOpen);
        yield return Motion(.8f,p=>{passengerDoor.localRotation=Quaternion.Euler(0,-68*p,0);rightTarget=doorHandle.position;lean=8*p;
            transform.position=passengerStand.position+D.getawayCar.right*(.65f*p);});
        start=transform.position;startRotation=transform.rotation;Sound(cloth);
        yield return Motion(1.3f,p=>{
            transform.position=Vector3.Lerp(start,passengerSeat.position,p);
            transform.rotation=Quaternion.Slerp(startRotation,passengerSeat.rotation,p);
            rightWeight=1-p;lean=35*Mathf.Sin(Mathf.PI*p);
            // 허벅지와 종아리를 접으며 좌석으로 몸을 낮춘다.
            seatBend=p;
        });
        yield return Motion(.65f,p=>{passengerDoor.localRotation=Quaternion.Euler(0,-68*(1-p),0);});
        Sound(doorClose);EnteredCar=true;Busy=false;rightWeight=leftWeight=lean=0;
        partner.Board();
    }
    float seatBend;
    void LateUpdate(){
        if(!Anim||!Anim.isHuman)return;
        if(partner.Boarded){foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})Anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(-75,0,0);foreach(var bone in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})Anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(85,0,0);return;}
        if(shoulderStraps)shoulderStraps.SetActive(partner.bag.parent==shoulderMount);
        var chest=Anim.GetBoneTransform(HumanBodyBones.Chest);if(chest)chest.localRotation*=Quaternion.Euler(lean+duck*13,0,0);
        // 이동 루트는 유지하고 상체만 낮춰 작업과 동행을 끊지 않는다.
        var hips=Anim.GetBoneTransform(HumanBodyBones.Hips);if(hips&&duck>0)hips.position-=Vector3.up*(duck*.09f);
        // 서로 다른 리그의 가슴 뼈 축에 상관없이 가방은 등 쪽에 밀착시킨다.
        if(chest){
            shoulderMount.SetPositionAndRotation(chest.position-transform.forward*.28f-Vector3.up*.28f,transform.rotation);
            if(shoulderStraps)shoulderStraps.transform.SetPositionAndRotation(chest.position-Vector3.up*.02f,transform.rotation);
        }
        Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,rightTarget,rightWeight,1);
        Arm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,leftTarget,leftWeight,-1);
        if(!Busy&&D.CanPlay){
            if(Time.time<lookUntil){
                var head=Anim.GetBoneTransform(HumanBodyBones.Head);
                if(head){
                    var local=transform.InverseTransformDirection(D.player.view.transform.position-head.position);
                    float yaw=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-55,55);
                    float weight=Mathf.Clamp01((lookUntil-Time.time)*4);
                    head.rotation=Quaternion.AngleAxis(yaw*weight,transform.up)*head.rotation;
                }
            }
            if(D.Current==BankPrologue.Phase.Escape&&Time.time<beckonUntil&&duck<.2f){
                float p=1-(beckonUntil-Time.time)/1.1f;
                float weight=Mathf.Sin(Mathf.Clamp01(p)*Mathf.PI);
                Vector3 gesture=transform.position+Vector3.up*1.4f+transform.right*.28f+transform.forward*(.33f+.14f*Mathf.Sin(p*Mathf.PI*4));
                Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,gesture,weight,1);
            }
        }
        if(grippingWheel||handlingCash){
            Vector3 fingers=grippingWheel?wheel.forward:transform.forward-Vector3.up*.65f;
            AimFingers(HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,fingers,rightWeight);
            AimFingers(HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,fingers,leftWeight);
        }
        // 돈은 손목 뼈 축을 따라 뒤집히지 않고 수평을 유지한다.
        if(handCash.gameObject.activeSelf)handCash.SetPositionAndRotation(cashPosition,Quaternion.LookRotation(transform.forward,Vector3.up));
        if(seatBend>0){
            foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})Anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(-75*seatBend,0,0);
            foreach(var bone in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})Anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(85*seatBend,0,0);
        }
    }
    void AimFingers(HumanBodyBones wrist,HumanBodyBones finger,Vector3 direction,float weight){
        var hand=Anim.GetBoneTransform(wrist);var tip=Anim.GetBoneTransform(finger);
        if(!hand||!tip||weight<=0)return;
        var current=tip.position-hand.position;
        if(current.sqrMagnitude>.00001f)hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(current,direction)*hand.rotation,weight);
    }
    void Arm(HumanBodyBones a,HumanBodyBones b,HumanBodyBones c,Vector3 goal,float weight,float side){
        if(weight<=0)return;
        var upper=Anim.GetBoneTransform(a);var lower=Anim.GetBoneTransform(b);var hand=Anim.GetBoneTransform(c);
        goal=Vector3.Lerp(hand.position,goal,weight);
        Vector3 origin=upper.position;float l1=Vector3.Distance(origin,lower.position),l2=Vector3.Distance(lower.position,hand.position);
        Vector3 direction=(goal-origin).normalized;float distance=Mathf.Clamp(Vector3.Distance(origin,goal),.02f,l1+l2-.005f);
        float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
        var bend=Vector3.ProjectOnPlane(transform.right*side-Vector3.up*.65f,direction).normalized;
        Vector3 elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,origin+direction*distance-lower.position)*lower.rotation;
    }
}
}
