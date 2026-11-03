using UnityEngine;
using UnityEngine.UI;

public class RacerNPCInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("대화 내용 설정")]
    [SerializeField] private string npcName = "레이서";
    [TextArea(3, 5)]
    [SerializeField] private string dialogueMessage = "어이, 나와 속도 경쟁을 하고 싶은 건가?";

    // 씬에서 동적으로 찾아 연결할 참조 변수들
    private GameObject dialoguePanel;
    private Text npcNameText;
    private Text dialogueText;

    private Transform playerTransform;
    private PlayerMove playerMove;
    private CameraMove cameraMove;
    private NPCControl npcControl;
    private bool isDialogueActive = false;

    private void Start()
    {
        npcControl = GetComponent<NPCControl>();

        // 1. 플레이어 및 카메라 자동 탐색
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMove = playerObj.GetComponent<PlayerMove>();
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraMove = mainCam.GetComponent<CameraMove>();
        }

        // 2. 씬에 배치된 대화 UI 자동 찾아 연결
        FindSceneDialogueUI();
    }

    private void FindSceneDialogueUI()
    {
        // DialoguePanel 태그로 패널 오브젝트 찾기
        dialoguePanel = GameObject.FindGameObjectWithTag("DialoguePanel");

        if (dialoguePanel != null)
        {
            // 패널 자식 오브젝트 중 텍스트 컴포넌트 자동 바인딩
            Text[] texts = dialoguePanel.GetComponentsInChildren<Text>(true);
            foreach (Text t in texts)
            {
                if (t.gameObject.name.Contains("Name"))
                {
                    npcNameText = t;
                }
                else if (t.gameObject.name.Contains("Dialogue") || t.gameObject.name.Contains("Text") || t.gameObject.name.Contains("Message"))
                {
                    dialogueText = t;
                }
            }

            // 시작할 때는 대화창 끄기
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if ((npcControl != null && npcControl.IsDead) || playerTransform == null)
        {
            if (isDialogueActive) EndDialogue();
            return;
        }

        // UI 참조가 비어있다면 다시 탐색 시도
        if (dialoguePanel == null)
        {
            FindSceneDialogueUI();
            if (dialoguePanel == null) return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (isDialogueActive)
        {
            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape))
            {
                EndDialogue();
            }
            return;
        }

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
        {
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (dialoguePanel == null) FindSceneDialogueUI();
        if (dialoguePanel == null) return;

        isDialogueActive = true;

        dialoguePanel.SetActive(true);
        if (npcNameText != null) npcNameText.text = npcName;
        if (dialogueText != null) dialogueText.text = dialogueMessage;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMove != null) playerMove.enabled = false;
        if (cameraMove != null) cameraMove.enabled = false;
    }

    public void EndDialogue()
    {
        isDialogueActive = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMove != null) playerMove.enabled = true;
        if (cameraMove != null) cameraMove.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}