using System;
using System.Collections;
using DeliveryService.Mok;
using DeliveryService.Mok.Navigation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 야마자키 챕터 진행
// 1) 시작: Player·canvas 비활성화, 시네마틱 활성화 → 암전 1→0 페이드인
// 2) 오프닝 컷신: 민기 대사 → 플레이어(DeliveryPlayer (2))가 이전 씬에서 고른 무기를 보관 → 민기 주방으로
// 3) 조작 넘김 → 배달 2건 (JJinCity 방식: 카운터의 포장 피자 → 오토바이 배달통 → 배달지에서 꺼내 전달)
// 4) 민기 문자 (JJinCity StoryPhoneInbox) → 마지막 피자를 카운터에서 들면 "다녀올게"
// 5) 골목의 가짜 손님에게 건네는 순간 납치 (1인칭 컷신)
//
// 표현은 다른 스토리 미션과 같은 공용 UI를 쓴다.
// - 목표: StoryObjectiveCard (미션 제목 카드, "캡션 · 목표" 형식, 둘째 줄 [F] 행동은 가까워지면 마커로 이동)
// - 마커: StoryTargetDiamond + StoryInteractionCue
// - 문자: 오른쪽 "새 문자 · 보낸 사람 / 제목" 알림 + 알림음 → 내리면 휴대폰 연락함(StoryPhoneApp) 문자 화면이 자동으로 열림
// - 자막: StorySpeakerColors (화자별 색)
// - 짧은 안내: DeliveryRequestNotice
public class ChpaterYamajaki : MonoBehaviour
{
    public enum Stage { Intro, Deliver, WaitMessage, ReadMessage, ReturnToShop, GoToFinal, Abducted, ArrivedFinal }

    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)] public string text;
        [Tooltip("비워 두면 카메라를 옮기지 않는다.")]
        public Transform cameraPoint;
    }

    [Serializable]
    public class DeliveryStop
    {
        public string label = "배달지";
        [Tooltip("NavigationPlace가 붙은 위치. 진행 전에는 꺼 둔다.")]
        public Transform point;
        [Tooltip("[F]로 피자를 건네는 대상 (문, 손님). 비우면 point 아래에서 찾는다.")]
        public StoryDeliveryTarget target;
        [TextArea(1, 3)] public string handoffLine;
    }

    [Header("진행 상태 (확인용)")]
    [SerializeField] Stage stage = Stage.Intro;
    public Stage CurrentStage => stage;

    [Header("미션 카드 제목")]
    public string missionTitle = "퇴근 전 배달";
    [Tooltip("휴대폰 미션 앱의 '이 미션을 하는 이유'")]
    [TextArea(2, 4)] public string missionReason = "야마자키파의 눈을 피해 새 가게에서 피자 배달원으로 지내고 있다. 오늘 들어온 주문을 마저 배달하고 민기와 퇴근하자.";

    [Header("시작 시 끄는 오브젝트")]
    public GameObject player;
    public GameObject gameplayCanvas;

    [Header("시네마틱")]
    public GameObject cinematicRoot;
    public GameObject cinemaCamera;
    [Tooltip("시네마틱에서만 보이는 대역 등. 조작 시작 시 꺼진다.")]
    public GameObject[] cinematicOnlyObjects;
    public Image blackout;
    public Image[] letterbox;
    public Text dialogueText;
    public Text skipHint;
    [Min(0)] public float fadeInSeconds = 1f;
    [Tooltip("컷신이 끝날 때 화면이 검게 덮이는 시간. 인물 이동이 끝나기 전에 시작된다.")]
    [Min(0)] public float handoffFadeSeconds = .6f;
    [Tooltip("조작을 넘긴 뒤 화면이 밝아지는 시간")]
    [Min(0)] public float handoffRevealSeconds = .6f;
    public DialogueLine[] introLines =
    {
        new DialogueLine { text = "민기: 거기다 둬. 셔터 옆에 빈칸 있어." },
    };
    [Tooltip("대사 한 줄이 자동으로 넘어가는 시간 (F로 바로 넘김, Space로 건너뛰기)")]
    [Min(.5f)] public float lineSeconds = 3f;

    [Header("오프닝 · 무기 보관")]
    [Tooltip("컷신용 플레이어 (DeliveryPlayer (2))")]
    public Animator cutscenePlayer;
    public AftermathActor mingiActor;
    public Transform storageCamera;
    public Transform kitchenCamera;
    [Tooltip("플레이어가 보관 자리 앞까지 걷는 경로. 마지막 점의 방향이 보관 자리를 바라보는 방향")]
    public Transform[] storageWalkPath;
    [Tooltip("무기를 내려놓는 자리")]
    public Transform weaponSlot;
    [Tooltip("민기가 주방으로 들어가는 경로. 마지막 점의 방향으로 선다.")]
    public Transform[] mingiKitchenPath;
    [Tooltip("이전 씬의 무기 기록이 없을 때 (이 씬만 단독 테스트할 때) 보여 줄 무기")]
    public WeaponKind fallbackWeapon = WeaponKind.Pistol;
    [Tooltip("컷신 플레이어 Finger.R_end 아래의 무기 쥐는 자리. 위치·회전을 Scene에서 조정할 수 있다.")]
    public Transform weaponGrip;
    [Min(.1f)] public float cutsceneWalkSpeed = 1.5f;
    [Min(.1f)] public float placeSeconds = 1.2f;
    [Tooltip("민기가 주방으로 걸어가는 컷의 길이. 끝나기 handoffFadeSeconds 전부터 암전이 시작된다.")]
    [Min(0)] public float kitchenShotSeconds = 3.5f;
    [Tooltip("조작을 넘겨받을 때 실제 Player가 설 위치와 방향. 비우면 컷신 플레이어 자리")]
    public Transform gameplayStart;

    [Header("배달 (순서대로 진행 · JJinCity 배달 방식)")]
    public DeliveryStop[] deliveries;
    [Tooltip("민기가 만든 포장 피자가 나오는 카운터 픽업대")]
    public PizzaPickUpZone pizzaPickup;
    [Tooltip("오토바이 배달통")]
    public BikePizzaStorage bikeStorage;

    [Header("민기 문자 (JJinCity StoryPhoneInbox)")]
    public StoryPhoneInbox inbox;
    [Min(0)] public float messageDelay = 2.5f;
    public string messageSender = "민기";
    public string messageTitle = "마지막 주문";
    [TextArea(2, 4)] public string messageBody = "주문 하나 더 왔어. 이것만 배달하고 퇴근하자. 아, 그리고 너가 먹을 피자 두 조각 따로 빼놨어.";

    [Header("가게 카운터 · 마지막 주문")]
    [Tooltip("가게 카운터 위치 (NavigationPlace). 진행 중에는 쓰지 않고 꺼 둔다.")]
    public DeliveryStop counterPickup;
    [Tooltip("납치 컷신에서 손에 들고 건네는 상자 모델")]
    public GameObject counterBox;
    [Tooltip("주방 접시에 남겨 둔 피자 두 조각")]
    public GameObject dinnerSlices;
    public Vector3 heldBoxLocalPosition = new Vector3(0f, -.36f, .62f);
    public Vector3 heldBoxLocalEuler = new Vector3(8f, 0f, 0f);
    [TextArea(1, 2)] public string departLine = "플레이어: 다녀올게.";

    [Header("마지막 배달지 (골목)")]
    [Tooltip("point는 가짜 손님이 서 있는 자리")]
    public DeliveryStop finalDelivery;
    [Min(.5f)] public float handoverRadius = 3f;

    [Header("골목 · 납치 (1인칭 컷신)")]
    [Tooltip("야마자키파가 보낸 가짜 손님")]
    public Animator fakeCustomer;
    [Tooltip("뒤에서 내려치는 야마자키파 조직원")]
    public Animator attacker;
    [Tooltip("피자를 건넬 때 플레이어가 서는 자리. 방향은 손님을 바라보는 방향")]
    public Transform handoverStandPoint;
    [Tooltip("조직원이 처음 숨어 있는 자리 (플레이어 뒤)")]
    public Transform attackerHidePoint;
    [Tooltip("쓰러진 뒤 조직원이 걸어와 서는 자리")]
    public Transform attackerEndPoint;
    [Tooltip("쓰러진 카메라가 놓이는 자리와 방향 (바닥 높이)")]
    public Transform fallenViewPoint;
    [Tooltip("눈 감기 연출용 위/아래 눈꺼풀 이미지 (시네마틱 캔버스)")]
    public RectTransform eyelidTop, eyelidBottom;
    [Tooltip("건네기 전 짧은 대사 (비워 두면 대사 없이 진행)")]
    public DialogueLine[] handoverLines;
    [Min(.1f)] public float approachSeconds = 1.2f;
    [Min(.1f)] public float handoverSeconds = 1.1f;
    [Min(.1f)] public float fallSeconds = 1.1f;
    [Tooltip("눈꺼풀 닫힘 정도(0 열림 ~ 1 감김) 순서. 천천히 몇 번 깜빡이다 감긴다.")]
    public float[] eyelidKeys = { 0f, .55f, .3f, .8f, .62f, 1f };
    [Tooltip("각 눈꺼풀 구간 시간(초). eyelidKeys보다 하나 적게")]
    public float[] eyelidDurations = { 1.1f, .7f, 1.1f, .6f, 1.4f };
    [Min(0)] public float holdBlackSeconds = 1.5f;
    [Tooltip("완전히 암전된 뒤 호출 (다음 장면 연결용)")]
    public UnityEvent onFinalArrived;

    [Header("UI")]
    [Tooltip("StoryObjectiveCard가 카드로 보여 줄 목표 원문")]
    public Text objectiveText;
    [Tooltip("StoryTargetDiamond 마커 텍스트")]
    public Text marker;
    public float markerHeight = 2f;

    enum WeaponPose { None, InHand, OnShelf }

    Transform markerTarget;
    string markerLabel;
    PhoneController phone;
    NavigationManager navigation;
    AudioSource sfx;
    AudioClip orderBell, thud, ringing, bodyHit;
    bool inSubtitle;

    Transform cutsceneWeapon;
    WeaponKind shownWeapon = WeaponKind.None;
    WeaponPose weaponPose;
    float placeBlend; // 0 = 손, 1 = 보관 자리
    float reachWeight;

    void Awake()
    {
        if (player == null)
        {
            var combat = FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
            if (combat != null) player = combat.gameObject;
        }
        navigation = FindFirstObjectByType<NavigationManager>(FindObjectsInactive.Include);
        if (player != null) phone = player.GetComponentInChildren<PhoneController>(true);
        if (!bikeStorage) bikeStorage = FindFirstObjectByType<BikePizzaStorage>(FindObjectsInactive.Include);
        if (!inbox) inbox = FindFirstObjectByType<StoryPhoneInbox>(FindObjectsInactive.Include);
        if (inbox && player && !inbox.player) inbox.player = player.GetComponent<PlayerCombat>();

        SetActive(player, false);
        SetActive(gameplayCanvas, false);
        SetActive(cinematicRoot, true);
        SetActive(cinemaCamera, true);
        if (cinematicOnlyObjects != null) foreach (var o in cinematicOnlyObjects) SetActive(o, true);
        SetCinematicFrameVisible(true);
        if (dialogueText)
        {
            dialogueText.text = "";
            if (!dialogueText.GetComponent<StorySpeakerColors>()) dialogueText.gameObject.AddComponent<StorySpeakerColors>();
        }
        SetBlackout(1f);

        if (deliveries != null) foreach (var d in deliveries) SetStopActive(d, false);
        SetStopActive(counterPickup, false);
        SetStopActive(finalDelivery, false);
        SetActive(counterBox, false);
        SetActive(dinnerSlices, false);
        if (marker) marker.gameObject.SetActive(false);
        SetObjective("");

        // 다른 미션과 같은 목표 카드·근접 행동 안내를 붙인다.
        StoryObjectiveCard.Bind(objectiveText, missionTitle);
        StoryInteractionCue.Bind(marker, objectiveText);

        // 휴대폰 미션 앱에는 밤 미션 목록 대신 이 챕터의 현재 목표를 보여 준다.
        StoryChapterBrief.Title = missionTitle;
        StoryChapterBrief.Reason = missionReason;
        StoryChapterBrief.Goal = CurrentGoal;
        StoryChapterBrief.Missions = PhoneMissions;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        sfx.volume = .18f;
        orderBell = MakeTone("Yamajaki order bell", new[] { 988f, 784f }, .32f, .3f);
        thud = MakeImpact("Yamajaki blunt hit", 58f, .45f, .9f);
        bodyHit = MakeImpact("Yamajaki body on ground", 85f, .25f, .55f);
        ringing = MakeRinging("Yamajaki ear ringing", 3100f, 3.2f);

        if (fakeCustomer) { fakeCustomer.applyRootMotion = false; fakeCustomer.gameObject.SetActive(false); }
        if (attacker) { attacker.applyRootMotion = false; attacker.gameObject.SetActive(false); }
        SetEyelids(0f);

        if (cutscenePlayer) cutscenePlayer.applyRootMotion = false;
        if (mingiActor && mingiActor.animator) mingiActor.animator.applyRootMotion = false;
        SpawnCutsceneWeapon();
    }

    void OnDestroy()
    {
        foreach (var clip in new[] { orderBell, thud, ringing, bodyHit }) if (clip) Destroy(clip);
        if (StoryChapterBrief.Goal == (Func<string>)CurrentGoal) StoryChapterBrief.Clear();
    }

    // 목표 카드의 "캡션 · 목표"에서 목표 부분만
    string CurrentGoal()
    {
        if (!objectiveText || string.IsNullOrWhiteSpace(objectiveText.text)) return "";
        StoryObjectiveCard.Split(objectiveText.text.Split('\n')[0], missionTitle, out _, out string goal);
        return goal;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return Opening();
        yield return HandOffToPlayer();
        // 컷신이 끝나면 첫 미션부터 차례로 진행한다. 이후 미션은 앞 미션을 마치면 자동으로 이어진다.
        StartMission(0);
    }

    // ───────── 챕터 미션 (휴대폰 미션 앱 · 경찰에 잡히면 실패 → 미션 앱에서 Enter로 다시 시작) ─────────
    [Serializable]
    public class ChapterMission
    {
        public string title;
        [TextArea(2, 4)] public string reason;
        [TextArea(1, 3)] public string result;
    }

    [Header("챕터 미션 (휴대폰 미션 앱)")]
    [Tooltip("배달 미션 순서와 같게: 배달 1, 배달 2, 마지막 주문")]
    public ChapterMission[] chapterMissions =
    {
        new ChapterMission { title = "첫 번째 배달", reason = "새 가게에서 피자 배달원으로 지내며 눈에 띄지 않게 생활하고 있다. 민기가 만든 피자를 첫 손님에게 배달하자.", result = "다음 주문이 들어온다." },
        new ChapterMission { title = "두 번째 배달", reason = "주문이 이어지고 있다. 가게를 오래 비우지 않도록 두 번째 주문도 빠르게 배달하자.", result = "오늘 들어온 배달을 마무리한다." },
        new ChapterMission { title = "마지막 주문", reason = "민기가 마지막 주문 하나만 더 배달하고 퇴근하자고 했다. 주방에는 내 몫의 피자 두 조각이 남아 있다.", result = "가게로 돌아가 민기와 퇴근한다." },
    };
    [Tooltip("미션 실패 후 민기가 보내는 문자 제목 (JJinCity와 같은 문구)")]
    public string failureTextTitle = "괜찮아? 다시 준비하자";

    enum MissionState { Locked, Running, Failed, Completed }
    MissionState[] missionStates;
    int currentMission = -1;
    Coroutine missionRoutine;
    bool messageSent;

    MissionState StateOf(int i)
    {
        if (missionStates == null || missionStates.Length != MissionCount) missionStates = new MissionState[MissionCount];
        return missionStates[i];
    }

    int MissionCount => (deliveries != null ? deliveries.Length : 0) + 1;

    string MissionTitle(int i) => chapterMissions != null && i < chapterMissions.Length ? chapterMissions[i].title : $"미션 {i + 1}";

    public bool StartMission(int i)
    {
        if (i < 0 || i >= MissionCount || PoliceControl.IsArrestInProgress) return false;
        StateOf(i);
        if (missionRoutine != null && currentMission >= 0 && missionStates[currentMission] == MissionState.Running) return false;
        if (missionStates[i] == MissionState.Completed) return false;
        currentMission = i;
        missionStates[i] = MissionState.Running;
        missionRoutine = StartCoroutine(MissionRoutine(i));
        return true;
    }

    IEnumerator MissionRoutine(int i)
    {
        if (i < deliveries.Length)
            yield return Deliver(deliveries[i], i);
        else
        {
            if (!messageSent) yield return MingiMessage();
            yield return GoToFinal();
        }
        missionStates[i] = MissionState.Completed;
        missionRoutine = null;
        if (i + 1 < MissionCount) StartMission(i + 1);
    }

    void Update()
    {
        // 배달 중 경찰에 붙잡히면 미션 실패. (납치 컷신이 시작된 뒤에는 실패하지 않는다.)
        if (currentMission < 0 || StateOf(currentMission) != MissionState.Running) return;
        if (stage == Stage.Abducted || stage == Stage.ArrivedFinal) return;
        if (PoliceControl.IsArrestInProgress) FailMission("경찰에 붙잡혔습니다.");
    }

    void FailMission(string reason)
    {
        if (missionRoutine != null) StopCoroutine(missionRoutine);
        missionRoutine = null;
        missionStates[currentMission] = MissionState.Failed;

        // 들고 있던 피자와 배달통은 비운다. 다시 시작하면 민기가 카운터에 새 피자를 내놓는다.
        var inventory = player ? player.GetComponent<ToppingInventory>() : null;
        if (inventory && inventory.HasPackagedPizzas()) inventory.ClearPackagedPizzas();
        if (bikeStorage) bikeStorage.TryStealPizzas();
        if (deliveries != null) foreach (var d in deliveries) { if (d != null && d.target) d.target.Accepting = false; SetStopActive(d, false); }
        if (finalDelivery != null && finalDelivery.target) finalDelivery.target.Accepting = false;
        SetStopActive(finalDelivery, false);
        ClearGuide();
        SetObjective($"미션 실패 · {reason}\n휴대폰 미션 앱에서 다시 시작할 수 있습니다.");
        StartCoroutine(SendFailureText(reason));
    }

    // JJinCity 밤 미션과 같은 실패 문자.
    IEnumerator SendFailureText(string reason)
    {
        while (PoliceControl.IsArrestInProgress) yield return null;
        yield return WaitScaled(1f);
        if (!inbox) inbox = FindFirstObjectByType<StoryPhoneInbox>();
        if (inbox) inbox.Send(failureTextTitle, reason + "\n몸을 추스르고 미션 앱에서 다시 시작해.");
    }

    // 휴대폰 미션 앱 목록
    System.Collections.Generic.IList<StoryChapterBrief.Entry> PhoneMissions()
    {
        var list = new System.Collections.Generic.List<StoryChapterBrief.Entry>();
        for (int i = 0; i < MissionCount; i++)
        {
            var state = StateOf(i);
            if (state == MissionState.Locked) continue;
            int index = i;
            var info = chapterMissions != null && i < chapterMissions.Length ? chapterMissions[i] : null;
            string destination = i < deliveries.Length ? deliveries[i].label : finalDelivery.label;
            bool canStart = state == MissionState.Failed && !PoliceControl.IsArrestInProgress && missionRoutine == null;
            list.Add(new StoryChapterBrief.Entry
            {
                Title = MissionTitle(i),
                Status = state == MissionState.Running ? "진행 중" : state == MissionState.Failed ? "실패 · 다시 시작 가능" : "완료",
                Goal = state == MissionState.Running && !string.IsNullOrWhiteSpace(CurrentGoal()) ? CurrentGoal()
                     : $"가게 카운터에서 민기가 만든 피자를 챙겨 {destination}(으)로 배달하기",
                Reason = info != null ? info.reason : missionReason,
                Result = info != null ? info.result : "",
                Action = state == MissionState.Running ? "진행 중" : state == MissionState.Failed ? (canStart ? "다시 시작" : "지금은 시작할 수 없습니다") : "완료한 미션",
                Done = state == MissionState.Completed,
                CanStart = canStart,
                Start = () => StartMission(index),
            });
        }
        return list;
    }

    // ───────── 1. 오프닝 컷신 ─────────
    IEnumerator Opening()
    {
        stage = Stage.Intro;
        bool skipped = false;

        for (float t = 0; t < fadeInSeconds; t += Time.deltaTime)
        {
            if (SkipPressed()) { skipped = true; break; }
            SetBlackout(1f - t / fadeInSeconds);
            yield return null;
        }
        if (!skipped) SetBlackout(0f);

        if (!skipped && introLines != null)
        {
            for (int i = 0; i < introLines.Length && !skipped; i++)
            {
                ShowLine(introLines[i]);
                yield return WaitLine(); // 3초 뒤 자동으로 다음 대사
                skipped = lastLineSkipped;
            }
        }
        if (dialogueText) dialogueText.text = "";

        if (!skipped)
        {
            var result = new bool[1];
            yield return StoreWeapon(result);
            skipped = result[0];
        }
        if (!skipped)
        {
            var result = new bool[1];
            yield return MingiToKitchen(result);
        }
        // 무기·민기 위치 정리는 화면이 완전히 검게 덮인 뒤 HandOffToPlayer에서 한다.
    }

    // 플레이어가 보관 자리 앞으로 걸어가 무기를 내려놓는다.
    IEnumerator StoreWeapon(bool[] skipped)
    {
        if (!cutscenePlayer || storageWalkPath == null || storageWalkPath.Length == 0) yield break;
        MoveCamera(storageCamera);

        var body = cutscenePlayer.transform;
        for (int i = 0; i < storageWalkPath.Length; i++)
        {
            var target = storageWalkPath[i];
            if (!target) continue;
            while (true)
            {
                if (SkipPressed()) { skipped[0] = true; SetPlayerSpeed(0); yield break; }
                if (NextPressed()) { SetPlayerSpeed(0); FinishStoreWeapon(); yield break; }
                if (Time.timeScale <= 0f) { yield return null; continue; }
                if (!StepToward(body, target.position, cutsceneWalkSpeed)) break;
                SetPlayerSpeed(cutsceneWalkSpeed);
                yield return null;
            }
        }
        SetPlayerSpeed(0);

        var last = storageWalkPath[storageWalkPath.Length - 1];
        for (float t = 0; t < .35f; t += Time.deltaTime)
        {
            if (SkipPressed()) { skipped[0] = true; yield break; }
            if (last) body.rotation = Quaternion.Slerp(body.rotation, Flat(last.rotation), t / .35f);
            yield return null;
        }

        // 팔을 뻗어 내려놓고 다시 팔을 내린다.
        for (float t = 0; t < placeSeconds; t += Time.deltaTime)
        {
            if (SkipPressed()) { skipped[0] = true; yield break; }
            if (NextPressed()) { FinishStoreWeapon(); yield break; }
            float k = t / placeSeconds;
            reachWeight = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k));
            placeBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .6f, k));
            if (k >= .55f && weaponPose == WeaponPose.InHand) weaponPose = WeaponPose.OnShelf;
            yield return null;
        }
        reachWeight = 0f;
        yield return WaitOrSkip(.4f, skipped);
    }

    void FinishStoreWeapon()
    {
        reachWeight = 0f;
        placeBlend = 1f;
        weaponPose = cutsceneWeapon ? WeaponPose.OnShelf : WeaponPose.None;
        SetPlayerSpeed(0);
        if (cutscenePlayer && storageWalkPath != null && storageWalkPath.Length > 0)
        {
            var last = storageWalkPath[storageWalkPath.Length - 1];
            if (last) cutscenePlayer.transform.SetPositionAndRotation(Flat(last.position, cutscenePlayer.transform.position.y), Flat(last.rotation));
        }
    }

    // 주문 벨이 울리고 민기가 주방으로 돌아간다.
    // 컷이 끝나기 전에 암전을 시작하고, 민기는 화면이 다 덮일 때까지 계속 걷는다.
    IEnumerator MingiToKitchen(bool[] skipped)
    {
        if (!mingiActor || mingiKitchenPath == null || mingiKitchenPath.Length == 0) yield break;
        MoveCamera(kitchenCamera);
        if (sfx && orderBell) sfx.PlayOneShot(orderBell);
        yield return WaitOrSkip(.6f, skipped);
        if (skipped[0]) yield break;

        DisableMingiAgent();
        var body = mingiActor.transform;
        float fadeAt = Time.time + Mathf.Max(0f, kitchenShotSeconds - handoffFadeSeconds);
        bool fading = false;
        int point = 0;
        while (true)
        {
            if (SkipPressed()) { skipped[0] = true; break; }
            if (!fading && (NextPressed() || Time.time >= fadeAt))
            {
                fading = true;
                StartCoroutine(Fade(blackout ? blackout.color.a : 0f, 1f, handoffFadeSeconds));
            }
            if (fading && (!blackout || blackout.color.a >= .999f)) break;

            bool walking = false;
            if (Time.timeScale > 0f)
            {
                while (point < mingiKitchenPath.Length && (!mingiKitchenPath[point] || !StepToward(body, mingiKitchenPath[point].position, 1.4f))) point++;
                walking = point < mingiKitchenPath.Length;
            }
            mingiActor.walkSpeed = walking ? 1.4f : 0f;
            yield return null;
        }
        mingiActor.walkSpeed = 0f;
    }

    void FinishMingiToKitchen()
    {
        if (!mingiActor || mingiKitchenPath == null || mingiKitchenPath.Length == 0) return;
        DisableMingiAgent();
        mingiActor.walkSpeed = 0;
        mingiActor.lookWeight = 0;
        var last = mingiKitchenPath[mingiKitchenPath.Length - 1];
        if (last) mingiActor.transform.SetPositionAndRotation(Flat(last.position, mingiActor.transform.position.y), Flat(last.rotation));
    }

    void DisableMingiAgent()
    {
        var agent = mingiActor ? mingiActor.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
        if (agent && agent.enabled) agent.enabled = false;
    }

    void ShowLine(DialogueLine line)
    {
        if (dialogueText) dialogueText.text = line.text;
        if (line.cameraPoint) MoveCamera(line.cameraPoint);
    }

    void MoveCamera(Transform point)
    {
        if (cinemaCamera && point) cinemaCamera.transform.SetPositionAndRotation(point.position, point.rotation);
    }

    IEnumerator HandOffToPlayer()
    {
        // 아직 덮이지 않았으면 (건너뛰기 등) 먼저 검게 덮는다. 오브젝트는 완전히 덮인 뒤에만 옮긴다.
        if (blackout && blackout.color.a < .999f)
            yield return Fade(blackout.color.a, 1f, handoffFadeSeconds * Mathf.Max(.2f, 1f - blackout.color.a));

        FinishStoreWeapon();
        FinishMingiToKitchen();

        // 자막·레터박스·안내 문구는 함께 숨긴다.
        if (dialogueText) dialogueText.text = "";
        SetCinematicFrameVisible(false);

        // 컷신이 끝난 자리에서 실제 플레이어가 이어받는다.
        if (player && gameplayStart) PlacePlayer(gameplayStart.position, gameplayStart.rotation);
        else if (player && cutscenePlayer) PlacePlayer(cutscenePlayer.transform.position, cutscenePlayer.transform.rotation);
        if (cinematicOnlyObjects != null) foreach (var o in cinematicOnlyObjects) SetActive(o, false);
        SetActive(cinemaCamera, false);
        SetActive(player, true);
        SetActive(gameplayCanvas, true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (player) player.GetComponentInChildren<CameraMove>(true)?.SyncViewFromTransform();
        if (navigation != null && player != null) navigation.SetNavigationTarget(player.transform);

        yield return null;
        yield return Fade(1f, 0f, handoffRevealSeconds);
        SetActive(cinematicRoot, false);
    }

    void PlacePlayer(Vector3 position, Quaternion rotation)
    {
        var cc = player.GetComponent<CharacterController>();
        bool ccOn = cc && cc.enabled;
        if (ccOn) cc.enabled = false;
        position.y = Mathf.Max(position.y, player.transform.position.y);
        player.transform.SetPositionAndRotation(position, Flat(rotation));
        if (ccOn) cc.enabled = true;
    }

    // ───────── 2. 배달 (JJinCity 배달 방식) ─────────
    // 민기가 만든 피자가 카운터 픽업대에 나온다 → [F]로 들기 → 오토바이 배달통에 [F]로 싣기 → 배달지로 이동
    // → 배달통에서 [F]로 꺼내기 → 손님(문)에게 [F]로 전달. 한 건이 끝나면 다음 피자가 다시 카운터에 나온다.
    IEnumerator Deliver(DeliveryStop stop, int index)
    {
        if (stop == null || stop.point == null) yield break;
        stage = Stage.Deliver;
        int total = deliveries.Length;
        yield return DeliveryLoop(stop, $"배달 {index + 1}/{total}", null);
        DeliveryRequestNotice.Show(string.IsNullOrEmpty(stop.handoffLine)
            ? $"배달 완료 ({index + 1}/{total})"
            : $"배달 완료 ({index + 1}/{total})\n{stop.handoffLine}");
    }

    // 미션 카드에는 해야 할 일(배달지로 배달하기)만 보여 주고, 마커·내비도 배달지 하나만 가리킨다.
    // 피자 들기·싣기·꺼내기·건네기는 JJinCity처럼 각 물체의 [F] 상호작용 안내로만 진행한다.
    IEnumerator DeliveryLoop(DeliveryStop stop, string caption, Action onPickedUp)
    {
        var target = stop.target ? stop.target : stop.point.GetComponentInChildren<StoryDeliveryTarget>(true);
        var inventory = player ? player.GetComponent<ToppingInventory>() : null;
        if (!target || !inventory) yield break;

        // 민기가 만든 피자가 카운터에 나와 있다.
        if (pizzaPickup && pizzaPickup.PackedCount == 0 && !inventory.HasPackagedPizzas() && (!bikeStorage || bikeStorage.StoredCount == 0))
            pizzaPickup.AddPackagedPizzaData(new PizzaData(new System.Collections.Generic.List<string> { "도우", "소스", "치즈" }, true));

        SetStopActive(stop, true);
        target.ResetDelivery();
        target.Accepting = true;
        Guide(stop);
        SetObjective($"{caption} · {stop.label}(으)로 배달하기");

        bool pickedUp = false;
        while (!target.Delivered)
        {
            if (!pickedUp && (inventory.HasPackagedPizzas() || bikeStorage && bikeStorage.StoredCount > 0))
            {
                pickedUp = true;
                if (onPickedUp != null) { onPickedUp(); yield return WaitWhileSubtitle(); }
            }
            yield return null;
        }
        target.Accepting = false;
        SetStopActive(stop, false);
        ClearGuide();
        SetObjective("");
    }

    IEnumerator WaitWhileSubtitle()
    {
        yield return null;
        while (inSubtitle) yield return null;
    }

    // ───────── 3. 민기 문자 (JJinCity StoryPhoneInbox 그대로) ─────────
    // 알림·알림음·연락함 저장·내리면 휴대폰 자동 열림·읽음 처리는 모두 StoryPhoneInbox/StoryPhoneApp이 한다.
    IEnumerator MingiMessage()
    {
        stage = Stage.WaitMessage;
        yield return WaitScaled(messageDelay);

        stage = Stage.ReadMessage;
        SetActive(dinnerSlices, true);
        if (!inbox) inbox = FindFirstObjectByType<StoryPhoneInbox>();
        if (!inbox) yield break;
        inbox.Send(messageTitle, messageBody);
        messageSent = true; // 마지막 주문을 다시 시작할 때는 문자를 다시 보내지 않는다.
        var message = inbox.Messages.Count > 0 ? inbox.Messages[0] : null;
        SetObjective($"{messageSender}의 문자 · 휴대폰으로 확인하기");

        // 연락함에서 읽을 때까지 기다리고, 다 읽은 화면이 바로 지도에 덮이지 않게 휴대폰을 닫을 때까지 다음 안내를 미룬다.
        while (message != null && !message.read) yield return null;
        while (phone && phone.IsPhoneOpen() && !IsRiding()) yield return null;
    }

    // ───────── 4·5. 마지막 주문: 카운터에서 피자 → 오토바이 → 골목 ─────────
    IEnumerator GoToFinal()
    {
        if (finalDelivery == null || finalDelivery.point == null) yield break;
        stage = Stage.GoToFinal;
        if (fakeCustomer) fakeCustomer.gameObject.SetActive(true);
        if (attacker) attacker.gameObject.SetActive(false);

        // 카운터의 피자를 처음 드는 순간 "다녀올게".
        yield return DeliveryLoop(finalDelivery, "마지막 주문",
            () => { if (!string.IsNullOrEmpty(departLine)) StartCoroutine(SubtitleOverGameplay(departLine)); });

        yield return Abduction();
    }

    void HoldCounterBox()
    {
        if (!counterBox || !player) return;
        var cam = player.GetComponentInChildren<Camera>(true);
        if (!cam) return;
        counterBox.SetActive(true);
        foreach (var c in counterBox.GetComponentsInChildren<Collider>()) c.enabled = false;
        counterBox.transform.SetParent(cam.transform, false);
        counterBox.transform.localPosition = heldBoxLocalPosition;
        counterBox.transform.localRotation = Quaternion.Euler(heldBoxLocalEuler);
    }

    // 조작 중 한 줄 자막: 레터박스와 안내 문구를 함께 보이고 3초 뒤 또는 F/Space로 넘어간다.
    IEnumerator SubtitleOverGameplay(string line)
    {
        inSubtitle = true;
        var move = player ? player.GetComponent<PlayerMove>() : null;
        var look = player ? player.GetComponentInChildren<CameraMove>(true) : null;
        bool lookWasOn = look && look.enabled;
        if (move) move.isControllable = false;
        if (look) look.enabled = false;
        bool objectiveWasOn = objectiveText && objectiveText.gameObject.activeSelf;
        if (objectiveText) objectiveText.gameObject.SetActive(false);

        SetActive(cinematicRoot, true);
        SetBlackout(0f);
        SetCinematicFrameVisible(true);
        if (dialogueText) dialogueText.text = line;
        yield return WaitLine();

        if (dialogueText) dialogueText.text = "";
        SetCinematicFrameVisible(false);
        SetActive(cinematicRoot, false);
        if (objectiveText && objectiveWasOn) objectiveText.gameObject.SetActive(true);
        if (move) move.isControllable = true;
        if (look && lookWasOn) { look.SyncViewFromTransform(); look.enabled = true; }
        inSubtitle = false;
    }

    // 대사 한 줄: lineSeconds가 지나면 자동으로 넘어가고, F로 바로 넘기거나 Space로 건너뛸 수 있다.
    bool lastLineSkipped;
    IEnumerator WaitLine()
    {
        lastLineSkipped = false;
        yield return null; // 같은 프레임 입력이 다음 줄로 넘어가지 않게 한다.
        for (float t = 0; t < lineSeconds; t += Time.deltaTime)
        {
            if (SkipPressed()) { lastLineSkipped = true; yield break; }
            if (NextPressed()) yield break;
            yield return null;
        }
    }

    // ───────── 6. 납치: 1인칭으로 피자를 건네는 순간 뒤에서 맞고 쓰러지며 눈이 감긴다 ─────────
    IEnumerator Abduction()
    {
        stage = Stage.Abducted;
        var cam = player ? player.GetComponentInChildren<Camera>(true) : null;
        if (!cam) { onFinalArrived?.Invoke(); yield break; }

        // 조작·HUD를 잠그고 레터박스만 띄운다. (자막이 있을 때만 안내 문구 표시)
        var move = player.GetComponent<PlayerMove>();
        var look = player.GetComponentInChildren<CameraMove>(true);
        var cc = player.GetComponent<CharacterController>();
        if (move) move.isControllable = false;
        if (look) look.enabled = false;
        if (cc) cc.enabled = false;
        if (phone) phone.ClosePhone();
        SetActive(gameplayCanvas, false);
        // 조준점 등 플레이어에 붙은 화면 UI도 숨긴다. (휴대폰 화면은 월드 캔버스라 제외)
        foreach (var canvas in player.GetComponentsInChildren<Canvas>())
            if (canvas.renderMode != RenderMode.WorldSpace) canvas.enabled = false;
        if (objectiveText) objectiveText.gameObject.SetActive(false);
        inSubtitle = true;
        SetActive(cinematicRoot, true);
        SetBlackout(0f);
        SetCinematicFrameVisible(true);
        if (dialogueText) dialogueText.text = "";
        if (skipHint) skipHint.gameObject.SetActive(handoverLines != null && handoverLines.Length > 0);
        SetEyelids(0f);
        // 방금 건넨 배달 상자를 컷신용 모델로 손에 다시 보여 준다.
        HoldCounterBox();
        if (attacker && attackerHidePoint)
        {
            attacker.transform.SetPositionAndRotation(attackerHidePoint.position, attackerHidePoint.rotation);
            attacker.gameObject.SetActive(true);
        }

        Transform camT = cam.transform;
        Vector3 customerHead = fakeCustomer ? HeadPoint(fakeCustomer) : finalDelivery.point.position + Vector3.up * 1.6f;

        // 손님 앞으로 다가서며 얼굴을 본다.
        Vector3 bodyFrom = player.transform.position;
        Quaternion bodyRotFrom = player.transform.rotation;
        Vector3 bodyTo = handoverStandPoint ? Flat(handoverStandPoint.position, bodyFrom.y) : bodyFrom;
        Quaternion bodyRotTo = handoverStandPoint ? Flat(handoverStandPoint.rotation) : bodyRotFrom;
        Quaternion camFrom = camT.rotation;
        for (float t = 0; t < approachSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / approachSeconds);
            player.transform.SetPositionAndRotation(Vector3.Lerp(bodyFrom, bodyTo, k), Quaternion.Slerp(bodyRotFrom, bodyRotTo, k));
            camT.rotation = Quaternion.Slerp(camFrom, Quaternion.LookRotation(customerHead + Vector3.down * .35f - camT.position), k);
            yield return null;
        }

        if (handoverLines != null)
            foreach (var line in handoverLines)
            {
                if (dialogueText) dialogueText.text = line.text;
                yield return WaitLine();
                if (lastLineSkipped) break;
            }
        if (dialogueText) dialogueText.text = "";
        if (skipHint) skipHint.gameObject.SetActive(false);

        // 상자를 내밀고, 손님이 두 손으로 받으려는 순간.
        Transform box = counterBox ? counterBox.transform : null;
        Vector3 boxFrom = box ? box.localPosition : Vector3.zero;
        Vector3 boxTo = boxFrom + new Vector3(0f, .1f, .3f);
        Quaternion camHold = camT.rotation;
        for (float t = 0; t < handoverSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / handoverSeconds);
            if (box) box.localPosition = Vector3.Lerp(boxFrom, boxTo, k);
            customerReach = Mathf.Clamp01((k - .35f) / .55f);
            camT.rotation = Quaternion.Slerp(camHold, Quaternion.LookRotation((box ? box.position : customerHead) + Vector3.up * .15f - camT.position), k * .6f);
            yield return null;
        }

        // 둔탁한 소리와 함께 순간 암전, 앞으로 꺾인다.
        if (sfx && thud) sfx.PlayOneShot(thud, 1f);
        if (sfx && ringing) sfx.PlayOneShot(ringing, .35f);
        SetBlackout(1f);
        Vector3 hitPos = camT.position;
        Quaternion hitRot = camT.rotation * Quaternion.Euler(18f, -6f, 9f);
        camT.rotation = hitRot;
        if (box)
        {
            box.SetParent(null, true);
            StartCoroutine(DropBox(box, Flat(bodyTo + bodyRotTo * new Vector3(.15f, 0f, .55f), .02f)));
        }
        if (attacker && attackerHidePoint) attacker.transform.position = Flat(bodyTo + bodyRotTo * new Vector3(0.5f, 0f, -0.85f), attacker.transform.position.y);
        for (float t = 0; t < .14f; t += Time.deltaTime) yield return null;
        customerReach = 0f;

        // 시야가 돌아오며 바닥으로 쓰러진다.
        Vector3 fallPos = fallenViewPoint ? fallenViewPoint.position : Flat(hitPos, .2f);
        Quaternion fallRot = fallenViewPoint ? fallenViewPoint.rotation : hitRot * Quaternion.Euler(30f, 0f, 75f);
        bool landed = false;
        for (float t = 0; t < fallSeconds; t += Time.deltaTime)
        {
            float k = t / fallSeconds;
            SetBlackout(Mathf.Lerp(.85f, .15f, Mathf.Clamp01(k * 2.2f)));
            float drop = k * k; // 무게가 실리며 빨라진다
            float wobble = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 4f;
            camT.SetPositionAndRotation(
                Vector3.Lerp(hitPos, fallPos, drop),
                Quaternion.Slerp(hitRot, fallRot, Mathf.SmoothStep(0f, 1f, k)) * Quaternion.Euler(wobble, 0f, wobble * .5f));
            yield return null;
        }
        camT.SetPositionAndRotation(fallPos, fallRot);
        if (!landed && sfx && bodyHit) sfx.PlayOneShot(bodyHit, .9f);
        // 바닥에 부딪힌 작은 반동
        for (float t = 0; t < .25f; t += Time.deltaTime)
        {
            float b = Mathf.Sin(t / .25f * Mathf.PI) * .035f;
            camT.position = fallPos + Vector3.up * b;
            yield return null;
        }
        camT.position = fallPos;

        // 흐릿한 시야 너머로 조직원이 걸어와 서고, 눈이 천천히 감긴다.
        StartCoroutine(AttackerWalksIn());
        Quaternion lying = camT.rotation;
        int keys = Mathf.Min(eyelidKeys.Length - 1, eyelidDurations.Length);
        float total = 0f;
        for (int i = 0; i < keys; i++) total += eyelidDurations[i];
        float elapsed = 0f;
        for (int i = 0; i < keys; i++)
        {
            float from = eyelidKeys[i], to = eyelidKeys[i + 1], seconds = Mathf.Max(.05f, eyelidDurations[i]);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                SetEyelids(Mathf.Lerp(from, to, k));
                elapsed += Time.deltaTime;
                float daze = elapsed / Mathf.Max(.1f, total);
                SetBlackout(Mathf.Lerp(.15f, .45f, daze));
                camT.rotation = lying * Quaternion.Euler(Mathf.Sin(elapsed * .9f) * 1.2f, Mathf.Sin(elapsed * .6f) * 1.5f, 0f);
                yield return null;
            }
        }
        SetEyelids(1f);
        SetBlackout(1f);
        SetCinematicFrameVisible(false);
        for (float t = 0; t < holdBlackSeconds; t += Time.deltaTime) yield return null;

        stage = Stage.ArrivedFinal;
        onFinalArrived?.Invoke();
    }

    IEnumerator AttackerWalksIn()
    {
        if (!attacker || !attackerEndPoint) yield break;
        var body = attacker.transform;
        attacker.applyRootMotion = false;
        while (StepToward(body, attackerEndPoint.position, 1.1f))
        {
            attacker.SetFloat("MoveSpeed", 1.1f);
            yield return null;
        }
        attacker.SetFloat("MoveSpeed", 0f);
        Quaternion from = body.rotation;
        for (float t = 0; t < .5f; t += Time.deltaTime)
        {
            body.rotation = Quaternion.Slerp(from, Flat(attackerEndPoint.rotation), t / .5f);
            yield return null;
        }
    }

    IEnumerator DropBox(Transform box, Vector3 ground)
    {
        Vector3 from = box.position;
        Quaternion rotFrom = box.rotation;
        Quaternion rotTo = Quaternion.Euler(0f, rotFrom.eulerAngles.y + 25f, 14f);
        for (float t = 0; t < .45f; t += Time.deltaTime)
        {
            float k = t / .45f;
            box.SetPositionAndRotation(Vector3.Lerp(from, ground, k * k), Quaternion.Slerp(rotFrom, rotTo, k));
            yield return null;
        }
        box.SetPositionAndRotation(ground, rotTo);
    }

    static Vector3 HeadPoint(Animator a)
    {
        var head = a.isHuman ? a.GetBoneTransform(HumanBodyBones.Head) : null;
        return head ? head.position : a.transform.position + Vector3.up * 1.6f;
    }

    // 0 = 눈 뜸, 1 = 감김. 위·아래 눈꺼풀이 화면 가운데에서 만난다.
    void SetEyelids(float closed)
    {
        closed = Mathf.Clamp01(closed);
        bool show = closed > .001f;
        if (eyelidTop)
        {
            eyelidTop.gameObject.SetActive(show);
            eyelidTop.anchorMin = new Vector2(-.05f, 1f - .62f * closed);
            eyelidTop.anchorMax = new Vector2(1.05f, 1.02f);
        }
        if (eyelidBottom)
        {
            eyelidBottom.gameObject.SetActive(show);
            eyelidBottom.anchorMin = new Vector2(-.05f, -.02f);
            eyelidBottom.anchorMax = new Vector2(1.05f, .62f * closed);
        }
    }

    // 가짜 손님이 두 손을 상자로 뻗는다 (간단한 2본 IK).
    float customerReach;
    void ReachCustomerHands()
    {
        if (!fakeCustomer || customerReach <= 0f || !fakeCustomer.isHuman || !counterBox) return;
        var box = counterBox.transform;
        Vector3 side = box.right * .2f;
        ReachArm(fakeCustomer, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, box.position + side, customerReach, -fakeCustomer.transform.right);
        ReachArm(fakeCustomer, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, box.position - side, customerReach, fakeCustomer.transform.right);
    }

    // ───────── 컷신 무기 ─────────
    void SpawnCutsceneWeapon()
    {
        shownWeapon = SelectedWeaponCarry.HasWeapon ? SelectedWeaponCarry.Weapon : fallbackWeapon;
        if (shownWeapon == WeaponKind.None || !cutscenePlayer) return;
        var prefab = WeaponCatalog.Model(shownWeapon);
        if (!prefab) return;
        var holder = new GameObject("컷신 무기 · " + shownWeapon).transform;
        holder.SetParent(transform, false);
        var model = Instantiate(prefab, holder);
        model.name = prefab.name;
        foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var b in model.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
        model.transform.localPosition = -GripPoint(shownWeapon);
        model.transform.localRotation = Quaternion.identity;
        cutsceneWeapon = holder;
        weaponPose = WeaponPose.InHand;
        placeBlend = 0f;
        AttachWeaponToGrip();
    }

    // 손에 든 동안은 Finger.R_end 아래 '컷신 무기 쥐는 자리'의 자식으로 붙여 손가락과 함께 움직인다.
    // 쥐는 자리는 권총 기준으로 맞춰 두고, 다른 무기는 들었을 때의 기울기 차이만 더한다.
    void AttachWeaponToGrip()
    {
        if (!cutsceneWeapon || !weaponGrip) return;
        cutsceneWeapon.SetParent(weaponGrip, false);
        cutsceneWeapon.localPosition = Vector3.zero;
        cutsceneWeapon.localRotation = Quaternion.Inverse(HandHold(WeaponKind.Pistol)) * HandHold(shownWeapon);
        // 뼈대(FBX)는 스케일이 커서(약 111배) 무기 크기가 그대로 보이도록 되돌린다.
        Vector3 s = weaponGrip.lossyScale;
        cutsceneWeapon.localScale = new Vector3(1f / Mathf.Max(.0001f, s.x), 1f / Mathf.Max(.0001f, s.y), 1f / Mathf.Max(.0001f, s.z));
    }

    // 손에서 떼어 씬 기준으로 옮길 때는 원래 크기로 돌린다.
    void ReleaseWeaponFromGrip()
    {
        weaponReleased = true;
        if (!cutsceneWeapon || cutsceneWeapon.parent == transform) return;
        cutsceneWeapon.SetParent(transform, true);
        cutsceneWeapon.localScale = Vector3.one;
    }
    bool weaponReleased;

    // 무기 모델에서 손으로 쥐는 지점 (모델 로컬 좌표)
    static Vector3 GripPoint(WeaponKind kind)
    {
        switch (kind)
        {
            case WeaponKind.Bat: return new Vector3(0f, .1f, 0f);
            case WeaponKind.Tuna: return new Vector3(0f, .08f, 0f);
            case WeaponKind.Pistol: return new Vector3(0f, .17f, .04f);
            default: return Vector3.zero;
        }
    }

    // 손에 들었을 때 몸 기준 회전: 팔을 내린 채 아래로 늘어뜨린다.
    static Quaternion HandHold(WeaponKind kind)
    {
        switch (kind)
        {
            case WeaponKind.Bat:
            case WeaponKind.Tuna: return Quaternion.Euler(165f, 0f, 0f);
            case WeaponKind.Pistol: return Quaternion.Euler(70f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    // 보관 자리에 눕혀 둔 회전
    static Quaternion ShelfRest(WeaponKind kind)
    {
        switch (kind)
        {
            case WeaponKind.Bat:
            case WeaponKind.Tuna: return Quaternion.Euler(0f, 0f, 90f);
            case WeaponKind.Pistol: return Quaternion.Euler(0f, 90f, 90f);
            default: return Quaternion.identity;
        }
    }

    void LateUpdate()
    {
        UpdateCutsceneWeapon();
        ReachCustomerHands();
        if (marker == null) return;

        Camera cam = GameplayCamera();
        if (markerTarget == null || !markerTarget.gameObject.activeInHierarchy || cam == null || inSubtitle)
        {
            if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
            return;
        }
        marker.gameObject.SetActive(true);
        StoryTargetDiamond.Place(marker, cam, markerTarget.position + Vector3.up * markerHeight, PlayerPosition(), markerLabel);
    }

    void UpdateCutsceneWeapon()
    {
        if (!cutsceneWeapon || !cutscenePlayer) return;
        var hand = cutscenePlayer.isHuman ? cutscenePlayer.GetBoneTransform(HumanBodyBones.RightHand) : null;

        Vector3 shelfPos = weaponSlot ? weaponSlot.position : cutsceneWeapon.position;
        Quaternion shelfRot = (weaponSlot ? weaponSlot.rotation : Quaternion.identity) * ShelfRest(shownWeapon);

        // 팔을 보관 자리 쪽으로 뻗는다 (간단한 2본 IK).
        if (hand && weaponSlot && reachWeight > 0f) ReachRightArm(weaponSlot.position, reachWeight);

        if (weaponPose == WeaponPose.OnShelf || (!hand && !weaponGrip))
        {
            ReleaseWeaponFromGrip();
            cutsceneWeapon.SetPositionAndRotation(shelfPos, shelfRot);
            return;
        }

        // 손에 쥔 자세: 쥐는 자리(Finger.R_end 아래)가 있으면 그 자식으로 붙인다.
        Vector3 handPos;
        Quaternion handRot;
        if (weaponGrip)
        {
            if (!weaponReleased && cutsceneWeapon.parent != weaponGrip) AttachWeaponToGrip();
            handPos = weaponGrip.position;
            handRot = weaponGrip.rotation * Quaternion.Inverse(HandHold(WeaponKind.Pistol)) * HandHold(shownWeapon);
            if (placeBlend <= 0f) return;
            ReleaseWeaponFromGrip(); // 내려놓는 동안은 떼어서 보관 자리로 옮긴다.
        }
        else
        {
            handPos = hand.position;
            handRot = Flat(cutscenePlayer.transform.rotation) * HandHold(shownWeapon);
        }
        cutsceneWeapon.SetPositionAndRotation(
            Vector3.Lerp(handPos, shelfPos, placeBlend),
            Quaternion.Slerp(handRot, shelfRot, placeBlend));
    }

    void ReachRightArm(Vector3 target, float weight) =>
        ReachArm(cutscenePlayer, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, target, weight, cutscenePlayer.transform.right);

    static void ReachArm(Animator a, HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones handBone, Vector3 target, float weight, Vector3 side)
    {
        var upper = a.GetBoneTransform(upperBone);
        var lower = a.GetBoneTransform(lowerBone);
        var hand = a.GetBoneTransform(handBone);
        if (!upper || !lower || !hand) return;

        float la = Vector3.Distance(upper.position, lower.position);
        float lb = Vector3.Distance(lower.position, hand.position);
        Vector3 end = Vector3.Lerp(hand.position, target, weight);
        Vector3 delta = end - upper.position;
        float d = Mathf.Clamp(delta.magnitude, .01f, la + lb - .001f);
        Vector3 dir = delta.normalized;
        float along = (la * la - lb * lb + d * d) / (2 * d);
        Vector3 bend = Vector3.ProjectOnPlane(Vector3.down + side * .55f, dir).normalized;
        Vector3 elbow = upper.position + dir * along + bend * Mathf.Sqrt(Mathf.Max(0, la * la - along * along));
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, upper.position + dir * d - lower.position) * lower.rotation;
    }

    void SetPlayerSpeed(float speed)
    {
        if (cutscenePlayer) cutscenePlayer.SetFloat("Speed", speed);
    }

    // 목표점까지 수평 이동. 도착하면 false.
    static bool StepToward(Transform body, Vector3 target, float speed)
    {
        Vector3 to = target - body.position;
        to.y = 0f;
        if (to.magnitude < .05f) return false;
        Vector3 step = Vector3.ClampMagnitude(to, speed * Time.deltaTime);
        body.position += step;
        body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 10f * Time.deltaTime);
        return true;
    }

    // ───────── 마커·내비 ─────────
    void Guide(DeliveryStop stop)
    {
        markerTarget = stop.point;
        markerLabel = stop.label;
        var place = stop.point.GetComponent<NavigationPlace>();
        foreach (var pins in FindObjectsByType<NavigationMapPins>(FindObjectsSortMode.None)) pins.Rebuild();
        if (navigation != null && place != null) navigation.SelectPlace(place);
    }

    void ClearGuide()
    {
        markerTarget = null;
        if (marker) marker.gameObject.SetActive(false);
        if (navigation != null) navigation.StopGuidance();
        foreach (var pins in FindObjectsByType<NavigationMapPins>(FindObjectsSortMode.None)) pins.Rebuild();
    }

    // ───────── 공용 ─────────
    static MotorcycleController RiddenBike()
    {
        foreach (var bike in FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))
            if (bike != null && bike.isDriven) return bike;
        return null;
    }

    static bool IsRiding() => RiddenBike() != null;

    Vector3 PlayerPosition()
    {
        var bike = RiddenBike();
        if (bike != null) return bike.transform.position;
        return player != null ? player.transform.position : Vector3.zero;
    }

    Camera GameplayCamera()
    {
        var bike = RiddenBike();
        if (bike != null && bike.bikeCamera != null)
        {
            var c = bike.bikeCamera.GetComponentInChildren<Camera>();
            if (c != null && c.isActiveAndEnabled) return c;
        }
        if (player != null && player.activeInHierarchy)
        {
            var c = player.GetComponentInChildren<Camera>();
            if (c != null && c.isActiveAndEnabled) return c;
        }
        return null;
    }

    static float PlanarDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    static Quaternion Flat(Quaternion q) => Quaternion.Euler(0f, q.eulerAngles.y, 0f);
    static Vector3 Flat(Vector3 p, float y) => new Vector3(p.x, y, p.z);

    // 한 번 누른 키가 다음 대사·동작까지 넘기지 않도록 짧은 대기 시간을 둔다.
    float inputReadyAt;
    bool CanTakeInput() => Time.timeScale > 0f && Time.unscaledTime >= inputReadyAt;
    bool NextPressed() => Consume(Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame || EditorTestPress(ref editorNext));
    bool SkipPressed() => Consume(Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame || EditorTestPress(ref editorSkip));
    bool Consume(bool pressed)
    {
        if (!pressed || !CanTakeInput()) return false;
        inputReadyAt = Time.unscaledTime + .25f;
        return true;
    }

    // 에디터 자동 점검용 (빌드에는 영향 없음): 다음 프레임에 F/Space를 누른 것처럼 처리한다.
    bool editorNext, editorSkip;
    public void EditorTestNext() { if (Application.isEditor) editorNext = true; }
    public void EditorTestSkip() { if (Application.isEditor) editorSkip = true; }
    static bool EditorTestPress(ref bool flag)
    {
        if (!flag) return false;
        flag = false;
        return true;
    }

    IEnumerator WaitOrSkip(float seconds, bool[] skipped)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime)
        {
            if (SkipPressed()) { skipped[0] = true; yield break; }
            if (NextPressed()) yield break;
            yield return null;
        }
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        if (blackout) blackout.gameObject.SetActive(true);
        for (float t = 0; t < seconds; t += Time.deltaTime)
        {
            SetBlackout(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetBlackout(to);
    }

    static IEnumerator WaitScaled(float seconds)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
    }

    void SetBlackout(float alpha)
    {
        if (!blackout) return;
        var c = blackout.color;
        c.a = Mathf.Clamp01(alpha);
        blackout.color = c;
        blackout.raycastTarget = false;
    }

    void SetCinematicFrameVisible(bool visible)
    {
        if (letterbox != null) foreach (var bar in letterbox) if (bar) bar.gameObject.SetActive(visible);
        if (dialogueText) dialogueText.gameObject.SetActive(visible);
        if (skipHint) skipHint.gameObject.SetActive(visible);
    }

    void SetObjective(string text)
    {
        if (objectiveText) objectiveText.text = text;
    }

    static void SetStopActive(DeliveryStop stop, bool active)
    {
        if (stop != null && stop.point != null) SetActive(stop.point.gameObject, active);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    // 둔탁한 타격음: 낮은 울림 + 짧은 잡음
    static AudioClip MakeImpact(string name, float pitch, float seconds, float noise)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)];
        var random = new System.Random(7);
        float smooth = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float body = Mathf.Sin(2 * Mathf.PI * pitch * t * (1f - t * .6f)) * Mathf.Exp(-t * 9f);
            smooth = Mathf.Lerp(smooth, (float)random.NextDouble() * 2f - 1f, .18f);
            float crack = smooth * Mathf.Exp(-t * 38f) * noise;
            samples[i] = Mathf.Clamp((body * .9f + crack) * .8f, -1f, 1f);
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // 맞은 뒤 귀가 울리는 소리
    static AudioClip MakeRinging(string name, float pitch, float seconds)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Clamp01(t / .08f) * Mathf.Clamp01((seconds - t) / (seconds * .8f));
            samples[i] = Mathf.Sin(2 * Mathf.PI * pitch * t) * envelope * .12f;
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip MakeTone(string name, float[] notes, float noteLength, float sounding)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * noteLength * notes.Length)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            int n = Mathf.Min(notes.Length - 1, (int)(t / noteLength));
            float local = t - n * noteLength;
            float envelope = local < sounding ? Mathf.Sin(Mathf.PI * local / sounding) : 0f;
            samples[i] = Mathf.Sin(2 * Mathf.PI * notes[n] * t) * envelope * .4f;
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
