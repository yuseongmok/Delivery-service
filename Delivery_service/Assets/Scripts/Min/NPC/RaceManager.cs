using System.Collections;
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

    [Header("레이스 진행 및 승패 상태")]
    private bool isRaceActive = false;
    private bool playerFinished = false;
    private bool npcFinished = false;
    private bool winnerDetermined = false;
    private bool isPlayerWinner = false;

    // 결과 대기 상태 제어
    private bool isWaitingForResultConfirm = false;
    private Transform savedRacerNpcTransform;
    private GameObject dialoguePanelInstance;

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
        if (isWaitingForResultConfirm)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
            {
                ConfirmResultAndEndRace();
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

        // 경찰 수배 및 추격 해제
        PoliceManager policeManager = FindFirstObjectByType<PoliceManager>();
        if (policeManager != null)
        {
            policeManager.ClearAllPolice();
        }

        // 레이서 NPC가 npcs 하위에 있을 경우, npcs 비활성화 시 같이 꺼지지 않도록 부모 해제
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

        // 레이서 차 스폰
        SpawnRacerCar();

        // 바이크 워프 및 플레이어 강제 승차
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

        // 출발 전 조작 잠금
        SetControlsLocked(true);

        // 카운트다운 진행
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

    // 둘 다 끝까지 왔을 때 대화 상태 준비
    private void CompleteRaceAndShowResult()
    {
        // 오토바이 내리기
        MotorcycleController bike = FindFirstObjectByType<MotorcycleController>();
        if (bike != null && bike.isDriven)
        {
            bike.ExitBike();
        }

        // 레이서 차량 제거
        if (activeCarInstance != null)
        {
            Destroy(activeCarInstance);
            activeCarInstance = null;
        }

        // 레이서 NPC 재활성화 및 위치/회전 고정
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (savedRacerNpcTransform != null && playerObj != null)
        {
            savedRacerNpcTransform.gameObject.SetActive(true);

            // NavMesh 위치 샘플링
            Vector3 targetNpcPos = playerObj.transform.position + playerObj.transform.forward * 2f;
            if (NavMesh.SamplePosition(targetNpcPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                targetNpcPos = hit.position;
            }

            // NavMeshAgent 간섭을 막기 위해 잠시 비활성화 후 이동
            NavMeshAgent agent = savedRacerNpcTransform.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            savedRacerNpcTransform.position = targetNpcPos;

            // 서로 정확히 마주보도록 회전 설정
            Vector3 lookAtNpcDir = (targetNpcPos - playerObj.transform.position).normalized;
            lookAtNpcDir.y = 0f;

            if (lookAtNpcDir.sqrMagnitude > 0.001f)
            {
                playerObj.transform.rotation = Quaternion.LookRotation(lookAtNpcDir);
                savedRacerNpcTransform.rotation = Quaternion.LookRotation(-lookAtNpcDir);
            }

            // 회전이 적용된 후 Agent 재활성화 및 정지
            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
            }

            // 카메라 시선 동기화
            CameraMove camMove = FindFirstObjectByType<CameraMove>();
            if (camMove != null)
            {
                camMove.SyncViewFromTransform();
            }
        }

        // 대화 UI 패널 출력
        string resultMessage = isPlayerWinner ? "이 마을에 이정도 실력자가 있었다고?" : "돈이나 내놔";
        ShowResultDialogue(resultMessage);

        // 플레이어 조작 잠금 유지 및 결과 확인 대기
        SetControlsLocked(true);
        isWaitingForResultConfirm = true;
    }

    private void ShowResultDialogue(string message)
    {
        dialoguePanelInstance = null;
        Text npcNameText = null;
        Text dialogueText = null;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] transforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.CompareTag("DialoguePanel"))
                {
                    dialoguePanelInstance = t.gameObject;
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
                if (t.gameObject.name.Contains("Name")) npcNameText = t;
                else if (t.gameObject.name.Contains("Dialogue")) dialogueText = t;
            }

            // 대화창 내 버튼 비활성화
            Button[] buttons = dialoguePanelInstance.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                b.gameObject.SetActive(false);
            }

            dialoguePanelInstance.SetActive(true);
            if (npcNameText != null) npcNameText.text = "레이서";
            if (dialogueText != null) dialogueText.text = message;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }
    }

    private void ConfirmResultAndEndRace()
    {
        isWaitingForResultConfirm = false;

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

        // 대화 UI 닫기
        if (dialoguePanelInstance != null)
        {
            dialoguePanelInstance.SetActive(false);

            // 대화창 내 버튼 원래대로 복구
            Button[] buttons = dialoguePanelInstance.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                b.gameObject.SetActive(true);
            }
        }

        // 레이스 정리 및 통제 해제
        EndRace();
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