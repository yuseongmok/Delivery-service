using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    public enum ChickenIngredientType { Chicken, Flour, Water, Salt, Pepper, Oil }
    public enum CookingStationType { ChoppingBoard, MixingBowl, DeepFryer }

    public sealed class PrisonChickenQuest : MonoBehaviour
    {
        public static PrisonChickenQuest Current { get; private set; }

        public enum QuestState
        {
            Locked,             // 최형욱 퀘스트 수락 전 (입구 막힘)
            WaitingForKitchen,  // 최형욱 대화 완료 후 식당 진입 대기 (입구 열림)
            KitchenIntro,       // 식당 도착 시네마틱 연출 중
            Countdown,          // 3, 2, 1 카운트다운
            Gathering,          // 6가지 재료 모으기
            Step1_Chop,         // 1단계: 닭 토막내기 (다중 점선 & 각도 드래그)
            Step2_Pour,         // 2단계: 밀가루 & 물 정량 붓기 (손떨림 균형 + 울컥거림)
            Step3_Stir,         // 3단계: 반죽 휘젓기 (5초 저속 -> 15초 초고속 회전)
            Step4_Fry,          // 4단계: 치킨 튀기기 (가속 + 범위 축소 + 위치 랜덤 변경)
            ReadyToExit,        // 치킨 완성! 손에 들고 식당 밖으로 나가기 대기
            Failing,            // 주방장 기상 (얼굴 줌인 연출 및 리스폰 중)
            Completed           // 식당 탈출 및 미니게임 최종 클리어
        }

        [Header("1. 기존 감옥 시스템 연결")]
        public PrisonIntroduction introScript;
        public PlayerCombat player;
        public Camera filmCamera;

        [Header("2. 구역 및 오브젝트 연결")]
        public GameObject entranceBlocker;
        public Transform kitchenEnterPoint;
        public float kitchenEnterRadius = 3.0f;
        public Transform kitchenExitPoint;
        public float kitchenExitRadius = 3.0f;
        public Transform retrySpawnPoint;

        [Header("3. 시네마틱 카메라 & 잠든 주방장 NPC")]
        public Transform introCameraPoint;
        public Transform sleepingChefHead;
        public Transform chefZoomCameraPoint;
        public string[] kitchenIntroDialogues = {
            "나: 여기가 식당 주방인가… 주방장이 바닥에서 골아떨어져 있군.",
            "나: 바닥에 널브러진 주방 기구들을 건드리면 깰 거야. 조심해서 재료 6가지를 도마로 모으자."
        };
        public string[] failWakeUpDialogues = {
            "주방장: 으악! 누구야?! 감히 내 주방에서 뭘 훔쳐 먹는 거냐!!",
            "나: 젠장, 들켰다! 다시 처음부터 시도해야겠어."
        };

        [Header("4. 타이머 및 소음(깨움) 설정")]
        public float timeLimitSeconds = 300f;
        public int maxWakeUpCount = 3;

        [Header("5. 조리대 및 아이템 연결")]
        public PrisonCookingStation choppingStation;
        public PrisonCookingStation mixingStation;
        public PrisonCookingStation fryerStation;
        public PrisonCookingIngredient[] scatteredIngredients;
        public GameObject finishedChickenPrefab;

        public QuestState State { get; private set; } = QuestState.Locked;
        public float RemainingTime { get; private set; }
        public int WakeUpCount { get; private set; }
        public bool InMinigameUI { get; private set; }

        public Transform CurrentTarget => State switch
        {
            QuestState.WaitingForKitchen => kitchenEnterPoint,
            QuestState.Gathering => choppingStation != null ? choppingStation.transform : kitchenEnterPoint,
            QuestState.Step1_Chop => choppingStation != null ? choppingStation.transform : null,
            QuestState.Step2_Pour or QuestState.Step3_Stir => mixingStation != null ? mixingStation.transform : null,
            QuestState.Step4_Fry => fryerStation != null ? fryerStation.transform : null,
            QuestState.ReadyToExit => kitchenExitPoint,
            _ => null
        };

        public string CurrentObjective => State switch
        {
            QuestState.WaitingForKitchen => "식당 주방으로 이동하기",
            QuestState.Gathering => $"주방장 몰래 도마로 재료 모으기 ({placedIngredients.Count}/6)",
            QuestState.Step1_Chop => $"도마에서 닭 토막내기 ({chopScore}/{chopTargetScore})",
            QuestState.Step2_Pour => "반죽통에 밀가루와 물 정량 배합하기",
            QuestState.Step3_Stir => "반죽통에서 속도 맞춰 닭과 반죽물 섞기",
            QuestState.Step4_Fry => $"튀김기에서 치킨 튀기기 ({frySuccessCount}/5)",
            QuestState.ReadyToExit => "완성된 치킨을 들고 식당 밖으로 나가기",
            QuestState.Completed => "치킨 조리 임무 완료",
            _ => ""
        };

        private bool cinematic;
        private float fade = 0f;
        private string subtitle = "";
        private string centerBanner = "";
        private float bannerTimer = 0f;

        private PlayerMove move;
        private CameraMove look;
        private PlayerInteraction interaction;
        private ToppingInventory inventory;
        private float originalFov;

        private HashSet<ChickenIngredientType> placedIngredients = new HashSet<ChickenIngredientType>();
        private PrisonCookingIngredient currentlyHeldIngredient;
        private GameObject heldChickenVisual;

        // 공통 화면 진동 변수
        private float shakeIntensity = 0f;
        private float tempShakeTimer = 0f;
        private float tempShakePower = 0f;
        private Vector3 stationCamBasePos;
        private Quaternion stationCamBaseRot;

        // --- [업그레이드] 1단계: 닭 토막내기 (다중 점선, 2초 깜빡임, 각도 조절, PERFECT/BAD) ---
        private class ChopTargetLine
        {
            public Vector2 centerOffset; // 화면 중앙 기준 오프셋 (900p 스케일 기준)
            public float angleDeg;       // 잘라야 하는 점선의 각도
            public float length;
            public float maxLife = 2.0f; // 2초 뒤 소멸
            public float timer = 2.0f;
        }
        private List<ChopTargetLine> activeChopLines = new List<ChopTargetLine>();
        private int chopScore = 0;
        private int chopTargetScore = 10; // 플레이 타임을 늘리기 위해 10회 절단 요구
        private int chopBadCount = 0;
        private float chopSpawnTimer = 0f;
        private bool isDraggingKnife = false;
        private Vector2 dragStartGuiPos;
        private Vector2 dragCurrentGuiPos;
        private string chopJudgeText = "";
        private Color chopJudgeColor = Color.white;
        private float chopJudgeTimer = 0f;
        private float chickenBoardYaw = 0f; // 플레이어가 돌려가며 자를 수 있는 생닭 회전 각도
        private Transform chickenOnBoardTransform;

        // --- [업그레이드] 2단계: 밀가루 & 물 붓기 (손떨림 균형 + 울컥거림 + 움직이는 타겟) ---
        private int pourSubStep = 0; // 0: 밀가루, 1: 물
        private float pourAmount = 0f;
        private bool pourHolding = false;
        private float pourTargetMin = 0.70f, pourTargetMax = 0.84f;
        private float pourNozzlePos = 0f; // -1(좌) ~ 1(우), 중앙(-0.35~0.35) 안에 유지해야 함
        private float pourNozzleVel = 0f;
        private float pourSurgeTimer = 0f;
        private bool isSurgeActive = false;

        // --- [업그레이드] 3단계: 반죽 휘젓기 (초반 5초 천천히 -> 후반 15초 미친듯이 빠르게) ---
        private float stirElapsedTime = 0f;
        private float stirProgress = 0f;
        private float lastMouseAngle = 0f;
        private float currentAngularSpeed = 0f; // 초당 회전 각도(deg/s)
        private string stirStatusFeedback = "";

        // --- [업그레이드] 4단계: 스타포스 튀기기 (속도 증가 + 범위 축소 + 랜덤 위치 이동) ---
        private int frySuccessCount = 0;
        private float fryCursor = 0f;
        private float fryDirection = 1f;
        private float fryZoneCenter = 0.5f;
        private float fryZoneHalfWidth = 0.09f;

        private GUIStyle styleTitle, styleSub, stylePrompt, styleTimer, styleJudge;

        private void Awake() { Current = this; }
        private void OnDestroy() { if (Current == this) Current = null; }

        private void Start()
        {
            if (!introScript) introScript = FindFirstObjectByType<PrisonIntroduction>();
            if (!player && PrisonMission.Current) player = PrisonMission.Current.player;
            if (!player && introScript && introScript.mission) player = introScript.mission.player;
            if (!filmCamera && introScript) filmCamera = introScript.filmCamera;

            if (player)
            {
                move = player.GetComponent<PlayerMove>();
                look = player.GetComponentInChildren<CameraMove>();
                interaction = player.GetComponent<PlayerInteraction>();
                inventory = player.GetComponent<ToppingInventory>();

                // ★ 깨움 카운트 버그 픽스: 플레이어에게 충돌 감지 센서를 자동으로 부착합니다!
                if (!player.GetComponent<PlayerHazardSensor>())
                {
                    player.gameObject.AddComponent<PlayerHazardSensor>();
                }
            }

            if (entranceBlocker) entranceBlocker.SetActive(true);
            UpdateStationOutlines();
        }

        public void UnlockKitchenQuest()
        {
            if (State != QuestState.Locked) return;
            State = QuestState.WaitingForKitchen;
            if (entranceBlocker) entranceBlocker.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || !player) return;

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
                if (bannerTimer <= 0f) centerBanner = "";
            }

            if (State == QuestState.Locked && introScript != null && introScript.State == PrisonIntroduction.Stage.Complete)
            {
                UnlockKitchenQuest();
            }

            if (State == QuestState.WaitingForKitchen && kitchenEnterPoint != null)
            {
                if (Vector3.Distance(player.transform.position, kitchenEnterPoint.position) <= kitchenEnterRadius)
                {
                    StartCoroutine(PlayKitchenIntroAndCountdown());
                }
            }

            if (IsPlayingState())
            {
                RemainingTime -= Time.deltaTime;
                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    TriggerFail("시간 초과!");
                    return;
                }
            }

            if (currentlyHeldIngredient != null)
            {
                Transform holdPt = (inventory != null && inventory.HoldPoint != null) ? inventory.HoldPoint : player.View.transform;
                currentlyHeldIngredient.transform.position = holdPt.position + holdPt.forward * 0.45f - holdPt.up * 0.15f;
                currentlyHeldIngredient.transform.rotation = holdPt.rotation;
            }

            if (InMinigameUI)
            {
                UpdateActiveCookingMinigame();
            }

            if (State == QuestState.ReadyToExit && kitchenExitPoint != null)
            {
                if (Vector3.Distance(player.transform.position, kitchenExitPoint.position) <= kitchenExitRadius)
                {
                    StartCoroutine(CompleteQuestSequence());
                }
            }
        }

        public bool IsPlayingState()
        {
            return State == QuestState.Gathering || State == QuestState.Step1_Chop ||
                   State == QuestState.Step2_Pour || State == QuestState.Step3_Stir ||
                   State == QuestState.Step4_Fry || State == QuestState.ReadyToExit;
        }

        private IEnumerator PlayKitchenIntroAndCountdown()
        {
            State = QuestState.KitchenIntro;
            SetCinematic(true);

            if (filmCamera && introCameraPoint)
            {
                filmCamera.transform.SetPositionAndRotation(introCameraPoint.position, introCameraPoint.rotation);
            }

            yield return FadeTo(0f, 0.5f);

            foreach (string line in kitchenIntroDialogues)
            {
                subtitle = line;
                yield return WaitOrSkip(3.2f);
            }
            subtitle = "";

            yield return FadeTo(1f, 0.4f);
            SetCinematic(false);
            yield return FadeTo(0f, 0.4f);

            State = QuestState.Countdown;
            SetPlayerControl(false);

            string[] counts = { "3", "2", "1", "요리 시작!" };
            foreach (string c in counts)
            {
                centerBanner = c;
                PrisonAudio.Clink(player.transform.position, 0.4f);
                yield return new WaitForSeconds(0.9f);
            }
            centerBanner = "";

            RemainingTime = timeLimitSeconds;
            WakeUpCount = 0;
            State = QuestState.Gathering;
            SetPlayerControl(true);
            UpdateStationOutlines();
        }

        public void AddWakeUpNoise(Vector3 noisePos)
        {
            if (!IsPlayingState() || State == QuestState.Failing) return;

            WakeUpCount++;
            PrisonAudio.Clink(noisePos, 0.8f);
            ShowBanner($"조심! 주방장이 뒤척입니다! (깨움 카운트: {WakeUpCount}/{maxWakeUpCount})", 2.2f);

            if (WakeUpCount >= maxWakeUpCount)
            {
                TriggerFail("소음으로 인해 주방장이 깨어났습니다!");
            }
        }

        public void TriggerFail(string reason)
        {
            if (State == QuestState.Failing || State == QuestState.Completed) return;
            StopAllCoroutines();
            if (InMinigameUI) CloseCookingMinigameUI();
            StartCoroutine(FailAndRetrySequence(reason));
        }

        private IEnumerator FailAndRetrySequence(string reason)
        {
            State = QuestState.Failing;
            centerBanner = reason;
            bannerTimer = 2.0f;

            SetCinematic(true);

            if (filmCamera)
            {
                if (chefZoomCameraPoint)
                {
                    filmCamera.transform.SetPositionAndRotation(chefZoomCameraPoint.position, chefZoomCameraPoint.rotation);
                }
                else if (sleepingChefHead)
                {
                    filmCamera.transform.position = sleepingChefHead.position + sleepingChefHead.forward * 1.1f + Vector3.up * 0.2f;
                    filmCamera.transform.LookAt(sleepingChefHead.position);
                }
            }

            float startFov = 55f;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                if (filmCamera) filmCamera.fieldOfView = Mathf.Lerp(startFov, 28f, t / 0.45f);
                yield return null;
            }

            foreach (string line in failWakeUpDialogues)
            {
                subtitle = line;
                yield return WaitOrSkip(3.0f);
            }
            subtitle = "";

            yield return FadeTo(1f, 0.6f);
            ResetMinigameObjects();

            if (retrySpawnPoint)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.transform.SetPositionAndRotation(retrySpawnPoint.position, retrySpawnPoint.rotation);
                player.transform.root.eulerAngles = new Vector3(0, player.transform.root.eulerAngles.y, 0);
                if (cc) cc.enabled = true;
            }

            SetCinematic(false);
            yield return FadeTo(0f, 0.5f);

            State = QuestState.Countdown;
            SetPlayerControl(false);
            string[] counts = { "3", "2", "1", "재도전!" };
            foreach (string c in counts)
            {
                centerBanner = c;
                yield return new WaitForSeconds(0.8f);
            }
            centerBanner = "";

            RemainingTime = timeLimitSeconds;
            WakeUpCount = 0;
            State = QuestState.Gathering;
            SetPlayerControl(true);
            UpdateStationOutlines();
        }

        private void ResetMinigameObjects()
        {
            placedIngredients.Clear();
            currentlyHeldIngredient = null;
            if (heldChickenVisual) Destroy(heldChickenVisual);

            if (scatteredIngredients != null)
            {
                foreach (var ing in scatteredIngredients)
                {
                    if (ing) ing.ResetToStart();
                }
            }
            UpdateStationOutlines();
        }

        private IEnumerator CompleteQuestSequence()
        {
            State = QuestState.Completed;
            SetPlayerControl(false);
            yield return FadeTo(1f, 0.7f);
            centerBanner = "치킨 조리 및 탈출 성공!";
            bannerTimer = 3.0f;
            yield return new WaitForSeconds(0.6f);
            yield return FadeTo(0f, 0.7f);
            SetPlayerControl(true);
        }

        public void PickUpIngredient(PrisonCookingIngredient ing)
        {
            if (State != QuestState.Gathering || InMinigameUI) return;
            if (currentlyHeldIngredient != null)
            {
                ShowBanner("이미 손에 재료를 들고 있습니다! 도마에 먼저 가져다 놓으세요.", 2f);
                return;
            }
            currentlyHeldIngredient = ing;
            ing.OnPickedUp();
            PrisonAudio.Clink(ing.transform.position, 0.3f);
        }

        public void InteractWithStation(PrisonCookingStation station)
        {
            if (InMinigameUI || State == QuestState.Failing) return;

            if (State == QuestState.Gathering && station.stationType == CookingStationType.ChoppingBoard)
            {
                if (currentlyHeldIngredient != null)
                {
                    if (currentlyHeldIngredient.ingredientType == ChickenIngredientType.Chicken)
                    {
                        chickenOnBoardTransform = currentlyHeldIngredient.transform;
                    }

                    placedIngredients.Add(currentlyHeldIngredient.ingredientType);
                    currentlyHeldIngredient.PlaceOnBoard(station.transform, placedIngredients.Count);
                    currentlyHeldIngredient = null;
                    PrisonAudio.Clink(station.transform.position, 0.4f);

                    if (placedIngredients.Count >= 6)
                    {
                        State = QuestState.Step1_Chop;
                        UpdateStationOutlines();
                        ShowBanner("모든 재료가 모였습니다! 도마[F]에서 닭을 토막내세요.", 3f);
                    }
                    else
                    {
                        ShowBanner($"재료 배치 완료 ({placedIngredients.Count}/6)", 1.5f);
                    }
                }
                else
                {
                    ShowBanner($"도마에 재료를 가져다 놓으세요 ({placedIngredients.Count}/6)", 2f);
                }
                return;
            }

            if (State == QuestState.Step1_Chop && station.stationType == CookingStationType.ChoppingBoard)
                OpenCookingMinigameUI(station);
            else if ((State == QuestState.Step2_Pour || State == QuestState.Step3_Stir) && station.stationType == CookingStationType.MixingBowl)
                OpenCookingMinigameUI(station);
            else if (State == QuestState.Step4_Fry && station.stationType == CookingStationType.DeepFryer)
                OpenCookingMinigameUI(station);
        }

        private void OpenCookingMinigameUI(PrisonCookingStation station)
        {
            InMinigameUI = true;
            SetPlayerControl(false);

            if (filmCamera)
            {
                player.View.enabled = false;
                filmCamera.enabled = true;
                if (station.cameraViewPoint)
                {
                    filmCamera.transform.SetPositionAndRotation(station.cameraViewPoint.position, station.cameraViewPoint.rotation);
                }
                else
                {
                    filmCamera.transform.position = station.transform.position + Vector3.up * 1.1f - station.transform.forward * 0.4f;
                    filmCamera.transform.LookAt(station.transform.position);
                }
                stationCamBasePos = filmCamera.transform.position;
                stationCamBaseRot = filmCamera.transform.rotation;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            shakeIntensity = 0f;
            tempShakeTimer = 0f;

            if (State == QuestState.Step1_Chop)
            {
                ResetStep1Chop();
            }
            else if (State == QuestState.Step2_Pour)
            {
                pourSubStep = 0;
                ResetStep2PourSubStep();
            }
            else if (State == QuestState.Step3_Stir)
            {
                stirElapsedTime = 0f;
                stirProgress = 0f;
                currentAngularSpeed = 0f;
                stirStatusFeedback = "처음 5초간은 반죽이 튀지 않게 천천히 돌리세요!";
            }
            else if (State == QuestState.Step4_Fry)
            {
                frySuccessCount = 0;
                fryCursor = 0f;
                fryDirection = 1f;
                SetupStep4FryZone();
            }
        }

        private void CloseCookingMinigameUI()
        {
            InMinigameUI = false;
            shakeIntensity = 0f;
            tempShakeTimer = 0f;
            if (filmCamera) filmCamera.enabled = false;
            if (player && player.View) player.View.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetPlayerControl(true);
        }

        private void TriggerTempShake(float power, float duration)
        {
            tempShakePower = power;
            tempShakeTimer = duration;
        }

        // --- 1단계 초기화 및 점선 생성 헬퍼 ---
        private void ResetStep1Chop()
        {
            activeChopLines.Clear();
            chopScore = 0;
            chopBadCount = 0;
            chopSpawnTimer = 0.2f;
            isDraggingKnife = false;
            chopJudgeText = "";
            chopJudgeTimer = 0f;
        }

        private void SpawnChopLine()
        {
            // 한 오브젝트 위에 최대 3개의 점선이 동시에 나타나 정신없이 자르게 함
            if (activeChopLines.Count >= 3) return;
            var line = new ChopTargetLine
            {
                centerOffset = new Vector2(Random.Range(-110f, 110f), Random.Range(-75f, 75f)),
                angleDeg = Random.Range(-75f, 75f),
                length = Random.Range(150f, 210f),
                maxLife = 2.0f,
                timer = 2.0f
            };
            activeChopLines.Add(line);
        }

        private void RecordChopJudge(bool perfect)
        {
            if (perfect)
            {
                chopScore++;
                chopJudgeText = "PERFECT!";
                chopJudgeColor = new Color(0.2f, 1f, 0.4f);
                chopJudgeTimer = 0.8f;
                TriggerTempShake(0.032f, 0.16f); // 자를 때 타격 진동 효과!
                PrisonAudio.Clink(choppingStation.transform.position, 0.6f);

                if (chopScore >= chopTargetScore)
                {
                    CloseCookingMinigameUI();
                    State = QuestState.Step2_Pour;
                    UpdateStationOutlines();
                    ShowBanner("닭 토막내기 완벽 성공! 옆의 반죽통[F]에서 밀가루와 물을 배합하세요.", 3f);
                }
            }
            else
            {
                chopBadCount++;
                chopJudgeText = $"BAD! ({chopBadCount}/2)";
                chopJudgeColor = new Color(1f, 0.25f, 0.25f);
                chopJudgeTimer = 0.9f;
                TriggerTempShake(0.055f, 0.22f);
                PrisonAudio.Clink(choppingStation.transform.position, 0.3f);

                // BAD가 2번 뜨면 처음부터 다시!
                if (chopBadCount >= 2)
                {
                    ResetStep1Chop();
                    ShowBanner("BAD 2회 누적! 닭 토막내기를 처음(0/10)부터 다시 진행합니다!", 2.2f);
                }
            }
        }

        // --- 2단계 초기화 헬퍼 ---
        private void ResetStep2PourSubStep()
        {
            pourAmount = 0f;
            pourHolding = false;
            pourNozzlePos = 0f;
            pourNozzleVel = 0f;
            pourSurgeTimer = Random.Range(0.7f, 1.4f);
            isSurgeActive = false;
            float startMin = Random.Range(0.64f, 0.76f);
            pourTargetMin = startMin;
            pourTargetMax = startMin + 0.11f; // 기존보다 더 좁고 까다로운 목표 구간
        }

        // --- 4단계 목표 구간 랜덤 배치 및 축소 헬퍼 ---
        private void SetupStep4FryZone()
        {
            // 성공할수록(0~4) 성공 범위 폭이 점점 좁아지고 위치가 매번 랜덤하게 바뀜!
            fryZoneHalfWidth = Mathf.Max(0.032f, 0.085f - frySuccessCount * 0.013f);
            fryZoneCenter = Random.Range(0.18f + fryZoneHalfWidth, 0.82f - fryZoneHalfWidth);
        }

        private void UpdateActiveCookingMinigame()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null || kb == null) return;

            // 카메라 진동 처리 (4단계 지속 진동 + 1단계/기타 순간 타격 진동 합산)
            if (filmCamera && filmCamera.enabled)
            {
                float currentShake = shakeIntensity;
                if (tempShakeTimer > 0f)
                {
                    tempShakeTimer -= Time.deltaTime;
                    currentShake = Mathf.Max(currentShake, tempShakePower);
                }

                if (currentShake > 0.001f)
                    filmCamera.transform.position = stationCamBasePos + Random.insideUnitSphere * currentShake;
                else
                    filmCamera.transform.position = stationCamBasePos;
            }

            float guiScale = Screen.height / 900f;
            Vector2 mScreen = mouse.position.ReadValue();
            Vector2 mGui = new Vector2(mScreen.x / guiScale, (Screen.height - mScreen.y) / guiScale);
            Vector2 screenCenterGui = new Vector2((Screen.width / guiScale) * 0.5f, 340f);

            switch (State)
            {
                // =====================================================================
                // 1단계: 닭 토막내기 (생닭 회전 + 다중 2초 깜빡임 점선 + 각도 슬라이스 + 진동)
                // =====================================================================
                case QuestState.Step1_Chop:
                    if (chopJudgeTimer > 0f) chopJudgeTimer -= Time.deltaTime;

                    // 우클릭 드래그 또는 Q/E 키로 생닭 오브젝트 및 절단 각도 직접 회전 기능
                    float rotInput = 0f;
                    if (kb.qKey.isPressed) rotInput -= 120f * Time.deltaTime;
                    if (kb.eKey.isPressed) rotInput += 120f * Time.deltaTime;
                    if (mouse.rightButton.isPressed) rotInput += mouse.delta.ReadValue().x * 0.45f;

                    if (Mathf.Abs(rotInput) > 0.01f)
                    {
                        chickenBoardYaw += rotInput;
                        if (chickenOnBoardTransform != null)
                        {
                            chickenOnBoardTransform.rotation = Quaternion.Euler(0f, chickenBoardYaw, 0f);
                        }
                        foreach (var l in activeChopLines) l.angleDeg += rotInput;
                    }

                    // 점선 지속 생성 (정신없이 여러 개가 동시에 뜨도록 빠른 템포로 스폰)
                    chopSpawnTimer -= Time.deltaTime;
                    if (chopSpawnTimer <= 0f)
                    {
                        SpawnChopLine();
                        chopSpawnTimer = Random.Range(0.55f, 0.95f);
                    }

                    // 점선 2초 타이머 감소 (제시간에 못 자르면 BAD!)
                    for (int i = activeChopLines.Count - 1; i >= 0; i--)
                    {
                        activeChopLines[i].timer -= Time.deltaTime;
                        if (activeChopLines[i].timer <= 0f)
                        {
                            activeChopLines.RemoveAt(i);
                            RecordChopJudge(false); // 시간 초과 BAD!
                            if (State != QuestState.Step1_Chop) return;
                        }
                    }

                    // 좌클릭 드래그로 칼질
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        isDraggingKnife = true;
                        dragStartGuiPos = mGui;
                        dragCurrentGuiPos = mGui;
                    }
                    else if (mouse.leftButton.isPressed && isDraggingKnife)
                    {
                        dragCurrentGuiPos = mGui;
                    }
                    else if (mouse.leftButton.wasReleasedThisFrame && isDraggingKnife)
                    {
                        isDraggingKnife = false;
                        Vector2 slashVec = mGui - dragStartGuiPos;
                        if (slashVec.magnitude >= 65f) // 최소 드래그 길이 충족 시 판정
                        {
                            Vector2 slashMid = (dragStartGuiPos + mGui) * 0.5f;
                            float slashAngle = Mathf.Atan2(slashVec.y, slashVec.x) * Mathf.Rad2Deg;

                            int hitIndex = -1;
                            float bestDist = 999f;

                            for (int i = 0; i < activeChopLines.Count; i++)
                            {
                                var line = activeChopLines[i];
                                Vector2 linePos = screenCenterGui + line.centerOffset;
                                float dist = Vector2.Distance(slashMid, linePos);

                                // 양방향(정방향/역방향 드래그 모두 인정) 각도 차이 계산
                                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(slashAngle, line.angleDeg));
                                if (angleDiff > 90f) angleDiff = 180f - angleDiff;

                                // 위치(75px 이내)와 자르는 각도(28도 이내)가 모두 맞아야 PERFECT!
                                if (dist < 75f && angleDiff <= 28f && dist < bestDist)
                                {
                                    bestDist = dist;
                                    hitIndex = i;
                                }
                            }

                            if (hitIndex >= 0)
                            {
                                activeChopLines.RemoveAt(hitIndex);
                                RecordChopJudge(true); // PERFECT!
                            }
                            else
                            {
                                RecordChopJudge(false); // 각도가 틀렸거나 엉뚱한 곳을 자름 -> BAD!
                            }
                        }
                    }
                    break;

                // =====================================================================
                // 2단계: 밀가루 & 물 붓기 (주둥이 좌우 흔들림 제어 + 울컥거림 기믹)
                // =====================================================================
                case QuestState.Step2_Pour:
                    // 목표 구간 자체가 좌우로 살랑살랑 움직여 난이도 상승
                    float shift = Mathf.Sin(Time.time * 2.2f) * 0.04f;
                    float currentMin = Mathf.Clamp(pourTargetMin + shift, 0.55f, 0.82f);
                    float currentMax = currentMin + (pourSubStep == 0 ? 0.11f : 0.09f);

                    // 마우스를 움직여 흔들리는 주둥이(Nozzle)를 중앙으로 잡아당김
                    float mouseDeltaX = mouse.delta.ReadValue().x;
                    pourNozzleVel -= mouseDeltaX * 0.025f;

                    if (mouse.leftButton.isPressed)
                    {
                        pourHolding = true;

                        // 부을 때 손이 떨리며 주둥이가 좌우로 마구 밀려남!
                        float randomDrift = Mathf.Sin(Time.time * 9.5f) * 2.4f + Random.Range(-1.8f, 1.8f);
                        pourNozzleVel += randomDrift * Time.deltaTime;
                        pourNozzlePos += pourNozzleVel * Time.deltaTime * 2.2f;

                        // 주둥이가 양푼 중앙(-0.45 ~ 0.45)을 벗어나면 밖에 쏟아져서 즉시 실패!
                        if (Mathf.Abs(pourNozzlePos) > 0.48f)
                        {
                            TriggerTempShake(0.04f, 0.2f);
                            ResetStep2PourSubStep();
                            ShowBanner("손이 흔들려 재료를 양푼 밖에 쏟았습니다! 마우스로 중심을 잡으세요!", 2.0f);
                            return;
                        }

                        // 울컥거림(Surge) 기믹: 갑자기 재료가 2.2배 속도로 확 쏟아지는 구간
                        pourSurgeTimer -= Time.deltaTime;
                        if (pourSurgeTimer <= 0f)
                        {
                            isSurgeActive = !isSurgeActive;
                            pourSurgeTimer = isSurgeActive ? Random.Range(0.25f, 0.45f) : Random.Range(0.6f, 1.1f);
                            if (isSurgeActive) TriggerTempShake(0.015f, 0.2f);
                        }

                        float baseFillSpeed = pourSubStep == 0 ? 0.34f : 0.42f;
                        pourAmount += Time.deltaTime * baseFillSpeed * (isSurgeActive ? 2.25f : 1.0f);

                        if (pourAmount > 1.0f)
                        {
                            TriggerTempShake(0.045f, 0.25f);
                            ResetStep2PourSubStep();
                            ShowBanner("정량 초과! 양푼이 넘쳤습니다. 다시 부으세요!", 1.8f);
                        }
                    }
                    else
                    {
                        // 손을 떼고 있을 때는 주둥이가 서서히 중앙으로 돌아옴
                        pourNozzlePos = Mathf.Lerp(pourNozzlePos, 0f, Time.deltaTime * 5f);
                        pourNozzleVel = 0f;

                        if (pourHolding && mouse.leftButton.wasReleasedThisFrame)
                        {
                            pourHolding = false;
                            if (pourAmount >= currentMin && pourAmount <= currentMax)
                            {
                                PrisonAudio.Clink(mixingStation.transform.position, 0.5f);
                                TriggerTempShake(0.025f, 0.15f);
                                if (pourSubStep == 0)
                                {
                                    pourSubStep = 1;
                                    ResetStep2PourSubStep();
                                    ShowBanner("밀가루 정량 성공! 이제 물줄기 흔들림을 잡으며 정량을 부으세요!", 2.2f);
                                }
                                else
                                {
                                    State = QuestState.Step3_Stir;
                                    stirElapsedTime = 0f;
                                    stirProgress = 0f;
                                    ShowBanner("배합 완료! 처음 5초는 천천히, 이후 15초는 미친 듯이 빠르게 저으세요!", 3.0f);
                                }
                            }
                            else
                            {
                                TriggerTempShake(0.035f, 0.2f);
                                ResetStep2PourSubStep();
                                ShowBanner("정량 맞추기 실패! 움직이는 초록색 구간 안에서 정확히 떼세요!", 1.8f);
                            }
                        }
                    }
                    break;

                // =====================================================================
                // 3단계: 반죽 휘젓기 (첫 5초 천천히 -> 이후 15초 점차 초고속 스퍼트)
                // =====================================================================
                case QuestState.Step3_Stir:
                    Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 dir = mScreen - center;
                    float frameDeg = 0f;

                    if (dir.magnitude > 20f)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        float delta = Mathf.Abs(Mathf.DeltaAngle(lastMouseAngle, angle));
                        if (delta > 0.5f && delta < 95f)
                        {
                            frameDeg = delta;
                        }
                        lastMouseAngle = angle;
                    }

                    // 현재 마우스 회전 속도(초당 각도)를 부드럽게 측정
                    float instantSpeed = frameDeg / Mathf.Max(0.0001f, Time.deltaTime);
                    currentAngularSpeed = Mathf.Lerp(currentAngularSpeed, instantSpeed, Time.deltaTime * 8f);

                    stirElapsedTime += Time.deltaTime;

                    if (stirElapsedTime <= 5.0f)
                    {
                        // [전반부 0~5초] 천천히 젓기 구간 (적정 속도: 180 ~ 680 deg/s)
                        if (currentAngularSpeed > 750f)
                        {
                            stirStatusFeedback = "너무 빠릅니다! 가루가 다 튑니다! 천천히 돌리세요!";
                            stirProgress = Mathf.Max(0f, stirProgress - Time.deltaTime * 0.08f);
                            TriggerTempShake(0.02f, 0.1f);
                        }
                        else if (currentAngularSpeed >= 160f)
                        {
                            stirStatusFeedback = "좋습니다! 그 속도로 부드럽게 반죽을 풀어주세요!";
                            stirProgress += (Time.deltaTime / 20.0f);
                        }
                        else
                        {
                            stirStatusFeedback = "마우스로 원을 그리며 천천히 저어주세요!";
                        }
                    }
                    else
                    {
                        // [후반부 5~20초] 점차 엄청 빠르게 젓기 구간!
                        float sprintFactor = Mathf.Clamp01((stirElapsedTime - 5.0f) / 15.0f);
                        float requiredMinSpeed = Mathf.Lerp(650f, 1650f, sprintFactor); // 갈수록 요구 속도가 폭증!

                        if (currentAngularSpeed >= requiredMinSpeed)
                        {
                            stirStatusFeedback = "?? 완벽합니다! 팔이 빠져라 더 빠르게 휘저으세요!! ??";
                            stirProgress += (Time.deltaTime / 14.5f);
                            TriggerTempShake(0.01f + sprintFactor * 0.025f, 0.05f);
                        }
                        else
                        {
                            stirStatusFeedback = "? 너무 느립니다! 반죽이 굳습니다! 마우스를 미친 듯이 빨리 돌리세요! ?";
                            stirProgress = Mathf.Max(0.2f, stirProgress - Time.deltaTime * 0.06f);
                        }
                    }

                    if (stirProgress >= 1.0f)
                    {
                        PrisonAudio.Clink(mixingStation.transform.position, 0.6f);
                        CloseCookingMinigameUI();
                        State = QuestState.Step4_Fry;
                        UpdateStationOutlines();
                        ShowBanner("황금 반죽 완성! 이제 튀김기[F]에서 치킨을 튀기세요.", 3f);
                    }
                    break;

                // =====================================================================
                // 4단계: 스타포스 튀기기 (속도 대폭 가속 + 범위 축소 + 랜덤 위치 이동)
                // =====================================================================
                case QuestState.Step4_Fry:
                    // 성공할 때마다 속도가 훨씬 더 가파르게 빨라짐 (1.2 -> 1.75 -> 2.3 -> 2.85 -> 3.4)
                    float frySpeed = 1.2f + frySuccessCount * 0.55f;
                    fryCursor += fryDirection * frySpeed * Time.deltaTime;
                    if (fryCursor >= 1f) { fryCursor = 1f; fryDirection = -1f; }
                    else if (fryCursor <= 0f) { fryCursor = 0f; fryDirection = 1f; }

                    if (kb.spaceKey.wasPressedThisFrame)
                    {
                        float minZone = fryZoneCenter - fryZoneHalfWidth;
                        float maxZone = fryZoneCenter + fryZoneHalfWidth;

                        if (fryCursor >= minZone && fryCursor <= maxZone)
                        {
                            frySuccessCount++;
                            PrisonAudio.Clink(fryerStation.transform.position, 0.7f);

                            // 1번은 흔들림 없음, 2~4번부터 점차 격양되게 화면 흔들림 강도 상승
                            if (frySuccessCount == 1) shakeIntensity = 0f;
                            else if (frySuccessCount == 2) shakeIntensity = 0.022f;
                            else if (frySuccessCount == 3) shakeIntensity = 0.048f;
                            else if (frySuccessCount >= 4) shakeIntensity = 0.095f;

                            if (frySuccessCount >= 5)
                            {
                                CloseCookingMinigameUI();
                                EquipFinishedChickenInHand();
                                State = QuestState.ReadyToExit;
                                UpdateStationOutlines();
                                ShowBanner("치킨이 완벽하게 튀겨졌습니다! 치킨을 들고 식당 밖으로 나가세요!", 4f);
                            }
                            else
                            {
                                // 다음 단계: 성공 범위가 더 좁아지고 위치가 새로운 곳으로 랜덤 변경됨!
                                SetupStep4FryZone();
                            }
                        }
                        else
                        {
                            frySuccessCount = 0;
                            shakeIntensity = 0f;
                            SetupStep4FryZone();
                            TriggerTempShake(0.06f, 0.25f);
                            ShowBanner("타이밍 실패! 처음(0/5)부터 다시 튀깁니다!", 2f);
                        }
                    }
                    break;
            }
        }

        private void EquipFinishedChickenInHand()
        {
            Transform holdPt = (inventory != null && inventory.HoldPoint != null) ? inventory.HoldPoint : player.View.transform;
            if (finishedChickenPrefab != null)
            {
                heldChickenVisual = Instantiate(finishedChickenPrefab, holdPt);
                heldChickenVisual.transform.localPosition = Vector3.zero;
                heldChickenVisual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var boxPrefab = Resources.Load<GameObject>("MokPizzaArt/Box");
                heldChickenVisual = boxPrefab ? Instantiate(boxPrefab, holdPt) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                heldChickenVisual.name = "Crispy Fried Chicken";
                heldChickenVisual.transform.SetParent(holdPt, false);
                heldChickenVisual.transform.localScale = Vector3.one * 0.4f;
                heldChickenVisual.transform.localPosition = new Vector3(0, -0.1f, 0.45f);
            }
            foreach (var c in heldChickenVisual.GetComponentsInChildren<Collider>()) Destroy(c);
        }

        private void UpdateStationOutlines()
        {
            if (choppingStation) choppingStation.SetHighlight(State == QuestState.Step1_Chop);
            if (mixingStation) mixingStation.SetHighlight(State == QuestState.Step2_Pour || State == QuestState.Step3_Stir);
            if (fryerStation) fryerStation.SetHighlight(State == QuestState.Step4_Fry);
        }

        private void ShowBanner(string msg, float duration)
        {
            centerBanner = msg;
            bannerTimer = duration;
        }

        private void SetPlayerControl(bool canControl)
        {
            if (move) move.isControllable = canControl;
            if (look) look.isControllable = canControl;
            if (interaction) interaction.enabled = canControl;
        }

        private void SetCinematic(bool value)
        {
            if (cinematic == value) return;
            cinematic = value;
            if (value && player && player.View) originalFov = player.View.fieldOfView;
            SetPlayerControl(!value);

            if (player && player.View) player.View.enabled = !value;
            if (filmCamera)
            {
                filmCamera.enabled = value;
                filmCamera.fieldOfView = 55f;
            }
            if (!value && player && player.View)
            {
                player.View.fieldOfView = originalFov;
                player.transform.root.eulerAngles = new Vector3(0, player.transform.root.eulerAngles.y, 0);
                if (look) look.SyncViewFromTransform();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = fade;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                fade = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            fade = target;
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) break;
                yield return null;
            }
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0 || State == QuestState.Locked) return;

            float scale = Screen.height / 900f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            float width = Screen.width / scale;

            styleTitle ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleSub ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            stylePrompt ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 21, alignment = TextAnchor.MiddleCenter };
            styleTimer ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleJudge ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            if (cinematic)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, width, 65), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, 835, width, 65), Texture2D.whiteTexture);
            }

            if (IsPlayingState())
            {
                GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);
                GUI.DrawTexture(new Rect(width / 2 - 260, 20, 520, 85), Texture2D.whiteTexture);

                int mins = Mathf.FloorToInt(RemainingTime / 60f);
                int secs = Mathf.FloorToInt(RemainingTime % 60f);
                styleTimer.normal.textColor = RemainingTime < 60f ? Color.red : new Color(1f, 0.8f, 0.3f);
                GUI.color = Color.white;
                GUI.Label(new Rect(width / 2 - 250, 25, 500, 35), $"남은 시간: {mins:00}:{secs:00}  |  주방장 깨움: {WakeUpCount} / {maxWakeUpCount}", styleTimer);

                for (int i = 0; i < maxWakeUpCount; i++)
                {
                    GUI.color = i < WakeUpCount ? new Color(1f, 0.25f, 0.2f) : new Color(0.25f, 0.3f, 0.35f);
                    GUI.DrawTexture(new Rect(width / 2 - 180 + i * 125, 68, 110, 14), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
            }

            if (InMinigameUI)
            {
                DrawCookingMinigameOverlay(width, scale);
            }
            else if (IsPlayingState() && player && player.View)
            {
                if (Physics.Raycast(player.View.transform.position, player.View.transform.forward, out var hit, 3.2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    string hint = "";
                    var ing = hit.collider.GetComponentInParent<PrisonCookingIngredient>();
                    var st = hit.collider.GetComponentInParent<PrisonCookingStation>();

                    if (ing && State == QuestState.Gathering && !ing.IsPlaced)
                        hint = $"{ing.GetKoreanName()} 줍기";
                    else if (st)
                    {
                        if (State == QuestState.Gathering && st.stationType == CookingStationType.ChoppingBoard)
                            hint = currentlyHeldIngredient ? $"도마에 {currentlyHeldIngredient.GetKoreanName()} 올려놓기 ({placedIngredients.Count}/6)" : $"재료 모으기 ({placedIngredients.Count}/6)";
                        else if (State == QuestState.Step1_Chop && st.stationType == CookingStationType.ChoppingBoard)
                            hint = "도마에서 닭 토막내기";
                        else if (State == QuestState.Step2_Pour && st.stationType == CookingStationType.MixingBowl)
                            hint = "밀가루와 물 정량 배합하기";
                        else if (State == QuestState.Step3_Stir && st.stationType == CookingStationType.MixingBowl)
                            hint = "닭과 반죽물 섞기";
                        else if (State == QuestState.Step4_Fry && st.stationType == CookingStationType.DeepFryer)
                            hint = "튀김기에서 치킨 튀기기";
                    }

                    if (!string.IsNullOrEmpty(hint))
                    {
                        stylePrompt.normal.textColor = new Color(1f, 0.85f, 0.3f);
                        GUI.Label(new Rect(width / 2 - 300, 640, 600, 40), $"[F] {hint}", stylePrompt);
                    }
                }
            }

            if (!string.IsNullOrEmpty(centerBanner))
            {
                styleTitle.normal.textColor = Color.white;
                GUI.color = new Color(0, 0, 0, 0.75f);
                GUI.DrawTexture(new Rect(width / 2 - 400, 380, 800, 80), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(width / 2 - 390, 390, 780, 60), centerBanner, styleTitle);
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                styleSub.normal.textColor = Color.white;
                GUI.Label(new Rect(width / 2 - 500, 740, 1000, 90), subtitle, styleSub);
            }

            if (fade > 0f)
            {
                GUI.color = new Color(0, 0, 0, fade);
                GUI.DrawTexture(new Rect(0, 0, width, 900), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void DrawCookingMinigameOverlay(float width, float scale)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.84f);
            GUI.DrawTexture(new Rect(width / 2 - 350, 520, 700, 240), Texture2D.whiteTexture);
            GUI.color = Color.white;
            stylePrompt.normal.textColor = Color.white;

            if (State == QuestState.Step1_Chop)
            {
                GUI.Label(new Rect(width / 2 - 330, 535, 660, 35), $"[1단계: 닭 토막내기] 점선 각도에 맞춰 마우스로 베세요! (성공: {chopScore}/{chopTargetScore} | BAD: {chopBadCount}/2)", stylePrompt);
                stylePrompt.normal.textColor = new Color(0.8f, 0.9f, 1f);
                GUI.Label(new Rect(width / 2 - 330, 570, 660, 30), "?? [우클릭 드래그] 또는 [Q / E] 키로 생닭과 절단 각도를 회전시킬 수 있습니다!", stylePrompt);
                GUI.Label(new Rect(width / 2 - 330, 600, 660, 30), "?? 점선은 생성 후 2초 뒤에 사라집니다! 깜빡거리는 점선을 먼저 베세요!", stylePrompt);

                Vector2 centerGui = new Vector2(width * 0.5f, 340f);

                // 다중 점선 렌더링 (사라지기 직전 점점 빠르게 깜빡거림)
                foreach (var line in activeChopLines)
                {
                    float lifeRatio = Mathf.Clamp01(line.timer / line.maxLife);
                    // 남은 시간이 적을수록 깜빡이는 주파수(freq)가 최대 35까지 급격히 빨라짐
                    float blinkFreq = Mathf.Lerp(38f, 4f, lifeRatio);
                    if (lifeRatio < 0.75f && Mathf.Sin(Time.time * blinkFreq) < 0f) continue;

                    Color lineCol = Color.Lerp(Color.red, new Color(1f, 0.9f, 0.2f), lifeRatio);
                    DrawDashedLine(centerGui + line.centerOffset, line.angleDeg, line.length, lineCol);
                }

                // 현재 마우스 칼질 궤적 표시
                if (isDraggingKnife)
                {
                    Vector2 diff = dragCurrentGuiPos - dragStartGuiPos;
                    if (diff.magnitude > 10f)
                    {
                        float ang = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                        DrawSolidLine((dragStartGuiPos + dragCurrentGuiPos) * 0.5f, ang, diff.magnitude, Color.cyan);
                    }
                }

                // PERFECT! / BAD! 판정 텍스트 출력
                if (chopJudgeTimer > 0f)
                {
                    styleJudge.normal.textColor = chopJudgeColor;
                    GUI.Label(new Rect(width / 2 - 200, 650, 400, 65), chopJudgeText, styleJudge);
                }
            }
            else if (State == QuestState.Step2_Pour)
            {
                string item = pourSubStep == 0 ? "밀가루" : "물";
                GUI.Label(new Rect(width / 2 - 330, 535, 660, 35), $"[2단계: {item} 넣기] 마우스를 좌우로 움직여 주둥이 균형을 잡고 좌클릭으로 부으세요!", stylePrompt);

                // 1) 상단: 주둥이 좌우 흔들림 균형 게이지
                Rect balanceBg = new Rect(width / 2 - 180, 580, 360, 22);
                GUI.color = new Color(0.35f, 0.15f, 0.15f);
                GUI.DrawTexture(balanceBg, Texture2D.whiteTexture);
                // 양푼 안전 구역 (-0.48 ~ 0.48)
                GUI.color = new Color(0.2f, 0.55f, 0.7f, 0.8f);
                GUI.DrawTexture(new Rect(balanceBg.x + balanceBg.width * 0.26f, balanceBg.y, balanceBg.width * 0.48f, balanceBg.height), Texture2D.whiteTexture);
                // 현재 주둥이 위치 마커
                float nozzleNorm = Mathf.Clamp01((pourNozzlePos + 1f) * 0.5f);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(balanceBg.x + balanceBg.width * nozzleNorm - 4, balanceBg.y - 4, 8, balanceBg.height + 8), Texture2D.whiteTexture);

                // 2) 하단: 채워진 양 게이지 (움직이는 초록 타겟 + 울컥거림 표시)
                Rect barBg = new Rect(width / 2 - 240, 630, 480, 36);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.DrawTexture(barBg, Texture2D.whiteTexture);

                float shift = Mathf.Sin(Time.time * 2.2f) * 0.04f;
                float currentMin = Mathf.Clamp(pourTargetMin + shift, 0.55f, 0.82f);
                float currentMax = currentMin + (pourSubStep == 0 ? 0.11f : 0.09f);

                GUI.color = new Color(0.2f, 0.85f, 0.3f, 0.9f);
                GUI.DrawTexture(new Rect(barBg.x + barBg.width * currentMin, barBg.y, barBg.width * (currentMax - currentMin), barBg.height), Texture2D.whiteTexture);

                GUI.color = isSurgeActive ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.75f, 0.2f);
                GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(pourAmount), barBg.height), Texture2D.whiteTexture);

                if (isSurgeActive)
                {
                    stylePrompt.normal.textColor = new Color(1f, 0.4f, 0.3f);
                    GUI.Label(new Rect(width / 2 - 250, 680, 500, 35), "?? 재료가 울컥 쏟아집니다! 넘치지 않게 주의하세요!", stylePrompt);
                }
            }
            else if (State == QuestState.Step3_Stir)
            {
                string phaseTitle = stirElapsedTime <= 5.0f
                    ? $"[전반부 {Mathf.CeilToInt(5f - stirElapsedTime)}초: 천천히 젓기]"
                    : "[후반부 스퍼트: 최고 속도로 미친 듯이 빠르게 젓기!!]";

                GUI.Label(new Rect(width / 2 - 330, 535, 660, 35), $"{phaseTitle} ({Mathf.RoundToInt(stirProgress * 100)}%)", stylePrompt);
                stylePrompt.normal.textColor = stirElapsedTime <= 5.0f ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.45f, 0.2f);
                GUI.Label(new Rect(width / 2 - 330, 575, 660, 35), stirStatusFeedback, stylePrompt);

                Rect barBg = new Rect(width / 2 - 240, 630, 480, 34);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.DrawTexture(barBg, Texture2D.whiteTexture);
                GUI.color = stirElapsedTime <= 5.0f ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.5f, 0.15f);
                GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(stirProgress), barBg.height), Texture2D.whiteTexture);
            }
            else if (State == QuestState.Step4_Fry)
            {
                GUI.Label(new Rect(width / 2 - 330, 535, 660, 35), $"[4단계: 치킨 튀기기] 좁아지고 위치가 바뀌는 초록 구간에서 [Space]! ({frySuccessCount} / 5)", stylePrompt);
                GUI.Label(new Rect(width / 2 - 330, 575, 660, 30), "※ 성공할 때마다 속도가 빨라집니다! 실패 시 0회부터 다시 시작!", stylePrompt);

                Rect barBg = new Rect(width / 2 - 240, 625, 480, 38);
                GUI.color = new Color(0.18f, 0.18f, 0.18f);
                GUI.DrawTexture(barBg, Texture2D.whiteTexture);

                float minZone = fryZoneCenter - fryZoneHalfWidth;
                float widthZone = fryZoneHalfWidth * 2f;

                GUI.color = new Color(0.2f, 0.9f, 0.3f, 0.95f);
                GUI.DrawTexture(new Rect(barBg.x + barBg.width * minZone, barBg.y, barBg.width * widthZone, barBg.height), Texture2D.whiteTexture);

                GUI.color = Color.yellow;
                GUI.DrawTexture(new Rect(barBg.x + barBg.width * fryCursor - 5, barBg.y - 6, 10, barBg.height + 12), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        // GUI 회전 점선 그리기 헬퍼
        private void DrawDashedLine(Vector2 center, float angleDeg, float length, Color color)
        {
            Matrix4x4 oldMat = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, center);
            GUI.color = color;

            int segments = 9;
            float segTotal = length / segments;
            float dashWidth = segTotal * 0.65f;
            float startX = center.x - length * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                GUI.DrawTexture(new Rect(startX + i * segTotal, center.y - 3f, dashWidth, 6f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.matrix = oldMat;
        }

        private void DrawSolidLine(Vector2 center, float angleDeg, float length, Color color)
        {
            Matrix4x4 oldMat = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, center);
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - length * 0.5f, center.y - 2f, length, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.matrix = oldMat;
        }
    }
}