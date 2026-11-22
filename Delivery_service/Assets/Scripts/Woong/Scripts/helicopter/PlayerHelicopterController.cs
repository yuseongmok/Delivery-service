using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerHelicopterController : MonoBehaviour, IInteractable
    {
        public static PlayerHelicopterController Current { get; private set; }

        [Header("모델 / 카메라")]
        public Transform visualBody;
        public Transform mainRotor;
        public Transform tailRotor;
        public Camera helicopterCamera;
        public Transform seatAnchor;

        [Header("예열")]
        public float spoolUpDuration = 2.8f;
        public float liftoffRpmThreshold = 0.75f;
        public float maxMainRotorSpinSpeed = 1350f;
        public float maxTailRotorSpinSpeed = 2000f;

        [Header("비행")]
        public float maxForwardSpeed = 24f;
        public float maxReverseSpeed = 13f;
        public float maxVerticalSpeed = 13f;
        public float tiltThrustPower = 36f;
        public float horizontalAirDrag = 0.85f;
        public float verticalAirDrag = 1.45f;
        public float verticalLiftPower = 24f;
        public float yawTurnSpeed = 56f;
        public float yawInertia = 1.8f;
        public float forwardPitchAngle = 28f;
        public float backwardPitchAngle = -24f;
        public float turnBankAngle = 36f;
        public float tiltSpringStiffness = 18f;
        public float tiltSpringDamping = 4.2f;

        [Header("시점")]
        public float mouseLookSensitivity = 0.18f;
        public float cameraDistance = 16.5f;
        public float cameraHeight = 5.4f;
        public float minCamPitch = -18f;
        public float maxCamPitch = 42f;
        public bool autoRecenterCamera = true;
        public float recenterDelay = 2.2f;

        [Header("구역 제한")]
        public Transform domeCenter;
        public float domeMaxRadius = 420f;
        public float minAltitude = 2f;
        public float maxAltitude = 195f;

        [Header("무기")]
        public Transform leftMuzzle;
        public Transform rightMuzzle;
        public GameObject bulletPrefab;
        public float fireInterval = 0.08f;
        public float gatlingDamage = 14f;
        public float bulletSpeed = 185f;
        public float convergenceDistance = 95f;
        public float aimAssistAngle = 18f;
        public float aimAssistRange = 290f;
        public float bulletMagnetismRate = 22f;
        public float firingCameraShake = 0.14f;
        public float gatlingBaseSpread = 2.4f;
        public float gatlingMaxBloom = 3.2f;
        public float bloomPerShot = 0.18f;
        public float bloomRecoverySpeed = 3.8f;

        [Header("과열")]
        public float heatPerShot = 0.032f;
        public float coolRate = 0.38f;
        public float overheatCoolRate = 0.32f;
        public float activeCoolMin = 0.32f;
        public float activeCoolMax = 0.54f;

        [Header("추격 / 체력")]
        public float maxHealth = 250f;
        public EnemyHelicopterAI enemyHelicopter;
        public float maxChaseDistance = 220f;
        public float escapeFailTime = 10f;
        public UnityEvent onChaseFailed;

        public bool IsPiloting { get; private set; }
        public float CurrentRpm { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsAirborne => CurrentRpm >= liftoffRpmThreshold;
        public bool HasTargetLock { get; private set; }
        public bool IsChaseFailed { get; private set; }
        public Vector3 CurrentVelocity => currentVelocity;

        private Rigidbody rb;
        private Transform boardedPlayer;
        private CharacterController playerCC;
        private PlayerMove playerMove;
        private CameraMove playerLook;
        private PlayerInteraction playerInteraction;
        private Camera playerViewCam;

        private float currentYaw;
        private float currentYawVelocity;
        private float currentPitch;
        private float pitchVelocity;
        private float currentRoll;
        private float rollVelocity;
        private Vector3 currentVelocity;
        private Vector3 bodyBaseLocalPos;
        private bool hasBodyBasePos;

        private float camOrbitYaw;
        private float camOrbitPitch = 8f;
        private float lastMouseMoveTime;

        private float nextFireTime;
        private float currentSpreadBloom;
        private float currentHeat;
        private bool isOverheated;
        private bool activeCoolLocked;
        private float activeCoolSuccessTimer;

        private float cameraShakePower;
        private float damageFlashTimer;
        private float missileWarningTimer;
        private string missileWarningLabel = "적 미사일 접근 중";
        private float hitMarkerTimer;
        private Vector3 predictedLeadWorldPos;
        private bool hasPredictedLead;

        private float escapeCountdown;
        private bool isOutOfRange;
        private float currentEnemyDist;

        private Texture2D redArrowTex;
        private GUIStyle styleHud, styleWarn, styleSub, styleArrowDist, styleSmall;

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
            escapeCountdown = escapeFailTime;

            if (visualBody != null)
            {
                bodyBaseLocalPos = visualBody.localPosition;
                hasBodyBasePos = true;
            }
            if (helicopterCamera != null) helicopterCamera.enabled = false;

            redArrowTex = CreateRedArrowTexture();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (redArrowTex != null) Destroy(redArrowTex);
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
            IsChaseFailed = false;
            escapeCountdown = escapeFailTime;
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

        public void NotifyMissileIncoming(string patternName = "적 미사일 접근 중")
        {
            missileWarningLabel = patternName;
            missileWarningTimer = 3.0f;
        }

        public void NotifyHitConfirmed()
        {
            hitMarkerTimer = 0.16f;
        }

        public void TakeDamage(float dmg, bool fromMissile)
        {
            if (!IsPiloting || IsChaseFailed) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            damageFlashTimer = fromMissile ? 0.45f : 0.2f;
            cameraShakePower = Mathf.Max(cameraShakePower, fromMissile ? 0.7f : 0.25f);

            pitchVelocity += fromMissile ? -55f : -18f;
            rollVelocity += Random.Range(-1f, 1f) * (fromMissile ? 75f : 25f);

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
            if (hitMarkerTimer > 0f) hitMarkerTimer -= Time.deltaTime;
            if (activeCoolSuccessTimer > 0f) activeCoolSuccessTimer -= Time.deltaTime;
            cameraShakePower = Mathf.Lerp(cameraShakePower, 0f, Time.deltaTime * 7f);

            if (Mouse.current == null || !Mouse.current.leftButton.isPressed || isOverheated)
            {
                currentSpreadBloom = Mathf.MoveTowards(currentSpreadBloom, 0f, bloomRecoverySpeed * Time.deltaTime);
            }

            UpdateMouseFreeLook();
            UpdateSpoolAndFlightInputs();
            UpdateOverheatAndCooling();
            UpdateGatlingWeapons();
            UpdateChaseDistanceCheck();
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

            Quaternion orbitRot = Quaternion.Euler(camOrbitPitch, currentYaw + camOrbitYaw, 0f);
            Vector3 pivot = transform.position + Vector3.up * 2.2f;

            float speedPullBack = Mathf.Clamp(currentVelocity.magnitude * 0.08f, 0f, 2.2f);
            Vector3 desiredCamPos = pivot - (orbitRot * Vector3.forward) * (cameraDistance + speedPullBack) + Vector3.up * (cameraHeight - 2.2f);

            helicopterCamera.transform.position = Vector3.Lerp(helicopterCamera.transform.position, desiredCamPos, Time.deltaTime * 7.5f);

            if (cameraShakePower > 0.005f)
            {
                helicopterCamera.transform.position += Random.insideUnitSphere * cameraShakePower;
            }

            Vector3 lookTarget = pivot + (orbitRot * Vector3.forward) * 40f;
            Quaternion desiredLookRot = Quaternion.LookRotation((lookTarget - helicopterCamera.transform.position).normalized, Vector3.up);
            helicopterCamera.transform.rotation = Quaternion.Slerp(helicopterCamera.transform.rotation, desiredLookRot, Time.deltaTime * 11f);
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
                camOrbitYaw = Mathf.LerpAngle(camOrbitYaw, 0f, Time.deltaTime * 2.0f);
                camOrbitPitch = Mathf.Lerp(camOrbitPitch, 8f, Time.deltaTime * 2.0f);
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

            float dt = Time.deltaTime;
            bool pressUp = kb.spaceKey.isPressed;
            bool pressDown = kb.leftShiftKey.isPressed;
            float forwardInput = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float turnInput = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float strafeInput = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);

            // 예열
            if (pressUp)
            {
                float accelFactor = Mathf.Lerp(0.55f, 1.35f, CurrentRpm);
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 1f, (dt / spoolUpDuration) * accelFactor);
            }
            else if (!IsAirborne)
            {
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0f, dt * 0.25f);
            }
            else
            {
                CurrentRpm = Mathf.MoveTowards(CurrentRpm, 0.95f, dt * 0.5f);
            }

            if (!IsAirborne)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, dt * 3f);
                currentYawVelocity = Mathf.Lerp(currentYawVelocity, 0f, dt * 3f);
                UpdateSpringVisualTilt(0f, 0f, 0f, dt);
                return;
            }

            // 회전
            float targetYawVel = turnInput * yawTurnSpeed;
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, targetYawVel, dt * yawInertia);
            currentYaw += currentYawVelocity * dt;

            Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 flatForward = yawRot * Vector3.forward;
            Vector3 flatRight = yawRot * Vector3.right;

            float targetPitch = forwardInput > 0.05f ? forwardPitchAngle : (forwardInput < -0.05f ? backwardPitchAngle : 0f);
            float yawRatio = currentYawVelocity / Mathf.Max(1f, yawTurnSpeed);
            float targetRoll = (-yawRatio * turnBankAngle) + (-strafeInput * 22f);

            // 기울기 기반 수평 추력
            Vector3 horizVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float pitchSin = Mathf.Sin(currentPitch * Mathf.Deg2Rad);
            float rollSin = -Mathf.Sin(currentRoll * Mathf.Deg2Rad);

            Vector3 thrustAccel = flatForward * (pitchSin * tiltThrustPower)
                                + flatRight * (rollSin * tiltThrustPower * 0.42f);

            horizVel += thrustAccel * dt;
            horizVel *= Mathf.Exp(-horizontalAirDrag * dt);

            float maxAllowedHoriz = forwardInput < -0.1f ? maxReverseSpeed : maxForwardSpeed;
            if (horizVel.magnitude > maxAllowedHoriz)
            {
                horizVel = Vector3.Lerp(horizVel, horizVel.normalized * maxAllowedHoriz, dt * 3.5f);
            }

            // 수직 이동
            float verticalInput = (pressUp ? 1f : 0f) - (pressDown ? 1f : 0f);
            float vertVel = currentVelocity.y;
            vertVel += verticalInput * verticalLiftPower * dt;
            if (verticalInput == 0f && Mathf.Abs(currentPitch) > 15f)
            {
                vertVel -= Mathf.Abs(pitchSin) * 1.4f * dt;
            }
            vertVel *= Mathf.Exp(-verticalAirDrag * dt);
            vertVel = Mathf.Clamp(vertVel, -maxVerticalSpeed, maxVerticalSpeed);

            float prevVertVel = currentVelocity.y;
            currentVelocity = new Vector3(horizVel.x, vertVel, horizVel.z);

            float vertAccel = (vertVel - prevVertVel) / Mathf.Max(0.001f, dt);
            UpdateSpringVisualTilt(targetPitch, targetRoll, vertAccel, dt);
        }

        private void UpdateSpringVisualTilt(float targetPitch, float targetRoll, float vertAccel, float dt)
        {
            if (visualBody == null) return;

            float hoverPitch = IsAirborne ? Mathf.Sin(Time.time * 1.7f) * 1.6f : 0f;
            float hoverRoll = IsAirborne ? Mathf.Cos(Time.time * 1.35f) * 2.1f : 0f;

            pitchVelocity += ((targetPitch + hoverPitch) - currentPitch) * tiltSpringStiffness * dt;
            pitchVelocity *= Mathf.Exp(-tiltSpringDamping * dt);
            currentPitch = Mathf.Clamp(currentPitch + pitchVelocity * dt, -38f, 38f);

            rollVelocity += ((targetRoll + hoverRoll) - currentRoll) * tiltSpringStiffness * dt;
            rollVelocity *= Mathf.Exp(-tiltSpringDamping * dt);
            currentRoll = Mathf.Clamp(currentRoll + rollVelocity * dt, -46f, 46f);

            visualBody.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);

            if (hasBodyBasePos)
            {
                float verticalBounce = IsAirborne
                    ? Mathf.Clamp(-vertAccel * 0.018f, -0.24f, 0.24f) + Mathf.Sin(Time.time * 2.1f) * 0.06f
                    : 0f;
                Vector3 desiredPos = bodyBaseLocalPos + new Vector3(0f, verticalBounce, 0f);
                visualBody.localPosition = Vector3.Lerp(visualBody.localPosition, desiredPos, dt * 4.5f);
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

        // 과열 및 액티브 쿨링
        private void UpdateOverheatAndCooling()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (isOverheated)
            {
                currentHeat -= overheatCoolRate * Time.deltaTime;

                if (kb != null && kb.rKey.wasPressedThisFrame && !activeCoolLocked)
                {
                    if (currentHeat >= activeCoolMin && currentHeat <= activeCoolMax)
                    {
                        currentHeat = 0f;
                        isOverheated = false;
                        activeCoolLocked = false;
                        activeCoolSuccessTimer = 0.8f;
                        return;
                    }
                    else
                    {
                        activeCoolLocked = true;
                    }
                }

                if (currentHeat <= 0f)
                {
                    currentHeat = 0f;
                    isOverheated = false;
                    activeCoolLocked = false;
                }
            }
            else if (mouse == null || !mouse.leftButton.isPressed || !IsAirborne)
            {
                currentHeat = Mathf.Max(0f, currentHeat - coolRate * Time.deltaTime);
            }
        }

        // 거리 이탈 카운트다운
        private void UpdateChaseDistanceCheck()
        {
            if (IsChaseFailed || enemyHelicopter == null || !IsAirborne)
            {
                isOutOfRange = false;
                return;
            }

            var st = enemyHelicopter.State;
            bool inActiveChase = st == EnemyHelicopterAI.AIState.Phase1_Combat
                              || st == EnemyHelicopterAI.AIState.Phase2_RetreatToOcean
                              || st == EnemyHelicopterAI.AIState.Phase2_OceanBoss;

            if (!inActiveChase)
            {
                isOutOfRange = false;
                escapeCountdown = escapeFailTime;
                return;
            }

            currentEnemyDist = Vector3.Distance(transform.position, enemyHelicopter.transform.position);

            if (currentEnemyDist > maxChaseDistance)
            {
                isOutOfRange = true;
                escapeCountdown -= Time.deltaTime;
                if (escapeCountdown <= 0f)
                {
                    escapeCountdown = 0f;
                    IsChaseFailed = true;
                    onChaseFailed?.Invoke();
                }
            }
            else
            {
                isOutOfRange = false;
                escapeCountdown = escapeFailTime;
            }
        }

        private void UpdateGatlingWeapons()
        {
            var mouse = Mouse.current;
            if (mouse == null || helicopterCamera == null) return;

            Vector3 aimPoint = CalculateConvergencePoint(out bool locked);
            HasTargetLock = locked;

            if (IsChaseFailed || isOverheated || !IsAirborne) return;

            if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireInterval;
                Transform homingRef = (locked && enemyHelicopter != null) ? enemyHelicopter.transform : null;
                float homing = locked ? bulletMagnetismRate : 0f;

                FireFromMuzzle(leftMuzzle, aimPoint, homingRef, homing);
                FireFromMuzzle(rightMuzzle, aimPoint, homingRef, homing);

                currentSpreadBloom = Mathf.Min(gatlingMaxBloom, currentSpreadBloom + bloomPerShot);
                currentHeat = Mathf.Min(1f, currentHeat + heatPerShot);
                if (currentHeat >= 1f)
                {
                    isOverheated = true;
                    activeCoolLocked = false;
                }

                cameraShakePower = Mathf.Max(cameraShakePower, firingCameraShake);
                pitchVelocity -= 4.2f;
            }
        }

        private Vector3 CalculateConvergencePoint(out bool lockedOn)
        {
            lockedOn = false;
            hasPredictedLead = false;

            Vector3 camPos = helicopterCamera.transform.position;
            Vector3 camFwd = helicopterCamera.transform.forward;

            Vector3 heliFwd = transform.forward;
            Vector3 fireAimDir = Vector3.Angle(heliFwd, camFwd) <= 40f
                ? camFwd
                : Vector3.RotateTowards(heliFwd, camFwd, 40f * Mathf.Deg2Rad, 0f);

            if (enemyHelicopter != null && !enemyHelicopter.IsDead)
            {
                Vector3 enemyPos = enemyHelicopter.transform.position;
                Vector3 toEnemy = enemyPos - transform.position;
                float dist = toEnemy.magnitude;

                float travelTime = dist / Mathf.Max(20f, bulletSpeed);
                predictedLeadWorldPos = enemyPos + enemyHelicopter.CurrentVelocity * (travelTime * 0.65f);
                hasPredictedLead = dist <= aimAssistRange * 1.15f;

                Vector3 toLeadFromCam = predictedLeadWorldPos - camPos;
                if (dist <= aimAssistRange &&
                    (Vector3.Angle(fireAimDir, toEnemy) <= aimAssistAngle || Vector3.Angle(camFwd, toLeadFromCam) <= aimAssistAngle))
                {
                    lockedOn = true;
                    return predictedLeadWorldPos;
                }
            }

            return camPos + fireAimDir * convergenceDistance;
        }

        private void FireFromMuzzle(Transform muzzle, Vector3 targetPoint, Transform homingTarget, float homingRate)
        {
            Vector3 spawnPos = muzzle != null ? muzzle.position : (transform.position + transform.forward * 2.5f);
            Vector3 baseDir = (targetPoint - spawnPos).normalized;

            float totalSpread = gatlingBaseSpread + currentSpreadBloom;
            float spreadX = Random.Range(-totalSpread, totalSpread);
            float spreadY = Random.Range(-totalSpread, totalSpread);
            Vector3 fireDir = Quaternion.LookRotation(baseDir) * Quaternion.Euler(spreadX, spreadY, 0f) * Vector3.forward;

            if (bulletPrefab != null)
            {
                GameObject obj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(fireDir));
                var proj = obj.GetComponent<HelicopterCombatProjectile>() ?? obj.AddComponent<HelicopterCombatProjectile>();
                proj.Launch(fireDir, true, false, gatlingDamage, bulletSpeed, homingTarget, homingRate);
            }
            else
            {
                HelicopterCombatProjectile.SpawnFallback(spawnPos, fireDir, true, false, gatlingDamage, bulletSpeed, homingTarget, homingRate);
            }
        }

        private static Texture2D CreateRedArrowTexture()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2f - 1f;
                    float ny = (y / (float)(size - 1)) * 2f - 1f;

                    float tipLimit = (1f - nx) * 0.55f;
                    bool inBody = nx >= -0.65f && nx <= 0.92f && Mathf.Abs(ny) <= tipLimit && nx >= (Mathf.Abs(ny) * 0.45f - 0.65f);
                    bool inOutline = nx >= -0.78f && nx <= 0.98f && Mathf.Abs(ny) <= (tipLimit + 0.12f) && nx >= (Mathf.Abs(ny) * 0.45f - 0.78f);

                    if (inBody)
                        pixels[y * size + x] = new Color(1f, 0.16f, 0.14f, 0.96f);
                    else if (inOutline)
                        pixels[y * size + x] = new Color(0.15f, 0f, 0f, 0.85f);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!IsPiloting || Time.timeScale <= 0) return;

            InitStyles();

            Matrix4x4 prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            DrawOffscreenEnemyArrowAndLeadMarker();

            float scale = Screen.height / 1080f;
            float guiW = Screen.width / scale;
            float cx = guiW * 0.5f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            // 크로스헤어
            Color crossColor = isOverheated
                ? new Color(0.55f, 0.55f, 0.55f, 0.7f)
                : (HasTargetLock ? new Color(1f, 0.25f, 0.2f, 0.95f) : new Color(0.25f, 1f, 0.45f, 0.88f));
            GUI.color = crossColor;
            GUI.DrawTexture(new Rect(cx - 24f, 540f - 2f, 15f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 9f, 540f - 2f, 15f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 2f, 540f - 24f, 4f, 15f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 2f, 540f + 9f, 4f, 15f), Texture2D.whiteTexture);

            if (hitMarkerTimer > 0f)
            {
                GUI.color = new Color(1f, 0.9f, 0.2f, 1f);
                GUI.DrawTexture(new Rect(cx - 12f, 540f - 12f, 8f, 8f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 4f, 540f - 12f, 8f, 8f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 12f, 540f + 4f, 8f, 8f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 4f, 540f + 4f, 8f, 8f), Texture2D.whiteTexture);
            }

            if (HasTargetLock && !isOverheated)
            {
                styleHud.normal.textColor = new Color(1f, 0.3f, 0.2f);
                GUI.color = Color.white;
                GUI.Label(new Rect(cx - 130f, 566f, 260f, 30f), "[ LOCK-ON ]", styleHud);
            }

            DrawOverheatGauge(cx);

            // 좌측 하단 계기판
            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            GUI.DrawTexture(new Rect(36f, 808f, 470f, 232f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            styleSub.normal.textColor = Color.white;
            GUI.Label(new Rect(56f, 824f, 430f, 32f), $"엔진 예열 RPM : {Mathf.RoundToInt(CurrentRpm * 100)}% {(IsAirborne ? "(비행 가능)" : "(Space 꾹 눌러 예열)")}", styleSub);
            GUI.Label(new Rect(56f, 860f, 430f, 32f), $"속도 : {currentVelocity.magnitude:0} m/s   |   고도 : {transform.position.y:0} m", styleSub);
            GUI.Label(new Rect(56f, 896f, 430f, 32f), $"헬기 내구도 : {CurrentHealth:0} / {maxHealth:0}", styleSub);
            styleSub.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
            GUI.Label(new Rect(56f, 938f, 430f, 88f), "[Space/Shift] 상승·하강  |  [W/S/A/D] 비행\n[Q/E] 측면 이동  |  [마우스] 둘러보기\n[좌클릭] 게틀링  |  [R] 과열 시 즉시 냉각", styleSub);

            // 상단 적 HP
            if (enemyHelicopter != null && enemyHelicopter.State != EnemyHelicopterAI.AIState.IdleWaiting)
            {
                GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.9f);
                GUI.DrawTexture(new Rect(cx - 420f, 26f, 840f, 82f), Texture2D.whiteTexture);

                string phaseText = enemyHelicopter.State switch
                {
                    EnemyHelicopterAI.AIState.TakeoffEscape => "적 헬리콥터 이륙 중! 예열을 마치고 추격하세요!",
                    EnemyHelicopterAI.AIState.Phase1_Combat => "1페이즈: 도주하는 적 헬리콥터를 격추하세요!",
                    EnemyHelicopterAI.AIState.Phase2_RetreatToOcean => "적 HP 40% 이하! 바다 상공으로 도주 중 - 추격하세요!",
                    EnemyHelicopterAI.AIState.Phase2_OceanBoss => "2페이즈 결전: 바다 위에서 적 헬리콥터를 완전히 격추하세요!",
                    EnemyHelicopterAI.AIState.Crashing => "격추 성공! 적 헬리콥터가 바다로 추락합니다!",
                    _ => "임무 완료"
                };

                styleHud.normal.textColor = new Color(1f, 0.82f, 0.32f);
                GUI.color = Color.white;
                GUI.Label(new Rect(cx - 400f, 32f, 800f, 34f), phaseText, styleHud);

                Rect hpBg = new Rect(cx - 360f, 74f, 720f, 20f);
                GUI.color = new Color(0.2f, 0.2f, 0.25f);
                GUI.DrawTexture(hpBg, Texture2D.whiteTexture);
                GUI.color = enemyHelicopter.HealthRatio <= 0.4f ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0.15f);
                GUI.DrawTexture(new Rect(hpBg.x, hpBg.y, hpBg.width * enemyHelicopter.HealthRatio, hpBg.height), Texture2D.whiteTexture);
            }

            // 미사일 경고
            if (missileWarningTimer > 0f && Mathf.Sin(Time.time * 16f) > -0.25f)
            {
                Rect warnBg = new Rect(cx - 520f, 126f, 1040f, 72f);
                GUI.color = new Color(0.45f, 0.04f, 0.04f, 0.88f);
                GUI.DrawTexture(warnBg, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.25f, 0.2f, 0.95f);
                GUI.DrawTexture(new Rect(warnBg.x, warnBg.y, warnBg.width, 4f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(warnBg.x, warnBg.yMax - 4f, warnBg.width, 4f), Texture2D.whiteTexture);

                styleWarn.normal.textColor = new Color(1f, 0.92f, 0.9f);
                GUI.color = Color.white;
                GUI.Label(new Rect(warnBg.x + 20f, warnBg.y + 6f, warnBg.width - 40f, warnBg.height - 12f),
                    $"[ 경고 ] {missileWarningLabel}  -  급선회 또는 고도 변경으로 회피하세요!", styleWarn);
            }

            // 거리 이탈 카운트다운 경고
            if (IsChaseFailed)
            {
                Rect failBg = new Rect(cx - 420f, 440f, 840f, 110f);
                GUI.color = new Color(0.1f, 0.02f, 0.02f, 0.92f);
                GUI.DrawTexture(failBg, Texture2D.whiteTexture);
                styleWarn.normal.textColor = new Color(1f, 0.3f, 0.25f);
                GUI.color = Color.white;
                GUI.Label(failBg, "추격 실패 - 적 헬리콥터를 놓쳤습니다!", styleWarn);
            }
            else if (isOutOfRange)
            {
                float pulse = Mathf.Sin(Time.time * 14f) * 0.2f + 0.8f;
                Rect distBg = new Rect(cx - 380f, 212f, 760f, 68f);
                GUI.color = new Color(0.55f, 0.08f, 0.04f, 0.88f * pulse);
                GUI.DrawTexture(distBg, Texture2D.whiteTexture);
                styleWarn.normal.textColor = Color.white;
                GUI.color = Color.white;
                GUI.Label(distBg, $"타겟 이탈 ({currentEnemyDist:0}m)  -  {escapeCountdown:0.0}초 안에 거리를 좁히세요!", styleWarn);
            }

            if (damageFlashTimer > 0f)
            {
                GUI.color = new Color(1f, 0.1f, 0.1f, damageFlashTimer * 0.55f);
                GUI.DrawTexture(new Rect(0f, 0f, guiW, 1080f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.matrix = prevMatrix;
        }

        private void DrawOverheatGauge(float cx)
        {
            if (currentHeat <= 0.01f && !isOverheated && activeCoolSuccessTimer <= 0f) return;

            Rect bg = new Rect(cx - 90f, 604f, 180f, 14f);
            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            GUI.DrawTexture(bg, Texture2D.whiteTexture);

            if (isOverheated && !activeCoolLocked)
            {
                GUI.color = new Color(0.2f, 0.85f, 1f, 0.85f);
                float minX = bg.x + bg.width * activeCoolMin;
                float zoneW = bg.width * (activeCoolMax - activeCoolMin);
                GUI.DrawTexture(new Rect(minX, bg.y, zoneW, bg.height), Texture2D.whiteTexture);
            }

            Color heatColor = isOverheated
                ? new Color(1f, 0.22f, 0.18f, 0.95f)
                : Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.25f, 0.15f), currentHeat);

            GUI.color = heatColor;
            GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width * Mathf.Clamp01(currentHeat), bg.height), Texture2D.whiteTexture);

            if (isOverheated)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(bg.x + bg.width * Mathf.Clamp01(currentHeat) - 2f, bg.y - 3f, 4f, bg.height + 6f), Texture2D.whiteTexture);

                styleSmall.normal.textColor = activeCoolLocked ? new Color(1f, 0.45f, 0.4f) : new Color(0.4f, 0.95f, 1f);
                string label = activeCoolLocked ? "과열! 냉각 대기 중..." : "과열! 파란 구간에서 [R] 냉각";
                GUI.Label(new Rect(cx - 160f, 622f, 320f, 28f), label, styleSmall);
            }
            else if (activeCoolSuccessTimer > 0f)
            {
                styleSmall.normal.textColor = new Color(0.3f, 1f, 0.55f);
                GUI.color = Color.white;
                GUI.Label(new Rect(cx - 120f, 622f, 240f, 28f), "액티브 쿨링 성공!", styleSmall);
            }
        }

        private void DrawOffscreenEnemyArrowAndLeadMarker()
        {
            if (helicopterCamera == null || enemyHelicopter == null || enemyHelicopter.IsDead) return;
            if (enemyHelicopter.State == EnemyHelicopterAI.AIState.IdleWaiting) return;

            Vector3 enemyPos = enemyHelicopter.transform.position;
            Vector3 vp = helicopterCamera.WorldToViewportPoint(enemyPos);
            bool enemyVisibleOnScreen = vp.z > 0f && vp.x >= 0.03f && vp.x <= 0.97f && vp.y >= 0.03f && vp.y <= 0.97f;

            if (enemyVisibleOnScreen)
            {
                if (hasPredictedLead)
                {
                    Vector3 leadSp = helicopterCamera.WorldToScreenPoint(predictedLeadWorldPos);
                    if (leadSp.z > 0f)
                    {
                        float lx = leadSp.x;
                        float ly = Screen.height - leadSp.y;
                        float boxSize = HasTargetLock ? 28f : 20f;
                        GUI.color = HasTargetLock ? new Color(1f, 0.25f, 0.2f, 0.95f) : new Color(1f, 0.85f, 0.2f, 0.85f);
                        GUI.DrawTexture(new Rect(lx - boxSize * 0.5f, ly - boxSize * 0.5f, boxSize, 2f), Texture2D.whiteTexture);
                        GUI.DrawTexture(new Rect(lx - boxSize * 0.5f, ly + boxSize * 0.5f - 2f, boxSize, 2f), Texture2D.whiteTexture);
                        GUI.DrawTexture(new Rect(lx - boxSize * 0.5f, ly - boxSize * 0.5f, 2f, boxSize), Texture2D.whiteTexture);
                        GUI.DrawTexture(new Rect(lx + boxSize * 0.5f - 2f, ly - boxSize * 0.5f, 2f, boxSize), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                }
                return;
            }

            Vector3 localPos = helicopterCamera.transform.InverseTransformPoint(enemyPos);
            Vector2 guiDir = new Vector2(localPos.x, -localPos.y);
            if (guiDir.sqrMagnitude < 0.0001f) guiDir = Vector2.down;
            guiDir.Normalize();

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float marginX = Mathf.Max(52f, Screen.width * 0.045f);
            float marginY = Mathf.Max(52f, Screen.height * 0.065f);
            float halfW = screenCenter.x - marginX;
            float halfH = screenCenter.y - marginY;

            float tx = Mathf.Abs(guiDir.x) > 0.0001f ? halfW / Mathf.Abs(guiDir.x) : float.MaxValue;
            float ty = Mathf.Abs(guiDir.y) > 0.0001f ? halfH / Mathf.Abs(guiDir.y) : float.MaxValue;
            Vector2 arrowPos = screenCenter + guiDir * Mathf.Min(tx, ty);

            float angleDeg = Mathf.Atan2(guiDir.y, guiDir.x) * Mathf.Rad2Deg;
            float arrowSize = Mathf.Clamp(Screen.height * 0.055f, 40f, 68f);

            Matrix4x4 oldMat = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, arrowPos);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(arrowPos.x - arrowSize * 0.5f, arrowPos.y - arrowSize * 0.5f, arrowSize, arrowSize), redArrowTex);
            GUI.matrix = oldMat;

            float distMeters = Vector3.Distance(transform.position, enemyPos);
            Vector2 textPos = arrowPos - guiDir * (arrowSize * 0.85f);
            styleArrowDist.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.02f), 14, 24);
            styleArrowDist.normal.textColor = new Color(1f, 0.35f, 0.3f);
            GUI.Label(new Rect(textPos.x - 45f, textPos.y - 16f, 90f, 32f), $"{distMeters:0}m", styleArrowDist);
        }

        private void InitStyles()
        {
            Font customFont = MokUIFont.Font;
            styleHud ??= new GUIStyle(GUI.skin.label) { font = customFont, fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            styleWarn ??= new GUIStyle(GUI.skin.label) { font = customFont, fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            styleSub ??= new GUIStyle(GUI.skin.label) { font = customFont, fontSize = 20, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            styleArrowDist ??= new GUIStyle(GUI.skin.label) { font = customFont, fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleSmall ??= new GUIStyle(GUI.skin.label) { font = customFont, fontSize = 19, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
    }
}