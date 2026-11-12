using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RacerNPCInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private float maxDialogueDistance = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float rotateSpeed = 5f;

    [Header("대화 내용 설정")]
    [SerializeField] private string npcName = "레이서";
    [TextArea(3, 5)]
    [SerializeField] private string dialogueMessage = "어이, 나와 속도 경쟁을 하고 싶은 건가?";

    private GameObject dialoguePanel;
    private Text npcNameText;
    private Text dialogueText;
    private Button yesButton;
    private Button noButton;

    private Transform playerTransform;
    private PlayerMove playerMove;
    private CameraMove cameraMove;
    private DeliveryService.Mok.PlayerCombat playerCombat;
    private NPCControl npcControl;
    private NavMeshAgent agent;
    private Animator anim;

    private bool isDialogueActive = false;
    private Coroutine rotateCoroutine;

    // 정비사 미션 전용 대사 진행 상태 및 중복 입력 방지 쿨타임
    private bool isMissionDialogueActive = false;
    private string[] missionDialogueLines;
    private int missionDialogueIndex = 0;
    private float missionInputCooldownUntil = 0f;

    // 레이스 결과 대화창 종료 직후 재상호작용 방지용 전역 쿨타임
    private static float globalInteractCooldownUntil = 0f;

    public static void BlockInteractionForSeconds(float seconds = 0.35f)
    {
        globalInteractCooldownUntil = Mathf.Max(globalInteractCooldownUntil, Time.unscaledTime + seconds);
    }

    public bool IsDialogueActive => isDialogueActive;

    private bool IsMissionRacerStep =>
        DeliveryService.Mok.MechanicMission.Current != null &&
        DeliveryService.Mok.MechanicMission.Current.IsRacerMissionStep;

    private void Start()
    {
        npcControl = GetComponent<NPCControl>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMove = playerObj.GetComponent<PlayerMove>();
            playerCombat = playerObj.GetComponent<DeliveryService.Mok.PlayerCombat>();
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraMove = mainCam.GetComponent<CameraMove>();
        }

        FindSceneDialogueUI();
    }

    private void OnDisable()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    private void FindSceneDialogueUI()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] transforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.CompareTag("DialoguePanel"))
                {
                    dialoguePanel = t.gameObject;
                    break;
                }
            }
            if (dialoguePanel != null) break;
        }

        if (dialoguePanel != null)
        {
            Text[] texts = dialoguePanel.GetComponentsInChildren<Text>(true);
            foreach (Text t in texts)
            {
                if (t.gameObject.name.Contains("Name"))
                {
                    npcNameText = t;
                }
                else if (t.gameObject.name.Contains("Dialogue"))
                {
                    dialogueText = t;
                }
            }

            Button[] buttons = dialoguePanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                string btnName = b.gameObject.name.ToLower();
                if (btnName.Contains("yes"))
                {
                    yesButton = b;
                }
                else if (btnName.Contains("no"))
                {
                    noButton = b;
                }
            }
        }
    }

    private bool IsInteractKeyPressed()
    {
        bool newInputF = DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F);
        bool legacyInteract = (interactKey != KeyCode.F) && Input.GetKeyDown(interactKey);
        return newInputF || legacyInteract;
    }

    private void Update()
    {
        // 레이스 진행 중이거나 결과 대화창 대기 중일 때는 상호작용 차단
        if (RaceManager.Instance != null && RaceManager.Instance.IsRaceActive)
        {
            return;
        }

        // 방금 결과 대화창을 닫은 직후라면 재대화 방지
        if (Time.unscaledTime < globalInteractCooldownUntil)
        {
            return;
        }

        float distance = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 999f;

        // 정비사 미션의 레이서 대화 진행 중일 때 입력 처리
        if (isMissionDialogueActive)
        {
            bool npcIsKnockedOut = npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown);
            bool playerIsKnockedOut = playerCombat != null && playerCombat.IsDown;

            if (PoliceControl.IsArrestInProgress || npcIsKnockedOut || playerIsKnockedOut || playerTransform == null)
            {
                CancelMissionDialogue();
                return;
            }

            // 첫 대사가 열린 직후 0.25초 동안은 같은 키 입력으로 다음 대사로 넘어가지 않도록 보호
            if (Time.unscaledTime < missionInputCooldownUntil)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelMissionDialogue();
                return;
            }

            if (DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.Space))
            {
                CompleteMissionDialogue();
                return;
            }

            if (IsInteractKeyPressed())
            {
                AdvanceMissionDialogue();
            }
            return;
        }

        // 정비사 미션의 레이서 거래 단계일 때 상호작용 키 감지
        if (IsMissionRacerStep)
        {
            if (isDialogueActive) EndDialogue();

            if (distance <= Mathf.Max(interactDistance, 3.5f) && IsInteractKeyPressed())
            {
                DeliveryService.Mok.MechanicMission.Current?.InteractWithRacer(transform);
            }
            return;
        }

        if (isDialogueActive)
        {
            bool npcIsKnockedOut = npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown);
            bool playerIsKnockedOut = playerCombat != null && playerCombat.IsDown;
            bool tooFar = distance > maxDialogueDistance;

            if (PoliceControl.IsArrestInProgress || npcIsKnockedOut || playerIsKnockedOut || tooFar || playerTransform == null)
            {
                EndDialogue();
                return;
            }

            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape))
            {
                OnClick_No();
            }
            return;
        }

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // 정비사 미션용 레이서 대화창 열기 (항상 0번 인덱스 = 첫 번째 대사부터 시작)
    public void StartMissionDialogue(string[] lines)
    {
        if (isMissionDialogueActive) return;

        if (lines == null || lines.Length == 0)
        {
            DeliveryService.Mok.MechanicMission.Current?.OnRacerDialogueFinished();
            return;
        }

        if (dialoguePanel == null) FindSceneDialogueUI();
        if (dialoguePanel == null || PoliceControl.IsArrestInProgress) return;
        if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) return;

        isDialogueActive = true;
        isMissionDialogueActive = true;
        missionDialogueLines = lines;
        missionDialogueIndex = 0;
        missionInputCooldownUntil = Time.unscaledTime + 0.25f;

        if (npcControl != null)
        {
            npcControl.ExternalControl = true;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetBool("IsWalking", false);
            anim.speed = 1f;
        }

        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(RotateDialogueLook());

        if (yesButton != null) yesButton.gameObject.SetActive(false);
        if (noButton != null) noButton.gameObject.SetActive(false);

        dialoguePanel.SetActive(true);
        if (npcNameText != null) npcNameText.text = npcName;
        UpdateMissionDialogueText();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMove != null) playerMove.enabled = false;
        if (cameraMove != null) cameraMove.enabled = false;
    }

    public void AdvanceMissionDialogue()
    {
        if (!isMissionDialogueActive) return;
        if (Time.unscaledTime < missionInputCooldownUntil) return;

        missionInputCooldownUntil = Time.unscaledTime + 0.25f;
        missionDialogueIndex++;

        if (missionDialogueLines != null && missionDialogueIndex < missionDialogueLines.Length)
        {
            UpdateMissionDialogueText();
        }
        else
        {
            CompleteMissionDialogue();
        }
    }

    private void UpdateMissionDialogueText()
    {
        if (dialogueText != null && missionDialogueLines != null && missionDialogueIndex < missionDialogueLines.Length)
        {
            dialogueText.text = missionDialogueLines[missionDialogueIndex] + "\n\n[F] 계속  ·  [Space] 건너뛰기";
        }
    }

    private void CompleteMissionDialogue()
    {
        BlockInteractionForSeconds(0.35f);
        isMissionDialogueActive = false;
        missionDialogueLines = null;
        EndDialogue();

        DeliveryService.Mok.MechanicMission.Current?.OnRacerDialogueFinished();
    }

    private void CancelMissionDialogue()
    {
        BlockInteractionForSeconds(0.35f);
        isMissionDialogueActive = false;
        missionDialogueLines = null;
        EndDialogue();

        DeliveryService.Mok.MechanicMission.Current?.OnRacerDialogueCancelled();
    }

    public void OnClick_Yes()
    {
        if (!isDialogueActive) return;
        StartRaceFromDialogue();
    }

    public void OnClick_No()
    {
        if (!isDialogueActive) return;
        EndDialogue();
    }

    public void StartRaceFromDialogue()
    {
        EndDialogue();

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.StartRace(transform);
        }
    }

    public void Interact(object inventory = null)
    {
        if (RaceManager.Instance != null && RaceManager.Instance.IsRaceActive) return;
        if (Time.unscaledTime < globalInteractCooldownUntil) return;

        if (isMissionDialogueActive)
        {
            AdvanceMissionDialogue();
            return;
        }

        if (IsMissionRacerStep)
        {
            DeliveryService.Mok.MechanicMission.Current?.InteractWithRacer(transform);
            return;
        }

        if (isDialogueActive)
        {
            OnClick_No();
        }
        else
        {
            if (PoliceControl.IsArrestInProgress) return;
            if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) return;
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (RaceManager.Instance != null && RaceManager.Instance.IsRaceActive) return;
        if (Time.unscaledTime < globalInteractCooldownUntil) return;

        if (IsMissionRacerStep)
        {
            DeliveryService.Mok.MechanicMission.Current?.InteractWithRacer(transform);
            return;
        }

        if (dialoguePanel == null) FindSceneDialogueUI();
        if (dialoguePanel == null || PoliceControl.IsArrestInProgress) return;
        if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) return;

        isDialogueActive = true;
        isMissionDialogueActive = false;

        if (npcControl != null)
        {
            npcControl.ExternalControl = true;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetBool("IsWalking", false);
            anim.speed = 1f;
        }

        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(RotateDialogueLook());

        if (yesButton != null) yesButton.gameObject.SetActive(true);
        if (noButton != null) noButton.gameObject.SetActive(true);

        dialoguePanel.SetActive(true);
        if (npcNameText != null) npcNameText.text = npcName;
        if (dialogueText != null) dialogueText.text = dialogueMessage;

        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnClick_Yes);
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnClick_No);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMove != null) playerMove.enabled = false;
        if (cameraMove != null) cameraMove.enabled = false;
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        isMissionDialogueActive = false;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.gameObject.SetActive(true);
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.gameObject.SetActive(true);
        }

        if (npcControl != null)
        {
            npcControl.ExternalControl = false;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (playerCombat != null && playerCombat.IsDown)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(RestoreControlAfterKnockdown());
            }
            return;
        }

        ApplyPlayerControlRestore();
    }

    private IEnumerator RestoreControlAfterKnockdown()
    {
        while (playerCombat != null && playerCombat.IsDown)
        {
            yield return null;
        }

        if (!isDialogueActive)
        {
            ApplyPlayerControlRestore();
        }
    }

    private void ApplyPlayerControlRestore()
    {
        if (PoliceControl.IsArrestInProgress) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        if (playerMove != null)
        {
            playerMove.enabled = true;
            playerMove.isControllable = true;
        }

        if (cameraMove != null)
        {
            cameraMove.SyncViewFromTransform();
            cameraMove.enabled = true;
        }
    }

    private IEnumerator RotateDialogueLook()
    {
        Transform mainCamTransform = Camera.main != null ? Camera.main.transform : null;

        while (isDialogueActive && playerTransform != null && !PoliceControl.IsArrestInProgress)
        {
            if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) yield break;
            if (playerCombat != null && playerCombat.IsDown) yield break;

            Vector3 npcToPlayerDir = (playerTransform.position - transform.position).normalized;
            npcToPlayerDir.y = 0f;
            if (npcToPlayerDir.sqrMagnitude > 0.001f)
            {
                Quaternion npcTargetRot = Quaternion.LookRotation(npcToPlayerDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, npcTargetRot, Time.deltaTime * rotateSpeed);
            }

            Vector3 playerToNpcDir = (transform.position - playerTransform.position).normalized;
            playerToNpcDir.y = 0f;
            if (playerToNpcDir.sqrMagnitude > 0.001f)
            {
                Quaternion playerTargetRot = Quaternion.LookRotation(playerToNpcDir);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, playerTargetRot, Time.deltaTime * rotateSpeed);
            }

            if (mainCamTransform != null)
            {
                Vector3 npcEyePos = transform.position + Vector3.up * 1.5f;
                Vector3 camToNpcDir = (npcEyePos - mainCamTransform.position).normalized;
                if (camToNpcDir.sqrMagnitude > 0.001f)
                {
                    Quaternion camTargetRot = Quaternion.LookRotation(camToNpcDir);
                    mainCamTransform.rotation = Quaternion.Slerp(mainCamTransform.rotation, camTargetRot, Time.deltaTime * rotateSpeed);
                }
            }

            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDialogueDistance);
    }
}