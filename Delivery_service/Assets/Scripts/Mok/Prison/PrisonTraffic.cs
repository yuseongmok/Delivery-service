using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 목적지 예약과 경로 요청 예산을 공유해 군중이 한 지점/한 프레임에 몰리지 않게 한다.
    public static class PrisonTraffic
    {
        static readonly Dictionary<Transform, PrisonResident> reservations = new Dictionary<Transform, PrisonResident>();
        static int frame = -1, requests;
        public static int PathRequests { get; private set; }
        public static int CartYields { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { reservations.Clear(); frame = -1; requests = 0; PathRequests = 0; CartYields = 0; }
        public static bool PathBudget()
        {
            if (frame != Time.frameCount) { frame = Time.frameCount; requests = 0; }
            if (requests >= 2) return false;
            requests++; PathRequests++; return true;
        }
        public static bool Available(Transform point, PrisonResident owner) => !reservations.TryGetValue(point, out var other) || !other || other == owner;
        public static void Reserve(Transform point, PrisonResident owner) { reservations[point] = owner; }
        public static void Release(Transform point, PrisonResident owner)
        { if (!object.ReferenceEquals(point, null) && reservations.TryGetValue(point, out var other) && other == owner) reservations.Remove(point); }

        public static bool YieldToCart(NavMeshAgent agent, NavMeshPath path)
        {
            var mission = PrisonMission.Current;
            if (!mission || !mission.cart) return false;
            var cart = mission.cart.transform.position;
            var forward = mission.player.transform.forward;
            if (mission.cart.controller.cartRigidbody.isKinematic)
            {
                // 카트를 세워 배식하는 동안에도 플레이어와 배식 대상 사이를 비워 준다.
                if (mission.State != PrisonMission.Phase.Deliver || !mission.Target || (cart-mission.player.transform.position).sqrMagnitude>25) return false;
                var toRecipient=Vector3.ProjectOnPlane(mission.Target.position-mission.player.transform.position,Vector3.up);
                if(toRecipient.sqrMagnitude>16 || toRecipient.sqrMagnitude<.01f)return false;
                cart=mission.player.transform.position;cart.y=agent.transform.position.y;forward=toRecipient.normalized;
            }
            var right = Vector3.Cross(Vector3.up, forward);
            var delta = agent.transform.position - cart;
            float along = Vector3.Dot(delta, forward), side = Vector3.Dot(delta, right);
            if (Mathf.Abs(delta.y) > 2 || along < -2 || along > 6 || Mathf.Abs(side) > 1.8f) return false;
            for (int i = 0; i < 2; i++)
            {
                float sign = (side >= 0 ? 1 : -1) * (i == 0 ? 1 : -1);
                var candidate = cart + forward * Mathf.Clamp(along, 0, 5) + right * sign * 2.6f;
                if (!NavMesh.SamplePosition(candidate, out var hit, .65f, agent.areaMask)) continue;
                if (Mathf.Abs(Vector3.Dot(hit.position - cart, right)) < 1.9f || Mathf.Abs(hit.position.y - agent.transform.position.y) > .6f) continue;
                if (!PathBudget()) return false;
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                agent.isStopped = false; agent.SetPath(path); CartYields++; return true;
            }
            return false;
        }
    }
}
