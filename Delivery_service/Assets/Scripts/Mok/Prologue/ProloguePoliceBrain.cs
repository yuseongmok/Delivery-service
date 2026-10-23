using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 이동은 NavMesh로만 처리하며, 시야 밖 플레이어의 현재 위치를 추적하지 않는다.
[DefaultExecutionOrder(-20)]
public sealed class ProloguePoliceBrain : MonoBehaviour {
    public enum Action { Approach, Flank, ToCover, Hidden, Peek, Aim, Search, Recover }
    public PrologueOfficer officer;
    public ProloguePoliceSquad squad;
    public bool flanker;
    public int side=1;
    public Action Current {get;private set;}
    public Vector3 LastSeen {get;private set;}
    public int CoverIndex {get;private set;}=-1;
    public int CoverVisits {get;private set;}
    public int FlankMoves {get;private set;}
    public int Bursts {get;private set;}
    public float CrouchOffset=>crouch*.48f;
    NavMeshAgent agent;
    CapsuleCollider capsule;
    NavMeshPath path;
    float height;Vector3 center;
    float senseAt,seenAt,stateUntil,planAt,aimAt,nextShot,burstAt,crouch;
    int remaining;
    bool visible,suspended;
    Vector3 destination;
    void Awake(){agent=GetComponent<NavMeshAgent>();capsule=GetComponent<CapsuleCollider>();height=capsule.height;center=capsule.center;path=new NavMeshPath();}
    public void ResetPlan(){
        Halt();LastSeen=officer.director.player.transform.position;seenAt=Time.time;
        senseAt=0;planAt=Time.time+.3f;nextShot=Time.time+2.5f;Current=Action.Approach;suspended=false;
    }
    public void Halt(){
        if(squad)squad.Release(this);CoverIndex=-1;remaining=0;visible=false;
        if(capsule){capsule.height=height;capsule.center=center;}crouch=0;
    }
    public void Suspend(){if(suspended)return;Halt();suspended=true;Current=Action.Recover;}
    bool Move(Vector3 goal,Action action){
        if(!NavMesh.SamplePosition(goal,out var nav,.7f,NavMesh.AllAreas)||!agent.CalculatePath(nav.position,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
        destination=nav.position;agent.stoppingDistance=.18f;agent.isStopped=false;agent.SetPath(path);Current=action;stateUntil=Time.time+7;return true;
    }
    bool Arrived=>!agent.pathPending&&Vector3.Distance(transform.position,destination)<.45f;
    void Choose(){
        squad.Release(this);CoverIndex=-1;planAt=Time.time+2;
        if(Time.time-seenAt>6){if(!Move(LastSeen,Action.Search)){agent.isStopped=true;Current=Action.Search;stateUntil=Time.time+1;}return;}
        if(!flanker){
            CoverIndex=squad.Claim(this,agent,LastSeen,path);
            if(CoverIndex>=0&&Move(squad.hiding[CoverIndex].position,Action.ToCover))return;
            squad.Release(this);CoverIndex=-1;
        }
        var away=transform.position-LastSeen;away.y=0;if(away.sqrMagnitude<.1f)away=transform.forward;
        var lateral=Vector3.Cross(Vector3.up,away.normalized)*side;
        if(flanker&&Move(LastSeen+away.normalized*6+lateral*4,Action.Flank)){FlankMoves++;return;}
        if(!Move(LastSeen,Time.time-seenAt>4?Action.Search:Action.Approach)){agent.isStopped=true;Current=Action.Search;stateUntil=Time.time+1;}
    }
    void Aim(){Current=Action.Aim;agent.isStopped=true;aimAt=Time.time+.75f;stateUntil=Time.time+3.5f;remaining=0;}
    void Hide(){
        remaining=0;
        if(CoverIndex>=0&&Move(squad.hiding[CoverIndex].position,Action.ToCover))return;
        Choose();
    }
    public bool Tick(){
        if(suspended){suspended=false;Current=Action.Recover;stateUntil=Time.time+.8f;agent.isStopped=true;return false;}
        if(Current==Action.Recover){if(Time.time<stateUntil)return false;Choose();}
        if(Time.time>=senseAt){
            senseAt=Time.time+.22f;visible=officer.ClearShot();
            if(visible){LastSeen=officer.director.player.transform.position;seenAt=Time.time;}
        }
        if(Current==Action.Hidden){
            agent.isStopped=true;
            if(Time.time-seenAt>6||Vector3.Distance(LastSeen,squad.peeking[CoverIndex].position)>20){Choose();return false;}
            if(Time.time>=stateUntil){if(!Move(squad.peeking[CoverIndex].position,Action.Peek))Choose();}
            return false;
        }
        if(Current==Action.ToCover){
            if(Arrived){Current=Action.Hidden;agent.isStopped=true;stateUntil=Time.time+Random.Range(1.1f,1.9f);CoverVisits++;}
            else if(Time.time>stateUntil)Choose();
            return false;
        }
        if(Current==Action.Peek){if(Arrived)Aim();else if(Time.time>stateUntil)Choose();return false;}
        if(Current==Action.Aim){
            if(!visible){remaining=0;aimAt=Time.time+.75f;}
            if(Time.time>=stateUntil){if(CoverIndex>=0)Hide();else Choose();return false;}
            return visible&&!officer.director.player.KnockedDown;
        }
        float range=Vector3.Distance(transform.position,LastSeen);
        if(visible&&range<15&&(Current!=Action.Flank||Arrived||range<4)){Aim();return true;}
        if(Current==Action.Search&&Arrived){agent.isStopped=true;transform.Rotate(0,35*Time.deltaTime,0);}
        if(Time.time>=planAt&&(Arrived||Time.time>=stateUntil||Current==Action.Approach))Choose();
        return false;
    }
    public bool TryFire(){
        if(Current!=Action.Aim||!visible||Time.time<aimAt||Time.time<nextShot||officer.director.player.KnockedDown||!officer.director.CanPlay)return false;
        if(remaining==0){if(Time.time<burstAt)return false;remaining=2;Bursts++;}
        if(!squad.PermitShot())return false;
        remaining--;nextShot=Time.time+.32f;
        if(remaining==0){burstAt=Time.time+Random.Range(2.3f,3.3f);stateUntil=Mathf.Min(stateUntil,Time.time+.4f);}
        return true;
    }
    void LateUpdate(){
        if(!officer.Active||officer.Dead||officer.director.Paused||(officer.ragdoll&&officer.ragdoll.IsDown)||!officer.animator.isHuman)return;
        crouch=Mathf.MoveTowards(crouch,Current==Action.Hidden?1:0,Time.deltaTime*5);
        capsule.height=height-.48f*crouch;capsule.center=center-Vector3.up*.24f*crouch;
        if(crouch<=0)return;
        var anim=officer.animator;var left=anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;var right=anim.GetBoneTransform(HumanBodyBones.RightFoot).position;
        anim.GetBoneTransform(HumanBodyBones.Hips).position-=Vector3.up*.48f*crouch;
        Leg(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,left);
        Leg(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,right);
    }
    void Leg(HumanBodyBones a,HumanBodyBones b,HumanBodyBones c,Vector3 goal){
        var anim=officer.animator;var upper=anim.GetBoneTransform(a);var lower=anim.GetBoneTransform(b);var foot=anim.GetBoneTransform(c);
        Vector3 origin=upper.position;float l1=Vector3.Distance(origin,lower.position),l2=Vector3.Distance(lower.position,foot.position);
        var direction=(goal-origin).normalized;float distance=Mathf.Clamp(Vector3.Distance(origin,goal),.02f,l1+l2-.005f);
        float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
        var bend=Vector3.ProjectOnPlane(transform.forward,direction).normalized;
        var knee=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,knee-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,goal-lower.position)*lower.rotation;
    }
    void OnDisable(){Halt();}
}
}
