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

    public bool IsDialogueActive => isDialogueActive;

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

    private void Update()
    {
        float distance = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 999f;

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
        if (dialoguePanel == null) FindSceneDialogueUI();
        if (dialoguePanel == null || PoliceControl.IsArrestInProgress) return;
        if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) return;

        isDialogueActive = true;

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

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        if (yesButton != null) yesButton.onClick.RemoveAllListeners();
        if (noButton != null) noButton.onClick.RemoveAllListeners();

        if (npcControl != null)
        {
            npcControl.ExternalControl = false;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (!PoliceControl.IsArrestInProgress)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerMove != null) playerMove.enabled = true;
            if (cameraMove != null)
            {
                cameraMove.SyncViewFromTransform();
                cameraMove.enabled = true;
            }
        }
    }

    // NPC와 플레이어가 대화하는 동안 상대를 서로 바라보도록 회전
    private IEnumerator RotateDialogueLook()
    {
        Transform mainCamTransform = Camera.main != null ? Camera.main.transform : null;

        while (isDialogueActive && playerTransform != null && !PoliceControl.IsArrestInProgress)
        {
            if (npcControl != null && (npcControl.IsDead || npcControl.IsKnockedDown)) yield break;
            if (playerCombat != null && playerCombat.IsDown) yield break;

            // NPC가 플레이어를 바라봄
            Vector3 npcToPlayerDir = (playerTransform.position - transform.position).normalized;
            npcToPlayerDir.y = 0f;
            if (npcToPlayerDir.sqrMagnitude > 0.001f)
            {
                Quaternion npcTargetRot = Quaternion.LookRotation(npcToPlayerDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, npcTargetRot, Time.deltaTime * rotateSpeed);
            }

            // 플레이어 몸체가 NPC를 바라봄
            Vector3 playerToNpcDir = (transform.position - playerTransform.position).normalized;
            playerToNpcDir.y = 0f;
            if (playerToNpcDir.sqrMagnitude > 0.001f)
            {
                Quaternion playerTargetRot = Quaternion.LookRotation(playerToNpcDir);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, playerTargetRot, Time.deltaTime * rotateSpeed);
            }

            // 메인 카메라인 플레이어 시선이 NPC 눈높이
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