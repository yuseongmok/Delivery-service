using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerHelicopterController : MonoBehaviour, IInteractable
    {
        public static PlayerHelicopterController Current { get; private set; }

        [Header("모델링 및 프로펠러 연결")]
        public Transform visualBody;
        public Transform mainRotor;
        public Transform tailRotor;
        public Camera helicopterCamera;
        public Transform seatAnchor;

        [Header("프로펠러 예열 설정")]
        public float spoolUpDuration = 2.8f;
        public float liftoffRpmThreshold = 0.75f;
        public float maxMainRotorSpinSpeed = 1350f;
        public float maxTailRotorSpinSpeed = 2000f;

        [Header("비행 속도 및 기울기")]
        public float maxForwardSpeed = 38f;
        public float maxReverseSpeed = 20f;
        public float maxVerticalSpeed = 22f;
        public float yawTurnSpeed = 75f;
        public float moveAcceleration = 3.2f;
        [Tooltip("W 전진 시 앞머리가 숙여지는 각도")]
        public float forwardPitchAngle = 20f;
        [Tooltip("S 후진 시 앞머리가 들리는 각도")]
        public float backwardPitchAngle = -16f;
        [Tooltip("A/D 선회 시 안쪽으로 눕는 뱅킹 각도")]
        public float turnBankAngle = 18f;
        public float tiltSmoothSpeed = 4.5f;

        [Header("경계 및 고도 제한")]
        public Transform domeCenter;
        [Tooltip("돔 내부에서 비행 가능한 최대 수평 반경")]
        public float domeMaxRadius = 420f;
        public float minAltitude = 2f;
        public float maxAltitude = 195f;

        [Header("좌우 교차 게틀링 건 설정")]
        public Transform leftMuzzle;
        public Transform rightMuzzle;
        public GameObject bulletPrefab;
        public float fireInterval = 0.08f;
        public float gatlingDamage = 14f;
        public float bulletSpeed = 160f;
        [Tooltip("좌우 총알이 교차하는 기본 거리 (m)")]
        public float convergenceDistance = 85f;
        [Tooltip("적 헬기가 조준선 몇 도 이내일 때 소프트 짐벌 보정이 걸릴지")]
        public float aimAssistAngle = 11f;
        public float aimAssistRange = 260f;

        [Header("플레이어 체력")]
        public float maxHealth = 250f;
        public EnemyHelicopterAI enemyHelicopter;

        public bool IsPiloting { get; private set; }
        public float CurrentRpm { get; private set; } // 0 ~ 1
        public float CurrentHealth { get; private set; }
        public bool IsAirborne => CurrentRpm >= liftoffRpmThreshold;
        public bool HasTargetLock { get; private set; }

        private Rigidbody rb;
        private Transform boardedPlayer;
        private CharacterController playerCC;
        private PlayerMove playerMove;
        private CameraMove playerLook;
        private PlayerInteraction playerInteraction;
        private Camera playerViewCam;

        private float currentYaw;
        private float currentPitch;
        private float currentRoll;
        private Vector3 currentVelocity;
        private float nextFireTime;
        private float damageFlashTimer;
        private float missileWarningTimer;

        private GUIStyle styleHud, styleWarn, styleSub;

        private void Awake()
        {
            Current = this;
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            CurrentHealth = maxHealth;
            currentYaw = transform.eulerAngles.y;

            if (helicopterCamera != null) helicopterCamera.enabled = false;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            if (enemyHelicopter == null) enemyHelicopter = FindFirstObjectByType<EnemyHelicopterAI>();
        }
        public void Interact(ToppingInventory inventory)
        {
            if (IsPiloting || inventory == null) return;
            BoardHelicopter(inventory.transform);
        }

        public void BoardHelicopter(Transform playerRoot)
        {
            if (IsPiloting || playerRoot == null) return;

            IsPiloting = true;
            boardedPlayer = playerRoot;
            playerCC = playerRoot.GetComponent<CharacterController>();
            playerMove = playerRoot.GetComponent<PlayerMove>();
            playerLook = playerRoot.GetComponentInChildren<CameraMove>();
            playerInteraction = playerRoot.GetComponent<PlayerInteraction>();

            var combat = playerRoot.GetComponent<PlayerCombat>();
            playerViewCam = combat != null ? combat.View : playerRoot.GetComponentInChildren<Camera>();

            if (playerMove) playerMove.isControllable = false;
            if (playerLook) playerLook.isControllable = false;
            if (playerInteraction) playerInteraction.enabled = false;
            if (playerCC) playerCC.enabled = false;

            // 플레이어를 헬기 좌석에 고정하고 기존 카메라 끄기
            if (seatAnchor != null)
            {
                playerRoot.SetParent(seatAnchor);
                playerRoot.localPosition = Vector3.zero;
                playerRoot.localRotation = Quaternion.identity;
            }
            else
            {
                playerRoot.SetParent(transform);
                playerRoot.localPosition = Vector3.zero;
            }

            if (playerViewCam != null)
            {
                playerViewCam.enabled = false;
                var ears = playerViewCam.GetComponent<AudioListener>();
                if (ears) ears.enabled = false;
            }

            EnsureHelicopterCamera();
            helicopterCamera.enabled = true;
            var heliEars = helicopterCamera.GetComponent<AudioListener>();
            if (heliEars) heliEars.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //플레이어가 F키로 헬기에 탑승하는 순간 적 헬기가 먼저 이륙해 도망 시작
            if (enemyHelicopter != null)
            {
                enemyHelicopter.StartEscapeSequence(this);
            }
        }

        private void EnsureHelicopterCamera()
        {
            if (helicopterCamera != null) return;
            GameObject camObj = new GameObject("PlayerHeliChaseCam");
            helicopterCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            helicopterCamera.fieldOfView = 65f;
            helicopterCamera.farClipPlane = 2000f;
            helicopterCamera.transform.position = transform.position - transform.forward * 14f + Vector3.up * 5f;
            helicopterCamera.transform.LookAt(transform.position + transform.forward * 20f);
        }

        public void NotifyMissileIncoming()
        {
            missileWarningTimer = 2.4f;
        }

        public void TakeDamage(float dmg, bool fromMissile)
        {
            if (!IsPiloting) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            damageFlashTimer = fromMissile ? 0.45f : 0.2f;

            if (CurrentHealth <= 0f)
            {
                // 플레이어 헬기 파괴 시 체력 복구 후 재도전 또는 실패 처리 연동 가능
                CurrentHealth = maxHealth;
            }
        }

        private void Update()
        {
            SpinRotors();

            if (!IsPiloting || Time.timeScale <= 0) return;

            if (damageFlashTimer > 0f) damageFlashTimer -= Time.deltaTime;
            if (missileWarningTimer > 0f) missileWarningTimer -= Time.deltaTime;

            UpdateSpoolAndFlightInputs();
            UpdateGatlingWeapons();
        }

        private void FixedUpdate()
        {
            if (!IsPiloting) return;

            rb.linearVelocity = currentVelocity;
            rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));

            EnforceDomeAndAltitudeBounds();
        }

        private void LateUpdate()
        {
            if (!IsPiloting || helicopterCamera == null) return;
            Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
            Vector3 desiredCamPos = transform.position - flatForward * 15.5f + Vector3.up * 5.2f;
            helicopterCamera.transform.position = Vector3.Lerp(helicopterCamera.transform.position, desiredCamPos, Time.deltaTime * 8f);

            Vector3 lookPoint = transform.position + flatForward * 45f + Vector3.up * 1.5f;
            Quaternion desiredCamRot = Quaternion.LookRotation((lookPoint - helicopterCamera.transform.position).normalized, Vector3.up);
            helicopterCamera.transform.rotation = Quaternion.Slerp(helicopterCamera.transform.rotation, desiredCamRot, Time.deltaTime * 10f);
        }

        private void SpinRotors()
        {
            if (CurrentRpm <= 0.001f) return;
            if (mainRotor != null)
                mainRotor.Rotate(Vector3.up, maxMainRotorSpinSpeed * CurrentRpm * Time.deltaTime, Space.Self);
            if (tailRotor != null)
                tailRotor.Rotate(Vector3.right, maxTailRotorSpinSpeed * CurrentRpm * Time.deltaTime, Space.Self);
        }

        private void UpdateSpoolAndFlightInputs()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool pressUp = kb.spaceKey.isPressed;
            bool pressDown = kb.leftShiftKey.isPressed;
            float forwardInput = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float turnInput = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);

            if (pressUp)
            {
                // 초반에는 묵직하게, 40% 이후엔 빠르게 가속되는 느낌
                float accelFactor = Mathf.Lerp(0.55f, 1.35f, CurrentRpm);
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 1f, (Time.deltaTime / spoolUpDuration) * accelFactor);
            }
            else if (!IsAirborne)
            {
                // 이륙 전 지면에서 Space를 떼면 서서히 감속
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0f, Time.deltaTime * 0.25f);
            }
            else
            {
                // 공중에 한 번 뜬 뒤에는 엔진 RPM을 안정적으로 유지
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0.95f, Time.deltaTime * 0.5f);
            }

            if (!IsAirborne)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 5f);
                ApplyVisualTilt(0f, 0f);
                return;
            }
            currentYaw += turnInput * yawTurnSpeed * Time.deltaTime;
            Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

            float targetHorizSpeed = forwardInput >= 0f ? forwardInput * maxForwardSpeed : forwardInput * maxReverseSpeed;
            float verticalInput = (pressUp ? 1f : 0f) - (pressDown ? 1f : 0f);
            float targetVertSpeed = verticalInput * maxVerticalSpeed;

            Vector3 targetVel = flatForward * targetHorizSpeed + Vector3.up * targetVertSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVel, Time.deltaTime * moveAcceleration);

            float targetPitch = forwardInput > 0.05f ? forwardPitchAngle : (forwardInput < -0.05f ? backwardPitchAngle : 0f);
            float targetRoll = -turnInput * turnBankAngle;
            ApplyVisualTilt(targetPitch, targetRoll);
        }

        private void ApplyVisualTilt(float targetPitch, float targetRoll)
        {
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * tiltSmoothSpeed);
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * tiltSmoothSpeed);

            if (visualBody != null)
            {
                visualBody.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
            }
        }

        private void EnforceDomeAndAltitudeBounds()
        {
            Vector3 pos = rb.position;

            // 고도 제한
            if (pos.y < minAltitude)
            {
                pos.y = minAltitude;
                if (currentVelocity.y < 0f) currentVelocity.y = 0f;
            }
            else if (pos.y > maxAltitude)
            {
                pos.y = maxAltitude;
                if (currentVelocity.y > 0f) currentVelocity.y = 0f;
            }

            // 파란 돔 내부 수평 반경 제한 
            if (domeCenter != null)
            {
                Vector3 center = domeCenter.position;
                Vector2 flatOffset = new Vector2(pos.x - center.x, pos.z - center.z);
                float dist = flatOffset.magnitude;
                if (dist > domeMaxRadius)
                {
                    Vector2 clamped = flatOffset.normalized * domeMaxRadius;
                    pos.x = center.x + clamped.x;
                    pos.z = center.z + clamped.y;

                    Vector3 inward = new Vector3(-flatOffset.x, 0f, -flatOffset.y).normalized;
                    if (Vector3.Dot(currentVelocity, inward) < 0f)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, inward);
                    }
                }
            }

            rb.position = pos;
        }

        private void UpdateGatlingWeapons()
        {
            var mouse = Mouse.current;
            if (mouse == null || helicopterCamera == null) return;

            Vector3 aimPoint = CalculateConvergencePoint(out bool locked);
            HasTargetLock = locked;

            if (mouse.leftButton.isPressed && Time.time >= nextFireTime && IsAirborne)
            {
                nextFireTime = Time.time + fireInterval;
                FireFromMuzzle(leftMuzzle, aimPoint);
                FireFromMuzzle(rightMuzzle, aimPoint);
            }
        }

        private Vector3 CalculateConvergencePoint(out bool lockedOn)
        {
            lockedOn = false;
            Vector3 camPos = helicopterCamera.transform.position;
            Vector3 camFwd = helicopterCamera.transform.forward;

            if (enemyHelicopter != null && !enemyHelicopter.IsDead)
            {
                Vector3 enemyPos = enemyHelicopter.transform.position;
                Vector3 toEnemy = enemyPos - camPos;
                float dist = toEnemy.magnitude;

                if (dist <= aimAssistRange && Vector3.Angle(camFwd, toEnemy) <= aimAssistAngle)
                {
                    lockedOn = true;
                    float travelTime = dist / Mathf.Max(10f, bulletSpeed);
                    return enemyPos + enemyHelicopter.CurrentVelocity * (travelTime * 0.45f);
                }
            }

            return camPos + camFwd * convergenceDistance;
        }

        private void FireFromMuzzle(Transform muzzle, Vector3 targetPoint)
        {
            Vector3 spawnPos = muzzle != null ? muzzle.position : (transform.position + transform.forward * 2.5f);
            Vector3 fireDir = (targetPoint - spawnPos).normalized;

            if (bulletPrefab != null)
            {
                GameObject obj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(fireDir));
                var proj = obj.GetComponent<HelicopterCombatProjectile>() ?? obj.AddComponent<HelicopterCombatProjectile>();
                proj.Launch(fireDir, true, false, gatlingDamage, bulletSpeed);
            }
            else
            {
                HelicopterCombatProjectile.SpawnFallback(spawnPos, fireDir, true, false, gatlingDamage, bulletSpeed);
            }
        }

        private void OnGUI()
        {
            if (!IsPiloting || Time.timeScale <= 0) return;

            Matrix4x4 prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));

            styleHud ??= new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleWarn ??= new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleSub ??= new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };

            // 중앙 크로스헤어
            Color crossColor = HasTargetLock ? new Color(1f, 0.25f, 0.2f, 0.95f) : new Color(0.25f, 1f, 0.45f, 0.85f);
            GUI.color = crossColor;
            GUI.DrawTexture(new Rect(960f - 22f, 540f - 2f, 14f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(960f + 8f, 540f - 2f, 14f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(960f - 2f, 540f - 22f, 4f, 14f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(960f - 2f, 540f + 8f, 4f, 14f), Texture2D.whiteTexture);

            if (HasTargetLock)
            {
                styleHud.normal.textColor = new Color(1f, 0.3f, 0.2f);
                GUI.Label(new Rect(960f - 100f, 565f, 200f, 32f), "[ LOCK-ON ]", styleHud);
            }

            //좌측 하단 계기판
            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            GUI.DrawTexture(new Rect(40f, 830f, 420f, 210f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            styleSub.normal.textColor = Color.white;
            GUI.Label(new Rect(60f, 845f, 380f, 30f), $"엔진 예열 RPM : {Mathf.RoundToInt(CurrentRpm * 100)}% {(IsAirborne ? "(비행 가능)" : "(Space 꾹 눌러 예열)")}", styleSub);
            GUI.Label(new Rect(60f, 880f, 380f, 30f), $"현재 고도 (Y) : {transform.position.y:0} m", styleSub);
            GUI.Label(new Rect(60f, 915f, 380f, 30f), $"헬기 내구도 : {CurrentHealth:0} / {maxHealth:0}", styleSub);
            styleSub.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
            GUI.Label(new Rect(60f, 955f, 380f, 70f), "[Space/Shift] 상승·하강 | [W/S/A/D] 비행\n[좌클릭] 좌우 교차 게틀링 사격", styleSub);

            //  상단 적 헬기 체력바 및 페이즈 안내
            if (enemyHelicopter != null && enemyHelicopter.State != EnemyHelicopterAI.AIState.IdleWaiting)
            {
                GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.9f);
                GUI.DrawTexture(new Rect(560f, 30f, 800f, 78f), Texture2D.whiteTexture);

                string phaseText = enemyHelicopter.State switch
                {
                    EnemyHelicopterAI.AIState.TakeoffEscape => "적 헬리콥터 이륙 중! 예열을 마치고 추격하세요!",
                    EnemyHelicopterAI.AIState.Phase1_Combat => "1페이즈: 도주하는 적 헬리콥터를 격추하세요!",
                    EnemyHelicopterAI.AIState.Phase2_RetreatToOcean => "적 HP 40% 이하! 바다 상공으로 도주 중 ? 추격하세요!",
                    EnemyHelicopterAI.AIState.Phase2_OceanBoss => "2페이즈 결전: 바다 위에서 적 헬리콥터를 완전히 격추하세요!",
                    EnemyHelicopterAI.AIState.Crashing => "격추 성공! 적 헬리콥터가 바다로 추락합니다!",
                    _ => "임무 완료"
                };

                styleHud.normal.textColor = new Color(1f, 0.8f, 0.3f);
                GUI.color = Color.white;
                GUI.Label(new Rect(560f, 36f, 800f, 32f), phaseText, styleHud);

                Rect hpBg = new Rect(610f, 74f, 700f, 20f);
                GUI.color = new Color(0.2f, 0.2f, 0.25f);
                GUI.DrawTexture(hpBg, Texture2D.whiteTexture);
                GUI.color = enemyHelicopter.HealthRatio <= 0.4f ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0.15f);
                GUI.DrawTexture(new Rect(hpBg.x, hpBg.y, hpBg.width * enemyHelicopter.HealthRatio, hpBg.height), Texture2D.whiteTexture);
            }

            //미사일 접근 경고 & 피격 화면 플래시
            if (missileWarningTimer > 0f && Mathf.Sin(Time.time * 18f) > 0f)
            {
                styleWarn.normal.textColor = new Color(1f, 0.2f, 0.2f);
                GUI.color = Color.white;
                GUI.Label(new Rect(560f, 150f, 800f, 55f), "! 경고: 적 미사일 접근 중 ? 선회/고도 변경으로 회피하세요 !", styleWarn);
            }

            if (damageFlashTimer > 0f)
            {
                GUI.color = new Color(1f, 0.1f, 0.1f, damageFlashTimer * 0.6f);
                GUI.DrawTexture(new Rect(0f, 0f, 1920f, 1080f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.matrix = prev;
        }
    }
}