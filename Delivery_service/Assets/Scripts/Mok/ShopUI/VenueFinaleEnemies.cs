using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Phase = DeliveryService.Mok.VenueFinale.Phase;

namespace DeliveryService.Mok
{
    // 화면 밖 생성 대기와 적 목록의 정리를 담당한다.
    internal sealed class VenueFinaleEnemies
    {
        private readonly VenueFinale mission;
        public VenueFinaleEnemies(VenueFinale mission)
        {
            this.mission = mission;
        }

        private readonly List<CitizenCombat> enemies = new();
        public IReadOnlyList<CitizenCombat> ActiveEnemies => enemies;
        public CitizenCombat[] Snapshot => enemies.Where(enemy => enemy).ToArray();
        public void Add(CitizenCombat enemy)
        {
            enemies.Add(enemy);
        }

        public void ResetSpawnCount()
        {
            HiddenSpawnCount = 0;
        }

        public void Tick()
        {
            if (Time.timeScale > 0 && pendingSpawns.Count > 0 && Time.time >= nextSpawnCheck)
            {
                nextSpawnCheck = Time.time + .35f;
                TryPendingSpawns();
            }
        }

        public void Clear()
        {
            pendingSpawns.Clear();
            foreach (var enemy in enemies)
            {
                if (!enemy)
                {
                    continue;
                }

                enemy.EndFight();
                Object.Destroy(enemy.gameObject);
            }

            enemies.Clear();
        }

        // 화면에 보이는 위치에서는 생성하지 않고 다음 안전한 위치 확인까지 기다린다.
        class PendingEnemy
        {
            public Transform post;
            public float speed;
        }

        readonly List<PendingEnemy> pendingSpawns = new();
        float nextSpawnCheck;
        public int PendingSpawnCount => pendingSpawns.Count;
        public int HiddenSpawnCount { get; private set; }

        public void StopStorySpawning()
        {
            pendingSpawns.Clear();
        }

        public void QueueSpawns(Transform[] points, int count, float speed)
        {
            foreach (var post in points.Take(count))
            {
                if (post)
                {
                    pendingSpawns.Add(new PendingEnemy{post = post, speed = speed});
                }
            }

            TryPendingSpawns();
        }

        public bool SpawnOutsideView(Vector3 point)
        {
            var camera = mission.cinema.enabled ? mission.cinema : mission.Player.View;
            // 몸 전체와 화면 가장자리 여유까지 검사한다. 벽 뒤라도 화면 안이면 생성하지 않는다.
            return !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), new Bounds(point + Vector3.up, new Vector3(2.4f, 2.8f, 2.4f)));
        }

        bool FindHiddenSpawn(Transform post, out Vector3 point)
        {
            var path = new NavMeshPath();
            for (int i = 0; i < 49; i++)
            {
                var candidate = post.position;
                if (i > 0)
                {
                    float angle = (i - 1) * Mathf.PI / 4;
                    candidate += new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (2 + ((i - 1) / 8) * 2);
                }

                if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - post.position.y) > 1.5f || Vector3.Distance(hit.position, mission.Player.transform.position) < 6 || !SpawnOutsideView(hit.position))
                {
                    continue;
                }

                if (enemies.Any(e => e && Vector3.Distance(e.transform.position, hit.position) < 1.2f))
                {
                    continue;
                }

                if (!NavMesh.CalculatePath(hit.position, mission.Player.transform.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                point = hit.position;
                return true;
            }

            point = default;
            return false;
        }

        void TryPendingSpawns()
        {
            for (int i = pendingSpawns.Count - 1; i >= 0; i--)
            {
                var request = pendingSpawns[i];
                if (!request.post)
                {
                    pendingSpawns.RemoveAt(i);
                    continue;
                }

                if (!FindHiddenSpawn(request.post, out var point))
                {
                    continue;
                }

                var e = Object.Instantiate(mission.preparation.enemyTemplate, point, request.post.rotation, mission.transform);
                e.missionEnemy = true;
                e.gameObject.SetActive(true);
                e.chaseSpeed = request.speed;
                e.punchCooldown = 2.1f;
                e.Provoke(mission.Player);
                enemies.Add(e);
                if (mission.aftermath && mission.aftermath.parkSequence && mission.aftermath.parkSequence.Current >= ParkEscapeSequence.Beat.LookBack && mission.aftermath.parkSequence.Current <= ParkEscapeSequence.Beat.Defend)
                {
                    mission.aftermath.parkSequence.PrepareParkEnemy(e);
                }

                VenueActorAudio.Attach(e);
                HiddenSpawnCount++;
                pendingSpawns.RemoveAt(i);
            }
        }
    }
}
