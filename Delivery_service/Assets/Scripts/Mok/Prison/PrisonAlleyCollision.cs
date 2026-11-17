using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // NavMesh가 벽을 포함해 구워져 있어도 실제 벽 콜라이더를 관통하지 않는다.
    [DefaultExecutionOrder(500)]
    public sealed class PrisonAlleyCollision:MonoBehaviour
    {
        NavMeshAgent agent;
        public static void Attach(NavMeshAgent actor){var safety=actor.GetComponent<PrisonAlleyCollision>();if(!safety)safety=actor.gameObject.AddComponent<PrisonAlleyCollision>();safety.enabled=true;}
        void Awake(){agent=GetComponent<NavMeshAgent>();agent.updatePosition=false;}
        void OnEnable(){if(agent)agent.updatePosition=false;}
        void LateUpdate(){
            if(!agent||!agent.enabled||!agent.isOnNavMesh||Time.timeScale<=0)return;
            Vector3 origin=transform.position,target=agent.nextPosition,delta=target-origin;
            if(delta.magnitude>.4f)delta=delta.normalized*.4f;
            Vector3 allowed=Clip(origin,delta,out var normal);
            if(normal.sqrMagnitude>.1f){var slide=Vector3.ProjectOnPlane(delta-allowed,normal);allowed+=Clip(origin+allowed,slide,out _);}
            transform.position=origin+allowed;agent.nextPosition=transform.position;
        }
        Vector3 Clip(Vector3 origin,Vector3 delta,out Vector3 normal){
            normal=Vector3.zero;float distance=delta.magnitude;if(distance<.0001f)return delta;
            float limit=distance;
            foreach(var hit in Physics.CapsuleCastAll(origin+Vector3.up*.4f,origin+Vector3.up*1.45f,.2f,delta/distance,distance+.035f,~0,QueryTriggerInteraction.Ignore)){
                if(hit.transform.IsChildOf(transform)||hit.normal.y>.55f||hit.collider.GetComponentInParent<NavMeshAgent>()||hit.collider.GetComponentInParent<PlayerCombat>())continue;
                if(hit.distance-.035f<limit){limit=Mathf.Max(0,hit.distance-.035f);normal=hit.normal;}
            }
            return delta.normalized*limit;
        }
        void OnDisable(){if(agent)agent.updatePosition=true;}
    }
}
