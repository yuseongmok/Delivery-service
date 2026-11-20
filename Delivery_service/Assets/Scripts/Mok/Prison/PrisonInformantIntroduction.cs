using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 감사 인사, 응답, 퇴장을 보여 준 뒤 정보거래 대화에 제어를 돌려준다.
    [DefaultExecutionOrder(200)]
    public sealed class PrisonInformantIntroduction : MonoBehaviour
    {
        public PrisonIntroduction intro;
        public Transform petitioner;
        public NavMeshAgent agent;
        public Animator animator;
        public Transform exitPoint;
        [Min(1)] public float entranceLineSeconds = 3.2f;
        [Range(10, 45)] public float bowDegrees = 30;
        public bool Playing { get; private set; }
        public bool Departed { get; private set; }
        public float BowAmount { get; private set; }
        bool walking, hostSpeaking;
        float lineTime;
        Quaternion hostRotation;
        Transform spine, head, hostArm, hostForearm;
        Quaternion spineRest, headRest, armRest, forearmRest;

        public IEnumerator Play(Camera camera)
        {
            if (!petitioner || !intro.informant) yield break;
            var host = intro.informant;
            hostRotation = host.rotation;
            Playing = true; Departed = false; walking = false;
            petitioner.gameObject.SetActive(true);
            if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
            petitioner.rotation = Face(petitioner.position, host.position);
            if (animator) { animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.SetFloat("MoveSpeed", 0); animator.Update(0); }
            spine = Bone(animator, HumanBodyBones.Spine, "Spine_01");
            head = Bone(animator, HumanBodyBones.Head, "Head");
            var hostAnimator = host.GetComponentInChildren<Animator>();
            hostArm = Bone(hostAnimator, HumanBodyBones.RightUpperArm, "UpperArm_R");
            hostForearm = Bone(hostAnimator, HumanBodyBones.RightLowerArm, "LowerArm_R");
            if (spine) spineRest = spine.localRotation;
            if (head) headRest = head.localRotation;
            if (hostArm) armRest = hostArm.localRotation;
            if (hostForearm) forearmRest = hostForearm.localRotation;
            host.rotation = Face(host.position, petitioner.position);
            var midpoint = (host.position + petitioner.position) * .5f + Vector3.up * 1.1f;
            // 두 얼굴과 상체가 함께 들어오는 옆 구도로 누가 누구에게 인사하는지 먼저 보여 준다.
            Vector3 forward = hostRotation * Vector3.forward;
            Vector3 right = hostRotation * Vector3.right;
            camera.transform.SetPositionAndRotation(host.position + forward * .7f + right * 3.3f + Vector3.up * 1.7f,
                Quaternion.LookRotation(midpoint - (host.position + forward * .7f + right * 3.3f + Vector3.up * 1.7f)));
            camera.fieldOfView = 53;
            try
            {
                yield return new WaitForSeconds(.45f);
                foreach (var line in intro.dialogue.informantEntrance)
                {
                    intro.PresentLine(line);
                    hostSpeaking = line.TrimStart().StartsWith("최형욱:");
                    for (lineTime = 0; lineTime < entranceLineSeconds; lineTime += Time.deltaTime)
                    {
                        if (lineTime > .3f && PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.Space)) break;
                        BowAmount = hostSpeaking ? 0 : Mathf.SmoothStep(0, 1, lineTime / .65f) * (1 - Mathf.SmoothStep(0, 1, (lineTime - 1.25f) / .7f));
                        yield return null;
                    }
                }
                BowAmount = 0; hostSpeaking = false; intro.PresentLine("");
                if (agent && agent.enabled && agent.isOnNavMesh && exitPoint)
                {
                    var path = new NavMeshPath();
                    if (agent.CalculatePath(exitPoint.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        agent.speed = 2.2f; agent.updateRotation = true; agent.isStopped = false; agent.SetPath(path); walking = true;
                        for (float t = 0; t < 9 && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + .12f); t += Time.deltaTime)
                        {
                            host.rotation = Quaternion.Slerp(host.rotation, Face(host.position, petitioner.position), Time.deltaTime * 3);
                            yield return null;
                        }
                        Departed = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .12f;
                    }
                }
                walking = false;
                if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
                if (animator) animator.SetFloat("MoveSpeed", 0);
                // 퇴장을 따라보던 시선을 플레이어 쪽으로 되돌린 뒤 다음 대사를 시작한다.
                var from = host.rotation;
                for (float t = 0; t < .65f; t += Time.deltaTime) { host.rotation = Quaternion.Slerp(from, hostRotation, Mathf.SmoothStep(0, 1, t / .65f)); yield return null; }
                yield return new WaitForSeconds(.2f);
            }
            finally { Finish(); intro.PresentLine(""); }
        }

        static Transform Bone(Animator a, HumanBodyBones bone, string fallback)
        {
            if (!a) return null;
            return a.isHuman ? a.GetBoneTransform(bone) : a.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == fallback);
        }
        static Quaternion Face(Vector3 from, Vector3 to)
        {
            var direction = Vector3.ProjectOnPlane(to - from, Vector3.up);
            return direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
        }
        void Update()
        {
            if (walking && animator && agent && agent.enabled && agent.isOnNavMesh) animator.SetFloat("MoveSpeed", agent.velocity.magnitude, .15f, Time.deltaTime);
        }
        void LateUpdate()
        {
            if (!Playing) return;
            if (!walking)
            {
                if (spine) spine.localRotation = spineRest * Quaternion.Euler(bowDegrees * BowAmount, 0, 0);
                if (head) head.localRotation = headRest * Quaternion.Euler(10 * BowAmount, 0, 0);
            }
            float gesture = hostSpeaking ? Mathf.Sin(Mathf.Clamp01(lineTime / entranceLineSeconds) * Mathf.PI) : 0;
            if (hostArm) hostArm.localRotation = armRest * Quaternion.Euler(-12 * gesture, 0, 18 * gesture);
            if (hostForearm) hostForearm.localRotation = forearmRest * Quaternion.Euler(0, 25 * gesture, 0);
        }
        public void Finish()
        {
            if (Playing)
            {
                if (spine) spine.localRotation = spineRest;
                if (head) head.localRotation = headRest;
                if (hostArm) hostArm.localRotation = armRest;
                if (hostForearm) hostForearm.localRotation = forearmRest;
                if (intro && intro.informant) intro.informant.rotation = hostRotation;
            }
            Playing = false; walking = false; BowAmount = 0;
            if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
            if (petitioner) petitioner.gameObject.SetActive(false);
        }
        void OnDisable() { Finish(); }
    }
}
