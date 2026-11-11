using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 조직원 증원, 엄폐 제어, 민기 지원 사격과 조직원 철수를 담당한다.
    internal sealed class ParkEscapeCombat
    {
        private readonly ParkEscapeSequence sequence;
        public ParkEscapeCombat(ParkEscapeSequence sequence)
        {
            this.sequence = sequence;
        }

        private ParkCoverActor partnerCoverActor;
        private int coverIndex;
        private int waves;
        private float nextSupport;
        private Vector3 aimPoint;
        private Animator partnerAnimator;
        private VenueFinale M => sequence.chapter.mission;
        private PlayerCombat Player => M.Player;
        public int SupportShots { get; private set; }

        public int Waves => waves;
        public void Initialize()
        {
            partnerAnimator = sequence.chapter.mingi.GetComponentInChildren<Animator>();
        }

        public void BeginCover()
        {
            coverIndex = 0;
            if (sequence.partnerCover)
            {
                partnerCoverActor = sequence.chapter.mingi.GetComponent<ParkCoverActor>();
                if (!partnerCoverActor)
                {
                    partnerCoverActor = sequence.chapter.mingi.gameObject.AddComponent<ParkCoverActor>();
                }

                partnerCoverActor.cover = sequence.partnerCover;
                partnerCoverActor.phase = 2;
            }
        }

        public void StopPartner()
        {
            if (partnerCoverActor)
            {
                partnerCoverActor.Stop();
            }
        }

        public void Reset()
        {
            StopCover();
            coverIndex = 0;
            SupportShots = 0;
            waves = 0;
            nextSupport = 0;
        }

        public void ApplyPartnerPose()
        {
            if (sequence.Current == ParkEscapeSequence.Beat.Fight && !sequence.Injured && sequence.mingiGun.gameObject.activeSelf)
            {
                ArmedHandPose.Apply(partnerAnimator, sequence.mingiGun, aimPoint, partnerCoverActor ? partnerCoverActor.CanFire : true, false);
            }
        }

        public void SpawnWave()
        {
            var points = waves == 0 ? sequence.enemyPosts : sequence.backupPosts;
            M.SpawnStreetPursuers(points, Mathf.Min(3, points.Length), 4);
            waves++;
        }

        public void PrepareParkEnemy(CitizenCombat e)
        {
            if (!e)
            {
                return;
            }

            e.ExternalAttackControl = true;
            e.usesFirearm = true;
            e.Provoke(Player);
            if (sequence.enemyCover == null || sequence.enemyCover.Length == 0)
            {
                return;
            }

            var c = e.GetComponent<ParkCoverActor>();
            if (!c)
            {
                c = e.gameObject.AddComponent<ParkCoverActor>();
                c.cover = sequence.enemyCover[coverIndex % sequence.enemyCover.Length];
                c.phase = coverIndex++ * 1.6f;
            }
        }

        public void UpdateCover(bool allow)
        {
            foreach (var e in M.Enemies)
            {
                if (!e)
                {
                    continue;
                }

                PrepareParkEnemy(e);
                e.GetComponent<ParkCoverActor>()?.Tick(allow, Player);
            }

            if (!sequence.Injured && partnerCoverActor)
            {
                partnerCoverActor.Tick(allow, Player);
            }
        }

        public void StopCover()
        {
            foreach (var e in M.Enemies)
            {
                if (e)
                {
                    e.GetComponent<ParkCoverActor>()?.Stop();
                }
            }

            if (partnerCoverActor)
            {
                partnerCoverActor.Stop();
            }
        }

        public void ArmEnemies()
        {
            foreach (var e in M.Enemies)
            {
                if (!e || e.Health <= 0)
                {
                    continue;
                }

                if (!e.usesFirearm)
                {
                    e.usesFirearm = true;
                    e.Provoke(Player);
                }
            }
        }

        public void SupportFire()
        {
            if (partnerCoverActor && !partnerCoverActor.CanFire)
            {
                return;
            }

            var target = M.Enemies.Where(e => e && e.Health > 0 && !e.Npc.IsKnockedDown && !e.Npc.IsDead).OrderBy(e => (e.transform.position - sequence.chapter.mingi.position).sqrMagnitude).FirstOrDefault();
            if (!target)
            {
                return;
            }

            aimPoint = target.transform.position + Vector3.up * 1.25f;
            ParkEscapeCinematics.Face(sequence.chapter.mingi, aimPoint);
            if (Time.time < nextSupport)
            {
                return;
            }

            var origin = sequence.chapter.mingi.position + Vector3.up * 1.35f;
            var d = aimPoint - origin;
            if (d.magnitude > 35)
            {
                return;
            }

            // 엄폐물에 가려져도 제압 사격은 유지하되 탄환은 벽을 관통하지 않는다.
            bool struck = FirearmBallistics.Cast(origin, d.normalized, d.magnitude, sequence.chapter.mingi, out var hit);
            bool blocked = struck && !hit.transform.IsChildOf(target.transform);
            var impact = blocked ? hit.point : aimPoint;
            nextSupport = Time.time + 1.5f;
            if (struck)
            {
                BulletImpactFeedback.Play(hit);
            }

            SupportShots++;
            GunshotSound.Play(sequence.chapter.mingi.gameObject, true);
            GunShotFeedback.Fire(sequence.mingiGun, HandgunGeometry.Get(sequence.mingiGun).Muzzle);
            PrologueTracer.Spawn(sequence.mingiGun.TransformPoint(HandgunGeometry.Get(sequence.mingiGun).Muzzle), impact, new Color(1, .8f, .3f));
            if (!blocked && SupportShots % 3 == 0)
            {
                target.ReceiveBullet(Player, d.normalized, 18);
            }
        }

        public void FreezeEnemies()
        {
            StopCover();
            M.StopStorySpawning();
            foreach (var e in M.Enemies)
            {
                if (!e)
                {
                    continue;
                }

                e.ExternalAttackControl = true;
                var agent = e.GetComponent<NavMeshAgent>();
                if (agent && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }
        }

        public void UnfreezeEnemies()
        {
            foreach (var e in M.Enemies)
            {
                if (e)
                {
                    e.ExternalAttackControl = true;
                }
            }
        }

        public void RetreatGang()
        {
            foreach (var e in M.Enemies)
            {
                if (!e || e.Health <= 0 || e.Npc.IsDead || e.Npc.IsKnockedDown)
                {
                    continue;
                }

                var agent = e.GetComponent<NavMeshAgent>();
                if (!agent || !agent.enabled || !agent.isOnNavMesh)
                {
                    continue;
                }

                agent.isStopped = false;
                agent.speed = 3;
                agent.updateRotation = true;
                if (!agent.hasPath)
                {
                    agent.SetDestination(sequence.gangExit.position);
                }

                var anim = e.GetComponentInChildren<Animator>();
                ParkEscapeCinematics.SetSpeed(anim, agent.velocity.magnitude);
            }
        }
    }
}
