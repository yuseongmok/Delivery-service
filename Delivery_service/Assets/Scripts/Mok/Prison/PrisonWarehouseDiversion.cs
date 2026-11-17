using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 열쇠 획득 후 대화로 붙잡고, 문 개방 뒤 들어왔던 골목으로 이동한다.
    public sealed class PrisonWarehouseDiversion:MonoBehaviour
    {
        public bool ReadyToRejoin { get; private set; }
        public bool Talking { get; private set; }
        public bool TalkReady { get; private set; }
        PrisonKeysMission mission;
        PrisonCompanion mingi;
        NavMeshAgent guardAgent;
        Animator guardAnimator;
        float companionStop;
        public void BeginTalk(PrisonKeysMission owner){
            mission=owner;mingi=owner.previous.prison.introduction.mingi;
            guardAgent=owner.keyGuard.GetComponent<NavMeshAgent>();guardAnimator=owner.keyGuard.GetComponentInChildren<Animator>();
            companionStop=mingi.agent.stoppingDistance;ReadyToRejoin=TalkReady=false;Talking=true;
            owner.keyGuard.enabled=false;owner.keyGuard.GetComponent<NPCControl>().ExternalControl=true;
            guardAgent.ResetPath();guardAgent.isStopped=true;
            mingi.Following=false;mingi.ScriptedTravel=true;mingi.agent.stoppingDistance=.6f;mingi.agent.speed=2.8f;mingi.agent.isStopped=false;
            PrisonAlleyCollision.Attach(mingi.agent);PrisonAlleyCollision.Attach(guardAgent);
            StartCoroutine(Conversation());
        }
        IEnumerator Conversation(){
            mission.ShowDiversionLine(mission.dialogue.keyDistraction);
            var towardEntry=Vector3.ProjectOnPlane(mission.checkpoint.position-guardAgent.transform.position,Vector3.up).normalized;
            yield return Walk(mingi.agent,guardAgent.transform.position+towardEntry*1.5f);
            Face(mingi.transform,guardAgent.transform.position);Face(guardAgent.transform,mingi.transform.position);
            mission.ShowDiversionLine(mission.dialogue.keyConversation);yield return new WaitForSeconds(2.8f);
            mission.ShowDiversionLine(mission.dialogue.keyGuardReply);TalkReady=true;
        }
        public IEnumerator LeadAway(PrisonKeysMission owner){
            if(!Talking)BeginTalk(owner);
            while(!TalkReady)yield return null;
            owner.ShowDiversionLine(owner.dialogue.alleyLead);
            guardAgent.stoppingDistance=.7f;guardAgent.speed=2.4f;
            // 창고 정면에서 북쪽 골목 입구까지. 카트 쪽 건물을 가로지르는 경로는 사용하지 않는다.
            Vector3 start=owner.keyLock.transform.position;start.y=guardAgent.transform.position.y;
            Vector3 forward=-owner.keyLock.transform.forward;
            for(int i=0;i<3;i++){
                Vector3 point=start+forward*(12+i*9);
                yield return WalkPair(point);
            }
            guardAgent.ResetPath();guardAgent.isStopped=true;Face(guardAgent.transform,start+forward*40);
            mingi.agent.ResetPath();mingi.agent.isStopped=true;
            owner.ShowDiversionLine(owner.dialogue.alleyGather);
            StartCoroutine(WaitForPlayer());
        }
        IEnumerator WaitForPlayer(){
            var player=mission.previous.prison.player;
            while(mission.State!=PrisonKeysMission.Stage.Rejoin||Vector3.Dot(player.transform.position-mission.keyLock.transform.position,mission.keyLock.transform.forward)>-1.2f)yield return null;
            yield return Walk(mingi.agent,mission.checkpoint.position);
            ReadyToRejoin=true;
        }
        IEnumerator WalkPair(Vector3 point){
            var lead=Walk(mingi.agent,point+mission.keyLock.transform.right*.6f);
            var follow=Walk(guardAgent,point-mission.keyLock.transform.right*.6f);
            bool a=true,b=true;while(a||b){if(a)a=lead.MoveNext();if(b)b=follow.MoveNext();yield return null;}
        }
        IEnumerator Walk(NavMeshAgent actor,Vector3 target){
            target.y=actor.transform.position.y;
            if(!NavMesh.SamplePosition(target,out var hit,1.4f,actor.areaMask))yield break;
            actor.isStopped=false;actor.SetDestination(hit.position);
            while(actor.pathPending||actor.remainingDistance>actor.stoppingDistance+.15f){
                if(!actor.pathPending&&actor.pathStatus==NavMeshPathStatus.PathInvalid)yield break;
                yield return null;
            }
            actor.ResetPath();
        }
        void Update(){if(guardAgent&&guardAnimator&&guardAgent.enabled&&guardAgent.isOnNavMesh)guardAnimator.SetFloat("MoveSpeed",guardAgent.velocity.magnitude,.15f,Time.deltaTime);}
        public void Rejoin(){if(!mingi)return;StopAllCoroutines();if(mingi.agent){mingi.agent.stoppingDistance=companionStop;mingi.agent.isStopped=false;}mingi.ScriptedTravel=false;mingi.Following=true;Talking=false;}
        public void ResetDiversion(){Rejoin();if(mission&&mission.keyGuard){mission.keyGuard.enabled=true;var collision=mission.keyGuard.GetComponent<PrisonAlleyCollision>();if(collision)collision.enabled=false;}TalkReady=ReadyToRejoin=false;}
        static void Face(Transform actor,Vector3 target){Vector3 direction=target-actor.position;direction.y=0;if(direction.sqrMagnitude>.01f)actor.rotation=Quaternion.LookRotation(direction);}
        void OnDisable(){Rejoin();}
    }
}
