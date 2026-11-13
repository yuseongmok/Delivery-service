using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 문과 동행자의 입장만 담당하며 기존 배식 미션의 진행은 유지한다.
    public sealed class PrisonOfficeEntrance : MonoBehaviour
    {
        public Transform hinge;
        public Transform insideWait;
        public PrisonKeysMission mission;
        public float openSeconds = 1.6f;
        public float openAngle = 100;
        public bool IsOpen { get; private set; }
        public bool CompanionArrived { get; private set; }
        Quaternion closed;
        bool opening, entering;
        PrisonCompanion companion;

        void Awake() { closed = hinge.localRotation; }

        public IEnumerator Open()
        {
            if (IsOpen || opening) yield break;
            opening = true;
            PrisonAudio.Clink(hinge.position, .09f);
            var target = closed * Quaternion.Euler(0, openAngle, 0);
            for (float t = 0; t < openSeconds; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(closed, target,
                    Mathf.SmoothStep(0, 1, t / openSeconds));
                yield return null;
            }
            hinge.localRotation = target;
            IsOpen = true;
            opening = false;
        }

        void Update()
        {
            if (!IsOpen || entering || CompanionArrived || Time.timeScale <= 0) return;
            var player = mission.previous.prison.player;
            if (mission.State != PrisonKeysMission.Stage.Office ||
                Vector3.Distance(player.transform.position, hinge.position) > 5) return;
            StartCoroutine(EnterTogether());
        }

        IEnumerator EnterTogether()
        {
            entering = true;
            companion = mission.previous.prison.introduction.mingi;
            var agent = companion.agent;
            var originalFollow = companion.Following;
            var originalDistance = agent.stoppingDistance;
            companion.ScriptedTravel = true;
            agent.stoppingDistance = .3f;
            // 문이 완전히 열린 뒤 실제 길을 따라 들어가 통로 옆에서 기다린다.
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = 2.2f;
                agent.SetDestination(insideWait.position);
                for (float t = 0; t < 24; t += Time.deltaTime)
                {
                    if (Vector3.Distance(companion.transform.position, insideWait.position) < .6f)
                    { CompanionArrived = true; break; }
                    if (!agent.pathPending && !agent.hasPath) agent.SetDestination(insideWait.position);
                    yield return null;
                }
                agent.ResetPath();
            }
            agent.stoppingDistance = originalDistance;
            if (!CompanionArrived) { companion.Following = originalFollow; companion.ScriptedTravel = false; entering = false; yield break; }
            companion.Following = false;
            companion.ScriptedTravel = false;
            while (mission.State != PrisonKeysMission.Stage.Complete)
            {
                var delta = mission.warden.transform.position - companion.transform.position;
                delta.y = 0;
                if (delta.sqrMagnitude > .01f)
                    companion.transform.rotation = Quaternion.RotateTowards(companion.transform.rotation,
                        Quaternion.LookRotation(delta), 100 * Time.deltaTime);
                yield return null;
            }
            companion.Following = originalFollow;
        }
    }
}
