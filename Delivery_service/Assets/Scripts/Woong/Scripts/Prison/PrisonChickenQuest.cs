using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    public enum ChickenIngredientType { Chicken, Flour, Water, Salt, Pepper, Oil }
    public enum CookingStationType { ChoppingBoard, MixingBowl, DeepFryer }

    public sealed class PrisonChickenQuest : MonoBehaviour
    {
        public static PrisonChickenQuest Current { get; private set; }

        public enum CustomStep
        {
            Step1_Cut,          // 1. 화면 전체 와이드 생닭 토막내기 (점선-시작/끝 완벽 일치, 8회)
            Step2_Flour,        // 2. 밀가루 붓기 (움직이는 양푼 조준 + 2배속 황금 스팟)
            Step3_Water,        // 3. 물 붓기 (움직이는 양푼 조준 + 2배속 황금 스팟)
            Step4_Stir,         // 4. 30초 반죽 휘젓기 (판정 완화 + 30초 초과 리셋 + 100% 완료)
            Step5_Oil,          // 5. 식용유 붓기 (움직이는 양푼 조준 + 2배속 황금 스팟)
            Step6_SafeDialHeat, // 6. 금고 다이얼 3개(20칸 불규칙)로 1, 8, 0 맞춰 180도 세팅
            Step7_StarforceFry, // 7. 스타포스 5연속 튀기기
            Step8_Sauce,        // 8. 브러시로 화면 전체 빨간 양념소스 칠하기 (페인팅 기믹!)
            Done
        }

        [Header("팀원 스크립트 자동 연동")]
        public PrisonChickenMission teamMission;
        public ChickenCooking teamCooking;

        [Header("주방 입구 자동 진입 설정")]
        public GameObject kitchenEntranceWall;
        public float autoEnterRadius = 3.2f;

        [Header("1단계: 닭 자르기 횟수 설정")]
        [Tooltip("닭을 토막내야 하는 총 횟수 (기본 8회)")]
        public int cutTargetCount = 8;

        [HideInInspector] public bool InMinigameUI { get; private set; }
        [HideInInspector] public PlayerCombat player;

        [Header("현재 진행 중인 내 미니게임 단계")]
        public CustomStep currentStep = CustomStep.Step1_Cut;
        public bool IsPlayingCustomCooking { get; private set; }

        private bool autoEnteringKitchen = false;
        private Vector3 camBasePos;
        private bool hasCamBase;
        private float continuousShake = 0f;
        private float tempShakePower = 0f;
        private float tempShakeTimer = 0f;

        private string feedbackMessage = "";
        private string bigJudgeText = "";
        private Color bigJudgeColor = Color.white;
        private float bigJudgeTimer = 0f;

        // --- [1단계] 화면 전체 와이드 생닭 토막내기 ---
        private int cutCurrentCount = 0;
        private Vector2 cutStartGui; // 1920x1080 기준 절대 좌표 (오차 원천 차단!)
        private Vector2 cutEndGui;
        private float cutProgress = 0f;
        private bool isTracingCut = false;

        // --- [2, 3, 5단계] 움직이는 양푼 조준해 붓기 (Catch & Pour) ---
        private float pourFillProgress = 0f; // 0 ~ 1.0 (100% 채우면 완료)
        private float spoutX = 0.5f;         // 마우스로 조종하는 상단 주둥이 위치 (0 ~ 1)
        private float bowlX = 0.5f;          // 좌우로 움직이는 하단 양푼 위치 (0 ~ 1)
        private float bowlTargetX = 0.7f;
        private float bowlSpeed = 0.35f;
        private bool isPouringNow = false;
        private bool feverSpotActive = false;
        private float feverTimer = 0f;

        // --- [4단계] 30초 반죽 휘젓기 ---
        private const float StirTimeLimit = 30.0f;
        private float stirRemainingTime = 30.0f;
        private float stirProgress = 0f;
        private float lastMouseAngle = 0f;
        private Vector2 lastMousePos;
        private float currentStirPower = 0f;

        // --- [6단계] 180도 금고 다이얼 3개 ---
        private int[][] safeDials = new int[3][];
        private int[] dialIndices = new int[3];
        private bool[] dialLocked = new bool[3];
        private int currentDialSlot = 0;
        private readonly int[] targetDialNumbers = { 1, 8, 0 };
        private float dialTurnCooldown = 0f;

        // --- [7단계] 스타포스 5연속 튀기기 ---
        private int frySuccessCount = 0;
        private const int FryTargetCount = 5;
        private float fryCursor = 0f;
        private float fryDir = 1f;
        private float fryZoneCenter = 0.5f;
        private float fryZoneHalfWidth = 0.095f;

        // --- [8단계] 화면 전체 브러시 양념소스 페인팅 ---
        private const int SauceCols = 14;
        private const int SauceRows = 6;
        private bool[,] sauceGrid = new bool[SauceCols, SauceRows];
        private int saucePaintedCount = 0;
        private int sauceTotalCells = SauceCols * SauceRows;
        private Rect sauceCanvasRect = new Rect(410f, 160f, 1100f, 490f); // 화면 중앙 전체를 덮는 넓은 페인팅 영역

        private GUIStyle styleTitle, styleGuide, styleSub, styleJudge, styleDialNum, styleDialSub, styleTopTimer;

        private void Awake()
        {
            Current = this;
            ApplyResponsiveCanvasScaling();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            if (!teamMission) teamMission = FindFirstObjectByType<PrisonChickenMission>();
            if (!teamCooking && teamMission) teamCooking = teamMission.cooking;
            if (!teamCooking) teamCooking = FindFirstObjectByType<ChickenCooking>();
            if (!player && teamMission && teamMission.prison) player = teamMission.prison.player;

            ApplyResponsiveCanvasScaling();
            SetupCurrentStep();
        }

        private void ApplyResponsiveCanvasScaling()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (!scaler) scaler = c.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        public bool IsPlayingState()
        {
            return IsPlayingCustomCooking || (teamMission != null && teamMission.CookingActive);
        }

        public void AddWakeUpNoise(Vector3 noisePos)
        {
            if (!IsPlayingState()) return;
            if (teamMission) teamMission.MakeNoise(false);
        }

        public void PickUpIngredient(PrisonCookingIngredient ing)
        {
            if (IsPlayingCustomCooking || InMinigameUI) return;
            if (teamMission)
            {
                ing.OnPickedUp();
                PrisonAudio.Clink(ing.transform.position, 0.3f);
            }
        }

        public void InteractWithStation(PrisonCookingStation station)
        {
            if (InMinigameUI || !IsPlayingState()) return;
            BeginCustomCooking();
        }

        public void ResetCustomRecipe()
        {
            currentStep = CustomStep.Step1_Cut;
            continuousShake = 0f;
            tempShakeTimer = 0f;
            if (teamCooking && teamCooking.knife) teamCooking.knife.gameObject.SetActive(true);
            SetupCurrentStep();
        }

        public void BeginCustomCooking()
        {
            IsPlayingCustomCooking = true;
            InMinigameUI = true;
            if (teamMission && teamMission.camera)
            {
                camBasePos = teamMission.camera.transform.position;
                hasCamBase = true;
            }
            if (currentStep == CustomStep.Done)
            {
                currentStep = CustomStep.Step1_Cut;
            }
            SetupCurrentStep();
        }

        public void CloseCustomCooking()
        {
            IsPlayingCustomCooking = false;
            InMinigameUI = false;
            continuousShake = 0f;
            tempShakeTimer = 0f;
            if (hasCamBase && teamMission && teamMission.camera)
            {
                teamMission.camera.transform.position = camBasePos;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0) return;

            if (teamMission != null && player != null)
            {
                if (teamMission.State != PrisonChickenMission.Stage.Waiting && kitchenEntranceWall != null && kitchenEntranceWall.activeSelf)
                {
                    kitchenEntranceWall.SetActive(false);
                }

                if (teamMission.State == PrisonChickenMission.Stage.Kitchen && !autoEnteringKitchen && teamMission.entrance != null)
                {
                    float dist = Vector3.Distance(player.transform.position, teamMission.entrance.transform.position);
                    if (dist <= autoEnterRadius)
                    {
                        StartCoroutine(AutoEnterKitchenRoutine());
                    }
                }
            }

            if (teamCooking != null && teamMission != null)
            {
                if (teamCooking.Open && teamMission.CookingActive && !IsPlayingCustomCooking)
                    BeginCustomCooking();
                else if ((!teamCooking.Open || !teamMission.CookingActive) && IsPlayingCustomCooking)
                    CloseCustomCooking();
            }

            if (!IsPlayingCustomCooking) return;

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (teamMission) teamMission.LeaveCooking();
                return;
            }

            if (bigJudgeTimer > 0f) bigJudgeTimer -= Time.deltaTime;

            if (hasCamBase && teamMission && teamMission.camera)
            {
                float shake = continuousShake;
                if (tempShakeTimer > 0f)
                {
                    tempShakeTimer -= Time.deltaTime;
                    shake = Mathf.Max(shake, tempShakePower);
                }
                teamMission.camera.transform.position = shake > 0.001f
                    ? camBasePos + Random.insideUnitSphere * shake
                    : camBasePos;
            }

            UpdateMinigameStepLogic();
        }

        private IEnumerator AutoEnterKitchenRoutine()
        {
            autoEnteringKitchen = true;
            var move = player.GetComponent<PlayerMove>();
            if (move) move.isControllable = false;

            teamMission.ResetKitchen();
            yield return new WaitForSeconds(0.4f);

            if (move) move.isControllable = true;
            autoEnteringKitchen = false;
        }

        private void TriggerShake(float power, float duration)
        {
            tempShakePower = power;
            tempShakeTimer = duration;
        }

        private void ShowJudge(string text, Color col, float duration = 0.85f)
        {
            bigJudgeText = text;
            bigJudgeColor = col;
            bigJudgeTimer = duration;
        }

        private void SetupCurrentStep()
        {
            if (teamCooking && teamCooking.knife)
            {
                teamCooking.knife.gameObject.SetActive(currentStep == CustomStep.Step1_Cut);
            }

            switch (currentStep)
            {
                case CustomStep.Step1_Cut:
                    cutCurrentCount = 0;
                    GenerateNextWideCutLine();
                    feedbackMessage = "노란 [시작] 박스를 좌클릭으로 꾹 누른 채 점선을 따라 파란 [끝] 박스까지 시원하게 그으세요!";
                    break;

                case CustomStep.Step2_Flour:
                case CustomStep.Step3_Water:
                case CustomStep.Step5_Oil:
                    pourFillProgress = 0f;
                    spoutX = 0.5f;
                    bowlX = 0.5f;
                    bowlTargetX = Random.Range(0.2f, 0.8f);
                    bowlSpeed = currentStep == CustomStep.Step2_Flour ? 0.32f : (currentStep == CustomStep.Step3_Water ? 0.42f : 0.48f);
                    feverSpotActive = false;
                    feverTimer = Random.Range(1.5f, 2.5f);
                    feedbackMessage = "마우스를 좌우로 움직여 움직이는 [양푼] 위에 조준하고 [좌클릭]을 꾹 눌러 재료를 받아내세요!";
                    break;

                case CustomStep.Step4_Stir:
                    stirRemainingTime = StirTimeLimit;
                    stirProgress = 0f;
                    currentStirPower = 0f;
                    if (Mouse.current != null) lastMousePos = Mouse.current.position.ReadValue();
                    feedbackMessage = "30초 안에 반죽을 100%까지 섞으세요! (초반 8초는 천천히, 이후엔 점점 빠르게 마우스를 돌리세요!)";
                    break;

                case CustomStep.Step6_SafeDialHeat:
                    GenerateThreeSafeDials();
                    feedbackMessage = "[A / D] 키 또는 [마우스 휠]로 다이얼을 돌려 숫자 1 -> 8 -> 0 을 찾고 [Space] 키로 확정하세요!";
                    break;

                case CustomStep.Step7_StarforceFry:
                    frySuccessCount = 0;
                    fryCursor = 0f;
                    fryDir = 1f;
                    continuousShake = 0f;
                    RandomizeFryZone();
                    feedbackMessage = "노란 바늘이 초록색 구간에 올 때 [Space] 키를 누르세요! (총 5회 성공 시 완성)";
                    break;

                case CustomStep.Step8_Sauce:
                    saucePaintedCount = 0;
                    for (int x = 0; x < SauceCols; x++)
                        for (int y = 0; y < SauceRows; y++)
                            sauceGrid[x, y] = false;
                    continuousShake = 0f;
                    feedbackMessage = "마우스 좌클릭을 꾹 누른 채 화면 중앙 전체를 브러시로 문질러 빨간 양념소스를 100% 바르세요!";
                    break;
            }
        }

        private void AdvanceToNextStep()
        {
            if (currentStep == CustomStep.Step8_Sauce)
            {
                currentStep = CustomStep.Done;
                if (teamCooking)
                {
                    if (teamCooking.rawChicken) teamCooking.rawChicken.SetActive(false);
                    if (teamCooking.cookedChicken) teamCooking.cookedChicken.SetActive(true);
                    if (teamCooking.knife) teamCooking.knife.gameObject.SetActive(false);
                }
                CloseCustomCooking();
                if (teamMission) teamMission.FinishCooking();
                return;
            }
            currentStep++;
            SetupCurrentStep();
        }

        // ★ 1번 요청 해결: 화면 중앙 도마 전체를 시원하게 가로지르는 긴 절단선 생성 (380~520px 길이)
        private void GenerateNextWideCutLine()
        {
            isTracingCut = false;
            cutProgress = 0f;

            // 1920x1080 화면 중앙(도마 위 영역)에서 넓은 범위로 중심점과 각도 설정
            Vector2 center = new Vector2(960f + Random.Range(-180f, 180f), 420f + Random.Range(-90f, 90f));
            float angleRad = Random.Range(-70f, 70f) * Mathf.Deg2Rad;
            if (Random.value > 0.5f) angleRad += Mathf.PI; // 방향 다양화

            float halfLen = Random.Range(190f, 260f); // 전체 길이 380 ~ 520px로 큼직하게!
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            cutStartGui = center - dir * halfLen;
            cutEndGui = center + dir * halfLen;

            // 화면 상단 타이머나 하단 UI 패널을 침범하지 않도록 안전 클램프
            cutStartGui.x = Mathf.Clamp(cutStartGui.x, 350f, 1570f);
            cutStartGui.y = Mathf.Clamp(cutStartGui.y, 160f, 640f);
            cutEndGui.x = Mathf.Clamp(cutEndGui.x, 350f, 1570f);
            cutEndGui.y = Mathf.Clamp(cutEndGui.y, 160f, 640f);
        }

        private void GenerateThreeSafeDials()
        {
            int[] distractors = { 2, 3, 4, 5, 6, 7, 9 };
            for (int d = 0; d < 3; d++)
            {
                safeDials[d] = new int[20];
                dialLocked[d] = false;
                List<int> emptySlots = new List<int>();
                for (int i = 0; i < 20; i++) emptySlots.Add(i);

                int[] uniqueTargets = { 1, 8, 0 };
                foreach (int uNum in uniqueTargets)
                {
                    int pickIdx = Random.Range(0, emptySlots.Count);
                    int slot = emptySlots[pickIdx];
                    emptySlots.RemoveAt(pickIdx);
                    safeDials[d][slot] = uNum;
                }

                int lastNum = -1;
                foreach (int slot in emptySlots)
                {
                    int rNum = distractors[Random.Range(0, distractors.Length)];
                    if (rNum == lastNum) rNum = distractors[Random.Range(0, distractors.Length)];
                    safeDials[d][slot] = rNum;
                    lastNum = rNum;
                }

                dialIndices[d] = Random.Range(0, 20);
                if (safeDials[d][dialIndices[d]] == targetDialNumbers[d])
                    dialIndices[d] = (dialIndices[d] + 5) % 20;
            }
            currentDialSlot = 0;
        }

        private void RandomizeFryZone()
        {
            fryZoneHalfWidth = Mathf.Max(0.048f, 0.095f - frySuccessCount * 0.011f);
            fryZoneCenter = Random.Range(0.20f + fryZoneHalfWidth, 0.80f - fryZoneHalfWidth);
        }

        private void UpdateMinigameStepLogic()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null || kb == null) return;

            Vector2 mScreen = mouse.position.ReadValue();
            Vector2 mGui = new Vector2(mScreen.x / Screen.width * 1920f, (Screen.height - mScreen.y) / Screen.height * 1080f);

            switch (currentStep)
            {
                // =========================================================================
                // 1. 와이드 생닭 자르기 (점선과 시작/끝 박스 100% 일치 + 넓은 칼질)
                // =========================================================================
                case CustomStep.Step1_Cut:
                    if (teamCooking && teamCooking.knife && teamCooking.rawChicken && teamMission && teamMission.camera)
                    {
                        Plane boardPlane = new Plane(Vector3.up, teamCooking.rawChicken.transform.position + Vector3.up * (isTracingCut ? 0.02f : 0.09f));
                        Ray ray = teamMission.camera.ScreenPointToRay(mScreen);
                        if (boardPlane.Raycast(ray, out float enterDist))
                        {
                            Vector3 targetWorldPos = ray.GetPoint(enterDist);
                            teamCooking.knife.position = Vector3.Lerp(teamCooking.knife.position, targetWorldPos, Time.deltaTime * 25f);
                        }
                    }

                    Vector2 lineVec = cutEndGui - cutStartGui;
                    float lineLen = Mathf.Max(1f, lineVec.magnitude);
                    Vector2 lineDir = lineVec.normalized;

                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        if (Vector2.Distance(mGui, cutStartGui) <= 115f)
                        {
                            isTracingCut = true;
                            cutProgress = 0f;
                        }
                        else if (Vector2.Distance(mGui, cutEndGui) <= 115f)
                        {
                            Vector2 temp = cutStartGui; cutStartGui = cutEndGui; cutEndGui = temp;
                            isTracingCut = true;
                            cutProgress = 0f;
                        }
                        else
                        {
                            feedbackMessage = "노란색 [시작] 박스를 마우스 좌클릭으로 누른 채 점선을 따라 [끝]까지 그어주세요!";
                        }
                    }
                    else if (mouse.leftButton.isPressed && isTracingCut)
                    {
                        Vector2 toMouse = mGui - cutStartGui;
                        float proj = Vector2.Dot(toMouse, lineDir);
                        float perpDist = Vector2.Distance(cutStartGui + lineDir * Mathf.Clamp(proj, 0, lineLen), mGui);

                        if (perpDist > 125f)
                        {
                            isTracingCut = false;
                            cutProgress = 0f;
                            ShowJudge("삐뚤어짐! 다시 그으세요", new Color(1f, 0.4f, 0.3f));
                            TriggerShake(0.025f, 0.15f);
                            feedbackMessage = "선에서 너무 벗어났습니다! 점선을 따라 시원하게 다시 그어주세요.";
                            return;
                        }

                        float newRatio = Mathf.Clamp01(proj / lineLen);
                        if (newRatio > cutProgress) cutProgress = newRatio;

                        if (cutProgress >= 0.86f)
                        {
                            isTracingCut = false;
                            cutCurrentCount++;
                            PrisonAudio.Clink(transform.position, 0.55f);
                            TriggerShake(0.038f, 0.18f);
                            ShowJudge($"PERFECT! ({cutCurrentCount}/{cutTargetCount})", new Color(0.25f, 1f, 0.45f));

                            if (cutCurrentCount >= cutTargetCount)
                            {
                                AdvanceToNextStep();
                            }
                            else
                            {
                                GenerateNextWideCutLine();
                                feedbackMessage = $"잘했습니다! 다음 점선을 따라 크게 베어내세요! ({cutCurrentCount} / {cutTargetCount})";
                            }
                        }
                    }
                    else if (mouse.leftButton.wasReleasedThisFrame && isTracingCut)
                    {
                        isTracingCut = false;
                        if (cutProgress >= 0.78f)
                        {
                            cutCurrentCount++;
                            PrisonAudio.Clink(transform.position, 0.55f);
                            TriggerShake(0.038f, 0.18f);
                            ShowJudge($"PERFECT! ({cutCurrentCount}/{cutTargetCount})", new Color(0.25f, 1f, 0.45f));
                            if (cutCurrentCount >= cutTargetCount) AdvanceToNextStep();
                            else GenerateNextWideCutLine();
                        }
                        else
                        {
                            cutProgress = 0f;
                            feedbackMessage = "중간에 떼지 말고 파란색 [끝] 박스까지 쭉 그어주세요!";
                        }
                    }
                    break;

                // =========================================================================
                // 2, 3, 5. 밀가루 / 물 / 기름 붓기 (신규 기믹: 움직이는 양푼 조준 + 황금 피버 스팟!)
                // =========================================================================
                case CustomStep.Step2_Flour:
                case CustomStep.Step3_Water:
                case CustomStep.Step5_Oil:
                    // 1) 마우스 X 좌표로 상단 주둥이(spoutX) 위치 직접 조종
                    spoutX = Mathf.Clamp01((mGui.x - 560f) / 800f);

                    // 2) 하단 양푼(bowlX)이 좌우로 불규칙하게 이동함
                    bowlX = Mathf.MoveTowards(bowlX, bowlTargetX, bowlSpeed * Time.deltaTime);
                    if (Mathf.Abs(bowlX - bowlTargetX) < 0.02f)
                    {
                        bowlTargetX = Random.Range(0.15f, 0.85f);
                        bowlSpeed = Random.Range(0.28f, 0.52f);
                    }

                    // 3) 황금 2배속 피버 스팟 주기적 발동
                    feverTimer -= Time.deltaTime;
                    if (feverTimer <= 0f)
                    {
                        feverSpotActive = !feverSpotActive;
                        feverTimer = feverSpotActive ? Random.Range(1.8f, 2.8f) : Random.Range(1.5f, 2.5f);
                    }

                    isPouringNow = mouse.leftButton.isPressed;
                    if (isPouringNow)
                    {
                        float diff = Mathf.Abs(spoutX - bowlX);

                        // 황금 피버 구간(정중앙 0.05 이내) 명중 시 2.5배 속도로 초고속 충전!
                        if (feverSpotActive && diff <= 0.055f)
                        {
                            pourFillProgress += Time.deltaTime * 0.55f;
                            TriggerShake(0.015f, 0.05f);
                            feedbackMessage = "★ 황금 스팟 명중!! 2.5배 속도로 빠르게 차오릅니다!! ★";
                        }
                        // 일반 양푼 범위(0.14 이내) 명중 시 정상 충전
                        else if (diff <= 0.14f)
                        {
                            pourFillProgress += Time.deltaTime * 0.25f;
                            feedbackMessage = "좋습니다! 움직이는 양푼을 따라가며 계속 부으세요!";
                        }
                        // 양푼 밖으로 빗나간 상태에서 부으면 바닥에 흘려서 게이지 감소!
                        else
                        {
                            pourFillProgress = Mathf.Max(0f, pourFillProgress - Time.deltaTime * 0.20f);
                            TriggerShake(0.03f, 0.1f);
                            feedbackMessage = "경고! 양푼 밖에 쏟고 있습니다! 마우스를 움직여 초록색 양푼 위에 조준하세요!";
                        }
                    }
                    else
                    {
                        feedbackMessage = "마우스를 좌우로 움직여 초록색 [양푼] 위에 조준하고 [좌클릭]을 꾹 눌러 부으세요!";
                    }

                    if (pourFillProgress >= 1.0f)
                    {
                        pourFillProgress = 1.0f;
                        isPouringNow = false;
                        PrisonAudio.Clink(transform.position, 0.55f);
                        ShowJudge("100% 계량 성공!", new Color(0.3f, 1f, 0.4f));
                        AdvanceToNextStep();
                    }
                    break;

                // =========================================================================
                // 4. 30초 반죽 휘젓기 (판정 완화 + 30초 초과 리셋 + 100% 완료)
                // =========================================================================
                case CustomStep.Step4_Stir:
                    stirRemainingTime -= Time.deltaTime;
                    if (stirRemainingTime <= 0f)
                    {
                        stirRemainingTime = StirTimeLimit;
                        stirProgress = 0f;
                        currentStirPower = 0f;
                        TriggerShake(0.045f, 0.25f);
                        ShowJudge("시간 초과! 0%부터 다시!", Color.red, 1.0f);
                        feedbackMessage = "30초 안에 100%를 채우지 못해 반죽이 굳었습니다! 처음부터 다시 저으세요!";
                        return;
                    }

                    Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 deltaCenter = mScreen - screenCenter;
                    float frameDeg = 0f;
                    if (deltaCenter.magnitude > 10f)
                    {
                        float angle = Mathf.Atan2(deltaCenter.y, deltaCenter.x) * Mathf.Rad2Deg;
                        float dAngle = Mathf.Abs(Mathf.DeltaAngle(lastMouseAngle, angle));
                        if (dAngle > 0.1f && dAngle < 120f) frameDeg = dAngle;
                        lastMouseAngle = angle;
                    }

                    float mouseMoveDist = Vector2.Distance(mScreen, lastMousePos) / Mathf.Max(1f, Screen.height) * 900f;
                    lastMousePos = mScreen;

                    float instPower = (frameDeg * 1.4f + mouseMoveDist * 1.2f) / Mathf.Max(0.0001f, Time.deltaTime);
                    currentStirPower = Mathf.Lerp(currentStirPower, instPower, Time.deltaTime * 10f);

                    if (teamCooking && teamCooking.bowl && currentStirPower > 50f)
                    {
                        teamCooking.bowl.Rotate(0f, Time.deltaTime * Mathf.Min(currentStirPower * 0.4f, 360f), 0f);
                    }

                    float elapsed = StirTimeLimit - stirRemainingTime;
                    if (elapsed < 8.0f)
                    {
                        if (currentStirPower >= 80f)
                        {
                            feedbackMessage = "[초반 구간] 좋습니다! 부드럽게 마우스를 돌려 반죽을 풀어주세요.";
                            stirProgress += Time.deltaTime / 23.0f;
                        }
                        else feedbackMessage = "마우스를 움직여 반죽을 저어주세요!";
                    }
                    else if (elapsed < 18.0f)
                    {
                        if (currentStirPower >= 220f)
                        {
                            feedbackMessage = "[중반 구간] 속도를 조금 더 높여 반죽을 힘차게 섞어주세요!";
                            stirProgress += Time.deltaTime / 22.0f;
                        }
                        else feedbackMessage = "속도가 느립니다! 마우스를 조금 더 빠르게 돌리세요!";
                    }
                    else
                    {
                        if (currentStirPower >= 420f)
                        {
                            feedbackMessage = "[후반 스퍼트!] 마우스를 빠르게 돌려 100%까지 단숨에 채우세요!";
                            stirProgress += Time.deltaTime / 20.0f;
                            TriggerShake(0.015f, 0.05f);
                        }
                        else feedbackMessage = "후반부입니다! 마우스를 훨씬 더 빠르게 돌려야 게이지가 올라갑니다!";
                    }

                    if (stirProgress >= 1.0f)
                    {
                        stirProgress = 1.0f;
                        PrisonAudio.Clink(transform.position, 0.6f);
                        ShowJudge("100% 반죽 완성!", new Color(0.3f, 1f, 0.4f));
                        AdvanceToNextStep();
                    }
                    break;

                // =========================================================================
                // 6. 불 온도 180도 맞추기 (금고 다이얼 1 - 8 - 0)
                // =========================================================================
                case CustomStep.Step6_SafeDialHeat:
                    if (dialTurnCooldown > 0f) dialTurnCooldown -= Time.deltaTime;
                    int turnDir = 0;
                    if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) turnDir = -1;
                    else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) turnDir = 1;
                    else if (dialTurnCooldown <= 0f)
                    {
                        float scroll = mouse.scroll.ReadValue().y;
                        if (scroll > 0.1f) { turnDir = 1; dialTurnCooldown = 0.08f; }
                        else if (scroll < -0.1f) { turnDir = -1; dialTurnCooldown = 0.08f; }
                    }

                    if (turnDir != 0 && currentDialSlot < 3)
                    {
                        dialIndices[currentDialSlot] = (dialIndices[currentDialSlot] + turnDir + 20) % 20;
                        PrisonAudio.Clink(transform.position, 0.15f);
                    }

                    if (kb.spaceKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame)
                    {
                        int currentShownNum = safeDials[currentDialSlot][dialIndices[currentDialSlot]];
                        int targetNum = targetDialNumbers[currentDialSlot];

                        if (currentShownNum == targetNum)
                        {
                            dialLocked[currentDialSlot] = true;
                            currentDialSlot++;
                            PrisonAudio.Clink(transform.position, 0.55f);
                            TriggerShake(0.025f, 0.15f);
                            ShowJudge($"{targetNum}번 다이얼 고정!", new Color(0.3f, 1f, 0.4f), 0.7f);
                            if (currentDialSlot >= 3)
                            {
                                ShowJudge("180도 세팅 완료!", Color.cyan, 1.0f);
                                AdvanceToNextStep();
                            }
                        }
                        else
                        {
                            TriggerShake(0.05f, 0.25f);
                            ShowJudge("숫자 불일치! 다이얼 리셋!", Color.red, 0.8f);
                            GenerateThreeSafeDials();
                            feedbackMessage = $"[{targetNum}]번에서 확정해야 합니다! 다이얼 배열이 초기화되었습니다.";
                        }
                    }
                    break;

                // =========================================================================
                // 7. 스타포스 치킨 튀기기
                // =========================================================================
                case CustomStep.Step7_StarforceFry:
                    float speedFry = 0.75f + frySuccessCount * 0.20f;
                    fryCursor += fryDir * speedFry * Time.deltaTime;
                    if (fryCursor >= 1f) { fryCursor = 1f; fryDir = -1f; }
                    else if (fryCursor <= 0f) { fryCursor = 0f; fryDir = 1f; }

                    if (kb.spaceKey.wasPressedThisFrame)
                    {
                        float minZ = fryZoneCenter - fryZoneHalfWidth;
                        float maxZ = fryZoneCenter + fryZoneHalfWidth;

                        if (fryCursor >= minZ && fryCursor <= maxZ)
                        {
                            frySuccessCount++;
                            PrisonAudio.Clink(transform.position, 0.65f);
                            if (teamCooking && teamCooking.rawChicken) teamCooking.rawChicken.transform.Rotate(0, 0, 180f);

                            if (frySuccessCount == 1) continuousShake = 0f;
                            else if (frySuccessCount == 2) continuousShake = 0.018f;
                            else if (frySuccessCount == 3) continuousShake = 0.038f;
                            else if (frySuccessCount >= 4) continuousShake = 0.075f;

                            ShowJudge($"PERFECT! ({frySuccessCount} / 5)", new Color(0.25f, 1f, 0.45f), 0.6f);

                            if (frySuccessCount >= FryTargetCount)
                            {
                                continuousShake = 0f;
                                AdvanceToNextStep();
                            }
                            else RandomizeFryZone();
                        }
                        else
                        {
                            frySuccessCount = 0;
                            continuousShake = 0f;
                            RandomizeFryZone();
                            TriggerShake(0.05f, 0.25f);
                            ShowJudge("MISS! 0회부터 다시!", Color.red, 0.8f);
                            if (teamMission) teamMission.MakeNoise(false);
                        }
                    }
                    break;

                // =========================================================================
                // 8. 브러시로 화면 전체 빨간 양념소스 칠하기 (3번 요청 완벽 반영!)
                // =========================================================================
                case CustomStep.Step8_Sauce:
                    if (mouse.leftButton.isPressed)
                    {
                        float cellW = sauceCanvasRect.width / SauceCols;
                        float cellH = sauceCanvasRect.height / SauceRows;
                        bool newlyPainted = false;

                        // 브러시 반경(약 110px) 안에 들어온 모든 칸을 빨간 양념소스로 칠함!
                        for (int x = 0; x < SauceCols; x++)
                        {
                            for (int y = 0; y < SauceRows; y++)
                            {
                                if (sauceGrid[x, y]) continue;
                                Vector2 cellCenter = new Vector2(
                                    sauceCanvasRect.x + (x + 0.5f) * cellW,
                                    sauceCanvasRect.y + (y + 0.5f) * cellH
                                );
                                if (Vector2.Distance(mGui, cellCenter) <= 115f)
                                {
                                    sauceGrid[x, y] = true;
                                    saucePaintedCount++;
                                    newlyPainted = true;
                                }
                            }
                        }

                        if (newlyPainted && Random.value < 0.25f)
                        {
                            PrisonAudio.Clink(transform.position, 0.15f);
                        }

                        if (saucePaintedCount >= sauceTotalCells)
                        {
                            PrisonAudio.Clink(transform.position, 0.7f);
                            ShowJudge("양념 치킨 완성!!", new Color(1f, 0.3f, 0.2f), 1.0f);
                            AdvanceToNextStep();
                        }
                    }
                    break;
            }
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0) return;

            GUI.depth = -100;
            Matrix4x4 prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));

            styleTitle ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 30, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleGuide ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            styleSub ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 20, alignment = TextAnchor.MiddleCenter };
            styleJudge ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 46, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleDialNum ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styleDialSub ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 18, alignment = TextAnchor.MiddleCenter };
            styleTopTimer ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            if (teamMission != null && teamMission.CookingActive)
            {
                GUI.color = new Color(0.04f, 0.07f, 0.11f, 0.92f);
                GUI.DrawTexture(new Rect(960f - 310f, 28f, 620f, 54f), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.75f, 0.3f);
                GUI.DrawTexture(new Rect(960f - 310f, 28f, 620f, 3f), Texture2D.whiteTexture);

                int m = Mathf.FloorToInt(teamMission.Remaining / 60f);
                int s = Mathf.FloorToInt(teamMission.Remaining % 60f);
                styleTopTimer.normal.textColor = teamMission.Remaining < 60f ? new Color(1f, 0.35f, 0.35f) : Color.white;
                GUI.color = Color.white;
                GUI.Label(new Rect(960f - 300f, 34f, 600f, 42f), $"주방장 기상까지 {m:00}:{s:00}   |   소음 경고 {teamMission.Noise} / 3", styleTopTimer);
            }

            if (!IsPlayingCustomCooking)
            {
                GUI.matrix = prevMatrix;
                return;
            }

            Rect panelRect = new Rect(360f, 700f, 1200f, 330f);
            GUI.color = new Color(0.03f, 0.06f, 0.10f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.72f, 0.25f, 0.9f);
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int saucePercent = Mathf.RoundToInt((float)saucePaintedCount / sauceTotalCells * 100f);
            string stepTitle = currentStep switch
            {
                CustomStep.Step1_Cut => $"[1/8 단계] 생닭 점선 따라 크게 토막내기 ({cutCurrentCount} / {cutTargetCount})",
                CustomStep.Step2_Flour => $"[2/8 단계] 움직이는 양푼에 밀가루 받아내기 ({Mathf.RoundToInt(pourFillProgress * 100)}%)",
                CustomStep.Step3_Water => $"[3/8 단계] 움직이는 양푼에 물 받아내기 ({Mathf.RoundToInt(pourFillProgress * 100)}%)",
                CustomStep.Step4_Stir => $"[4/8 단계] 30초 황금 반죽 휘젓기 (진행도: {Mathf.RoundToInt(stirProgress * 100)}% | 남은 시간: {Mathf.CeilToInt(stirRemainingTime)}초)",
                CustomStep.Step5_Oil => $"[5/8 단계] 움직이는 냄비에 식용유 받아내기 ({Mathf.RoundToInt(pourFillProgress * 100)}%)",
                CustomStep.Step6_SafeDialHeat => "[6/8 단계] 금고 다이얼을 돌려 튀김 온도 1 - 8 - 0 도 맞추기",
                CustomStep.Step7_StarforceFry => $"[7/8 단계] 스타포스 치킨 튀기기 ({frySuccessCount} / {FryTargetCount})",
                CustomStep.Step8_Sauce => $"[8/8 단계] 브러시로 화면 전체 빨간 양념소스 바르기 ({saucePercent}%)",
                _ => "치킨 조리 완료"
            };

            styleTitle.normal.textColor = new Color(1f, 0.82f, 0.32f);
            GUI.Label(new Rect(380f, 716f, 1160f, 40f), stepTitle, styleTitle);

            styleGuide.normal.textColor = new Color(0.92f, 0.95f, 1f);
            GUI.Label(new Rect(400f, 760f, 1120f, 48f), feedbackMessage, styleGuide);

            switch (currentStep)
            {
                case CustomStep.Step1_Cut:
                    // ★ 회전 행렬 버그 없이 시작점(cutStartGui)과 끝점(cutEndGui) 사이를 100% 정확하게 잇는 점선 렌더링!
                    DrawSegmentedLineBetween(cutStartGui, cutEndGui, 18, new Color(1f, 0.85f, 0.2f, 0.95f), 10f);

                    if (cutProgress > 0.01f)
                    {
                        Vector2 curPt = Vector2.Lerp(cutStartGui, cutEndGui, cutProgress);
                        DrawSegmentedLineBetween(cutStartGui, curPt, Mathf.Max(2, Mathf.RoundToInt(18 * cutProgress)), new Color(0.2f, 1f, 0.45f), 12f);
                    }

                    // 시작점과 끝점 박스 (점선 양 끝단에 정확히 맞물림!)
                    GUI.color = new Color(1f, 0.8f, 0.1f);
                    GUI.DrawTexture(new Rect(cutStartGui.x - 34f, cutStartGui.y - 20f, 68f, 40f), Texture2D.whiteTexture);
                    styleDialSub.normal.textColor = Color.black;
                    GUI.Label(new Rect(cutStartGui.x - 34f, cutStartGui.y - 20f, 68f, 40f), "시작", styleDialSub);

                    GUI.color = new Color(0.2f, 0.9f, 1f);
                    GUI.DrawTexture(new Rect(cutEndGui.x - 34f, cutEndGui.y - 20f, 68f, 40f), Texture2D.whiteTexture);
                    GUI.Label(new Rect(cutEndGui.x - 34f, cutEndGui.y - 20f, 68f, 40f), "끝", styleDialSub);

                    // 하단 성공 칸 표시 (cutTargetCount 개수에 맞춰 자동 정렬)
                    float boxW = Mathf.Min(75f, 720f / cutTargetCount - 12f);
                    float totalW = cutTargetCount * (boxW + 12f) - 12f;
                    float startX = 960f - totalW * 0.5f;
                    for (int i = 0; i < cutTargetCount; i++)
                    {
                        GUI.color = i < cutCurrentCount ? new Color(0.25f, 0.92f, 0.45f) : new Color(0.22f, 0.27f, 0.32f);
                        GUI.DrawTexture(new Rect(startX + i * (boxW + 12f), 860f, boxW, 34f), Texture2D.whiteTexture);
                    }
                    break;

                case CustomStep.Step2_Flour:
                case CustomStep.Step3_Water:
                case CustomStep.Step5_Oil:
                    Rect trackRect = new Rect(560f, 825f, 800f, 65f);
                    GUI.color = new Color(0.14f, 0.17f, 0.22f);
                    GUI.DrawTexture(trackRect, Texture2D.whiteTexture);

                    // 1) 움직이는 초록색 양푼(Target Zone)
                    float bowlWidth = trackRect.width * 0.28f;
                    float bowlCenterPx = trackRect.x + trackRect.width * bowlX;
                    GUI.color = new Color(0.2f, 0.78f, 0.38f, 0.9f);
                    GUI.DrawTexture(new Rect(bowlCenterPx - bowlWidth * 0.5f, trackRect.y + 18f, bowlWidth, 44f), Texture2D.whiteTexture);

                    // 황금 2배속 스팟 표시
                    if (feverSpotActive)
                    {
                        float feverW = trackRect.width * 0.11f;
                        GUI.color = new Color(1f, 0.88f, 0.15f, 0.95f);
                        GUI.DrawTexture(new Rect(bowlCenterPx - feverW * 0.5f, trackRect.y + 18f, feverW, 44f), Texture2D.whiteTexture);
                        styleDialSub.normal.textColor = Color.black;
                        GUI.Label(new Rect(bowlCenterPx - 80f, trackRect.y + 24f, 160f, 32f), "★2배속 스팟★", styleDialSub);
                    }
                    else
                    {
                        styleDialSub.normal.textColor = Color.black;
                        GUI.Label(new Rect(bowlCenterPx - 80f, trackRect.y + 24f, 160f, 32f), "움직이는 양푼", styleDialSub);
                    }

                    // 2) 내 마우스 주둥이(Spout) 및 쏟아지는 물줄기/가루 연출
                    float spoutPx = trackRect.x + trackRect.width * spoutX;
                    if (isPouringNow)
                    {
                        Color streamCol = currentStep == CustomStep.Step2_Flour ? Color.white : (currentStep == CustomStep.Step3_Water ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.85f, 0.2f));
                        GUI.color = streamCol;
                        GUI.DrawTexture(new Rect(spoutPx - 8f, trackRect.y, 16f, 62f), Texture2D.whiteTexture);
                    }
                    GUI.color = Color.cyan;
                    GUI.DrawTexture(new Rect(spoutPx - 22f, trackRect.y - 8f, 44f, 20f), Texture2D.whiteTexture);

                    // 3) 하단 100% 누적 게이지
                    Rect fillRect = new Rect(560f, 910f, 800f, 36f);
                    GUI.color = new Color(0.18f, 0.2f, 0.24f);
                    GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 0.75f, 0.2f);
                    GUI.DrawTexture(new Rect(fillRect.x, fillRect.y, fillRect.width * Mathf.Clamp01(pourFillProgress), fillRect.height), Texture2D.whiteTexture);
                    styleDialSub.normal.textColor = Color.white;
                    GUI.color = Color.white;
                    GUI.Label(fillRect, $"채워진 양 : {Mathf.RoundToInt(pourFillProgress * 100)} / 100 %", styleDialSub);
                    break;

                case CustomStep.Step4_Stir:
                    Rect sRect = new Rect(560f, 855f, 800f, 44f);
                    GUI.color = new Color(0.18f, 0.2f, 0.24f);
                    GUI.DrawTexture(sRect, Texture2D.whiteTexture);
                    float elapsed = StirTimeLimit - stirRemainingTime;
                    GUI.color = elapsed < 8f ? new Color(0.3f, 0.85f, 1f) : (elapsed < 18f ? new Color(1f, 0.75f, 0.2f) : new Color(1f, 0.38f, 0.2f));
                    GUI.DrawTexture(new Rect(sRect.x, sRect.y, sRect.width * Mathf.Clamp01(stirProgress), sRect.height), Texture2D.whiteTexture);

                    styleSub.normal.textColor = Color.white;
                    GUI.color = Color.white;
                    GUI.Label(sRect, $"반죽 완성도 : {Mathf.RoundToInt(stirProgress * 100)}% (100% 달성 시 완료)", styleSub);
                    break;

                case CustomStep.Step6_SafeDialHeat:
                    for (int d = 0; d < 3; d++)
                    {
                        float dX = 960f + (d - 1) * 230f;
                        float dY = 875f;
                        bool isCurrent = (d == currentDialSlot);

                        GUI.color = dialLocked[d] ? new Color(0.2f, 0.85f, 0.4f) : (isCurrent ? new Color(1f, 0.78f, 0.2f) : new Color(0.3f, 0.35f, 0.4f));
                        GUI.DrawTexture(new Rect(dX - 90f, dY - 62f, 180f, 124f), Texture2D.whiteTexture);

                        GUI.color = new Color(0.07f, 0.09f, 0.13f);
                        GUI.DrawTexture(new Rect(dX - 84f, dY - 56f, 168f, 112f), Texture2D.whiteTexture);

                        int visibleNum = safeDials[d][dialIndices[d]];
                        styleDialNum.normal.textColor = dialLocked[d] ? new Color(0.3f, 1f, 0.45f) : (isCurrent ? Color.white : Color.gray);
                        GUI.color = Color.white;
                        GUI.Label(new Rect(dX - 80f, dY - 48f, 160f, 60f), visibleNum.ToString(), styleDialNum);

                        styleDialSub.normal.textColor = dialLocked[d] ? new Color(0.4f, 1f, 0.5f) : new Color(0.8f, 0.85f, 0.9f);
                        string slotLabel = dialLocked[d] ? "고정 완료" : $"목표 숫자: {targetDialNumbers[d]} ({dialIndices[d] + 1}/20칸)";
                        GUI.Label(new Rect(dX - 84f, dY + 18f, 168f, 32f), slotLabel, styleDialSub);
                    }
                    styleTitle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                    GUI.Label(new Rect(960f + 350f, 845f, 120f, 60f), "도(C)", styleTitle);
                    break;

                case CustomStep.Step7_StarforceFry:
                    Rect fRect = new Rect(560f, 855f, 800f, 44f);
                    GUI.color = new Color(0.18f, 0.2f, 0.24f);
                    GUI.DrawTexture(fRect, Texture2D.whiteTexture);

                    float minZ = fryZoneCenter - fryZoneHalfWidth;
                    float wZ = fryZoneHalfWidth * 2f;
                    GUI.color = new Color(0.2f, 0.9f, 0.35f, 0.95f);
                    GUI.DrawTexture(new Rect(fRect.x + fRect.width * minZ, fRect.y, fRect.width * wZ, fRect.height), Texture2D.whiteTexture);

                    GUI.color = Color.yellow;
                    GUI.DrawTexture(new Rect(fRect.x + fRect.width * fryCursor - 6f, fRect.y - 8f, 12f, fRect.height + 16f), Texture2D.whiteTexture);
                    break;

                case CustomStep.Step8_Sauce:
                    // ★ 3번 요청 해결: 화면 중앙 전체에 펼쳐진 양념소스 브러시 페인팅 캔버스!
                    GUI.color = new Color(1f, 0.9f, 0.7f, 0.18f);
                    GUI.DrawTexture(sauceCanvasRect, Texture2D.whiteTexture);

                    float cW = sauceCanvasRect.width / SauceCols;
                    float cH = sauceCanvasRect.height / SauceRows;

                    for (int x = 0; x < SauceCols; x++)
                    {
                        for (int y = 0; y < SauceRows; y++)
                        {
                            Rect cellR = new Rect(sauceCanvasRect.x + x * cW, sauceCanvasRect.y + y * cH, cW + 2f, cH + 2f);
                            if (sauceGrid[x, y])
                            {
                                // 칠해진 곳은 먹음직스러운 매콤달콤 빨간 양념소스 색상으로 채워짐!
                                GUI.color = new Color(0.88f, 0.16f, 0.12f, 0.88f);
                                GUI.DrawTexture(cellR, Texture2D.whiteTexture);
                                // 참깨 토핑 포인트
                                if ((x + y) % 3 == 0)
                                {
                                    GUI.color = new Color(1f, 0.92f, 0.65f, 0.9f);
                                    GUI.DrawTexture(new Rect(cellR.center.x - 4f, cellR.center.y - 4f, 8f, 8f), Texture2D.whiteTexture);
                                }
                            }
                            else
                            {
                                // 아직 안 칠해진 곳은 옅은 점선 격자로 표시
                                GUI.color = new Color(1f, 1f, 1f, 0.12f);
                                GUI.DrawTexture(new Rect(cellR.center.x - 6f, cellR.center.y - 6f, 12f, 12f), Texture2D.whiteTexture);
                            }
                        }
                    }

                    // 마우스 커서 위치에 브러시 표시
                    if (Mouse.current != null)
                    {
                        Vector2 ms = Mouse.current.position.ReadValue();
                        Vector2 mg = new Vector2(ms.x / Screen.width * 1920f, (Screen.height - ms.y) / Screen.height * 1080f);
                        GUI.color = new Color(1f, 0.35f, 0.2f, 0.75f);
                        GUI.DrawTexture(new Rect(mg.x - 45f, mg.y - 45f, 90f, 90f), Texture2D.whiteTexture);
                        styleDialSub.normal.textColor = Color.white;
                        GUI.color = Color.white;
                        GUI.Label(new Rect(mg.x - 45f, mg.y - 15f, 90f, 30f), "소스 붓", styleDialSub);
                    }

                    // 하단 양념 진행도 바
                    Rect sauceBar = new Rect(560f, 855f, 800f, 44f);
                    GUI.color = new Color(0.18f, 0.2f, 0.24f);
                    GUI.DrawTexture(sauceBar, Texture2D.whiteTexture);
                    GUI.color = new Color(0.92f, 0.22f, 0.15f);
                    GUI.DrawTexture(new Rect(sauceBar.x, sauceBar.y, sauceBar.width * ((float)saucePaintedCount / sauceTotalCells), sauceBar.height), Texture2D.whiteTexture);
                    styleSub.normal.textColor = Color.white;
                    GUI.color = Color.white;
                    GUI.Label(sauceBar, $"양념소스 도포율 : {saucePercent} / 100 %", styleSub);
                    break;
            }

            GUI.color = Color.white;
            styleSub.normal.textColor = new Color(0.68f, 0.74f, 0.8f);
            GUI.Label(new Rect(660f, 985f, 600f, 32f), "[F] 조리대에서 잠시 나오기", styleSub);

            if (bigJudgeTimer > 0f)
            {
                styleJudge.normal.textColor = bigJudgeColor;
                GUI.Label(new Rect(660f, 580f, 600f, 80f), bigJudgeText, styleJudge);
            }

            GUI.matrix = prevMatrix;
        }

        // ★ 회전 행렬(RotateAroundPivot)을 쓰지 않고 시작점~끝점 사이에 직접 점선 조각을 배치하여 위치 어긋남을 100% 해결한 함수
        private void DrawSegmentedLineBetween(Vector2 start, Vector2 end, int segments, Color col, float size)
        {
            GUI.color = col;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 pt = Vector2.Lerp(start, end, t);
                GUI.DrawTexture(new Rect(pt.x - size * 0.5f, pt.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }
}