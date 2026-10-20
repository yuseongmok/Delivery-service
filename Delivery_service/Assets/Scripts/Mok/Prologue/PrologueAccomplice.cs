using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 공범은 수집과 동행만 담당하며 본편 시민·경제 시스템을 실행하지 않는다.
public sealed class PrologueAccomplice : MonoBehaviour {
    public BankPrologue director;
    public Animator animator;
    public Transform bag;
    public Transform[] collectionPoints;
    public Transform boardingPoint;
    public bool Boarded { get; private set; }
    public float CollectionProgress { get; private set; }
    public bool ReadyToBoard => !Boarded && Vector3.Distance(transform.position,boardingPoint.position)<2;
    NavMeshAgent agent;
    float work, nextPath;
    int route;
    Vector3 handTarget;
    bool working;
    void Awake(){
        agent=GetComponent<NavMeshAgent>();
        animator.applyRootMotion=false;
        animator.SetFloat("RunRate",1.25f);
    }
    void Update(){
        if(!director||director.Paused||Boarded)return;
        working=false;
        if(director.Current==BankPrologue.Phase.Opening){
            working=true;handTarget=director.vaultDoor.TransformPoint(new Vector3(2.1f,-.6f,-.3f));
        }else if(director.Current==BankPrologue.Phase.Robbery){
            int i=director.Collected;
            if(i<collectionPoints.Length){
                Vector3 point=collectionPoints[i].position;
                Move(point);
                if(Vector3.Distance(transform.position,point)<.65f){
                    agent.isStopped=true;working=true;handTarget=director.cash[i].transform.position;
                    Face(handTarget);work+=Time.deltaTime;
                    CollectionProgress=Mathf.Clamp01(work/Mathf.Max(1,director.story.cashBundleSeconds));
                    if(CollectionProgress>=1&&director.SecureCash(i,this)){work=0;CollectionProgress=0;}
                }
            }
        }else if(director.Current==BankPrologue.Phase.Escape){
            // 같은 통로를 순서대로 통과하므로 플레이어를 따라 벽을 가로지르지 않는다.
            if(route<director.escapeRoute.Length){
                var goal=director.escapeRoute[route].position;
                if(Vector3.Distance(transform.position,goal)<1.4f)route++;
                else Move(goal);
            }else Move(boardingPoint.position);
        }else if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;
        animator.SetFloat("MoveSpeed",agent.enabled&&agent.isOnNavMesh?agent.velocity.magnitude:0);
    }
    void Move(Vector3 goal){
        if(!agent.enabled||!agent.isOnNavMesh)return;
        agent.isStopped=false;
        agent.speed=director.Current==BankPrologue.Phase.Escape?6:3;
        if(Time.time>=nextPath){agent.SetDestination(goal);nextPath=Time.time+.2f;}
    }
    void Face(Vector3 goal){
        var v=goal-transform.position;v.y=0;
        if(v.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(v),240*Time.deltaTime);
    }
    void LateUpdate(){
        if(!working||!animator.isHuman||Boarded)return;
        // 기존 걷기 리그 위에 금고 조작·돈 담기 손동작을 더한다.
        foreach(var side in new[]{HumanBodyBones.RightUpperArm,HumanBodyBones.LeftUpperArm}){
            var arm=animator.GetBoneTransform(side);
            var hand=animator.GetBoneTransform(side==HumanBodyBones.RightUpperArm?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
            Vector3 target=handTarget+Vector3.up*(.08f*Mathf.Sin(Time.time*5));
            arm.rotation=Quaternion.FromToRotation(hand.position-arm.position,target-arm.position)*arm.rotation;
        }
    }
    public void Board(){
        Boarded=true;agent.enabled=false;
        transform.SetParent(director.getawayCar,true);
        transform.localPosition=new Vector3(.45f,.25f,0);
        transform.localRotation=Quaternion.identity;
        // 차체 밖으로 서 있는 몸이 비치지 않게 탑승 후 외형을 숨긴다.
        foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;
    }
}
}
