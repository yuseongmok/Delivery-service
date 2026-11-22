using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerHelicopterController : MonoBehaviour, IInteractable
    {
        public static PlayerHelicopterController Current { get; private set; }

        [Header("1. 모델링 및 프로펠러 연결")]
        public Transform visualBody;
        public Transform mainRotor;
        public Transform tailRotor;
        public Camera helicopterCamera;
        public Transform seatAnchor;

        [Header("2. 프로펠러 예열 설정 (Space 길게 누르기)")]
        public float spoolUpDuration = 2.8f;
        public float liftoffRpmThreshold = 0.75f;
        public float maxMainRotorSpinSpeed = 1350f;
        public float maxTailRotorSpinSpeed = 2000f;

        [Header("3. 묵직한 GTA 5 비행 속도 · 관성 · 반동")]
        public float maxForwardSpeed = 26f;
        public float maxReverseSpeed = 14f;
        public float maxVerticalSpeed = 14f;
        [Tooltip("가속 시 얼마나 묵직하게 속도가 붙는지 (낮을수록 무거움)")]
        public float moveAcceleration = 1.25f;
        [Tooltip("키를 뗐을 때 관성으로 미끄러지는 감속력 (낮을수록 오래 밀림)")]
        public float moveDeceleration = 0.85f;
        public float verticalResponsiveness = 1.8f;

        [Tooltip("A/D 최대 회전 속도 및 회전 관성")]
        public float yawTurnSpeed = 62f;
        public float yawAcceleration = 2.6f;

        [Tooltip("W/S/A/D 입력 및 가감속 반동에 따른 최대 기울기 각도")]
        public float forwardPitchAngle = 25f;
        public float backwardPitchAngle = -20f;
        public float turnBankAngle = 32f;
        [Tooltip("급가속/급제동 시 기체가 추가로 출렁이는 반동 강도")]
        public float accelRecoilStrength = 0.65f;
        public float tiltSpringSpeed = 3.2f;

        [Header("4. 마우스 자유 시점(둘러보기) 설정")]
        public float mouseLookSensitivity = 0.18f;
        public float cameraDistance = 16f;
        public float cameraHeight = 5.4f;
        public float minCamPitch = -18f;
        public float maxCamPitch = 42f;
        [Tooltip("마우스를 멈추면 잠시 뒤 카메라가 정면으로 자동 복귀할지 여부")]
        public bool autoRecenterCamera = true;
        public float recenterDelay = 2.2f;

        [Header("5. 돔 경계 및 고도 제한")]
        public Transform domeCenter;
        public float domeMaxRadius = 420f;
        public float minAltitude = 2f;
        public float maxAltitude = 195f;

        [Header("6. 좌우 교차 게틀링 건 & 사격 반동")]
        public Transform leftMuzzle;
        public Transform rightMuzzle;
        public GameObject bulletPrefab;
        public float fireInterval = 0.085f;
        public float gatlingDamage = 14f;
        public float bulletSpeed = 160f;
        public float convergenceDistance = 85f;
        public float aimAssistAngle = 12f;
        public float aimAssistRange = 260f;
        public float firingCameraShake = 0.12f;

        [Header("7. 플레이어 헬기 체력 & 추격 대상")]
        public float maxHealth = 250f;
        public EnemyHelicopterAI enemyHelicopter;

        public bool IsPiloting { get; private set; }
        public float CurrentRpm { get; private set; }
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

        // 비행 물리 & 관성 변수
        private float currentYaw;
        private float currentYawVelocity;
        private float currentPitch;
        private float currentRoll;
        private Vector3 currentVelocity;
        private Vector3 previousVelocity;
        private Vector3 bodyBaseLocalPos;
        private bool hasBodyBasePos;

        // 마우스 둘러보기 카메라 변수
        private float camOrbitYaw;
        private float camOrbitPitch = 8f;
        private float lastMouseMoveTime;

        // 사격 및 피격 반동 변수
        private float nextFireTime;
        private float recoilKickPitch;
        private float cameraShakePower;
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

            if (visualBody != null)
            {
                bodyBaseLocalPos = visualBody.localPosition;
                hasBodyBasePos = true;
            }
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

            if (playerMove)
            {
                playerMove.isControllable = false;
                playerMove.enabled = false;
            }
            if (playerLook) playerLook.isControllable = false;
            if (playerInteraction) playerInteraction.enabled = false;
            if (playerCC) playerCC.enabled = false;

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

            camOrbitYaw = 0f;
            camOrbitPitch = 8f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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
            helicopterCamera.fieldOfView = 66f;
            helicopterCamera.farClipPlane = 2000f;
            helicopterCamera.transform.position = transform.position - transform.forward * cameraDistance + Vector3.up * cameraHeight;
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
            cameraShakePower = Mathf.Max(cameraShakePower, fromMissile ? 0.65f : 0.22f);
            recoilKickPitch += fromMissile ? -7f : -2.2f;

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = maxHealth;
            }
        }

        private void Update()
        {
            SpinRotors();

            if (!IsPiloting || Time.timeScale <= 0) return;

            if (damageFlashTimer > 0f) damageFlashTimer -= Time.deltaTime;
            if (missileWarningTimer > 0f) missileWarningTimer -= Time.deltaTime;
            cameraShakePower = Mathf.Lerp(cameraShakePower, 0f, Time.deltaTime * 8f);
            recoilKickPitch = Mathf.Lerp(recoilKickPitch, 0f, Time.deltaTime * 7f);

            UpdateMouseFreeLook();
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

            // 헬기 진행 방향(currentYaw) + 마우스 둘러보기 오프셋(camOrbitYaw/Pitch) 조합
            Quaternion orbitRot = Quaternion.Euler(camOrbitPitch, currentYaw + camOrbitYaw, 0f);
            Vector3 pivot = transform.position + Vector3.up * 2.2f;
            Vector3 desiredCamPos = pivot - (orbitRot * Vector3.forward) * cameraDistance + Vector3.up * (cameraHeight - 2.2f);

            // 카메라 지연 추종으로 비행 가속감·속도감 강조
            helicopterCamera.transform.position = Vector3.Lerp(helicopterCamera.transform.position, desiredCamPos, Time.deltaTime * 10f);

            Vector3 shakeOffset = cameraShakePower > 0.005f ? Random.insideUnitSphere * cameraShakePower : Vector3.zero;
            helicopterCamera.transform.position += shakeOffset;

            Vector3 lookTarget = pivot + (orbitRot * Vector3.forward) * 35f;
            Quaternion desiredLookRot = Quaternion.LookRotation((lookTarget - helicopterCamera.transform.position).normalized, Vector3.up);
            helicopterCamera.transform.rotation = Quaternion.Slerp(helicopterCamera.transform.rotation, desiredLookRot, Time.deltaTime * 14f);
        }

        private void UpdateMouseFreeLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.04f)
            {
                camOrbitYaw += delta.x * mouseLookSensitivity;
                camOrbitPitch = Mathf.Clamp(camOrbitPitch - delta.y * mouseLookSensitivity, minCamPitch, maxCamPitch);
                lastMouseMoveTime = Time.time;
            }
            else if (autoRecenterCamera && IsAirborne && (Time.time - lastMouseMoveTime) > recenterDelay)
            {
                // 마우스를 안 움직이면 서서히 기체 정면 뒤쪽으로 복귀 (GTA 5 차량/헬기 카메라 방식)
                camOrbitYaw = Mathf.LerpAngle(camOrbitYaw, 0f, Time.deltaTime * 2.2f);
                camOrbitPitch = Mathf.Lerp(camOrbitPitch, 8f, Time.deltaTime * 2.2f);
            }
        }

        private void SpinRotors()
        {
            if (CurrentRpm <= 0.001f) return;
            Transform refAxis = visualBody != null ? visualBody : transform;
            if (mainRotor != null)
                mainRotor.Rotate(refAxis.up, maxMainRotorSpinSpeed * CurrentRpm * Time.deltaTime, Space.World);
            if (tailRotor != null)
                tailRotor.Rotate(refAxis.right, maxTailRotorSpinSpeed * CurrentRpm * Time.deltaTime, Space.World);
        }

        private void UpdateSpoolAndFlightInputs()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool pressUp = kb.spaceKey.isPressed;
            bool pressDown = kb.leftShiftKey.isPressed;
            float forwardInput = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float turnInput = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);

            // 1. 지면 예열
            if (pressUp)
            {
                float accelFactor = Mathf.Lerp(0.55f, 1.35f, CurrentRpm);
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 1f, (Time.deltaTime / spoolUpDuration) * accelFactor);
            }
            else if (!IsAirborne)
            {
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0f, Time.deltaTime * 0.25f);
            }
            else
            {
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0.95f, Time.deltaTime * 0.5f);
            }

            if (!IsAirborne)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 3f);
                currentYawVelocity = Mathf.Lerp(currentYawVelocity, 0f, Time.deltaTime * 3f);
                ApplyDynamicVisualTilt(0f, 0f, Vector3.zero);
                return;
            }

            // 2. 관성이 느껴지는 좌우 회전 (A/D)
            float targetYawVel = turnInput * yawTurnSpeed;
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, targetYawVel, Time.deltaTime * yawAcceleration);
            currentYaw += currentYawVelocity * Time.deltaTime;

            Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

            // 3. 수평 가감속과 수직 상승·하강을 분리해 묵직한 비행 관성 구현
            Vector3 horizVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float targetHorizSpeed = forwardInput >= 0f ? forwardInput * maxForwardSpeed : forwardInput * maxReverseSpeed;
            Vector3 targetHorizVel = flatForward * targetHorizSpeed;

            float horizLerpRate = Mathf.Abs(forwardInput) > 0.05f ? moveAcceleration : moveDeceleration;
            horizVel = Vector3.Lerp(horizVel, targetHorizVel, Time.deltaTime * horizLerpRate);

            float verticalInput = (pressUp ? 1f : 0f) - (pressDown ? 1f : 0f);
            float targetVertVel = verticalInput * maxVerticalSpeed;
            float newVertVel = Mathf.Lerp(currentVelocity.y, targetVertVel, Time.deltaTime * verticalResponsiveness);

            previousVelocity = currentVelocity;
            currentVelocity = new Vector3(horizVel.x, newVertVel, horizVel.z);

            // 실제 가속도 벡터 계산 (급가속/급감속/드리프트 시 반동 쏠림용)
            Vector3 worldAccel = (currentVelocity - previousVelocity) / Mathf.Max(0.001f, Time.deltaTime);
            Vector3 localAccel = transform.InverseTransformDirection(worldAccel);

            // 4. 입력 + 실제 관성 가속도 반동을 합친 역동적 기체 기울기
            float inputPitch = forwardInput > 0.05f ? forwardPitchAngle : (forwardInput < -0.05f ? backwardPitchAngle : 0f);
            float dynamicPitch = inputPitch + Mathf.Clamp(localAccel.z * accelRecoilStrength, -12f, 12f) + recoilKickPitch;

            float yawRatio = currentYawVelocity / Mathf.Max(1f, yawTurnSpeed);
            float dynamicRoll = (-yawRatio * turnBankAngle) + Mathf.Clamp(-localAccel.x * accelRecoilStrength * 1.1f, -14f, 14f);

            ApplyDynamicVisualTilt(dynamicPitch, dynamicRoll, localAccel);
        }

        private void ApplyDynamicVisualTilt(float targetPitch, float targetRoll, Vector3 localAccel)
        {
            if (visualBody == null) return;

            // 공중 호버링 시 자연스러운 바람 흔들림(Micro-turbulence)
            float hoverPitch = IsAirborne ? Mathf.Sin(Time.time * 1.9f) * 1.4f : 0f;
            float hoverRoll = IsAirborne ? Mathf.Cos(Time.time * 1.5f) * 1.8f : 0f;

            currentPitch = Mathf.Lerp(currentPitch, Mathf.Clamp(targetPitch + hoverPitch, -34f, 34f), Time.deltaTime * tiltSpringSpeed);
            currentRoll = Mathf.Lerp(currentRoll, Mathf.Clamp(targetRoll + hoverRoll, -42f, 42f), Time.deltaTime * tiltSpringSpeed);
            visualBody.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);

            // 급상승/급하강 시 동체가 살짝 들리거나 눌리는 반동 위치 연출
            if (hasBodyBasePos)
            {
                float verticalBounce = IsAirborne ? Mathf.Clamp(-localAccel.y * 0.015f, -0.18f, 0.18f) + Mathf.Sin(Time.time * 2.3f) * 0.05f : 0f;
                Vector3 desiredPos = bodyBaseLocalPos + new Vector3(0f, verticalBounce, 0f);
                visualBody.localPosition = Vector3.Lerp(visualBody.localPosition, desiredPos, Time.deltaTime * 5f);
            }
        }

        private void EnforceDomeAndAltitudeBounds()
        {
            Vector3 pos = rb.position;

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

                // 게틀링 발사 시 묵직한 사격 반동과 미세한 카메라 진동 추가
                cameraShakePower = Mathf.Max(cameraShakePower, firingCameraShake);
                recoilKickPitch = Mathf.Clamp(recoilKickPitch - 0.55f, -3.5f, 3.5f);
            }
        }

        private Vector3 CalculateConvergencePoint(out bool lockedOn)
        {
            lockedOn = false;
            Vector3 camPos = helicopterCamera.transform.position;
            Vector3 camFwd = helicopterCamera.transform.forward;

            // 마우스로 옆이나 뒤를 보고 있을 때는 헬기 정면 35도 이내로 사격 각도를 제한해 꼬리를 뚫고 쏘지 않도록 보호
            Vector3 heliFwd = transform.forward;
            Vector3 fireAimDir = Vector3.Angle(heliFwd, camFwd) <= 35f
                ? camFwd
                : Vector3.RotateTowards(heliFwd, camFwd, 35f * Mathf.Deg2Rad, 0f);

            if (enemyHelicopter != null && !enemyHelicopter.IsDead)
            {
                Vector3 enemyPos = enemyHelicopter.transform.position;
                Vector3 toEnemy = enemyPos - transform.position;
                float dist = toEnemy.magnitude;

                if (dist <= aimAssistRange && Vector3.Angle(fireAimDir, toEnemy) <= aimAssistAngle)
                {
                    lockedOn = true;
                    float travelTime = dist / Mathf.Max(10f, bulletSpeed);
                    return enemyPos + enemyHelicopter.CurrentVelocity * (travelTime * 0.45f);
                }
            }

            return camPos + fireAimDir * convergenceDistance;
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

            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            GUI.DrawTexture(new Rect(40f, 820f, 440f, 220f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            styleSub.normal.textColor = Color.white;
            GUI.Label(new Rect(60f, 835f, 400f, 30f), $"엔진 예열 RPM : {Mathf.RoundToInt(CurrentRpm * 100)}% {(IsAirborne ? "(비행 가능)" : "(Space 꾹 눌러 예열)")}", styleSub);
            GUI.Label(new Rect(60f, 870f, 400f, 30f), $"속도 : {currentVelocity.magnitude:0} m/s  |  고도 : {transform.position.y:0} m", styleSub);
            GUI.Label(new Rect(60f, 905f, 400f, 30f), $"헬기 내구도 : {CurrentHealth:0} / {maxHealth:0}", styleSub);
            styleSub.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
            GUI.Label(new Rect(60f, 945f, 400f, 80f), "[Space/Shift] 상승·하강 | [W/S/A/D] 비행\n[마우스 이동] 주변 둘러보기 | [좌클릭] 게틀링", styleSub);

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