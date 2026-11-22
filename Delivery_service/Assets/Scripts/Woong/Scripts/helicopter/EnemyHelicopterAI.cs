using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DeliveryService.Mok
{
    public sealed class EnemyHelicopterAI : MonoBehaviour
    {
        public static EnemyHelicopterAI Current { get; private set; }

        public enum AIState
        {
            IdleWaiting,
            TakeoffEscape,
            Phase1_Combat,
            Phase2_RetreatToOcean,
            Phase2_OceanBoss,
            Crashing,
            Destroyed
        }

        [Header("모델")]
        public Transform visualBody;
        public Transform mainRotor;
        public Transform tailRotor;
        public Transform weaponMuzzle;
        public Transform leftMissilePod;
        public Transform rightMissilePod;
        public GameObject phase2SmokeEffect;

        [Header("구역")]
        public Transform domeCenter;
        public float domeMaxRadius = 400f;
        public Transform phase1Center;
        public float phase1Radius = 220f;
        public Transform oceanZoneCenter;
        public float oceanZoneRadius = 150f;

        [Header("비행")]
        public float targetCruiseAltitude = 150f;
        public float minAltitude = 85f;
        public float maxAltitude = 185f;
        public float baseFlySpeed = 21f;
        public float phase2RetreatSpeedMultiplier = 1.4f;
        public float steeringAcceleration = 14f;
        public float airDrag = 0.65f;
        public float turnResponsiveness = 1.45f;
        public float minDistanceFromPlayer = 34f;

        [Header("전투")]
        public float maxHealth = 1000f;
        public float phase2HealthRatio = 0.40f;
        public GameObject enemyBulletPrefab;
        public GameObject enemyMissilePrefab;
        public float minigunAttackCooldown = 4.4f;
        public float missileAttackCooldown = 6.8f;

        [Header("추락")]
        public float waterLevelY = 0f;
        public UnityEvent onShotDownInOcean;

        public AIState State { get; private set; } = AIState.IdleWaiting;
        public float CurrentHealth { get; private set; }
        public float HealthRatio => Mathf.Clamp01(CurrentHealth / Mathf.Max(1f, maxHealth));
        public bool IsDead => State == AIState.Crashing || State == AIState.Destroyed;
        public Vector3 CurrentVelocity { get; private set; }

        private PlayerHelicopterController playerHeli;
        private Vector3 currentWaypoint;
        private Vector3 previousVelocity;
        private float previousYaw;
        private float nextWaypointTime;
        private float nextMinigunTime;
        private float nextMissileTime;
        private int lastMissilePatternIndex = -1;

        private Vector3 evadeOffsetVel;
        private float evadeTimer;
        private float nextEvadeCooldown;

        private float currentPitch;
        private float pitchVel;
        private float currentRoll;
        private float rollVel;
        private float rotorRpm;
        private bool isFiringPattern;

        private void Awake()
        {
            Current = this;
            CurrentHealth = maxHealth;
            previousYaw = transform.eulerAngles.y;
            if (phase2SmokeEffect != null) phase2SmokeEffect.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void StartEscapeSequence(PlayerHelicopterController player)
        {
            if (State != AIState.IdleWaiting) return;
            playerHeli = player;
            baseFlySpeed = Mathf.Clamp(player.maxForwardSpeed * 0.88f, 18f, 24f);
            State = AIState.TakeoffEscape;

            nextMinigunTime = Time.time + 5.5f;
            nextMissileTime = Time.time + 8.0f;
        }

        public void TakeDamage(float amount, Vector3 hitWorldPos)
        {
            if (State == AIState.IdleWaiting || IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            Vector3 localHitDir = transform.InverseTransformPoint(hitWorldPos);
            rollVel += (localHitDir.x >= 0f ? 1f : -1f) * 42f;
            pitchVel += Random.Range(-25f, 25f);

            if (Time.time >= nextEvadeCooldown && State != AIState.Phase2_RetreatToOcean)
            {
                nextEvadeCooldown = Time.time + 2.0f;
                evadeTimer = 1.1f;
                float sideSign = Random.value > 0.5f ? 1f : -1f;
                evadeOffsetVel = transform.right * (sideSign * 11f) + Vector3.up * Random.Range(-3.5f, 4.5f);
            }

            if (State == AIState.Phase1_Combat && HealthRatio <= phase2HealthRatio)
            {
                State = AIState.Phase2_RetreatToOcean;
                if (phase2SmokeEffect != null) phase2SmokeEffect.SetActive(true);
            }

            if (CurrentHealth <= 0f)
            {
                State = AIState.Crashing;
                StopAllCoroutines();
                if (phase2SmokeEffect != null) phase2SmokeEffect.SetActive(true);
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0) return;

            SpinRotors();

            if (State == AIState.IdleWaiting || State == AIState.Destroyed) return;

            if (State == AIState.Crashing)
            {
                UpdateCrashingIntoOcean();
                return;
            }

            if (evadeTimer > 0f) evadeTimer -= Time.deltaTime;
            else evadeOffsetVel = Vector3.Lerp(evadeOffsetVel, Vector3.zero, Time.deltaTime * 2.0f);

            switch (State)
            {
                case AIState.TakeoffEscape:
                    UpdateTakeoffEscape();
                    break;
                case AIState.Phase1_Combat:
                    UpdatePhase1Combat();
                    break;
                case AIState.Phase2_RetreatToOcean:
                    UpdatePhase2Retreat();
                    break;
                case AIState.Phase2_OceanBoss:
                    UpdatePhase2OceanBoss();
                    break;
            }

            ApplyGtaSpringTilt();
        }

        private void SpinRotors()
        {
            float targetRpm = State == AIState.IdleWaiting ? 0.15f : (State == AIState.Destroyed ? 0f : 1f);
            rotorRpm = Mathf.MoveTowards(rotorRpm, targetRpm, Time.deltaTime * 0.8f);

            Transform refAxis = visualBody != null ? visualBody : transform;
            if (mainRotor != null)
                mainRotor.Rotate(refAxis.up, 1300f * rotorRpm * Time.deltaTime, Space.World);
            if (tailRotor != null)
                tailRotor.Rotate(refAxis.right, 1900f * rotorRpm * Time.deltaTime, Space.World);
        }

        private void UpdateTakeoffEscape()
        {
            Vector3 escapeDir = phase1Center != null
                ? (phase1Center.position - transform.position)
                : transform.forward * 80f;
            escapeDir.y = 0f;
            if (escapeDir.sqrMagnitude < 1f) escapeDir = transform.forward;
            escapeDir.Normalize();

            Vector3 targetPos = transform.position + escapeDir * 30f;
            targetPos.y = targetCruiseAltitude;

            MoveHelicopterWithInertia(targetPos, baseFlySpeed * 0.85f, false);

            if (Mathf.Abs(transform.position.y - targetCruiseAltitude) < 8f)
            {
                State = AIState.Phase1_Combat;
                PickRandomWaypointInZone(phase1Center, phase1Radius);
            }
        }

        private void UpdatePhase1Combat()
        {
            if (Time.time >= nextWaypointTime || Vector3.Distance(transform.position, currentWaypoint) < 26f)
            {
                PickRandomWaypointInZone(phase1Center, phase1Radius);
            }

            MoveHelicopterWithInertia(currentWaypoint, baseFlySpeed, false);
            TryExecuteAttacks(isPhase2: false);
        }

        private void UpdatePhase2Retreat()
        {
            Vector3 oceanPos = oceanZoneCenter != null ? oceanZoneCenter.position : (transform.position + transform.forward * 150f);
            oceanPos.y = targetCruiseAltitude;

            MoveHelicopterWithInertia(oceanPos, baseFlySpeed * phase2RetreatSpeedMultiplier, false);

            float flatDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(oceanPos.x, oceanPos.z));
            if (flatDist <= oceanZoneRadius * 0.65f)
            {
                State = AIState.Phase2_OceanBoss;
                PickRandomWaypointInZone(oceanZoneCenter, oceanZoneRadius);
                nextMinigunTime = Time.time + 1.5f;
                nextMissileTime = Time.time + 2.2f;
            }
        }

        private void UpdatePhase2OceanBoss()
        {
            if (Time.time >= nextWaypointTime || Vector3.Distance(transform.position, currentWaypoint) < 24f)
            {
                PickRandomWaypointInZone(oceanZoneCenter, oceanZoneRadius);
            }

            MoveHelicopterWithInertia(currentWaypoint, baseFlySpeed * 1.05f, facePlayerWhenClose: true);
            TryExecuteAttacks(isPhase2: true);
        }

        private void PickRandomWaypointInZone(Transform centerTransform, float radius)
        {
            Vector3 center = centerTransform != null ? centerTransform.position : Vector3.zero;
            Vector2 rnd = Random.insideUnitCircle * (radius * 0.85f);
            float y = Mathf.Clamp(targetCruiseAltitude + Random.Range(-22f, 18f), minAltitude, maxAltitude);

            Vector3 candidate = new Vector3(center.x + rnd.x, y, center.z + rnd.y);

            if (playerHeli != null && Vector3.Distance(candidate, playerHeli.transform.position) < minDistanceFromPlayer * 1.8f)
            {
                Vector3 away = (candidate - playerHeli.transform.position).normalized;
                candidate = center + away * (radius * 0.7f);
                candidate.y = y;
            }

            currentWaypoint = ClampInsideDome(candidate);
            nextWaypointTime = Time.time + Random.Range(4.5f, 7.0f);
        }

        private void MoveHelicopterWithInertia(Vector3 targetPos, float maxSpeed, bool facePlayerWhenClose)
        {
            float dt = Time.deltaTime;
            Vector3 toTarget = targetPos - transform.position;
            Vector3 desiredDir = toTarget.sqrMagnitude > 1f ? toTarget.normalized : transform.forward;
            Vector3 steerForce = desiredDir * steeringAcceleration;

            // 플레이어 충돌 회피
            if (playerHeli != null)
            {
                Vector3 fromPlayer = transform.position - playerHeli.transform.position;
                float distToPlayer = fromPlayer.magnitude;

                if (distToPlayer < minDistanceFromPlayer)
                {
                    float pushStrength = Mathf.InverseLerp(minDistanceFromPlayer, 10f, distToPlayer);
                    Vector3 pushDir = fromPlayer.sqrMagnitude > 0.01f ? fromPlayer.normalized : transform.right;
                    steerForce += (pushDir + Vector3.up * 0.35f).normalized * (steeringAcceleration * (1.2f + pushStrength * 1.4f));
                }
            }

            steerForce += evadeOffsetVel;

            previousVelocity = CurrentVelocity;
            Vector3 newVel = CurrentVelocity + steerForce * dt;
            newVel *= Mathf.Exp(-airDrag * dt);

            if (newVel.magnitude > maxSpeed * 1.25f)
            {
                newVel = Vector3.Lerp(newVel, newVel.normalized * (maxSpeed * 1.25f), dt * 3f);
            }

            CurrentVelocity = newVel;
            Vector3 nextPos = transform.position + CurrentVelocity * dt;

            if (State == AIState.Phase2_OceanBoss && oceanZoneCenter != null)
            {
                nextPos = ClampInsideZone(nextPos, oceanZoneCenter.position, oceanZoneRadius);
            }

            nextPos = ClampInsideDome(nextPos);
            nextPos.y = Mathf.Clamp(nextPos.y, minAltitude, maxAltitude);
            transform.position = nextPos;

            Vector3 lookDir = CurrentVelocity;
            if ((facePlayerWhenClose || isFiringPattern) && playerHeli != null)
            {
                lookDir = playerHeli.transform.position - transform.position;
            }
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.5f)
            {
                Quaternion targetYawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetYawRot, dt * turnResponsiveness);
            }
        }

        private void ApplyGtaSpringTilt()
        {
            if (visualBody == null) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 localVel = transform.InverseTransformDirection(CurrentVelocity);
            Vector3 localAccel = transform.InverseTransformDirection((CurrentVelocity - previousVelocity) / dt);

            float curYaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(previousYaw, curYaw) / dt;
            previousYaw = curYaw;

            float normFwd = Mathf.Clamp(localVel.z / Mathf.Max(1f, baseFlySpeed), -1f, 1f);
            float normSide = Mathf.Clamp(localVel.x / Mathf.Max(1f, baseFlySpeed), -1f, 1f);

            float hoverPitch = Mathf.Sin(Time.time * 1.9f) * 1.8f;
            float hoverRoll = Mathf.Cos(Time.time * 1.6f) * 2.2f;

            float targetPitch = Mathf.Clamp(normFwd * 24f + localAccel.z * 0.85f + hoverPitch, -34f, 34f);
            float targetRoll = Mathf.Clamp(-normSide * 30f - yawRate * 0.55f - localAccel.x * 0.9f + hoverRoll, -46f, 46f);

            pitchVel += (targetPitch - currentPitch) * 16f * dt;
            pitchVel *= Mathf.Exp(-4.0f * dt);
            currentPitch = Mathf.Clamp(currentPitch + pitchVel * dt, -38f, 38f);

            rollVel += (targetRoll - currentRoll) * 16f * dt;
            rollVel *= Mathf.Exp(-4.0f * dt);
            currentRoll = Mathf.Clamp(currentRoll + rollVel * dt, -48f, 48f);

            visualBody.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
        }

        private void TryExecuteAttacks(bool isPhase2)
        {
            if (playerHeli == null || !playerHeli.IsAirborne || playerHeli.IsChaseFailed || isFiringPattern) return;

            float dist = Vector3.Distance(transform.position, playerHeli.transform.position);
            if (dist > 310f) return;

            if (Time.time >= nextMissileTime)
            {
                nextMissileTime = Time.time + (isPhase2 ? missileAttackCooldown * 0.68f : missileAttackCooldown);
                StartCoroutine(ExecuteVariedMissilePattern(isPhase2));
                return;
            }

            if (Time.time >= nextMinigunTime)
            {
                nextMinigunTime = Time.time + (isPhase2 ? minigunAttackCooldown * 0.72f : minigunAttackCooldown);
                StartCoroutine(FireMinigunBurstRoutine(isPhase2 ? 12 : 7));
            }
        }

        private IEnumerator ExecuteVariedMissilePattern(bool isPhase2)
        {
            isFiringPattern = true;

            int patternCount = isPhase2 ? 4 : 3;
            int pattern = Random.Range(0, patternCount);
            if (pattern == lastMissilePatternIndex) pattern = (pattern + 1) % patternCount;
            lastMissilePatternIndex = pattern;

            string warningText = pattern switch
            {
                0 => isPhase2 ? "4연발 교차 유도 미사일 발사!" : "3연발 교차 유도 미사일 발사!",
                1 => isPhase2 ? "5발 부채꼴 일제 미사일 발사!" : "3발 부채꼴 확산 미사일 발사!",
                2 => isPhase2 ? "상공 곡사 낙하 미사일 3발 접근!" : "상공 곡사 낙하 미사일 접근!",
                _ => "좌우 포위 협공 미사일 4발 접근!"
            };

            playerHeli?.NotifyMissileIncoming(warningText);
            yield return new WaitForSeconds(0.65f);

            if (IsDead || playerHeli == null) { isFiringPattern = false; yield break; }

            switch (pattern)
            {
                case 0: // 교차 연사
                    {
                        int count = isPhase2 ? 4 : 3;
                        for (int i = 0; i < count; i++)
                        {
                            if (IsDead || playerHeli == null) break;
                            bool useLeft = (i % 2 == 0);
                            Vector3 podPos = GetMissilePodPosition(useLeft);
                            Vector3 toPlayer = (playerHeli.transform.position - podPos).normalized;
                            Vector3 sideArc = (toPlayer + (useLeft ? -transform.right : transform.right) * 0.35f).normalized;
                            SpawnMissile(podPos, sideArc, 30f, 66f, sideArc, 0.35f);
                            yield return new WaitForSeconds(0.32f);
                        }
                        break;
                    }
                case 1: // 부채꼴 확산
                    {
                        int count = isPhase2 ? 5 : 3;
                        Vector3 basePos = weaponMuzzle != null ? weaponMuzzle.position : (transform.position + transform.forward * 3.5f - Vector3.up);
                        Vector3 toPlayer = (playerHeli.transform.position - basePos).normalized;
                        float spreadStep = 14f;
                        float startAngle = -(count - 1) * 0.5f * spreadStep;

                        for (int i = 0; i < count; i++)
                        {
                            float yawOffset = startAngle + i * spreadStep;
                            Vector3 spreadDir = Quaternion.AngleAxis(yawOffset, Vector3.up) * toPlayer;
                            SpawnMissile(basePos, spreadDir, 28f, 62f, spreadDir, 0.45f);
                        }
                        yield return new WaitForSeconds(0.4f);
                        break;
                    }
                case 2: // 상공 곡사
                    {
                        int count = isPhase2 ? 3 : 2;
                        for (int i = 0; i < count; i++)
                        {
                            if (IsDead || playerHeli == null) break;
                            Vector3 podPos = GetMissilePodPosition(i % 2 == 0);
                            Vector3 toPlayer = (playerHeli.transform.position - podPos).normalized;
                            Vector3 upArcDir = (toPlayer * 0.45f + Vector3.up * 0.9f).normalized;
                            SpawnMissile(podPos, upArcDir, 34f, 68f, upArcDir, 0.85f);
                            yield return new WaitForSeconds(0.38f);
                        }
                        break;
                    }
                case 3: // 포위 협공
                    {
                        Vector3 leftPos = GetMissilePodPosition(true);
                        Vector3 rightPos = GetMissilePodPosition(false);
                        Vector3 toPlayer = (playerHeli.transform.position - transform.position).normalized;

                        Vector3 leftWide = (toPlayer * 0.4f - transform.right * 0.85f + Vector3.up * 0.25f).normalized;
                        Vector3 rightWide = (toPlayer * 0.4f + transform.right * 0.85f + Vector3.up * 0.25f).normalized;

                        SpawnMissile(leftPos, leftWide, 30f, 70f, leftWide, 0.75f);
                        SpawnMissile(rightPos, rightWide, 30f, 70f, rightWide, 0.75f);
                        yield return new WaitForSeconds(0.45f);

                        if (!IsDead && playerHeli != null)
                        {
                            Vector3 leftLow = (toPlayer * 0.5f - transform.right * 0.7f - Vector3.up * 0.25f).normalized;
                            Vector3 rightLow = (toPlayer * 0.5f + transform.right * 0.7f - Vector3.up * 0.25f).normalized;
                            SpawnMissile(leftPos, leftLow, 30f, 70f, leftLow, 0.75f);
                            SpawnMissile(rightPos, rightLow, 30f, 70f, rightLow, 0.75f);
                        }
                        break;
                    }
            }

            isFiringPattern = false;
        }

        private Vector3 GetMissilePodPosition(bool left)
        {
            if (left && leftMissilePod != null) return leftMissilePod.position;
            if (!left && rightMissilePod != null) return rightMissilePod.position;
            Vector3 basePos = weaponMuzzle != null ? weaponMuzzle.position : (transform.position + transform.forward * 2.5f - Vector3.up * 0.6f);
            return basePos + (left ? -transform.right : transform.right) * 2.2f;
        }

        private void SpawnMissile(Vector3 pos, Vector3 launchDir, float dmg, float spd, Vector3 arcDir, float arcTime)
        {
            if (enemyMissilePrefab != null)
            {
                GameObject obj = Instantiate(enemyMissilePrefab, pos, Quaternion.LookRotation(launchDir));
                var proj = obj.GetComponent<HelicopterCombatProjectile>() ?? obj.AddComponent<HelicopterCombatProjectile>();
                proj.Launch(launchDir, false, true, dmg, spd, playerHeli.transform, 0f, arcDir, arcTime);
            }
            else
            {
                HelicopterCombatProjectile.SpawnFallback(pos, launchDir, false, true, dmg, spd, playerHeli.transform, 0f, arcDir, arcTime);
            }
        }

        private IEnumerator FireMinigunBurstRoutine(int shotCount)
        {
            isFiringPattern = true;
            for (int i = 0; i < shotCount; i++)
            {
                if (IsDead || playerHeli == null) break;

                Vector3 spawnPos = weaponMuzzle != null ? weaponMuzzle.position : (transform.position + transform.forward * 3f - Vector3.up * 0.8f);
                Vector3 aimPos = playerHeli.transform.position + Random.insideUnitSphere * 4.2f;
                Vector3 dir = (aimPos - spawnPos).normalized;

                if (enemyBulletPrefab != null)
                {
                    GameObject obj = Instantiate(enemyBulletPrefab, spawnPos, Quaternion.LookRotation(dir));
                    var proj = obj.GetComponent<HelicopterCombatProjectile>() ?? obj.AddComponent<HelicopterCombatProjectile>();
                    proj.Launch(dir, false, false, 8f, 115f);
                }
                else
                {
                    HelicopterCombatProjectile.SpawnFallback(spawnPos, dir, false, false, 8f, 115f);
                }

                yield return new WaitForSeconds(0.11f);
            }
            isFiringPattern = false;
        }

        private void UpdateCrashingIntoOcean()
        {
            transform.Rotate(Vector3.up, 260f * Time.deltaTime, Space.World);
            if (visualBody != null) visualBody.Rotate(new Vector3(40f, 0f, 95f) * Time.deltaTime, Space.Self);

            Vector3 crashTarget = oceanZoneCenter != null
                ? new Vector3(oceanZoneCenter.position.x, waterLevelY, oceanZoneCenter.position.z)
                : new Vector3(transform.position.x, waterLevelY, transform.position.z);

            Vector3 horizDir = crashTarget - transform.position;
            horizDir.y = 0f;
            Vector3 fallVel = horizDir.normalized * 18f + Vector3.down * 36f;
            transform.position += fallVel * Time.deltaTime;

            if (transform.position.y <= waterLevelY)
            {
                State = AIState.Destroyed;
                onShotDownInOcean?.Invoke();
            }
        }

        private Vector3 ClampInsideDome(Vector3 pos)
        {
            if (domeCenter == null) return pos;
            return ClampInsideZone(pos, domeCenter.position, domeMaxRadius);
        }

        private Vector3 ClampInsideZone(Vector3 pos, Vector3 center, float radius)
        {
            Vector2 offset = new Vector2(pos.x - center.x, pos.z - center.z);
            if (offset.magnitude > radius)
            {
                Vector2 clamped = offset.normalized * radius;
                pos.x = center.x + clamped.x;
                pos.z = center.z + clamped.y;
            }
            return pos;
        }

        private void OnDrawGizmos()
        {
            if (domeCenter != null)
            {
                Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.6f);
                Gizmos.DrawWireSphere(new Vector3(domeCenter.position.x, targetCruiseAltitude, domeCenter.position.z), domeMaxRadius);
            }
            if (phase1Center != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
                Gizmos.DrawWireSphere(new Vector3(phase1Center.position.x, targetCruiseAltitude, phase1Center.position.z), phase1Radius);
            }
            if (oceanZoneCenter != null)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(new Vector3(oceanZoneCenter.position.x, targetCruiseAltitude, oceanZoneCenter.position.z), oceanZoneRadius);
            }
        }
    }
}