using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 정보통을 찾는 이유와 치킨 의뢰의 다음 행동을 연결하는 짧은 장면.
    public sealed class PrisonInformantIntroduction : MonoBehaviour
    {
        public PrisonIntroduction intro;
        public Transform petitioner;
        public NavMeshAgent agent;
        public Animator animator;
        bool walking;
        public IEnumerator Play(Camera camera)
        {
            var host=intro.informant;
            camera.transform.position=host.position+host.forward*3.2f+host.right*2.8f+Vector3.up*1.65f;
            camera.transform.LookAt(host.position+host.forward*.6f+Vector3.up*1.25f);
            foreach(var line in intro.dialogue.informantEntrance){intro.PresentLine(line);yield return new WaitForSeconds(intro.dialogue.lineSeconds);}
            if(agent&&agent.enabled&&agent.isOnNavMesh){
                agent.isStopped=false;walking=true;agent.speed=2.2f;
                Vector3 exit=host.position+host.forward*4+host.right*3;
                if(NavMesh.SamplePosition(exit,out var hit,1,NavMesh.AllAreas))agent.SetDestination(hit.position);
                for(float t=0;t<3;t+=Time.deltaTime)yield return null;
            }
            camera.transform.position=host.position+host.forward*2.1f+host.right*.25f+Vector3.up*1.5f;
            camera.transform.LookAt(host.position+Vector3.up*1.35f);
            yield return new WaitForSeconds(.6f);
            Finish();intro.PresentLine("");
        }
        void Update(){if(walking&&animator&&agent&&agent.enabled&&agent.isOnNavMesh)animator.SetFloat("MoveSpeed",agent.velocity.magnitude,.15f,Time.deltaTime);}
        public void Finish(){walking=false;if(petitioner)petitioner.gameObject.SetActive(false);}
    }
}
