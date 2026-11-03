using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

public class RacerNPCInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private string npcName = "레이서";
    [TextArea(3, 5)] [SerializeField] private string dialogueMessage = "니가 그렇게 운전을 잘해? 도로로 따라와.";
    private NPCControl npc;
    private PlayerCombat player;
    private NavMeshAgent agent;
    private bool active, oldExternal, oldStopped, oldVisible;
    private CursorLockMode oldLock;
    private int openedFrame;
    private readonly Dictionary<Behaviour, bool> paused = new Dictionary<Behaviour, bool>();
    public bool IsDialogueActive => active;
    void Awake() { npc = GetComponent<NPCControl>(); agent = GetComponent<NavMeshAgent>(); }
    void Start() { player = FindFirstObjectByType<PlayerCombat>(); }
    public void Interact(ToppingInventory inventory)
    {
        if (inventory) player = inventory.GetComponent<PlayerCombat>();
        StartDialogue();
    }
    public void StartDialogue()
    {
        if (active || !player || !player.CanAct || !npc || npc.IsDead || npc.IsKnockedDown || npc.ExternalControl) return;
        if (Vector3.Distance(player.transform.position, transform.position) > interactDistance) return;
        active = true; openedFrame = Time.frameCount;
        oldExternal = npc.ExternalControl; npc.ExternalControl = true;
        var animator = GetComponent<Animator>();
        if (animator) { animator.SetBool("IsWalking", false); animator.speed = 1; }
        if (agent && agent.enabled && agent.isOnNavMesh) { oldStopped = agent.isStopped; agent.isStopped = true; }
        // 대화가 끝나면 진입 전의 입력 상태만 복원한다.
        foreach (var b in player.GetComponentsInChildren<MonoBehaviour>(true))
            if (b is PlayerMove || b is PlayerMovement || b is CameraMove || b is PlayerInteraction || b is PlayerInteract || b is PlayerWeapon)
            { paused[b] = b.enabled; b.enabled = false; }
        var phone = FindFirstObjectByType<PhoneController>();
        if (phone) { paused[phone] = phone.enabled; phone.enabled = false; }
        oldLock = Cursor.lockState; oldVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    void Update()
    {
        if (!active) return;
        if (!npc || npc.IsDead || npc.IsKnockedDown || !player || player.IsDown || PoliceControl.IsArrestInProgress)
        { EndDialogue(); return; }
        if (Time.frameCount > openedFrame && (PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F) || PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.Escape))) EndDialogue();
    }
    public void EndDialogue()
    {
        if (!active) return;
        active = false;
        foreach (var pair in paused) if (pair.Key) pair.Key.enabled = pair.Value;
        paused.Clear();
        if (npc) npc.ExternalControl = oldExternal;
        if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = oldStopped;
        Cursor.lockState = oldLock; Cursor.visible = oldVisible;
    }
    void OnDisable() { EndDialogue(); }
    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = Mathf.Max(18, Screen.height / 35), wordWrap = true, alignment = TextAnchor.MiddleCenter };
        if (active)
        {
            var rect = new Rect(Screen.width * .15f, Screen.height * .72f, Screen.width * .7f, Screen.height * .23f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 20, rect.y + 10, rect.width - 40, rect.height - 20), npcName + "\n" + GameDialogue.Resolve(dialogueMessage) + "\n[F] 대화 종료", style);
        }
        else if (player && player.CanAct && npc && !npc.IsDead && !npc.IsKnockedDown && !npc.ExternalControl &&
            Physics.Raycast(player.View.transform.position, player.View.transform.forward, out var hit, interactDistance) && hit.collider.GetComponentInParent<RacerNPCInteraction>() == this)
            GUI.Label(new Rect(Screen.width / 2 - 160, Screen.height - 110, 320, 40), "[F] 레이서와 대화", style);
    }
}
