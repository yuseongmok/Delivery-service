using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("레이스 시 비활성화/활성화 대상")]
    public GameObject npcs;
    public GameObject cars;
    public GameObject objects;
    public GameObject startSign;

    [Header("스타트 위치 설정")]
    [SerializeField] private Transform bikePos;
    [SerializeField] private Transform racerPos;

    [Header("레이서 차량 프리팹")]
    [SerializeField] private GameObject racerCarObject;
    private GameObject activeCarInstance;

    [Header("신호등 UI / 오브젝트 (레드, 주황, 그린)")]
    [SerializeField] private GameObject redLight;
    [SerializeField] private GameObject orangeLight;
    [SerializeField] private GameObject greenLight;

    [Header("체크포인트 설정")]
    [SerializeField] private GameObject[] checkpoints;
    [SerializeField] private GameObject[] racerCheckpoints;
    private int currentCheckpointIndex = 0;

    [Header("일반 레이스 완주 후 대사 (미션 중에는 MechanicMission 대사 우선 사용)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] defaultWinDialogue = { "이 마을에 이정도 실력자가 있었다고?" };
    [TextArea(2, 4)]
    [SerializeField] private string[] defaultLoseDialogue = { "역시 나는 못따라잡네" };

    [Header("레이스 진행 및 승패 상태")]
    private bool isRaceActive = false;
    private bool playerFinished = false;
    private bool npcFinished = false;
    private bool winnerDetermined = false;
    private bool isPlayerWinner = false;

    // 결과 대기 상태 제어
    private bool isWaitingForResultConfirm = false;
    private string[] currentResultLines;
    private int currentResultLineIndex = 0;
    private float resultInputCooldownUntil = 0f;

    private Transform savedRacerNpcTransform;
    private GameObject dialoguePanelInstance;
    private Text resultDialogueText;
    private readonly List<GameObject> hiddenResultButtons = new List<GameObject>();

    public bool IsRaceActive => isRaceActive;

    private Coroutine countdownCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 양쪽 완주 후 대화창이 열려있을 때 F키 또는 ESC키 입력 처리
        if (isWaitingForResultConfirm)
        {
            if (Time.unscaledTime < resultInputCooldownUntil) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ConfirmResultAndEndRace();
                return;
            }

            if (DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F))
            {
                currentResultLineIndex++;
                if (currentResultLines != null && currentResultLineIndex < currentResultLines.Length)
                {
                    resultInputCooldownUntil = Time.unscaledTime + 0.25f;
                    if (resultDialogueText != null)
                    {
                        resultDialogueText.text = currentResultLines[currentResultLineIndex] + "\n\n[F] 계속";
                    }
                }
                else
                {
                    ConfirmResultAndEndRace();
                }
            }
        }
    }

    public void StartRace(Transform racerNpcTransform = null)
    {
        if (isRaceActive) return;

        isRaceActive = true;

        playerFinished = false;
        npcFinished = false;
        winnerDetermined = false;
        isPlayerWinner = false;
        isWaitingForResultConfirm = false;

        savedRacerNpcTransform = racerNpcTransform;

        // 1. 경찰 수배 및 추격 해제
        PoliceManager policeManager = FindFirstObjectByType<PoliceManager>();
        if (policeManager != null)
        {
            policeManager.ClearAllPolice();
        }

        // 2. 레이서 NPC가 npcs 하위에 있을 경우 부모 해제 후 비활성화
        if (savedRacerNpcTransform != null)
        {
            if (npcs != null && savedRacerNpcTransform.IsChildOf(npcs.transform))
            {
                savedRacerNpcTransform.SetParent(null);
            }
            savedRacerNpcTransform.gameObject.SetActive(false);
        }

        if (npcs != null) npcs.SetActive(false);
        if (cars != null) cars.SetActive(false);
        if (objects != null) objects.SetActive(true);
        if (startSign != null) startSign.SetActive(true);

        // 3. 레이서 차 스폰
        SpawnRacerCar();

        // 4. 바이크 워프 및 플레이어 강제 승차
        MotorcycleController bike = FindFirstObjectByType<MotorcycleController>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (bike != null && bikePos != null)
        {
            if (!bike.isDriven && playerObj != null)
            {
                bike.EnterBike(playerObj);
            }

            bike.RespawnBike(bikePos);
        }

        // 5. 출발 전 조작 잠금
        SetControlsLocked(true);

        // 6. 카운트다운 진행
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(RaceCountdownSequence());
    }

    private void SpawnRacerCar()
    {
        if (racerCarObject == null || racerPos == null) return;

        if (activeCarInstance != null)
        {
            Destroy(activeCarInstance);
        }

        activeCarInstance = Instantiate(racerCarObject, racerPos.position, racerPos.rotation);
        activeCarInstance.SetActive(true);

        RacerCarAI racerAI = activeCarInstance.GetComponent<RacerCarAI>();
        if (racerAI != null)
        {
            GameObject[] targetPoints = (racerCheckpoints != null && racerCheckpoints.Length > 0) ? racerCheckpoints : checkpoints;
            racerAI.SetCheckpoints(targetPoints);
        }
    }

    private IEnumerator RaceCountdownSequence()
    {
        if (redLight != null) redLight.SetActive(false);
        if (orangeLight != null) orangeLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(false);

        yield return new WaitForSeconds(3f);

        if (redLight != null) redLight.SetActive(true);
        yield return new WaitForSeconds(2f);

        if (redLight != null) redLight.SetActive(false);
        if (orangeLight != null) orangeLight.SetActive(true);
        yield return new WaitForSeconds(2f);

        if (orangeLight != null) orangeLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(true);

        SetControlsLocked(false);

        if (activeCarInstance != null)
        {
            RacerCarAI racerAI = activeCarInstance.GetComponent<RacerCarAI>();
            if (racerAI != null)
            {
                racerAI.StartMoving();
            }
        }

        InitializeCheckpoints();

        yield return new WaitForSeconds(1.5f);
        if (greenLight != null) greenLight.SetActive(false);
    }

    private void SetControlsLocked(bool isLocked)
    {
        MotorcycleController bike = FindFirstObjectByType<MotorcycleController>();
        if (bike != null)
        {
            bike.SetControllable(!isLocked);
        }

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove != null)
        {
            playerMove.enabled = !isLocked;
        }
    }

    private void InitializeCheckpoints()
    {
        currentCheckpointIndex = 0;
        if (checkpoints == null || checkpoints.Length == 0) return;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].SetActive(i == 0);
            }
        }
    }

    public void OnCheckpointReached(GameObject checkpointObj)
    {
        if (!isRaceActive || checkpoints == null || currentCheckpointIndex >= checkpoints.Length) return;

        if (checkpoints[currentCheckpointIndex] == checkpointObj)
        {
            checkpoints[currentCheckpointIndex].SetActive(false);

            if (currentCheckpointIndex == checkpoints.Length - 1)
            {
                playerFinished = true;

                if (!winnerDetermined)
                {
                    isPlayerWinner = true;
                    winnerDetermined = true;
                }

                CheckBothFinished();
            }
            else
            {
                currentCheckpointIndex++;
                if (currentCheckpointIndex < checkpoints.Length && checkpoints[currentCheckpointIndex] != null)
                {
                    checkpoints[currentCheckpointIndex].SetActive(true);
                }
            }
        }
    }

    public void OnNpcFinished()
    {
        if (!isRaceActive || npcFinished) return;

        npcFinished = true;

        if (!winnerDetermined)
        {
            isPlayerWinner = false;
            winnerDetermined = true;
        }

        CheckBothFinished();
    }

    private void CheckBothFinished()
    {
        if (playerFinished && npcFinished)
        {
            CompleteRaceAndShowResult();
        }
    }

    // 둘 다 끝까지 왔을 때 바이크 하차 후 서로 마주보며 결과 대화 출력
    private void CompleteRaceAndShowResult()
    {
        // 1. 오토바이 내리기
        MotorcycleController bike = FindFirstObjectByType<MotorcycleController>();
        if (bike != null && bike.isDriven)
        {
            bike.ExitBike();
        }

        // 2. 레이서 차량 제거
        if (activeCarInstance != null)
        {
            Destroy(activeCarInstance);
            activeCarInstance = null;
        }

        // 3. 레이서 NPC 재활성화 및 위치/회전 고정
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (savedRacerNpcTransform != null && playerObj != null)
        {
            savedRacerNpcTransform.gameObject.SetActive(true);

            Vector3 targetNpcPos = playerObj.transform.position + playerObj.transform.forward * 2f;
            if (NavMesh.SamplePosition(targetNpcPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                targetNpcPos = hit.position;
            }

            NavMeshAgent agent = savedRacerNpcTransform.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            savedRacerNpcTransform.position = targetNpcPos;

            Vector3 lookAtNpcDir = (targetNpcPos - playerObj.transform.position).normalized;
            lookAtNpcDir.y = 0f;

            if (lookAtNpcDir.sqrMagnitude > 0.001f)
            {
                playerObj.transform.rotation = Quaternion.LookRotation(lookAtNpcDir);
                savedRacerNpcTransform.rotation = Quaternion.LookRotation(-lookAtNpcDir);
            }

            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
            }

            CameraMove camMove = FindFirstObjectByType<CameraMove>();
            if (camMove != null)
            {
                camMove.SyncViewFromTransform();
            }
        }

        // 4. 승리/패배 대사 가져오기 (정비사 미션 진행 중이면 미션 전용 승/패 대사 사용)
        string[] linesToShow = null;
        var mission = DeliveryService.Mok.MechanicMission.Current;
        if (mission != null && mission.Running && mission.State == DeliveryService.Mok.MechanicMission.Phase.Racing)
        {
            linesToShow = mission.GetRacerResultDialogue(isPlayerWinner);
        }

        if (linesToShow == null || linesToShow.Length == 0)
        {
            linesToShow = isPlayerWinner ? defaultWinDialogue : defaultLoseDialogue;
        }

        currentResultLines = linesToShow;
        currentResultLineIndex = 0;
        resultInputCooldownUntil = Time.unscaledTime + 0.25f;

        ShowResultDialogue(currentResultLines[0]);

        // 5. 플레이어 조작 잠금 유지 및 결과 확인 대기
        SetControlsLocked(true);
        isWaitingForResultConfirm = true;
    }

    private void ShowResultDialogue(string message)
    {
        dialoguePanelInstance = null;
        Text npcNameText = null;
        resultDialogueText = null;
        hiddenResultButtons.Clear();

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas targetCanvas = null;

        foreach (Canvas canvas in canvases)
        {
            Transform[] transforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.CompareTag("DialoguePanel"))
                {
                    dialoguePanelInstance = t.gameObject;
                    targetCanvas = canvas;
                    break;
                }
            }
            if (dialoguePanelInstance != null) break;
        }

        if (dialoguePanelInstance != null)
        {
            Text[] texts = dialoguePanelInstance.GetComponentsInChildren<Text>(true);
            foreach (Text t in texts)
            {
                // 버튼 내부에 속한 Text는 본문/이름 텍스트 후보에서 제외
                if (t.GetComponentInParent<Button>() != null) continue;
                string parentName = t.transform.parent != null ? t.transform.parent.name.ToLower() : "";
                if (parentName.Contains("yes") || parentName.Contains("no") || parentName.Contains("btn") || parentName.Contains("button"))
                    continue;

                string objName = t.gameObject.name;
                if (objName.Contains("Name"))
                {
                    npcNameText = t;
                }
                else if (objName.Contains("Dialogue") || objName.Contains("Message"))
                {
                    resultDialogueText = t;
                }
                else if (resultDialogueText == null && objName.Contains("Text"))
                {
                    resultDialogueText = t;
                }
            }

            // DialoguePanel 및 동일 Canvas 내 선택 버튼 숨김 처리
            Transform searchRoot = targetCanvas != null ? targetCanvas.transform : dialoguePanelInstance.transform;
            Button[] buttons = searchRoot.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                string btnName = b.gameObject.name.ToLower();
                if (b.transform.IsChildOf(dialoguePanelInstance.transform) || btnName.Contains("yes") || btnName.Contains("no"))
                {
                    if (b.gameObject.activeSelf)
                    {
                        b.gameObject.SetActive(false);
                        hiddenResultButtons.Add(b.gameObject);
                    }
                }
            }

            // Button 컴포넌트 없이 구성된 Yes/No 버튼 오브젝트도 함께 숨김
            Transform[] allTransforms = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t == dialoguePanelInstance.transform) continue;
                string tName = t.gameObject.name.ToLower();
                if ((tName.Contains("yes") || tName.Contains("no")) &&
                    (tName.Contains("btn") || tName.Contains("button") || t.GetComponent<Text>() == null))
                {
                    if (t.gameObject.activeSelf && !hiddenResultButtons.Contains(t.gameObject))
                    {
                        t.gameObject.SetActive(false);
                        hiddenResultButtons.Add(t.gameObject);
                    }
                }
            }

            dialoguePanelInstance.SetActive(true);
            if (npcNameText != null) npcNameText.text = "레이서";
            if (resultDialogueText != null) resultDialogueText.text = message + "\n\n[F] 계속";

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // F키 / ESC키 입력 시 돈 정산 후 대화창을 닫고 레이스 완전히 종료
    private void ConfirmResultAndEndRace()
    {
        isWaitingForResultConfirm = false;

        // 대화창을 닫는 F키 입력이 레이서 재상호작용으로 이어지지 않도록 쿨타임 부여
        RacerNPCInteraction.BlockInteractionForSeconds(0.4f);
        DeliveryService.Mok.MechanicMission.Current?.BlockInteractionForSeconds(0.4f);

        // 재화 정산
        if (isPlayerWinner)
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(10000);
            }
        }
        else
        {
            if (MoneyManager.Instance != null)
            {
                if (!MoneyManager.Instance.SpendMoney(10000, ExpenseType.Loss))
                {
                    MoneyManager.Instance.AccidentMoney(10000);
                }
            }
        }

        // 대화 UI 닫기 및 숨겼던 버튼 복구
        if (dialoguePanelInstance != null)
        {
            dialoguePanelInstance.SetActive(false);
        }

        foreach (GameObject btnObj in hiddenResultButtons)
        {
            if (btnObj != null)
            {
                btnObj.SetActive(true);
            }
        }
        hiddenResultButtons.Clear();

        // 레이스 정리 및 통제 해제
        EndRace();

        // 정비사 미션에 승패 결과 전달
        DeliveryService.Mok.MechanicMission.Current?.OnRaceFinished(isPlayerWinner);
    }

    public void EndRace()
    {
        if (!isRaceActive) return;

        isRaceActive = false;
        isWaitingForResultConfirm = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (redLight != null) redLight.SetActive(false);
        if (orangeLight != null) orangeLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(false);

        if (activeCarInstance != null)
        {
            Destroy(activeCarInstance);
            activeCarInstance = null;
        }

        if (checkpoints != null)
        {
            foreach (var cp in checkpoints)
            {
                if (cp != null) cp.SetActive(false);
            }
        }

        SetControlsLocked(false);

        if (npcs != null) npcs.SetActive(true);
        if (cars != null) cars.SetActive(true);
        if (objects != null) objects.SetActive(false);
        if (startSign != null) startSign.SetActive(false);

        CameraMove camMove = FindFirstObjectByType<CameraMove>();
        if (camMove != null)
        {
            camMove.SyncViewFromTransform();
        }
    }
}