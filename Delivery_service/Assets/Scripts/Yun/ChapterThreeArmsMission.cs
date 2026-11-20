using System;
using System.Collections;
using System.Collections.Generic;
using DeliveryService.Mok;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun
{
    // Saved UI and props live in the scene. Only chapter progress survives scene travel.
    public sealed class ChapterThreeArmsMission : MonoBehaviour, IInteractable
    {
        public enum Stage { Meet, Choose, ReadSlip, Compare, Carry, Purchase, Complete }
        public enum ScreenKind { None, Dialogue, Candidates, Slip, Box, Purchase }
        static bool unlocked;
        static Stage progress;
        static int credit;
        static bool granted;
        static bool restartingChapter;
        public Stage State => progress;
        public int MissionCredit => credit;
        public bool Available => !GameModeSession.IsChallenge && (unlocked || enableStandaloneTest);
        public bool ModalOpen => screen != ScreenKind.None;
        public ScreenKind CurrentScreen => screen;

        [Header("NewHarborCity handoff (no interaction here)")]
        public MissionManager introduction;
        public bool handoffOnly;
        [Header("Editor testing only; normal play unlocks after Mingi's conversation")]
        public bool enableStandaloneTest;
        [Header("Existing merchant and saved props")]
        public WeaponMerchant legacyMerchant;
        public GameObject props;
        public ChapterThreeArmsPoint slip;
        public ChapterThreeArmsPoint[] boxes;
        public Transform handoverRest;
        public string slipCode = "DS-318-B";
        [Min(0)] public int pistolPrice = 25000;
        [Header("Scene Canvas")]
        public GameObject ui, dialoguePanel, candidatesPanel, detailPanel, purchasePanel, objectivePanel;
        public Text objective, prompt, subtitle, detailTitle, detailBody, notice, purchaseSummary;
        public Button nextButton, skipButton, closeButton, pistolButton, gatlingButton, smgButton, takeButton, buyButton;
        [Header("Dialogue (F: next / Space: skip this conversation)")]
        [TextArea(2, 5)] public string[] greeting = {
            "무기상: 실장이 보냈지? 지갑 넣어. 요 며칠 사람 하나 모자라서 내가 더 손해야.",
            "플레이어: 나보고 일하라고?", "무기상: 하나 골라. 그거에 딸린 일 하나만 끝내면 딱 그 값이야." };
        [TextArea(2, 5)] public string[] briefing = {
            "무기상: 아까 들어온 상자 둘이 바뀌었어. 카운터에 내 전표 있을 거야.",
            "플레이어: 이런 건 우리 가게서도 맨날 하는데.", "무기상: 그럼 오늘은 틀리지 마." };
        [TextArea(2, 5)] public string[] wrongBox = { "직원: 앞번호는 같아요. 끝에 거 보세요." };
        [TextArea(2, 5)] public string[] accepted = { "무기상: 맞네. 이거 찾느라 나머지 상자까지 다 뜯을 뻔했어." };
        [TextArea(2, 5)] public string[] settlement = {
            "무기상: 일한 값이랑 물건값이랑, 이걸로 끝. 영수증은?",
            "플레이어: 됐어. 가게 장부에 쓸 일도 아니고." };

        ScreenKind screen;
        PlayerCombat player;
        ChapterThreeArmsPoint inspected, carried;
        string[] lines;
        int line;
        Action afterDialogue;
        readonly Dictionary<Behaviour, bool> suspended = new Dictionary<Behaviour, bool>();
        CursorLockMode previousLock;
        bool previousVisible, locked, legacyEnabled;
        float inputAfter;
        int openedFrame;
        Coroutine releasing;
        bool handingOver;
        CanvasGroup oldOrderVisibility;
        float findHudAfter;
        Vector3 boxRestPosition;
        Quaternion boxRestRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            restartingChapter = false;
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (restartingChapter && scene.name == "NewHarborCity")
            {
                unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
                restartingChapter = false;
            }
            if (scene.name != "Lobby" && scene.name != "BankPrologue") return;
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
        }
        public static void PrepareChapterRestart()
        {
            restartingChapter = true;
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
        }
        void ObserveIntroduction()
        {
            if (restartingChapter) return;
            if (introduction && !GameModeSession.IsChallenge &&
                (introduction.State == MissionManager.MissionState.GoToDestination || introduction.State == MissionManager.MissionState.Completed))
                unlocked = true;
        }
        void Awake()
        {
            if (handoffOnly) return;
            legacyEnabled = legacyMerchant && legacyMerchant.enabled;
            ui.SetActive(false); props.SetActive(false);
            nextButton.onClick.AddListener(Next);
            skipButton.onClick.AddListener(Skip);
            closeButton.onClick.AddListener(Close);
            pistolButton.onClick.AddListener(ChoosePistol);
            gatlingButton.onClick.AddListener(PreviewGatling);
            smgButton.onClick.AddListener(PreviewSmg);
            takeButton.onClick.AddListener(TakeInspectedBox);
            buyButton.onClick.AddListener(BuyPistol);
        }
        void Start()
        {
            player = FindFirstObjectByType<PlayerCombat>();
            // The story reward survives the existing single-scene tunnel travel in this session.
            if (granted && player)
            {
                var weapon = player.GetComponent<PlayerWeapon>();
                if (weapon && !weapon.HasWeapon) weapon.Equip(WeaponKind.Pistol);
            }
            if (handoffOnly) { ObserveIntroduction(); return; }
            RefreshAvailability();
            if (Available && progress == Stage.Carry) AttachCorrectBox();
        }
        void RefreshAvailability()
        {
            if (legacyMerchant) legacyMerchant.enabled = Available ? false : legacyEnabled;
            props.SetActive(Available && progress >= Stage.ReadSlip && progress <= Stage.Carry);
            if (objectivePanel) objectivePanel.SetActive(Available && !ModalOpen);
            objective.gameObject.SetActive(Available && !ModalOpen);
            if (!Available) prompt.text = "";
        }
        public void Interact(ToppingInventory inventory)
        {
            if (!Available) { if (legacyMerchant) legacyMerchant.Interact(inventory); return; }
            if (inventory) player = inventory.GetComponent<PlayerCombat>();
            if (!CanInteract() || handingOver) return;
            switch (progress)
            {
                case Stage.Meet: Talk(greeting, () => { progress = Stage.Choose; Show(ScreenKind.Candidates); }); break;
                case Stage.Choose: Show(ScreenKind.Candidates); break;
                case Stage.Carry: StartCoroutine(Handover()); break;
                case Stage.Purchase: ShowPurchase(); break;
                case Stage.Complete: Talk(new[] { "무기상: 거래는 끝났어. 물건 잘 챙겨." }, Close); break;
                default: Talk(new[] { "무기상: 카운터의 전표와 상자 끝번호를 확인해." }, Close); break;
            }
        }
        bool CanInteract() => Available && !ModalOpen && Time.timeScale > 0 && player && player.CanAct;
        public void Inspect(ChapterThreeArmsPoint point, ToppingInventory inventory)
        {
            if (inventory) player = inventory.GetComponent<PlayerCombat>();
            if (!CanInteract() || !point || point.mission != this || progress < Stage.ReadSlip || progress > Stage.Compare) return;
            if (point == slip)
            {
                if (progress == Stage.ReadSlip) progress = Stage.Compare;
                detailTitle.text = "입고 전표";
                detailBody.text = "인계할 상자\n\n" + slipCode + "\n\n상자의 표식과 끝번호까지 대조하세요.";
                Show(ScreenKind.Slip); return;
            }
            if (progress == Stage.ReadSlip)
            {
                Talk(new[] { "플레이어: 먼저 카운터의 전표부터 확인하자." }, Close); return;
            }
            inspected = point;
            detailTitle.text = "상자 표식";
            detailBody.text = point.code + "\n\n전표에 적힌 상자가 맞나요?";
            Show(ScreenKind.Box);
        }
        public void ChoosePistol()
        {
            if (screen != ScreenKind.Candidates || progress != Stage.Choose || Time.timeScale <= 0) return;
            Talk(briefing, () => { progress = Stage.ReadSlip; RefreshAvailability(); Close(); });
        }
        public void PreviewGatling() { if (screen == ScreenKind.Candidates) notice.text = "게틀링건 · 반송된 상자\n이 의뢰는 준비 중입니다. 지금은 권총 의뢰를 진행할 수 있습니다."; }
        public void PreviewSmg() { if (screen == ScreenKind.Candidates) notice.text = "기관단총 · 거래는 취소야\n이 의뢰는 준비 중입니다. 지금은 권총 의뢰를 진행할 수 있습니다."; }
        public void TakeInspectedBox()
        {
            if (screen != ScreenKind.Box || progress != Stage.Compare || !inspected || Time.timeScale <= 0) return;
            if (inspected.code != slipCode) { Talk(wrongBox, Close); return; }
            progress = Stage.Carry;
            AttachCorrectBox();
            Close();
        }
        void AttachCorrectBox()
        {
            if (!player) return;
            foreach (var box in boxes)
                if (box && box.code == slipCode)
                {
                    carried = box; boxRestPosition = box.transform.position; boxRestRotation = box.transform.rotation;
                    foreach (var c in box.GetComponentsInChildren<Collider>()) c.enabled = false;
                    break;
                }
        }
        IEnumerator Handover()
        {
            if (!carried) AttachCorrectBox();
            if (!carried) yield break;
            handingOver = true; Lock();
            var from = carried.transform.position; var rotation = carried.transform.rotation;
            float elapsed = 0;
            while (elapsed < .8f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .8f));
                carried.transform.SetPositionAndRotation(Vector3.Lerp(from, handoverRest.position, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .1f, Quaternion.Slerp(rotation, handoverRest.rotation, t));
                yield return null;
            }
            RestoreCarried();
            credit = Mathf.Max(0, pistolPrice); progress = Stage.Purchase;
            handingOver = false;
            RefreshAvailability(); Talk(accepted, ShowPurchase);
        }
        void ShowPurchase()
        {
            purchaseSummary.text = $"권총 · 바뀐 전표\n\n의뢰 대금  {credit:N0}원\n권총 가격  {pistolPrice:N0}원\n\n의뢰 대금으로 정산 · 추가 부담 0원";
            Show(ScreenKind.Purchase);
        }
        public void BuyPistol()
        {
            if (screen != ScreenKind.Purchase || progress != Stage.Purchase || granted || credit < pistolPrice || Time.timeScale <= 0) return;
            var weapon = player ? player.GetComponent<PlayerWeapon>() : null;
            if (!weapon || !WeaponCatalog.Model(WeaponKind.Pistol)) { purchaseSummary.text = "권총 지급 연결을 확인해야 합니다. 정산 대금은 보존됩니다."; return; }
            // Escrow: the earned fee pays for exactly one weapon, without touching daily sales.
            weapon.Equip(WeaponKind.Pistol);
            credit -= pistolPrice; granted = true; progress = Stage.Complete;
            Talk(settlement, Close);
        }
        void Talk(string[] dialogue, Action completed)
        {
            lines = dialogue; line = 0; afterDialogue = completed;
            Show(ScreenKind.Dialogue);
            if (lines == null || lines.Length == 0) FinishDialogue(); else subtitle.text = lines[0];
        }
        public void Next()
        {
            if (screen != ScreenKind.Dialogue || Time.timeScale <= 0 || Time.unscaledTime < inputAfter) return;
            if (++line >= lines.Length) FinishDialogue(); else { subtitle.text = lines[line]; inputAfter = Time.unscaledTime + .12f; }
        }
        public void Skip()
        {
            if (screen == ScreenKind.Dialogue && Time.timeScale > 0 && Time.unscaledTime >= inputAfter) FinishDialogue();
        }
        void FinishDialogue() { var callback = afterDialogue; afterDialogue = null; callback?.Invoke(); }
        void Show(ScreenKind kind)
        {
            Lock(); screen = kind; openedFrame = Time.frameCount; inputAfter = Time.unscaledTime + .2f;
            ui.SetActive(true);
            dialoguePanel.SetActive(kind == ScreenKind.Dialogue);
            candidatesPanel.SetActive(kind == ScreenKind.Candidates);
            detailPanel.SetActive(kind == ScreenKind.Slip || kind == ScreenKind.Box);
            purchasePanel.SetActive(kind == ScreenKind.Purchase);
            takeButton.gameObject.SetActive(kind == ScreenKind.Box);
            closeButton.gameObject.SetActive(kind != ScreenKind.Dialogue);
            if (kind == ScreenKind.Candidates) notice.text = "의뢰 하나를 마치면 그 대금으로 선택한 무기를 구매합니다.";
        }
        public void Close()
        {
            if (handingOver) return;
            ui.SetActive(false); screen = ScreenKind.None; afterDialogue = null;
            if (releasing != null) StopCoroutine(releasing);
            releasing = StartCoroutine(ReleaseAfterInput());
        }
        IEnumerator ReleaseAfterInput()
        {
            yield return null;
            // A skipped line must not also become a jump, shot or world interaction.
            while (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.fKey.isPressed) || Mouse.current != null && Mouse.current.leftButton.isPressed) yield return null;
            Cursor.lockState = previousLock; Cursor.visible = previousVisible;
            yield return null; // Consume the cursor recenter delta before restoring camera input.
            Unlock(); releasing = null;
        }
        void Lock()
        {
            if (releasing != null) { StopCoroutine(releasing); releasing = null; }
            if (locked || !player) return;
            locked = true; previousLock = Cursor.lockState; previousVisible = Cursor.visible;
            foreach (var b in player.GetComponentsInChildren<MonoBehaviour>(true))
                if (b is PlayerMove || b is PlayerMovement || b is CameraMove || b is PlayerInteraction || b is PlayerInteract || b is PlayerWeapon)
                { suspended[b] = b.enabled; b.enabled = false; }
            var phone = FindFirstObjectByType<PhoneController>();
            if (phone) { suspended[phone] = phone.enabled; phone.enabled = false; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void Unlock()
        {
            if (!locked) return;
            foreach (var pair in suspended) if (pair.Key) pair.Key.enabled = pair.Value;
            suspended.Clear(); locked = false;
            Cursor.lockState = previousLock; Cursor.visible = previousVisible;
        }
        void Update()
        {
            if (handoffOnly) { ObserveIntroduction(); return; }
            if (!player) player = FindFirstObjectByType<PlayerCombat>();
            RefreshAvailability();
            if (!Available) return;
            if (progress == Stage.Carry && !carried && player) AttachCorrectBox();
            objective.text = progress == Stage.Meet || progress == Stage.Choose ? "대비 · 무기상과 이야기하기" :
                progress == Stage.ReadSlip ? "바뀐 전표 · 카운터의 입고 전표 확인" :
                progress == Stage.Compare ? "바뀐 전표 · 전표와 상자 끝번호 대조" :
                progress == Stage.Carry ? "바뀐 전표 · 상자를 무기상에게 인계" :
                progress == Stage.Purchase ? "바뀐 전표 · 의뢰 대금으로 권총 구매" : "바뀐 전표 완료 · 권총 확보";
            prompt.text = "";
            if (player && (player.IsDown || PoliceControl.IsArrestInProgress) && (locked || handingOver))
            { StopAllCoroutines(); handingOver = false; ui.SetActive(false); screen = ScreenKind.None; Unlock(); return; }
            if (Time.timeScale <= 0) return;
            if (ModalOpen)
            {
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (Time.frameCount <= openedFrame || Time.unscaledTime < inputAfter) return;
                if (screen == ScreenKind.Dialogue)
                { if (PrologueKeyboard.Pressed(Key.Space)) Skip(); else if (PrologueKeyboard.Pressed(Key.F)) Next(); }
                else if (PrologueKeyboard.Pressed(Key.Escape)) Close();
            }
            else if (!handingOver && player && player.CanAct && player.View && Physics.Raycast(player.View.transform.position, player.View.transform.forward, out var hit, 3f))
            {
                var point = hit.collider.GetComponentInParent<ChapterThreeArmsPoint>();
                if (point && point.mission == this) prompt.text = point == slip ? "[F] 입고 전표 확인" : "[F] 상자 표식 확인";
                else if (hit.collider.GetComponentInParent<ChapterThreeArmsMission>() == this) prompt.text = "[F] 무기상과 이야기하기";
            }
        }
        void LateUpdate()
        {
            if (!handoffOnly)
            {
                // Suppress only the obsolete "next story" card, preserving the vehicle gauges.
                if (!oldOrderVisibility && Time.unscaledTime >= findHudAfter)
                {
                    findHudAfter = Time.unscaledTime + 1;
                    var hud = FindFirstObjectByType<CompactHUD>();
                    var order = hud ? hud.transform.Find("Order") : null;
                    if (order) oldOrderVisibility = order.gameObject.AddComponent<CanvasGroup>();
                }
                if (oldOrderVisibility) oldOrderVisibility.alpha = Available ? 0 : 1;
            }
            if (carried && !handingOver && player && player.View)
                carried.transform.SetPositionAndRotation(player.View.transform.TransformPoint(new Vector3(0, -.42f, .9f)), Quaternion.Euler(0, player.View.transform.eulerAngles.y, 0));
        }
        void RestoreCarried()
        {
            if (!carried) return;
            carried.transform.SetPositionAndRotation(boxRestPosition, boxRestRotation);
            foreach (var c in carried.GetComponentsInChildren<Collider>()) c.enabled = true;
            carried = null;
        }
        void OnDisable()
        {
            ObserveIntroduction(); StopAllCoroutines(); handingOver = false; releasing = null;
            if (ui) ui.SetActive(false);
            if (objective) objective.gameObject.SetActive(false);
            if (objectivePanel) objectivePanel.SetActive(false);
            if (prompt) prompt.text = "";
            screen = ScreenKind.None; Unlock(); RestoreCarried();
            if (legacyMerchant) legacyMerchant.enabled = legacyEnabled;
            if (oldOrderVisibility) oldOrderVisibility.alpha = 1;
        }
        void OnDestroy() { if (oldOrderVisibility) Destroy(oldOrderVisibility); }
    }
}
