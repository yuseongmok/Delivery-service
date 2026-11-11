using System;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 미션 인물과 별도로 생활 인원을 유지한다. 시야 밖에 안전한 자리가 생길 때만 보충한다.
    public sealed class PrisonPopulation : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public NPCControl actor;
            public bool guard, infirmaryWatch;
            public bool holdPost;
            public Vector3 postPosition;
            public float postYaw;
            public Transform[] route;
            public string actorName;
            public int guardIndex = -1;
            [NonSerialized] public float missingSince = -1;
        }
        public GameObject inmateTemplate, guardTemplate;
        public Slot[] slots;
        public Vector3[] spawnPoints;
        public float respawnDelay = 8f;
        float nextCheck;
        public int RespawnCount { get; private set; }

        void Update()
        {
            if (Time.time < nextCheck || !PrisonMission.Current || PrisonMission.Current.Failing) return;
            nextCheck = Time.time + 1f;
            foreach (var slot in slots)
            {
                if (slot.actor && !slot.actor.IsDead) { slot.missingSince = -1; continue; }
                if (slot.missingSince < 0) slot.missingSince = Time.time;
                if (slot.actor || Time.time - slot.missingSince < respawnDelay) continue;
                if (!FindHiddenPosition(out var position)) continue;
                Spawn(slot, position);
                break; // 한 프레임에 몰아서 생성하지 않는다.
            }
        }

        bool FindHiddenPosition(out Vector3 position)
        {
            position = default;
            if (spawnPoints == null || spawnPoints.Length == 0) return false;
            int start = UnityEngine.Random.Range(0, spawnPoints.Length);
            for (int i = 0; i < Mathf.Min(24, spawnPoints.Length); i++)
            {
                var point = spawnPoints[(start + i) % spawnPoints.Length];
                if (!NavMesh.SamplePosition(point, out var hit, .6f, NavMesh.AllAreas)) continue;
                if (!HiddenFromPlayer(hit.position)) continue;
                bool occupied = false;
                foreach (var col in Physics.OverlapCapsule(hit.position + Vector3.up * .5f, hit.position + Vector3.up * 1.5f, .45f, ~0, QueryTriggerInteraction.Ignore))
                    if (col.GetComponentInParent<NPCControl>() || col.GetComponentInParent<PlayerCombat>()) { occupied = true; break; }
                if (occupied) continue;
                position = hit.position;
                return true;
            }
            return false;
        }

        public bool HiddenFromPlayer(Vector3 point)
        {
            var player = PrisonMission.Current.player;
            var camera = player.View;
            if (!camera || Vector3.Distance(player.transform.position, point) < 12f) return false;
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), new Bounds(point + Vector3.up, new Vector3(1.4f, 2.2f, 1.4f)))) return true;
            // 몸의 일부라도 보이면 생성하지 않는다. 다른 NPC는 가림막으로 인정하지 않는다.
            foreach (var offset in new[] { Vector3.up * .3f, Vector3.up, Vector3.up * 1.9f, Vector3.up + Vector3.right * .5f, Vector3.up - Vector3.right * .5f })
            {
                var delta = point + offset - camera.transform.position;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(camera.transform.position, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.collider.GetComponentInParent<NPCControl>() && !hit.collider.GetComponentInParent<PlayerCombat>()) { blocked = true; break; }
                if (!blocked) return false;
            }
            return true;
        }

        void Spawn(Slot slot, Vector3 position)
        {
            var actor = Instantiate(slot.guard ? guardTemplate : inmateTemplate, position, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), transform);
            actor.name = slot.actorName;
            slot.actor = actor.GetComponent<NPCControl>();
            if (slot.guard)
            {
                var guard = actor.GetComponent<PrisonGuard>();
                guard.route = slot.route; guard.infirmaryWatch = slot.infirmaryWatch;
                guard.holdPost=slot.holdPost;guard.postPosition=slot.postPosition;guard.postYaw=slot.postYaw;
                if (slot.guardIndex >= 0) PrisonMission.Current.guards[slot.guardIndex] = guard;
                // 의무실 유인 장치가 새 간수도 계속 제어하도록 연결한다.
                if (slot.infirmaryWatch)
                    foreach (var pipe in PrisonMission.Current.GetComponentsInChildren<PrisonDistraction>())
                        for (int i = 0; i < pipe.guards.Length; i++) if (!pipe.guards[i]) { pipe.guards[i] = guard; break; }
            }
            else actor.GetComponent<PrisonResident>().route = slot.route;
            actor.GetComponent<NavMeshAgent>().avoidancePriority = UnityEngine.Random.Range(25, 75);
            actor.SetActive(true);
            slot.missingSince = -1;
            RespawnCount++;
        }
    }
}
