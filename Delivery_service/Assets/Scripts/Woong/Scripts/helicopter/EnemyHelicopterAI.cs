using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DeliveryService.Mok
{
    public sealed class EnemyHelicopterAI : MonoBehaviour  // AI에 힘을 좀
    {
        public static EnemyHelicopterAI Current { get; private set; }

        public enum AIState
        {
            IdleWaiting,            // 플레이어가 F로 헬기에 타기 전 대기
            TakeoffEscape,          // 플레이어 예열 중 먼저 목표 고도(Y=150)까지 이륙하며 도주
            Phase1_Combat,          // 1페이즈: 지정 구역 자유 비행 + 피격 회피 + 반격
            Phase2_RetreatToOcean,  // HP 40% 이하: 지정 바다 구역까지 고속 도주
            Phase2_OceanBoss,       // 2페이즈: 바다 상공 구역 내에서만 선회하며 강화 공격
            Crashing,               // HP 0: 바다로 회전하며 추락
            Destroyed               // 바다 충돌 완료 (엔딩 트리거)
        }

        [Header("1. 모델링 및 프로펠러 연결")]
        public Transform visualBody;
        public Transform mainRotor;
        public Transform tailRotor;
        public Transform weaponMuzzle;
        [Tooltip("2페이즈(HP 40% 이하) 돌입 시 켤 연기/화염 파티클 오브젝트 (선택)")]
        public GameObject phase2SmokeEffect;

        [Header("2. 구역 및 돔 경계 설정 (Scene 뷰 기즈모로 확인 가능!)")]
        [Tooltip("전체 파란색 돔 중심")]
        public Transform domeCenter;
        public float domeMaxRadius = 400f;

        [Tooltip("1페이즈 자유 비행 구역 중심 (도시 상공)")]
        public Transform phase1Center;
        public float phase1Radius = 220f;

        [Tooltip("2페이즈 바다 결전 구역 중심 (바다 위 빈 오브젝트)")]
        public Transform oceanZoneCenter;
        public float oceanZoneRadius = 150f;

        [Header("3. 고도 및 속도 설정")]
        [Tooltip("이륙 직후 목표 비행 고도 (기본 Y=150)")]
        public float targetCruiseAltitude = 150f;
        public float minAltitude = 85f;
        public float maxAltitude = 185f;
        [Tooltip("플레이어 헬기와 동일한 기본 추격 속도")]
        public float baseFlySpeed = 38f;
        [Tooltip("2페이즈 바다 구역 이동 시 가속 배율")]
        public float phase2RetreatSpeedMultiplier = 1.45f;
        [Tooltip("플레이어와 절대 충돌하지 않도록 유지하는 최소 거리")]
        public float minDistanceFromPlayer = 32f;

        [Header("4. 체력 및 공격 패턴 설정")]
        public float maxHealth = 1000f;
        public float phase2HealthRatio = 0.40f;
        public GameObject enemyBulletPrefab;
        public GameObject enemyMissilePrefab;
        public float minigunAttackCooldown = 4.2f;
        public float missileAttackCooldown = 7.5f;

        [Header("5. 바다 추락 엔딩 설정")]
        public float waterLevelY = 0f;
        public UnityEvent onShotDownInOcean;

        public AIState State { get; private set; } = AIState.IdleWaiting;
        public float CurrentHealth { get; private set; }
        public float HealthRatio => Mathf.Clamp01(CurrentHealth / Mathf.Max(1f, maxHealth));
        public bool IsDead => State == AIState.Crashing || State == AIState.Destroyed;
        public Vector3 CurrentVelocity { get; private set; }

        private PlayerHelicopterController playerHeli;
        private Vector3 currentWaypoint;
        private float nextWaypointTime;
        private float nextMinigunTime;
        private float nextMissileTime;

        // 피격 시 회피 기동(Jink) 변수
        private Vector3 evadeOffsetVel;
        private float evadeTimer;
        private float nextEvadeCooldown;

        // GTA 5 스타일 시각 기울기
        private float smoothPitch;
        private float smoothRoll;
        private float rotorRpm;
        private bool isFiringPattern;

        private void Awake()
        {
            Current = this;
            CurrentHealth = maxHealth;
            if (phase2SmokeEffect != null) phase2SmokeEffect.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // ★ 7번 요청: 플레이어가 F키로 탑승할 때만 호출되어 이륙 및 도주 시작!
        public void StartEscapeSequence(PlayerHelicopterController player)
        {
            if (State != AIState.IdleWaiting) return;
            playerHeli = player;
            // 플레이어 속도와 자연스럽게 동기화
            baseFlySpeed = Mathf.Max(25f, player.maxForwardSpeed);
            State = AIState.TakeoffEscape;

            nextMinigunTime = Time.time + 6f;
            nextMissileTime = Time.time + 9f;
        }

        public void TakeDamage(float amount, Vector3 hitWorldPos)
        {
            if (State == AIState.IdleWaiting || IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            // 3번 요청: 총을 맞으면 조금씩 좌우/상하로 회피 기동(Jink) 수행
            if (Time.time >= nextEvadeCooldown && State != AIState.Phase2_RetreatToOcean)
            {
                nextEvadeCooldown = Time.time + 1.4f;
                evadeTimer = 0.85f;
                float sideSign = Random.value > 0.5f ? 1f : -1f;
                evadeOffsetVel = transform.right * (sideSign * 16f) + Vector3.up * Random.Range(-5f, 6f);
            }

            // 3번·4번 요청: HP가 40% 이하가 되면 즉시 2페이즈 바다 구역으로 고속 이동 시작!
            if (State == AIState.Phase1_Combat && HealthRatio <= phase2HealthRatio)
            {
                State = AIState.Phase2_RetreatToOcean;
                if (phase2SmokeEffect != null) phase2SmokeEffect.SetActive(true);
            }

            // HP 0 -> 바다 추락 엔딩 시퀀스 진입
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
            else evadeOffsetVel = Vector3.Lerp(evadeOffsetVel, Vector3.zero, Time.deltaTime * 4f);

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

            ApplyGtaHelicopterTilt();
        }

        private void SpinRotors()
        {
            float targetRpm = State == AIState.IdleWaiting ? 0.15f : (State == AIState.Destroyed ? 0f : 1f);
            rotorRpm = Mathf.MoveTowards(rotorRpm, targetRpm, Time.deltaTime * 0.8f);

            if (mainRotor != null) mainRotor.Rotate(Vector3.up, 1300f * rotorRpm * Time.deltaTime, Space.Self);
            if (tailRotor != null) tailRotor.Rotate(Vector3.right, 1900f * rotorRpm * Time.deltaTime, Space.Self);
        }

        // 플레이어가 예열하는 동안 목표 고도(Y=150)까지 먼저 날아오르며 도주
        private void UpdateTakeoffEscape()
        {
            Vector3 escapeDir = phase1Center != null
                ? (phase1Center.position - transform.position)
                : transform.forward * 80f;
            escapeDir.y = 0f;
            if (escapeDir.sqrMagnitude < 1f) escapeDir = transform.forward;
            escapeDir.Normalize();

            Vector3 targetPos = transform.position + escapeDir * 25f;
            targetPos.y = targetCruiseAltitude;

            MoveHelicopterTowards(targetPos, baseFlySpeed * 0.85f, false);

            if (Mathf.Abs(transform.position.y - targetCruiseAltitude) < 8f)
            {
                State = AIState.Phase1_Combat;
                PickRandomWaypointInZone(phase1Center, phase1Radius);
            }
        }

        private void UpdatePhase1Combat()
        {
            if (Time.time >= nextWaypointTime || Vector3.Distance(transform.position, currentWaypoint) < 22f)
            {
                PickRandomWaypointInZone(phase1Center, phase1Radius);
            }

            MoveHelicopterTowards(currentWaypoint, baseFlySpeed, false);
            TryExecuteAttacks(isPhase2: false);
        }

        private void UpdatePhase2Retreat()
        {
            Vector3 oceanPos = oceanZoneCenter != null ? oceanZoneCenter.position : (transform.position + transform.forward * 150f);
            oceanPos.y = targetCruiseAltitude;

            // 지정된 바다 구역까지 빠른 속도로 이동
            MoveHelicopterTowards(oceanPos, baseFlySpeed * phase2RetreatSpeedMultiplier, false);

            float flatDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(oceanPos.x, oceanPos.z));
            if (flatDist <= oceanZoneRadius * 0.65f)
            {
                State = AIState.Phase2_OceanBoss;
                PickRandomWaypointInZone(oceanZoneCenter, oceanZoneRadius);
                nextMinigunTime = Time.time + 1.5f;
                nextMissileTime = Time.time + 2.5f;
            }
        }

        private void UpdatePhase2OceanBoss()
        {
            // 바다 구역 안에서만 플레이어 주변을 선회하며 공격적으로 비행
            if (Time.time >= nextWaypointTime || Vector3.Distance(transform.position, currentWaypoint) < 20f)
            {
                PickRandomWaypointInZone(oceanZoneCenter, oceanZoneRadius);
            }

            MoveHelicopterTowards(currentWaypoint, baseFlySpeed * 1.05f, facePlayerWhenClose: true);
            TryExecuteAttacks(isPhase2: true);
        }

        private void PickRandomWaypointInZone(Transform centerTransform, float radius)
        {
            Vector3 center = centerTransform != null ? centerTransform.position : Vector3.zero;
            Vector2 rnd = Random.insideUnitCircle * (radius * 0.85f);
            float y = Mathf.Clamp(targetCruiseAltitude + Random.Range(-25f, 20f), minAltitude, maxAltitude);

            Vector3 candidate = new Vector3(center.x + rnd.x, y, center.z + rnd.y);

            // 플레이어에게 너무 가까운 지점은 제외
            if (playerHeli != null && Vector3.Distance(candidate, playerHeli.transform.position) < minDistanceFromPlayer * 1.5f)
            {
                Vector3 away = (candidate - playerHeli.transform.position).normalized;
                candidate = center + away * (radius * 0.7f);
                candidate.y = y;
            }

            currentWaypoint = ClampInsideDome(candidate);
            nextWaypointTime = Time.time + Random.Range(3.5f, 6.0f);
        }

        private void MoveHelicopterTowards(Vector3 targetPos, float speed, bool facePlayerWhenClose)
        {
            Vector3 toTarget = targetPos - transform.position;
            Vector3 desiredVel = toTarget.sqrMagnitude > 1f ? toTarget.normalized * speed : Vector3.zero;

            // ★ 5번 요청: 플레이어 헬리콥터와 충돌 방지(능동적 회피 벡터)
            if (playerHeli != null)
            {
                Vector3 fromPlayer = transform.position - playerHeli.transform.position;
                float distToPlayer = fromPlayer.magnitude;

                if (distToPlayer < minDistanceFromPlayer)
                {
                    float pushStrength = Mathf.InverseLerp(minDistanceFromPlayer, 8f, distToPlayer);
                    Vector3 pushDir = fromPlayer.sqrMagnitude > 0.01f ? fromPlayer.normalized : transform.right;
                    // 수평으로 비켜나며 고도도 살짝 올려 충돌 원천 차단
                    Vector3 avoidVel = (pushDir + Vector3.up * 0.35f).normalized * (speed * 1.35f * (0.5f + pushStrength));
                    desiredVel = Vector3.Lerp(desiredVel, avoidVel, 0.85f);
                }
            }

            // 피격 회피(Jink) 속도 합산
            desiredVel += evadeOffsetVel;

            // 최대 속도 제한 및 부드러운 관성 이동
            if (desiredVel.magnitude > speed * 1.35f)
            {
                desiredVel = desiredVel.normalized * (speed * 1.35f);
            }

            CurrentVelocity = Vector3.Lerp(CurrentVelocity, desiredVel, Time.deltaTime * 2.8f);
            Vector3 nextPos = transform.position + CurrentVelocity * Time.deltaTime;

            // 2페이즈 바다 구역 이탈 방지 강제 클램프
            if (State == AIState.Phase2_OceanBoss && oceanZoneCenter != null)
            {
                nextPos = ClampInsideZone(nextPos, oceanZoneCenter.position, oceanZoneRadius);
            }

            // 전체 파란색 돔 내부 및 고도 클램프
            nextPos = ClampInsideDome(nextPos);
            nextPos.y = Mathf.Clamp(nextPos.y, minAltitude, maxAltitude);
            transform.position = nextPos;

            // 기수 회전 (2페이즈나 공격 시에는 플레이어를 향해 선회, 평상시에는 비행 방향으로 선회)
            Vector3 lookDir = CurrentVelocity;
            if ((facePlayerWhenClose || isFiringPattern) && playerHeli != null)
            {
                lookDir = playerHeli.transform.position - transform.position;
            }
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.5f)
            {
                Quaternion targetYawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetYawRot, Time.deltaTime * 2.6f);
            }
        }

        // ★ 6번 요청: 적 헬기 역시 GTA 5처럼 실제 이동 속도에 맞춰 기체가 앞뒤/좌우로 자연스럽게 기울어짐
        private void ApplyGtaHelicopterTilt()
        {
            if (visualBody == null) return;

            Vector3 localVel = transform.InverseTransformDirection(CurrentVelocity);
            float normFwd = Mathf.Clamp(localVel.z / Mathf.Max(1f, baseFlySpeed), -1f, 1f);
            float normSide = Mathf.Clamp(localVel.x / Mathf.Max(1f, baseFlySpeed), -1f, 1f);

            float targetPitch = normFwd * 20f;
            float targetRoll = -normSide * 24f;

            smoothPitch = Mathf.Lerp(smoothPitch, targetPitch, Time.deltaTime * 4f);
            smoothRoll = Mathf.Lerp(smoothRoll, targetRoll, Time.deltaTime * 4f);
            visualBody.localRotation = Quaternion.Euler(smoothPitch, 0f, smoothRoll);
        }

        private void TryExecuteAttacks(bool isPhase2)
        {
            if (playerHeli == null || !playerHeli.IsAirborne || isFiringPattern) return;

            float dist = Vector3.Distance(transform.position, playerHeli.transform.position);
            if (dist > 280f) return;

            // 미사일 공격 (2페이즈는 쿨타임이 짧고 2연발 발사!)
            if (Time.time >= nextMissileTime)
            {
                nextMissileTime = Time.time + (isPhase2 ? missileAttackCooldown * 0.65f : missileAttackCooldown);
                StartCoroutine(FireMissileRoutine(isPhase2 ? 2 : 1));
                return;
            }

            // 미니건 점사 공격
            if (Time.time >= nextMinigunTime)
            {
                nextMinigunTime = Time.time + (isPhase2 ? minigunAttackCooldown * 0.7f : minigunAttackCooldown);
                StartCoroutine(FireMinigunBurstRoutine(isPhase2 ? 12 : 7));
            }
        }

        private IEnumerator FireMinigunBurstRoutine(int shotCount)
        {
            isFiringPattern = true;
            for (int i = 0; i < shotCount; i++)
            {
                if (IsDead || playerHeli == null) break;

                Vector3 spawnPos = weaponMuzzle != null ? weaponMuzzle.position : (transform.position + transform.forward * 3f - Vector3.up * 0.8f);
                // 약간의 탄퍼짐을 주어 플레이어가 기동으로 회피 가능하게 설계
                Vector3 aimPos = playerHeli.transform.position + Random.insideUnitSphere * 3.8f;
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

        private IEnumerator FireMissileRoutine(int missileCount)
        {
            isFiringPattern = true;
            if (playerHeli != null) playerHeli.NotifyMissileIncoming();
            yield return new WaitForSeconds(0.6f); // 플레이어가 경고를 보고 회피 준비할 0.6초 여유

            for (int i = 0; i < missileCount; i++)
            {
                if (IsDead || playerHeli == null) break;

                Vector3 spawnPos = weaponMuzzle != null ? weaponMuzzle.position : (transform.position + transform.forward * 3.5f - Vector3.up);
                Vector3 dir = (playerHeli.transform.position - spawnPos).normalized;

                if (enemyMissilePrefab != null)
                {
                    GameObject obj = Instantiate(enemyMissilePrefab, spawnPos, Quaternion.LookRotation(dir));
                    var proj = obj.GetComponent<HelicopterCombatProjectile>() ?? obj.AddComponent<HelicopterCombatProjectile>();
                    proj.Launch(dir, false, true, 35f, 68f, playerHeli.transform);
                }
                else
                {
                    HelicopterCombatProjectile.SpawnFallback(spawnPos, dir, false, true, 35f, 68f, playerHeli.transform);
                }

                yield return new WaitForSeconds(0.55f);
            }
            isFiringPattern = false;
        }

        private void UpdateCrashingIntoOcean()
        {
            // 꼬리 로터 파손 연출: 빙글빙글 돌며 바다 수면으로 추락
            transform.Rotate(Vector3.up, 260f * Time.deltaTime, Space.World);
            if (visualBody != null) visualBody.Rotate(new Vector3(40f, 0f, 95f) * Time.deltaTime, Space.Self);

            Vector3 crashTarget = oceanZoneCenter != null
                ? new Vector3(oceanZoneCenter.position.x, waterLevelY, oceanZoneCenter.position.z)
                : new Vector3(transform.position.x, waterLevelY, transform.position.z);

            Vector3 horizDir = (crashTarget - transform.position);
            horizDir.y = 0f;
            Vector3 fallVel = horizDir.normalized * 18f + Vector3.down * 36f;
            transform.position += fallVel * Time.deltaTime;

            if (transform.position.y <= waterLevelY)
            {
                State = AIState.Destroyed;
                onShotDownInOcean?.Invoke(); // 엔딩 연출(시네마틱/씬 전환) 호출!
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

        // ★ Scene 뷰에서 돔(파랑), 1페이즈(노랑), 2페이즈 바다 구역(빨강) 범위를 눈으로 보며 조절!
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