using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 씬에 저장한 경찰 외형과 기존 달리기 애니메이션을 재사용한다.
public sealed class PrologueOfficer : MonoBehaviour {
    public BankPrologue director;
    public Animator animator;
    public Transform gun;
    public Light muzzle;
    public PrologueRagdoll ragdoll;
    public PrologueGunFeedback gunFeedback;
    public ProloguePoliceBrain brain;
    public bool roadGuard;
    public bool Active { get; private set; }
    public bool Dead { get; private set; }
    public int Health { get; private set; } = 70;
    public int ShotsFired { get; private set; }
    NavMeshAgent agent;
    CapsuleCollider body;
    float nextShot, flashUntil, nextPath;
    float gunKick;
    bool aiming;
    public AudioClip entranceFootstep;
    public AudioClip entranceVoice;
    AudioSource entranceAudio;
    float nextEntranceStep;
    readonly RaycastHit[] sightHits = new RaycastHit[16];
    void Awake() {
        HeadHitbox.Attach(gameObject);
        agent = GetComponent<NavMeshAgent>(); body = GetComponent<CapsuleCollider>();
        agent.speed = Mathf.Max(agent.speed, 5.4f); agent.acceleration = Mathf.Max(agent.acceleration, 16);
        animator.SetFloat("RunRate", 1.55f);
    }
    public void Activate() {
        if (Dead) return;
        Active = true; gameObject.SetActive(true); agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out var hit, 3, NavMesh.AllAreas)) agent.Warp(hit.position);
        agent.stoppingDistance = 1.6f;
        nextShot = Time.time + 3.5f; nextPath = 0;
        if(brain)brain.ResetPlan();
    }
    public void StageEntrance(Vector3 destination) {
        Activate();
        if(!entranceAudio){entranceAudio=gameObject.AddComponent<AudioSource>();entranceAudio.playOnAwake=false;entranceAudio.spatialBlend=1;entranceAudio.minDistance=5;entranceAudio.maxDistance=30;}
        if(entranceVoice)entranceAudio.PlayOneShot(entranceVoice);
        nextEntranceStep=Time.time;
        agent.stoppingDistance = .3f;
        agent.SetDestination(destination);
    }
    public void Stop() {
        Active = false; aiming = false;
        if(brain)brain.Halt();
        // 아직 출동하지 않은 경찰은 Awake가 호출되지 않았을 수 있다.
        if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        if (animator && gameObject.activeInHierarchy) animator.SetFloat("MoveSpeed", 0);
    }
    void Update() {
        if(!gunFeedback&&muzzle)muzzle.enabled = Time.time < flashUntil;
        gunKick=Mathf.MoveTowards(gunKick,0,Time.deltaTime*35);
        if (Dead || (ragdoll && ragdoll.IsDown)){aiming=false;if(brain)brain.Suspend();return;}
        if(roadGuard){RushCar();return;}
        if (Active && director != null && director.Current == BankPrologue.Phase.Opening && agent.enabled){
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
            aiming=agent.remainingDistance<1;
            if(aiming){var look=director.player.transform.position-transform.position;look.y=0;if(look.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(look),160*Time.deltaTime);}
            if(!director.Paused&&agent.velocity.sqrMagnitude>.2f&&Time.time>=nextEntranceStep){
                nextEntranceStep=Time.time+.3f;
                if(entranceFootstep&&entranceAudio)entranceAudio.PlayOneShot(SuppliedAudio.Current&&SuppliedAudio.Current.boots.Length>0?SuppliedAudio.Current.boots[Random.Range(0,SuppliedAudio.Current.boots.Length)]:entranceFootstep,.45f*SuppliedAudio.Volume);
            }
        }
        if (!Active || director == null || !director.CanPlay || !agent.enabled || !agent.isOnNavMesh) return;
        var target = director.player;
        Vector3 delta = target.transform.position - transform.position;
        bool visible = ClearShot();
        aiming = visible && delta.magnitude < 14;
        agent.isStopped = aiming;
        agent.stoppingDistance = aiming ? 5 : .5f;
        if (!aiming && Time.time >= nextPath) { agent.SetDestination(target.transform.position); nextPath = Time.time + .3f; }
        animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
        if (!aiming) return;
        delta.y = 0;
        if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 180 * Time.deltaTime);
        if (Time.time < nextShot || Vector3.Angle(transform.forward, delta) > 15) return;
        nextShot = Time.time + 2.8f + Random.value;
        ShootAtPlayer();
    }
    void ShootAtPlayer(){
        var target=director.player;
        flashUntil = Time.time + .08f;
        ShotsFired++;
        gunKick=6;
        if(gunFeedback)gunFeedback.Fire();else GunshotSound.Play(gameObject, true);
        // 경찰 수가 많아져도 엄폐하며 빠져나갈 여유를 준다.
        bool hitPlayer=Random.value<.35f;
        var endpoint=target.view.transform.position+(hitPlayer?Vector3.zero:transform.right*Random.Range(.6f,1.1f)+Vector3.up*.3f);
        Vector3 origin=muzzle.transform.position;
        if(FirearmBallistics.Cast(origin,(endpoint-origin).normalized,Vector3.Distance(origin,endpoint),transform,out var wall)
            && !wall.transform.IsChildOf(transform) && !wall.transform.IsChildOf(target.transform)){
            endpoint=wall.point;hitPlayer=false;
        }
        PrologueTracer.Spawn(origin,endpoint,new Color(1,.3f,.12f));
        if (hitPlayer) target.ReceiveHit(8);
    }
    bool attackPending;
    public bool VehicleStruck {get;private set;}
    Vector3 retreatDirection;
    float retreatUntil;
    void RushCar(){
        if(!director||director.Paused||director.Current!=BankPrologue.Phase.Driving){animator.SetFloat("MoveSpeed",0);return;}
        if(VehicleStruck){
            if(agent.enabled)agent.enabled=false;
            if(retreatUntil==0)retreatUntil=Time.time+2;
            if(Time.time<retreatUntil){transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(retreatDirection),240*Time.deltaTime);transform.position+=retreatDirection*Time.deltaTime*2;animator.SetFloat("MoveSpeed",2);}else animator.SetFloat("MoveSpeed",0);
            return;
        }
        // 직선 도주 도로에서는 오래된 은행 NavMesh 범위에 묶이지 않게 평면 추격한다.
        Vector3 delta=director.getawayCar.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude>28*28||delta.sqrMagnitude<1){animator.SetFloat("MoveSpeed",0);return;}
        if(agent.enabled)agent.enabled=false;
        var direction=delta.normalized;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),360*Time.deltaTime);
        transform.position+=direction*(3.6f*Time.deltaTime);
        animator.SetFloat("MoveSpeed",3.6f);
    }
    public bool ClearShot() {
        if (director == null) return false;
        var from = transform.position + Vector3.up * (1.45f-(brain?brain.CrouchOffset:0)) + transform.forward * .5f;
        var delta = director.player.view.transform.position - from;
        // 컨트롤러가 레이에 잡히지 않아도 중간 장애물이 없으면 시야가 열린 상태다.
        int count = Physics.RaycastNonAlloc(from, delta.normalized, sightHits, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++) {
            var hit = sightHits[i].transform;
            if (!hit.IsChildOf(transform) && !hit.IsChildOf(director.player.transform)) return false;
        }
        return true;
    }
    void LateUpdate(){if(animator&&gun)ArmedHandPose.Apply(animator,gun,director.player.view.transform.position,aiming,Dead||(ragdoll&&ragdoll.IsDown));}
    public bool ReceiveHit(int amount) {
        if(!director||!director.player)return false;
        return ReceiveHit(amount,transform.position+Vector3.up*1.2f,(transform.position-director.player.transform.position).normalized);
    }
    public bool ReceiveHit(int amount,Vector3 point,Vector3 direction) {
        if (!Active || Dead || (ragdoll&&ragdoll.IsDown) || director == null || !director.CanPlay || amount <= 0) return false;
        Health = Mathf.Max(0, Health - amount);
        if (Health == 0) { Dead = true; Stop(); StartCoroutine(LeaveAfterKnockout()); }
        if(ragdoll){
            attackPending=false;
            ragdoll.Knock(direction.normalized*7+Vector3.up*2.2f,Dead);
            // 실제 맞은 부위에 추가 충격을 주어 어깨·몸통의 회전이 달라지게 한다.
            Rigidbody closest=null;float distance=float.MaxValue;
            foreach(var bone in ragdoll.bodies){float ds=(bone.worldCenterOfMass-point).sqrMagnitude;if(ds<distance){closest=bone;distance=ds;}}
            if(closest&&!closest.isKinematic)closest.AddForceAtPosition(direction.normalized*1.6f,point,ForceMode.VelocityChange);
        }
        if(director.player.combatFeedback)director.player.combatFeedback.Impact(point,-direction.normalized,true,Dead);
        return true;
    }
    // 배열 참조와 미션 진행은 보존하고, 기절한 뒤 화면에서 퇴장한다.
    System.Collections.IEnumerator LeaveAfterKnockout(){
        yield return new WaitForSeconds(4.5f);
        gameObject.SetActive(false);
    }
    public void VehicleHit(Vector3 velocity){
        if(VehicleStruck||Dead||!roadGuard||!director||director.Paused||director.Current!=BankPrologue.Phase.Driving||velocity.magnitude<3)return;
        VehicleStruck=true;Stop();
        Health=Mathf.Max(0,Health-Mathf.RoundToInt(velocity.magnitude*4));
        if(Health==0){Dead=true;StartCoroutine(LeaveAfterKnockout());}
        var car=director.getawayCar;float side=Vector3.Dot(transform.position-car.position,car.right)>=0?1:-1;
        retreatDirection=car.right*side;
        ragdoll.Knock(velocity.normalized*Mathf.Clamp(velocity.magnitude*.72f,7,15)+retreatDirection*2.4f+Vector3.up*3.2f,Dead);
        // 일어난 뒤까지 차량과의 충돌을 무시해 반복 충격과 바퀴에 깔림을 막는다.
        foreach(var bone in GetComponentsInChildren<Collider>())foreach(var vehicle in car.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(bone,vehicle);
    }
}
}
