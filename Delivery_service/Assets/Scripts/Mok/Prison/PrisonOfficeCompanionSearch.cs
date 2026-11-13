using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class PrisonOfficeCompanionSearch : MonoBehaviour
    {
        public PrisonOfficeSearch mission;
        bool started;
        void Update() { if (!started && mission.Active) { started = true; StartCoroutine(Search()); } }
        IEnumerator Search()
        {
            var friend = mission.previous.previous.prison.introduction.mingi;
            friend.ScriptedTravel = true;
            var agent = friend.agent;
            int index = 0;
            while (!mission.Complete)
            {
                var point = mission.points[index % 2]; index++;
                var goal = point.transform.position + point.transform.right * 1.2f - point.transform.forward * .9f;
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false; agent.speed = 1.7f; agent.stoppingDistance = .25f;
                    agent.SetDestination(goal);
                    for (float t = 0; t < 8 && !mission.Complete; t += Time.deltaTime)
                    { if (!agent.pathPending && agent.remainingDistance < .5f) break; yield return null; }
                    agent.ResetPath();
                }
                var delta = point.transform.position - friend.transform.position; delta.y = 0;
                if (delta.sqrMagnitude > .01f) friend.transform.rotation = Quaternion.LookRotation(delta);
                var spine = friend.animator.GetBoneTransform(HumanBodyBones.Spine);
                for (float t = 0; t < 3 && !mission.Complete; t += Time.deltaTime)
                {
                    yield return new WaitForEndOfFrame(); if(Time.timeScale<=0)continue;
                    // 걷기 애니메이션 위에 가볍게 몸을 숙여 주변 수납장을 살펴본다.
                    spine.rotation = Quaternion.AngleAxis(Mathf.Sin(t / 3 * Mathf.PI) * 14, friend.transform.right) * spine.rotation;
                }
            }
            friend.ScriptedTravel = false; friend.Following = true;
        }
    }
}
