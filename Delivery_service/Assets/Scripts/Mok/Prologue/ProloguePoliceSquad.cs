using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 엄폐 위치를 한 명씩 예약하고 동시 사격량을 제한한다.
public sealed class ProloguePoliceSquad : MonoBehaviour {
    public Transform[] hiding, peeking;
    ProloguePoliceBrain[] owners;
    readonly RaycastHit[] hits=new RaycastHit[32];
    readonly float[] shots=new float[2];
    BankPrologue director;
    void Awake(){owners=new ProloguePoliceBrain[hiding.Length];shots[0]=shots[1]=-10;}
    public void Release(ProloguePoliceBrain owner){if(owners==null)return;for(int i=0;i<owners.Length;i++)if(owners[i]==owner)owners[i]=null;}
    public bool PermitShot(){
        if(!director)director=GetComponent<BankPrologue>();
        float interval=director&&director.CanPlay?director.SquadShotInterval:.65f;
        for(int i=0;i<shots.Length;i++)if(Time.time-shots[i]>=interval){shots[i]=Time.time;return true;}
        return false;
    }
    public bool Blocked(Vector3 from,Vector3 to){
        var delta=to-from;int count=Physics.RaycastNonAlloc(from,delta.normalized,hits,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(count==hits.Length)return true;
        for(int i=0;i<count;i++){
            var c=hits[i].collider;
            if(c.GetComponentInParent<PrologueOfficer>()||c.GetComponentInParent<ProloguePlayer>()||c.GetComponentInParent<PrologueAccomplice>())continue;
            return true;
        }
        return false;
    }
    public int Claim(ProloguePoliceBrain owner,NavMeshAgent agent,Vector3 threat,NavMeshPath path){
        int best=-1;float score=float.MaxValue;
        for(int i=0;i<hiding.Length;i++){
            if(owners[i]&&(!owners[i].officer.Active||owners[i].officer.Dead))owners[i]=null;
            if(owners[i]&&owners[i]!=owner)continue;
            // 한 책상의 반대편 자리까지 중복 점유하지 않는다.
            bool occupied=false;for(int j=0;j<owners.Length;j++)if(owners[j]&&owners[j]!=owner&&(hiding[j].position-hiding[i].position).sqrMagnitude<8){occupied=true;break;}
            if(occupied)continue;
            float travel=Vector3.Distance(agent.transform.position,hiding[i].position),range=Vector3.Distance(threat,peeking[i].position);
            if(travel>18||range<4||range>17)continue;
            float candidate=travel+Mathf.Abs(range-9)*.4f;
            if(candidate>=score||!Blocked(hiding[i].position+Vector3.up*1.02f,threat+Vector3.up*1.3f)||Blocked(peeking[i].position+Vector3.up*1.45f,threat+Vector3.up*1.3f))continue;
            if(!agent.CalculatePath(hiding[i].position,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            best=i;score=candidate;
        }
        if(best>=0)owners[best]=owner;
        return best;
    }
    void OnDrawGizmosSelected(){
        if(hiding==null||peeking==null)return;
        for(int i=0;i<Mathf.Min(hiding.Length,peeking.Length);i++)if(hiding[i]&&peeking[i]){
            Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(hiding[i].position+Vector3.up*.2f,.25f);
            Gizmos.color=Color.yellow;Gizmos.DrawLine(hiding[i].position,peeking[i].position);Gizmos.DrawWireSphere(peeking[i].position+Vector3.up*.2f,.2f);
        }
    }
}
}
