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
    public bool roadGuard;
    public bool Active { get; private set; }
    public bool Dead { get; private set; }
    public int Health { get; private set; } = 70;
    public int ShotsFired { get; private set; }
    NavMeshAgent agent;
    CapsuleCollider body;
    float nextShot, flashUntil, nextPath;
    bool aiming;
    readonly RaycastHit[] sightHits = new RaycastHit[16];
    void Awake() {
        agent = GetComponent<NavMeshAgent>(); body = GetComponent<CapsuleCollider>();
        animator.SetFloat("RunRate", 1.25f);
    }
    public void Activate() {
        if (Dead) return;
        Active = true; gameObject.SetActive(true); agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out var hit, 3, NavMesh.AllAreas)) agent.Warp(hit.position);
        agent.stoppingDistance = 5;
        nextShot = Time.time + 3.5f; nextPath = 0;
    }
    public void StageEntrance(Vector3 destination) {
        Activate();
        agent.stoppingDistance = .3f;
        agent.SetDestination(destination);
    }
    public void Stop() {
        Active = false; aiming = false;
        // 아직 출동하지 않은 경찰은 Awake가 호출되지 않았을 수 있다.
        if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        if (animator && gameObject.activeInHierarchy) animator.SetFloat("MoveSpeed", 0);
    }
    void Update() {
        muzzle.enabled = Time.time < flashUntil;
        if (Dead || (ragdoll && ragdoll.IsDown)) return;
        if(roadGuard){RushCar();return;}
        if (Active && director != null && director.Current == BankPrologue.Phase.Opening && agent.enabled)
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
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
        flashUntil = Time.time + .08f;
        ShotsFired++;
        GunshotSound.Play(gameObject, true);
        // 경찰 수가 많아져도 엄폐하며 빠져나갈 여유를 준다.
        bool hitPlayer=Random.value<.35f;
        var endpoint=target.view.transform.position+(hitPlayer?Vector3.zero:transform.right*Random.Range(.6f,1.1f)+Vector3.up*.3f);
        Vector3 origin=muzzle.transform.position;
        if(Physics.Raycast(origin,(endpoint-origin).normalized,out var wall,Vector3.Distance(origin,endpoint),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
            && !wall.transform.IsChildOf(transform) && !wall.transform.IsChildOf(target.transform)){
            endpoint=wall.point;hitPlayer=false;
        }
        PrologueTracer.Spawn(origin,endpoint,new Color(1,.3f,.12f));
        if (hitPlayer) target.ReceiveHit(8);
    }
    void RushCar(){
        if(!director||director.Paused||director.Current!=BankPrologue.Phase.Driving){animator.SetFloat("MoveSpeed",0);return;}
        // 직선 도주 도로에서는 오래된 은행 NavMesh 범위에 묶이지 않게 평면 추격한다.
        Vector3 delta=director.getawayCar.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude>38*38||delta.sqrMagnitude<1){animator.SetFloat("MoveSpeed",0);return;}
        if(agent.enabled)agent.enabled=false;
        var direction=delta.normalized;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),360*Time.deltaTime);
        transform.position+=direction*(5.2f*Time.deltaTime);
        animator.SetFloat("MoveSpeed",5.2f);
    }
    public bool ClearShot() {
        if (director == null) return false;
        var from = transform.position + Vector3.up * 1.45f + transform.forward * .5f;
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
    void LateUpdate() {
        if (!animator.isHuman || Dead || (ragdoll && ragdoll.IsDown)) return;
        var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (aiming) {
            var upper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var dir = (director.player.view.transform.position - upper.position).normalized;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, dir) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, dir) * lower.rotation;
        }
        gun.SetPositionAndRotation(hand.position, aiming ? Quaternion.LookRotation(director.player.view.transform.position - hand.position) : transform.rotation);
    }
    public bool ReceiveHit(int amount) {
        if (!Active || Dead || director == null || !director.CanPlay || amount <= 0) return false;
        Health = Mathf.Max(0, Health - amount);
        if (Health == 0) { Dead = true; Stop(); }
        if(ragdoll)ragdoll.Knock((transform.position-director.player.transform.position).normalized*3+Vector3.up*1.5f,Dead);
        return true;
    }
    public void VehicleHit(Vector3 velocity){
        if(Dead||!roadGuard||director.Current!=BankPrologue.Phase.Driving)return;
        Dead=true;Health=0;Stop();ragdoll.Knock(velocity,true);
    }
}
}
