using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 민기 경로 추종, 걷기·달리기 속도와 부상 자세를 담당한다.
    internal sealed class ShopEscapeCompanion
    {
        private readonly ShopEscapeChapter chapter;
        public ShopEscapeCompanion(ShopEscapeChapter chapter)
        {
            this.chapter = chapter;
        }

        private NavMeshPath followPath;
        private int followCorner;
        private int doorStep;
        private float nextPath;
        private float nextStep;
        private float followSpeed = 2.2f;
        private NavMeshAgent guide;
        private Animator animation;
        private Vector3 previousPlayer;
        private Vector3 lastDestination = new Vector3(float.PositiveInfinity, 0, 0);
        private bool injuryPose;
        private bool injuryAnimatorEnabled;
        private float injuryStarted;
        private Transform injurySpine;
        private Transform injuryHips;
        private Vector3 hipsRest;
        private Vector3 injuryPosition;
        private Vector3 leftFootGoal;
        private Vector3 rightFootGoal;
        private Quaternion injuryRotation;
        private Quaternion leftFootRotation;
        private Quaternion rightFootRotation;
        private readonly Dictionary<Transform, Quaternion> injuryBones = new();
        public bool HasPath => followPath != null && followPath.corners.Length > 1;
        public float FollowSpeed => followSpeed;
        public void Initialize()
        {
            followPath = new NavMeshPath();
            guide = chapter.mingi.GetComponent<NavMeshAgent>();
            animation = chapter.mingi.GetComponentInChildren<Animator>();
            if (animation)
            {
                animation.applyRootMotion = false;
            }
        }

        public void ResetRoute()
        {
            doorStep = 0;
            followPath ??= new NavMeshPath();
            followPath.ClearCorners();
            lastDestination = new Vector3(float.PositiveInfinity, 0, 0);
        }

        public void PrepareEscape()
        {
            ResetRoute();
            if (guide)
            {
                guide.enabled = false;
                guide.speed = 5.5f;
                guide.acceleration = 18;
                guide.stoppingDistance = 1.8f;
            }
        }

        public void BeginParkEscape()
        {
            previousPlayer = chapter.mission.Player.transform.position;
            followSpeed = 2.2f;
            ResetRoute();
        }

        public void Follow()
        {
            var position = chapter.mission.Player.transform.position;
            float moving = Vector3.ProjectOnPlane(position - previousPlayer, Vector3.up).magnitude / Mathf.Max(.001f, Time.deltaTime);
            previousPlayer = position;
            if (moving > 15)
            {
                moving = 0;
            }

            float gap = Vector3.Distance(chapter.mingi.position, position);
            float desired = moving > 3.4f ? Mathf.Max(5.2f, moving) : 2.2f;
            if (gap > 7)
            {
                desired = Mathf.Max(desired, 6.3f);
            }

            followSpeed = Mathf.MoveTowards(followSpeed, Mathf.Clamp(desired, 2.2f, 7.5f), Time.deltaTime * 5);
            if (chapter.doorway != null && doorStep < chapter.doorway.Length)
            {
                var goal = chapter.doorway[doorStep].position;
                var delta = goal - chapter.mingi.position;
                delta.y = 0;
                chapter.mingi.position = Vector3.MoveTowards(chapter.mingi.position, goal, followSpeed * Time.deltaTime);
                if (delta.sqrMagnitude > .01f)
                {
                    chapter.mingi.rotation = Quaternion.RotateTowards(chapter.mingi.rotation, Quaternion.LookRotation(delta), 240 * Time.deltaTime);
                }

                if (animation)
                {
                    animation.SetFloat("MoveSpeed", followSpeed);
                }

                if (Vector3.Distance(chapter.mingi.position, goal) < .15f)
                {
                    doorStep++;
                }

                return;
            }

            if (guide && guide.enabled)
            {
                guide.enabled = false;
            }

            var cc = chapter.mingi.GetComponent<CharacterController>();
            if (cc)
            {
                cc.enabled = false;
            }

            if (Time.time >= nextPath && ((lastDestination - chapter.mission.Player.transform.position).sqrMagnitude > 9 || followPath.corners.Length < 2))
            {
                nextPath = Time.time + .6f;
                if (NavMesh.SamplePosition(chapter.mingi.position, out var from, 14, NavMesh.AllAreas) && NavMesh.SamplePosition(chapter.mission.Player.transform.position, out var to, 4, NavMesh.AllAreas) && NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, followPath) && followPath.status == NavMeshPathStatus.PathComplete)
                {
                    lastDestination = chapter.mission.Player.transform.position;
                    followCorner = 1;
                }
            }

            float speed = 0;
            if (followPath.corners.Length > 1 && Vector3.Distance(chapter.mingi.position, chapter.mission.Player.transform.position) > 2)
            {
                followCorner = Mathf.Min(followCorner, followPath.corners.Length - 1);
                var corner = followPath.corners[followCorner];
                var delta = corner - chapter.mingi.position;
                if (delta.magnitude < .2f && followCorner < followPath.corners.Length - 1)
                {
                    followCorner++;
                }
                else
                {
                    var previous = chapter.mingi.position;
                    chapter.mingi.position = Vector3.MoveTowards(chapter.mingi.position, corner, followSpeed * Time.deltaTime);
                    speed = Vector3.Distance(previous, chapter.mingi.position) / Mathf.Max(.001f, Time.deltaTime);
                    delta.y = 0;
                    if (delta.sqrMagnitude > .01f)
                    {
                        chapter.mingi.rotation = Quaternion.RotateTowards(chapter.mingi.rotation, Quaternion.LookRotation(delta), 240 * Time.deltaTime);
                    }
                }
            }

            if (animation)
            {
                animation.SetFloat("MoveSpeed", speed);
            }

            if (speed > .5f && Time.time >= nextStep)
            {
                nextStep = Time.time + (speed > 3.4f ? .28f : .45f);
                var clips = SuppliedAudio.Current ? SuppliedAudio.Current.streetSteps : null;
                if (clips != null && clips.Length > 0)
                {
                    AudioSource.PlayClipAtPoint(clips[Random.Range(0, clips.Length)], chapter.mingi.position, .09f * SuppliedAudio.Volume);
                }
            }
        }

        public void StopGuide()
        {
            if (guide && guide.enabled)
            {
                if (guide.isOnNavMesh)
                {
                    guide.isStopped = true;
                }

                guide.enabled = false;
            }

            if (animation)
            {
                animation.SetFloat("MoveSpeed", 0);
            }
        }

        public void StartInjury()
        {
            if (injuryPose)
            {
                return;
            }

            StopGuide();
            injuryStarted = Time.time;
            injuryPosition = chapter.mingi.position;
            injuryRotation = chapter.mingi.rotation;
            var face = chapter.mission.Player.transform.position - chapter.mingi.position;
            face.y = 0;
            if (face.sqrMagnitude > .01f)
            {
                chapter.mingi.rotation = Quaternion.LookRotation(face);
            }

            if (animation && animation.isHuman)
            {
                injuryAnimatorEnabled = animation.enabled;
                if (animation.HasState(0, Animator.StringToHash("Base Layer.Idle")))
                {
                    animation.Play("Base Layer.Idle", 0, 0);
                }

                animation.Update(0);
                injurySpine = animation.GetBoneTransform(HumanBodyBones.Spine);
                injuryHips = animation.GetBoneTransform(HumanBodyBones.Hips);
                hipsRest = injuryHips.localPosition;
                foreach (var t in injuryHips.GetComponentsInChildren<Transform>())
                {
                    injuryBones[t] = t.localRotation;
                }

                var left = animation.GetBoneTransform(HumanBodyBones.LeftFoot);
                var right = animation.GetBoneTransform(HumanBodyBones.RightFoot);
                leftFootGoal = left.position;
                rightFootGoal = right.position;
                leftFootRotation = left.rotation;
                rightFootRotation = right.rotation;
                animation.enabled = false;
            }

            injuryPose = true;
        }

        public void RestoreInjury()
        {
            if (!injuryPose)
            {
                return;
            }

            foreach (var pair in injuryBones)
            {
                if (pair.Key)
                {
                    pair.Key.localRotation = pair.Value;
                }
            }

            if (injuryHips)
            {
                injuryHips.localPosition = hipsRest;
            }

            chapter.mingi.SetPositionAndRotation(injuryPosition, injuryRotation);
            if (animation)
            {
                animation.enabled = injuryAnimatorEnabled;
            }

            injuryBones.Clear();
            injuryPose = false;
        }

        private void PoseLimb(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c, Vector3 target, Vector3 pole)
        {
            var upper = animation.GetBoneTransform(a);
            var lower = animation.GetBoneTransform(b);
            var end = animation.GetBoneTransform(c);
            if (!upper || !lower || !end)
            {
                return;
            }

            var origin = upper.position;
            float l1 = Vector3.Distance(origin, lower.position), l2 = Vector3.Distance(lower.position, end.position);
            var d = target - origin;
            if (d.sqrMagnitude < .0001f || l1 + l2 < .01f)
            {
                return;
            }

            float distance = Mathf.Clamp(d.magnitude, .01f, l1 + l2 - .005f);
            var direction = d.normalized;
            float along = (l1 * l1 - l2 * l2 + distance * distance) / (2 * distance);
            var bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            var joint = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, l1 * l1 - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, joint - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, target - lower.position) * lower.rotation;
        }

        public void TickInjury()
        {
            if (!injuryPose || !injuryHips)
            {
                return;
            }

            float blend = Mathf.SmoothStep(0, 1, (Time.time - injuryStarted) / 1.6f);
            foreach (var pair in injuryBones)
            {
                if (pair.Key)
                {
                    pair.Key.localRotation = pair.Value;
                }
            }

            injuryHips.localPosition = hipsRest;
            injuryHips.position -= Vector3.up * .43f * blend;
            if (injurySpine)
            {
                injurySpine.rotation = Quaternion.AngleAxis(22 * blend, chapter.mingi.right) * injurySpine.rotation;
            }

            PoseLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, leftFootGoal, chapter.mingi.forward);
            PoseLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, rightFootGoal, chapter.mingi.forward);
            animation.GetBoneTransform(HumanBodyBones.LeftFoot).rotation = leftFootRotation;
            animation.GetBoneTransform(HumanBodyBones.RightFoot).rotation = rightFootRotation;
            var knee = Vector3.Lerp(animation.GetBoneTransform(HumanBodyBones.RightUpperLeg).position, animation.GetBoneTransform(HumanBodyBones.RightLowerLeg).position, .65f) + chapter.mingi.forward * .12f;
            var hand = animation.GetBoneTransform(HumanBodyBones.RightHand);
            PoseLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, Vector3.Lerp(hand.position, knee, blend), chapter.mingi.right);
        }
    }
}
