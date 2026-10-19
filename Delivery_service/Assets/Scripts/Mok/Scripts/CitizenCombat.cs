using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok {
// 시민 프리팹에만 적용하며 경찰과 강도의 기존 동작은 유지한다.
[RequireComponent(typeof(NPCControl))]
public sealed class CitizenCombat : MonoBehaviour {
    public int maximumHealth = 100;
    public float chaseSpeed = 4.5f, punchRange = 2.8f, punchCooldown = 1.4f;
    public float attackStartRange=2.6f, combatSpacing=2.2f;
    public int punchDamage = 10;
    public float knockdownSeconds = 3f;
    public int Health { get; private set; }
    public float RobberyReadyAt { get; private set; } = float.NegativeInfinity;
    DayNightCycle clock;
    public float GameMinute => clock!=null?((clock.currentDay-1)*24f+clock.currentTime)*60f:0;
    public bool WasRobbed => clock!=null&&GameMinute<RobberyReadyAt;
    public bool CanBePickpocketed => GetComponent<SlumRobberCombat>()==null&&clock!=null&&clock.isShopOpen&&!WasRobbed&&Health>0&&!npc.IsDead&&!npc.IsKnockedDown;
    public bool IsAggressive => target != null && Health > 0 && !npc.IsDead;
    public bool IsPunching => punching;
    public NPCControl Npc => npc;
    NPCControl npc; NavMeshAgent agent; Animator animator; PlayerCombat target;
    float nextPunch, repath, punchStarted, oldSpeed, oldStopping; int oldMask;
    bool punching; Transform upper, lower, hand, leftUpper, leftLower, leftHand;
    void Awake() {
        npc=GetComponent<NPCControl>();agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();
        clock=FindFirstObjectByType<DayNightCycle>();Health=maximumHealth; oldSpeed=agent.speed;oldStopping=agent.stoppingDistance;oldMask=agent.areaMask;
        var bones=GetComponentsInChildren<Transform>();
        upper=bones.FirstOrDefault(t=>t.name=="Upper Arm.R");lower=bones.FirstOrDefault(t=>t.name=="Lower Arm.R");hand=bones.FirstOrDefault(t=>t.name=="Hand.R");
        leftUpper=bones.FirstOrDefault(t=>t.name=="Upper Arm.L");leftLower=bones.FirstOrDefault(t=>t.name=="Lower Arm.L");leftHand=bones.FirstOrDefault(t=>t.name=="Hand.L");
        if(animator!=null)animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    }
    public bool Pickpocket(PlayerCombat player) {
        if(!CanBePickpocketed || player==null || !player.CanAct || MoneyManager.Instance==null)return false;
        if(Vector3.Distance(player.transform.position,transform.position)>2.6f)return false;
        RobberyReadyAt=GameMinute+30f;int reward=Random.Range(100,5001);IncomeFeedback.AddPickpocketIncome(reward);ShopReputation.Instance.Penalize(false);
        Provoke(player);return true;
    }
    public void Provoke(PlayerCombat player) {
        if(Health<=0||npc.IsDead)return;
        target=player;npc.ExternalControl=true;agent.areaMask=NavMesh.AllAreas;agent.speed=chaseSpeed;agent.stoppingDistance=combatSpacing;nextPunch=Time.time+.5f;
    }
    public bool ReceiveKick(PlayerCombat player,Vector3 direction,int damage) {
        if(Health<=0||npc.IsDead||npc.IsKnockedDown)return false;
        Health=Mathf.Max(0,Health-damage);punching=false;Provoke(player);
        if(Health==0){target=null;npc.Die(direction,22f);if(GetComponent<SlumRobberCombat>()==null)CitizenCrime.ReportPlayerKill(npc);return true;}
        npc.KnockDown(direction,5f);StartCoroutine(GetUp());return true;
    }
    IEnumerator GetUp() {
        yield return new WaitForSeconds(knockdownSeconds);
        // 착지점이 일시적으로 NavMesh 밖이면 다시 시도한다.
        while(!npc.IsDead && !npc.RecoverFromKnockDown())yield return new WaitForSeconds(.5f);
        if(!npc.IsDead){nextPunch=Time.time+1f;repath=0;}
    }
    public void EndFight() {
        target=null;punching=false;npc.ExternalControl=false;
        if(agent!=null){agent.speed=oldSpeed;agent.stoppingDistance=oldStopping;agent.areaMask=oldMask;if(agent.enabled&&agent.isOnNavMesh){agent.isStopped=false;agent.ResetPath();}}
    }
    void Update() {
        if(npc.IsDead){punching=false;return;}
        if(!IsAggressive||npc.IsKnockedDown)return;
        if(target.IsDown){if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;return;}
        if(!agent.enabled||!agent.isOnNavMesh)return;
        float distance=Vector3.Distance(transform.position,target.transform.position);
        if(!punching && Time.time>=repath){repath=Time.time+.2f;agent.isStopped=false;
            if(NavMesh.SamplePosition(target.transform.position,out var dest,3f,NavMesh.AllAreas))agent.SetDestination(dest.position);
        }
        if(distance<punchRange+.4f){Vector3 dir=target.transform.position-transform.position;dir.y=0;if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(dir),540f*Time.deltaTime);}
        if(!punching&&distance<=attackStartRange&&Time.time>=nextPunch&&target.CanReceiveHit)StartCoroutine(Punch());
        if(animator!=null){animator.speed=1;if(GetComponent<SlumRobberCombat>()!=null)animator.SetFloat("MoveSpeed",!punching?agent.velocity.magnitude:0);else animator.SetBool("IsWalking",!punching&&agent.velocity.sqrMagnitude>.05f);}
    }
    IEnumerator Punch() {
        punching=true;punchStarted=Time.time;nextPunch=Time.time+punchCooldown;agent.isStopped=true;
        yield return new WaitForSeconds(.38f);
        if(punching&&!npc.IsKnockedDown&&!npc.IsDead&&target!=null&&target.CanReceiveHit){
            Vector3 start=transform.position+Vector3.up*1.2f,end=target.transform.position+Vector3.up*1.1f;
            Vector3 delta=end-start;
            if(delta.magnitude<=punchRange&&Vector3.Dot(transform.forward,delta.normalized)>.3f&&ClearLine(start,end))target.ReceivePunch(punchDamage,transform.forward,GetComponent<SlumRobberCombat>()!=null);
        }
        yield return new WaitForSeconds(.4f);punching=false;
        if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=false;
    }
    bool ClearLine(Vector3 from,Vector3 to) {
        return !Physics.RaycastAll(from,(to-from).normalized,(to-from).magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(target.transform));
    }
    // 기존 뼈대를 조정하며 팀원의 걷기 Animator와 컨트롤러는 교체하지 않는다.
    void LateUpdate(){
        if(!IsAggressive||npc.IsKnockedDown||npc.IsDead||animator==null||!animator.enabled)return;
        float t=punching?Time.time-punchStarted:0;
        float thrust=punching?(t<.28f?0:t<.40f?Mathf.InverseLerp(.28f,.40f,t):1-Mathf.InverseLerp(.40f,.72f,t)):0;
        AimArm(leftUpper,leftLower,leftHand,transform.position+transform.rotation*new Vector3(-.18f,1.35f,.27f),-1);
        AimArm(upper,lower,hand,transform.position+transform.rotation*new Vector3(.18f,1.3f,Mathf.Lerp(.18f,.85f,thrust)),1);
    }
    void AimArm(Transform a,Transform b,Transform c,Vector3 goal,float side){
        if(a==null||b==null||c==null)return;
        float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
        Vector3 d=goal-a.position;float distance=Mathf.Clamp(d.magnitude,.03f,l1+l2-.005f);Vector3 axis=d.normalized;
        float x=(l1*l1-l2*l2+distance*distance)/(2*distance),h=Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));
        Vector3 bend=Vector3.ProjectOnPlane(transform.right*side-Vector3.up*.7f,axis).normalized;
        Vector3 elbow=a.position+axis*x+bend*h;
        a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
        b.rotation=Quaternion.FromToRotation(c.position-b.position,goal-b.position)*b.rotation;
    }
}
}

