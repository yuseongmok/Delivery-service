using System;
using System.Collections;
using System.Collections.Generic;
using DeliveryService.Mok;
using DeliveryService.Mok.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun
{
    // Saved UI and props live in the scene. Only chapter progress survives scene travel.
    public sealed class ChapterThreeArmsMission : MonoBehaviour, IInteractable
    {
        public enum Stage { Meet, Choose, ReadSlip, Compare, Carry, Purchase, Complete, GatlingTravel, GatlingDelivery, GatlingArrived }
        public enum ScreenKind { None, Dialogue, Candidates, Slip, Box, Purchase }
        static bool unlocked;
        static Stage progress;
        static int credit;
        static bool granted;
        static bool restartingChapter;
        static int searchedMask, fragmentMask, carriedIndex = -1;
        static bool assembled;
        public int FragmentCount => ((fragmentMask & 1) != 0 ? 1 : 0) + ((fragmentMask & 2) != 0 ? 1 : 0) + ((fragmentMask & 4) != 0 ? 1 : 0);
        public bool InvoiceAssembled => assembled;
        readonly List<int> assemblyOrder = new List<int>();
        public string[] fragmentTexts = { "318-", "B", "DS-" };
        public GameObject shade, inspectionPanel, previewStage;
        public Transform previewPivot;
        public TextMesh previewCode;
        [Header("Gatling request: destination only")]
        public NavigationPlace gatlingParking, gatlingDestination;
        public Text destinationMarker;
        [Min(1)] public float parkingArrivalRadius = 7f, deliveryArrivalRadius = 3f;
        [TextArea(2, 5)] public string[] gatlingBriefing = {
            "무기상: 반송 하나야. 서안골목 주소 찍어 놨어.",
            "플레이어: 가져다주는 게 아니라 가져오는 거지?",
            "무기상: 응. 이번엔 빈손으로 가.",
            "무기상: 골목 안에는 오토바이 못 들어가. 입구에 세우고 걸어 들어가." };
        Stage routedStage = (Stage)(-1);
        NavigationManager missionNavigation;
        public Text inspectionHint, assemblyStatus;
        public Button[] fragmentButtons;
        public Button clearAssemblyButton;
        public Camera previewCamera;
        [Min(10)] public float inspectionRotateSpeed = 100;
        ChapterThreeArmsPoint shaking;
        Vector3 shakeRest;
        Quaternion shakeRotation;
        Material previewFontInstance;
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
            ClearPuzzle();
            restartingChapter = false;
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (restartingChapter && scene.name == "NewHarborCity")
            {
                ClearPuzzle();
                unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
                restartingChapter = false;
            }
            if (scene.name != "Lobby" && scene.name != "BankPrologue") return;
            ClearPuzzle();
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
        }
        public static void PrepareChapterRestart()
        {
            ClearPuzzle();
            restartingChapter = true;
            unlocked = false; progress = Stage.Meet; credit = 0; granted = false;
        }
        static void ClearPuzzle() { searchedMask = fragmentMask = 0; carriedIndex = -1; assembled = false; }
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
            ui.SetActive(false); if (dialoguePanel) dialoguePanel.SetActive(false); props.SetActive(false);
            if (nextButton) nextButton.onClick.AddListener(Next);
            if (skipButton) skipButton.onClick.AddListener(Skip);
            closeButton.onClick.AddListener(Close);
            pistolButton.onClick.AddListener(ChoosePistol);
            gatlingButton.onClick.AddListener(ChooseGatling);
            smgButton.onClick.AddListener(PreviewSmg);
            takeButton.onClick.AddListener(TakeInspectedBox);
            buyButton.onClick.AddListener(BuyPistol);
            if (fragmentButtons != null) for (int i = 0; i < fragmentButtons.Length; i++) { int piece = i; fragmentButtons[i].onClick.AddListener(() => PlaceFragment(piece)); }
            if (clearAssemblyButton) clearAssemblyButton.onClick.AddListener(ClearAssembly);
            if (previewStage) previewStage.SetActive(false);
            if (previewCode) { previewFontInstance = previewCode.GetComponent<Renderer>().material; Font.textureRebuilt += RefreshPreviewFont; RefreshPreviewFont(previewCode.font); }
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
                case Stage.GatlingTravel:
                case Stage.GatlingDelivery: Talk(new[] { "무기상: 서안골목에 잘못 간 상자야. 찍어 준 주소로 가 봐." }, Close); break;
                case Stage.GatlingArrived: Talk(new[] { "무기상: 주소는 확인했지?" }, Close); break;
                default: Talk(new[] { assembled ? "무기상: 전표와 상자 표식을 끝까지 대조해 봐." : "무기상: 옆에 있는 상자들을 흔들어 봐. 찢어진 전표 조각부터 찾아야 해." }, Close); break;
            }
        }
        bool CanInteract() => Available && !ModalOpen && !shaking && !handingOver && Time.timeScale > 0 && player && player.CanAct;
        public void Inspect(ChapterThreeArmsPoint point, ToppingInventory inventory)
        {
            if (inventory) player = inventory.GetComponent<PlayerCombat>();
            if (!CanInteract() || !point || point.mission != this || progress < Stage.ReadSlip || progress > Stage.Compare) return;
            if (point == slip)
            {
                if (!assembled && FragmentCount < 3) { Talk(new[] { $"플레이어: 전표가 찢어져 있어. 상자에서 조각을 더 찾아야겠어. ({FragmentCount}/3)" }, Close); return; }
                ShowAssembly(); return;
            }
            if (!assembled)
            {
                StartCoroutine(SearchBox(point)); return;
            }
            inspected = point;
            previewCode.text = point.code;
            previewCode.transform.localPosition = point.markPosition;
            previewCode.transform.localRotation = Quaternion.Euler(point.markEuler);
            previewPivot.localRotation = Quaternion.Euler(18, 25, 0);
            inspectionHint.text = "전표: " + slipCode + "\n방향키 / WASD로 상자 회전 · 상자 화면 좌클릭으로 선택 · ESC 닫기";
            Show(ScreenKind.Box);
        }
        IEnumerator SearchBox(ChapterThreeArmsPoint point)
        {
            int index = Array.IndexOf(boxes, point);
            if (index < 0) yield break;
            if ((searchedMask & (1 << index)) != 0) { Talk(new[] { "플레이어: 이 상자는 이미 확인했어." }, Close); yield break; }
            shaking = point; shakeRest = point.transform.position; shakeRotation = point.transform.rotation; Lock();
            float t = 0;
            while (t < .9f)
            {
                t += Time.deltaTime;
                point.transform.position = shakeRest + Vector3.up * Mathf.Sin(Mathf.Clamp01(t / .9f) * Mathf.PI) * .28f + point.transform.right * Mathf.Sin(t * 38) * .035f;
                point.transform.rotation = shakeRotation * Quaternion.Euler(0, 0, Mathf.Sin(t * 38) * 7);
                yield return null;
            }
            RestoreShake(); searchedMask |= 1 << index;
            bool found = point.fragment >= 0 && point.fragment < 3 && (fragmentMask & (1 << point.fragment)) == 0;
            if (found) fragmentMask |= 1 << point.fragment;
            Talk(new[] { found ? $"플레이어: 전표 조각이다. ({FragmentCount}/3)" : "플레이어: 여기에는 없어." }, () => { if (FragmentCount == 3) ShowAssembly(); else Close(); });
        }
        void RestoreShake() { if (shaking) shaking.transform.SetPositionAndRotation(shakeRest, shakeRotation); shaking = null; }
        void ShowAssembly()
        {
            assemblyOrder.Clear(); detailTitle.text = assembled ? "복원한 입고 전표" : "찢어진 전표 맞추기";
            detailBody.text = assembled ? slipCode : "조각의 글자와 연결 순서를 보고 왼쪽부터 이어 붙이세요.";
            Show(ScreenKind.Slip); RefreshAssembly();
        }
        void RefreshAssembly()
        {
            string result = ""; foreach (int i in assemblyOrder) result += fragmentTexts[i];
            assemblyStatus.text = assembled ? "복원 완료 · 상자를 돌려 표식을 비교하세요." : "이어 붙인 전표: " + (result.Length == 0 ? "_  _  _" : result);
            for (int i = 0; i < fragmentButtons.Length; i++) { fragmentButtons[i].gameObject.SetActive(!assembled); fragmentButtons[i].interactable = !assemblyOrder.Contains(i); }
            clearAssemblyButton.gameObject.SetActive(!assembled);
        }
        public void ClearAssembly() { if (screen != ScreenKind.Slip || assembled || Time.timeScale <= 0) return; assemblyOrder.Clear(); RefreshAssembly(); }
        public void PlaceFragment(int index)
        {
            if (screen != ScreenKind.Slip || assembled || FragmentCount != 3 || index < 0 || index >= 3 || assemblyOrder.Contains(index) || Time.timeScale <= 0) return;
            assemblyOrder.Add(index); RefreshAssembly();
            if (assemblyOrder.Count != 3) return;
            string combined = ""; foreach (int i in assemblyOrder) combined += fragmentTexts[i];
            if (combined != slipCode) { assemblyStatus.text = "이어지는 표식이 맞지 않습니다. 다시 놓기를 눌러 재조립하세요."; return; }
            assembled = true; progress = Stage.Compare; detailBody.text = slipCode; detailTitle.text = "복원한 입고 전표"; RefreshAssembly();
        }
        public void RotateInspection(Vector2 direction)
        {
            if (screen != ScreenKind.Box || Time.timeScale <= 0) return;
            previewPivot.Rotate(Vector3.up, -direction.x * inspectionRotateSpeed * Time.deltaTime, Space.World);
            previewPivot.Rotate(Vector3.right, direction.y * inspectionRotateSpeed * Time.deltaTime, Space.World);
        }
        void RefreshPreviewFont(Font changed)
        {
            if (previewCode && previewFontInstance && changed == previewCode.font)
                previewFontInstance.mainTexture = changed.material.mainTexture;
        }
        public void ChoosePistol()
        {
            if (screen != ScreenKind.Candidates || progress != Stage.Choose || Time.timeScale <= 0) return;
            Talk(briefing, () => { progress = Stage.ReadSlip; RefreshAvailability(); Close(); });
        }
        public void ChooseGatling()
        {
            if (screen != ScreenKind.Candidates || progress != Stage.Choose || Time.timeScale <= 0) return;
            if (!gatlingParking || !gatlingDestination) { notice.text = "배송지 연결을 확인해 주세요."; return; }
            Talk(gatlingBriefing, () => { progress = Stage.GatlingTravel; Close(); RefreshAvailability(); });
        }
        void UpdateGatlingTravel()
        {
            bool travelling = Available && (progress == Stage.GatlingTravel || progress == Stage.GatlingDelivery);
            if (destinationMarker) destinationMarker.gameObject.SetActive(travelling && !ModalOpen);
            if (!travelling)
            {
                routedStage = (Stage)(-1);
                if (missionNavigation && (missionNavigation.SelectedPlace == gatlingParking || missionNavigation.SelectedPlace == gatlingDestination)) missionNavigation.StopGuidance();
            }
            if (!travelling || !player || !gatlingParking || !gatlingDestination) return;
            var target = progress == Stage.GatlingTravel ? gatlingParking : gatlingDestination;
            if (!missionNavigation) missionNavigation = FindFirstObjectByType<NavigationManager>();
            if (routedStage != progress && missionNavigation) { missionNavigation.SelectPlace(target); routedStage = progress; }
            var observer = player.RiddenBike ? player.RiddenBike.transform.position : player.transform.position;
            if (destinationMarker && player.View) StoryTargetDiamond.Place(destinationMarker, player.View, target.Destination.position + Vector3.up, observer, target.displayName);
            if (Time.timeScale <= 0 || ModalOpen || locked || player.IsDown || PoliceControl.IsArrestInProgress) return;
            var delta = observer - target.Destination.position;
            float height = Mathf.Abs(delta.y); delta.y = 0;
            if (height > 3 || delta.magnitude > (progress == Stage.GatlingTravel ? parkingArrivalRadius : deliveryArrivalRadius)) return;
            if (progress == Stage.GatlingTravel) progress = Stage.GatlingDelivery;
            else if (!player.RiddenBike)
            {
                progress = Stage.GatlingArrived;
                if (missionNavigation && missionNavigation.SelectedPlace == gatlingDestination) missionNavigation.StopGuidance();
                if (destinationMarker) destinationMarker.gameObject.SetActive(false);
            }
        }
        public void PreviewSmg() { if (screen == ScreenKind.Candidates) notice.text = "기관단총 · 거래는 취소야\n이 의뢰는 준비 중입니다. 지금은 권총 의뢰를 진행할 수 있습니다."; }
        public void TakeInspectedBox()
        {
            if (screen != ScreenKind.Box || progress != Stage.Compare || !inspected || Time.timeScale <= 0) return;
            if (!assembled || Time.unscaledTime < inputAfter) return;
            carriedIndex = Array.IndexOf(boxes, inspected);
            progress = Stage.Carry;
            AttachCorrectBox();
            Close();
        }
        void AttachCorrectBox()
        {
            if (!player) return;
            for (int i = 0; i < boxes.Length; i++)
                if (i == carriedIndex && boxes[i])
                {
                    var box = boxes[i];
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
            bool correct = carried.code == slipCode;
            RestoreCarried(); carriedIndex = -1;
            if (!correct)
            {
                progress = Stage.Compare; handingOver = false;
                Talk(wrongBox, Close); yield break;
            }
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
            bool merchantConversation = dialogue != null && Array.Exists(dialogue, text => text != null && (text.StartsWith("무기상:") || text.StartsWith("직원:")));
            foreach (var barName in new[] { "Top letterbox", "Bottom letterbox" })
            {
                var bar = dialoguePanel.transform.Find(barName);
                if (bar) bar.gameObject.SetActive(merchantConversation);
            }
            if (lines == null || lines.Length == 0) FinishDialogue(); else subtitle.text = lines[0];
        }
        public void Next()
        {
            if (screen != ScreenKind.Dialogue || Time.timeScale <= 0 || Time.unscaledTime < inputAfter) return;
            if (++line >= lines.Length) FinishDialogue(); else { subtitle.text = lines[line]; inputAfter = Time.unscaledTime + .2f; }
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
            if (shade) shade.SetActive(kind != ScreenKind.Dialogue);
            dialoguePanel.SetActive(kind == ScreenKind.Dialogue);
            candidatesPanel.SetActive(kind == ScreenKind.Candidates);
            detailPanel.SetActive(kind == ScreenKind.Slip);
            if (inspectionPanel) inspectionPanel.SetActive(kind == ScreenKind.Box);
            if (previewStage) previewStage.SetActive(kind == ScreenKind.Box);
            purchasePanel.SetActive(kind == ScreenKind.Purchase);
            takeButton.gameObject.SetActive(kind == ScreenKind.Box);
            closeButton.gameObject.SetActive(kind != ScreenKind.Dialogue);
            if (kind == ScreenKind.Candidates) notice.text = "의뢰 하나를 마치면 그 대금으로 선택한 무기를 구매합니다.";
        }
        public void Close()
        {
            if (handingOver || shaking) return;
            if (previewStage) previewStage.SetActive(false);
            ui.SetActive(false); if (dialoguePanel) dialoguePanel.SetActive(false); screen = ScreenKind.None; afterDialogue = null;
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
            UpdateGatlingTravel();
            if (!Available) return;
            if (progress == Stage.Carry && !carried && player) AttachCorrectBox();
            objective.text = progress == Stage.Meet || progress == Stage.Choose ? "대비 · 무기상과 이야기하기" :
                progress == Stage.ReadSlip ? (FragmentCount == 3 ? "바뀐 전표 · 카운터에서 전표 조각 이어 붙이기" : $"바뀐 전표 · 상자에서 전표 조각 찾기 ({FragmentCount}/3)") :
                progress == Stage.Compare ? "바뀐 전표 · 전표와 상자 끝번호 대조" :
                progress == Stage.Carry ? "바뀐 전표 · 상자를 무기상에게 인계" :
                progress == Stage.Purchase ? "바뀐 전표 · 의뢰 대금으로 권총 구매" :
                progress == Stage.GatlingTravel ? "반송된 상자 · 서안골목 입구로 이동" :
                progress == Stage.GatlingDelivery ? "반송된 상자 · 내려서 지정 배송지로 이동" :
                progress == Stage.GatlingArrived ? "반송된 상자 · 배송지 도착\n상자 회수 단계는 준비 중입니다." : "바뀐 전표 완료 · 권총 확보";
            prompt.text = "";
            if (player && (player.IsDown || PoliceControl.IsArrestInProgress) && (locked || handingOver))
            { StopAllCoroutines(); RestoreShake(); handingOver = false; if (previewStage) previewStage.SetActive(false); ui.SetActive(false); if (dialoguePanel) dialoguePanel.SetActive(false); screen = ScreenKind.None; Unlock(); return; }
            if (Time.timeScale <= 0) return;
            if (ModalOpen)
            {
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (Time.frameCount <= openedFrame || Time.unscaledTime < inputAfter) return;
                if (screen == ScreenKind.Dialogue)
                { if (PrologueKeyboard.Pressed(Key.Space)) Skip(); else if (PrologueKeyboard.Pressed(Key.F)) Next(); }
                else if (PrologueKeyboard.Pressed(Key.Escape)) Close();
                else if (screen == ScreenKind.Box)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null) RotateInspection(new Vector2((keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1 : 0), (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed ? 1 : 0)));
                }
            }
            else if (!handingOver && player && player.CanAct && player.View && Physics.Raycast(player.View.transform.position, player.View.transform.forward, out var hit, 3f))
            {
                var point = hit.collider.GetComponentInParent<ChapterThreeArmsPoint>();
                if (point && point.mission == this) prompt.text = point == slip ? "[F] 전표 작업대" : assembled ? "[F] 상자 돌려 보기" : "[F] 상자를 들어 흔들기";
                else if (hit.collider.GetComponentInParent<ChapterThreeArmsMission>() == this) prompt.text = "[F] 무기상과 이야기하기";
            }
        }
        void LateUpdate()
        {
            if (screen == ScreenKind.Box && previewCode) RefreshPreviewFont(previewCode.font);
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
            RestoreShake(); if (previewStage) previewStage.SetActive(false);
            if (ui) ui.SetActive(false); if (dialoguePanel) dialoguePanel.SetActive(false);
            if (objective) objective.gameObject.SetActive(false);
            if (objectivePanel) objectivePanel.SetActive(false);
            if (destinationMarker) destinationMarker.gameObject.SetActive(false);
            if (missionNavigation && (missionNavigation.SelectedPlace == gatlingParking || missionNavigation.SelectedPlace == gatlingDestination)) missionNavigation.StopGuidance();
            if (prompt) prompt.text = "";
            screen = ScreenKind.None; Unlock(); RestoreCarried();
            if (legacyMerchant) legacyMerchant.enabled = legacyEnabled;
            if (oldOrderVisibility) oldOrderVisibility.alpha = 1;
        }
        void OnDestroy() { Font.textureRebuilt -= RefreshPreviewFont; if (previewFontInstance) Destroy(previewFontInstance); if (oldOrderVisibility) Destroy(oldOrderVisibility); }
    }
}
