using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DeliveryService.Mok;
using DeliveryService.Mok.Navigation;

/// <summary>
/// 실장 전화 수신 및 민기 문자 이벤트 연출 스크립트.
/// 다른 팀원의 스크립트를 수정하지 않고 기존 PhoneController, NavigationManager와 연동된다.
///
/// 흐름:
/// 1. isEventTriggered가 true가 되면 사망한 민기의 문자 메시지가 먼저 표시됨
/// 2. 잠시 후 핸드폰이 아래에서 위로 올라오며 실장에게서 전화가 옴 (벨소리 + 진동)
/// 3. F 키로 전화를 받으면 위/아래 검은 레터박스가 생기며 언더테일식 글자당 사운드로 대사 출력
/// 4. 대사가 끝나면 레터박스가 사라지고 '회담장' 표시 및 기존 내비게이션으로 길 안내 시작
/// </summary>
[DefaultExecutionOrder(500)]
public class ChiefPhoneCallEvent : MonoBehaviour
{
    public enum SpeakerType
    {
        Chief,
        Player,
        Mingi
    }

    public enum EventState
    {
        Idle,
        WaitingForPlayerReady,
        MingiMessage,
        DelayBeforeCall,
        Ringing,
        InCall,
        Completed
    }

    [Serializable]
    public class DialogueLine
    {
        public SpeakerType speaker = SpeakerType.Chief;
        public string speakerLabel = "실장";
        [TextArea(2, 4)]
        public string text = "";
    }

    [Header("1. 이벤트 시작 트리거")]
    [Tooltip("이 값이 true가 되면 민기 문자 → 실장 전화 → 회담장 내비게이션 연출이 시작됩니다.")]
    public bool isEventTriggered = false;

    [Tooltip("이미 한 번 실행한 뒤 다시 실행하지 않을지 여부")]
    public bool triggerOnlyOnce = true;

    [Header("2. 민기 문자 설정 (이벤트 시작 시 먼저 출력)")]
    public string mingiMessageHeader = "문자 / 민기";

    [TextArea(2, 4)]
    public string mingiMessageText = "주문 하나만 더 왔어. 이것만 배달하고 퇴근하자. 네 몫 두 조각 빼놨어.";

    [Tooltip("문자 출력이 끝난 뒤 자동으로 전화 수신으로 넘어갈지 여부 (false면 F키로 넘김)")]
    public bool autoAdvanceMingiMessage = true;

    [Min(0.5f)]
    [Tooltip("문자 출력 완료 후 전화가 오기 전까지 머무르는 시간(초)")]
    public float mingiMessageHoldSeconds = 3.2f;

    [Min(0.1f)]
    [Tooltip("민기 문자가 사라진 뒤 전화가 울리기까지의 정적 시간(초)")]
    public float delayBeforeRingSeconds = 0.8f;

    [Header("3. 실장 전화 대사 목록 (Inspector에서 자유롭게 수정 가능)")]
    public string callerDisplayName = "실장";

    public DialogueLine[] callDialogue = new DialogueLine[]
    {
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장 [전화]", text = "박진웅이 움직인다. 이번엔 두목 자리까지 먹을 생각이야." },
        new DialogueLine { speaker = SpeakerType.Player, speakerLabel = "플레이어", text = "어디 있어." },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장 [전화]", text = "야마자키파랑 회담을 잡았어. 지난번 일을 정리하겠다고." },
        new DialogueLine { speaker = SpeakerType.Player, speakerLabel = "플레이어", text = "그놈이 일을 정리한다고?" },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장", text = "야마자키파 두목이 직접 나와. 금고 일에 매장 총격까지, 박진웅한테 따질 게 많겠지." },
        new DialogueLine { speaker = SpeakerType.Player, speakerLabel = "플레이어", text = "순순히 대답할 놈은 아니야." },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장", text = "그래. 자기 부하들부터 들여보냈어. 두목을 없애고 그쪽 세력까지 먹을 생각인 것 같아." },
        new DialogueLine { speaker = SpeakerType.Player, speakerLabel = "플레이어", text = "민기한테도 그랬어. 일만 끝나면 없애려고 했지." },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장", text = "탄약 챙겨. 회담장 위치도 보냈어." },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장", text = "난 밖에서 연락할게. 헬기도 하나 잡아놨어. 일이 틀어지면 위로 빠질 수 있어." },
        new DialogueLine { speaker = SpeakerType.Player, speakerLabel = "플레이어", text = "난 그놈 두고 안 나와." },
        new DialogueLine { speaker = SpeakerType.Chief, speakerLabel = "실장", text = "알아. 그러니까 연락 끊지 마." }
    };

    [Header("4. 타자기 & 언더테일식 텍스트 사운드 설정")]
    [Min(5f)]
    [Tooltip("초당 출력되는 글자 수")]
    public float charactersPerSecond = 26f;

    [Min(0f)]
    [Tooltip("마침표/쉼표/물음표에서 잠깐 숨 고르는 시간(초)")]
    public float punctuationPauseSeconds = 0.08f;

    [Min(1)]
    [Tooltip("몇 글자마다 보이스 사운드를 낼지 (1 = 매 글자마다)")]
    public int voiceBlipEveryNChars = 1;

    [Range(0f, 1f)]
    public float voiceBlipVolume = 0.42f;

    [Range(0f, 0.2f)]
    public float voicePitchJitter = 0.05f;

    [Tooltip("비워 두면 부드러운 기본 보이스 사운드가 자동 생성됩니다.")]
    public AudioClip chiefVoiceClip;
    public AudioClip playerVoiceClip;
    public AudioClip mingiVoiceClip;

    [Header("5. 문자 / 전화 효과음 (비워 두면 자동 생성)")]
    public AudioClip messageChimeClip;
    public AudioClip ringtoneClip;
    public AudioClip pickupClip;
    public AudioClip hangupClip;

    [Range(0f, 1f)]
    public float phoneSfxVolume = 0.5f;

    [Header("6. 회담장 내비게이션 목적지 설정")]
    [Tooltip("회담장 표시 이름")]
    public string meetingHallName = "회담장";

    [Tooltip("회담장 목적지 위치 (포탈 위치 또는 회담장 건물 앞 Transform을 연결하세요)")]
    public Transform meetingHallTarget;

    [Tooltip("선택 사항: 회담장이 골목/실내에 있다면 도로·인도 쪽 입구 Transform을 연결하세요 (비우면 위 위치 사용)")]
    public Transform meetingHallRoadEntrance;

    [Tooltip("전화 종료 후 화면 상단과 3D 공간에 '회담장' 목적지 안내를 표시할지 여부")]
    public bool showMeetingHallHudAndMarker = true;

    [Header("7. 씬 참조 (비워 두면 자동으로 찾습니다)")]
    public PhoneController phoneController;
    public NavigationManager navigationManager;
    public NavigationPlanner navigationPlanner;
    public Font uiFont;

    [Header("8. 완료 이벤트 (선택 사항)")]
    public UnityEvent onCallCompleted;

    // 외부(예: BuildingPortal)에서 상태 확인용
    public static bool AnySequenceActive { get; private set; }
    public EventState CurrentState => state;
    public bool IsCallCompleted => state == EventState.Completed;
    public bool IsMeetingHallGuidanceActive => meetingHallGuidanceActive;

    private EventState state = EventState.Idle;
    private bool hasTriggeredOnce;
    private bool meetingHallGuidanceActive;

    // 씬 컴포넌트 캐시
    private PlayerCombat playerCombat;
    private DayNightCycle dayNightCycle;
    private MotorcycleController[] bikes;
    private NavigationPlace meetingHallPlace;
    private bool plannerWasEnabled;
    private bool plannerSuspended;
    private bool inboxAutoOpenSaved;
    private bool savedInboxAutoOpen;
    private Quaternion savedPhoneLocalRotation;
    private bool hasSavedPhoneRotation;

    // 조작 잠금 관리
    private readonly Dictionary<Behaviour, bool> suspendedBehaviours = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Behaviour, bool> suspendedMessageInteractions = new Dictionary<Behaviour, bool>();
    private bool callControlsLocked;
    private bool messageInteractionsLocked;
    private Coroutine releaseControlRoutine;

    // 오디오
    private AudioSource voiceAudioSource;
    private AudioSource sfxAudioSource;
    private AudioClip generatedChiefClip;
    private AudioClip generatedPlayerClip;
    private AudioClip generatedMingiClip;
    private AudioClip generatedChimeClip;
    private AudioClip generatedRingtoneClip;
    private AudioClip generatedPickupClip;
    private AudioClip generatedHangupClip;
    private float nextBlipAllowedTime;
    private float nextRingtoneTime;
    private float ringStartedTime;
    private float callStartedTime;
    private float wavePhase;

    // 타이핑 상태
    private int currentLineIndex;
    private string fullLineText = "";
    private int revealedCharCount;
    private float charTimer;
    private bool isTyping;
    private float messageCompletedTime;
    private float delayBeforeRingEndTime;
    private float inputBlockedUntil;
    private int stateEnteredFrame = -1;

    // 레터박스 애니메이션
    private float letterboxBlend;
    private float letterboxTarget;

    // 런타임 생성 UI (Screen Space Overlay)
    private GameObject overlayCanvasRoot;
    private RectTransform topLetterboxBar;
    private RectTransform bottomLetterboxBar;
    private GameObject subtitleAreaRoot;
    private Text subtitleSpeakerText;
    private Text subtitleBodyText;
    private Text bottomRightHintText;

    private CanvasGroup mingiCardGroup;
    private Text mingiHeaderText;
    private Text mingiBodyText;
    private Text mingiHintText;

    private GameObject ringPromptRoot;
    private Text ringPromptText;

    private GameObject destinationBannerRoot;
    private Text destinationBannerTitle;
    private Text destinationBannerDetail;
    private RectTransform targetMarkerRect;
    private Text targetMarkerLabel;

    // 3D 핸드폰 화면 오버레이 (NavigationFinalDisplay 자식)
    private RectTransform phoneScreenOverlay;
    private Text phoneScreenStatusText;
    private Text phoneScreenCallerText;
    private Text phoneScreenSubText;
    private Image phoneScreenButtonBg;
    private Text phoneScreenButtonText;
    private Image[] phoneVoiceBars;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AnySequenceActive = false;
    }

    private void Awake()
    {
        SetupAudioSources();
        GenerateFallbackAudioClips();
    }

    private void Start()
    {
        ResolveSceneReferences();
        BuildOverlayUI();
        BuildPhoneScreenOverlay();
    }

    /// <summary>
    /// 외부 스크립트에서 이 이벤트를 시작할 때 호출할 수 있습니다.
    /// </summary>
    public void TriggerEvent()
    {
        if (triggerOnlyOnce && hasTriggeredOnce) return;
        isEventTriggered = true;
    }

    /// <summary>
    /// 테스트나 재실행을 위해 이벤트 상태를 초기화합니다.
    /// </summary>
    public void ResetEvent()
    {
        CleanupActiveSequence();
        ClearMeetingHallGuidance();
        isEventTriggered = false;
        hasTriggeredOnce = false;
        state = EventState.Idle;
    }

    /// <summary>
    /// 회담장 내비게이션 안내와 화면 표시를 종료합니다. (포탈 진입 시 호출됨)
    /// </summary>
    public void ClearMeetingHallGuidance()
    {
        meetingHallGuidanceActive = false;

        if (destinationBannerRoot != null) destinationBannerRoot.SetActive(false);
        if (targetMarkerRect != null) targetMarkerRect.gameObject.SetActive(false);

        if (navigationManager != null && meetingHallPlace != null && navigationManager.SelectedPlace == meetingHallPlace)
        {
            navigationManager.StopGuidance();
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Pause();
            return;
        }
        else
        {
            if (sfxAudioSource != null) sfxAudioSource.UnPause();
        }

        UpdateLetterboxAnimation();

        // 쓰러짐/체포 등 비상 상황 시 시퀀스 안전 해제
        if (IsSequenceBusy() && IsPlayerInterrupted())
        {
            AbortToCompletedWithoutSkipLoss();
            return;
        }

        switch (state)
        {
            case EventState.Idle:
                if (isEventTriggered && (!triggerOnlyOnce || !hasTriggeredOnce))
                {
                    ResolveSceneReferences();
                    hasTriggeredOnce = true;
                    state = EventState.WaitingForPlayerReady;
                }
                break;

            case EventState.WaitingForPlayerReady:
                if (IsPlayerReadyToStart())
                {
                    BeginMingiMessage();
                }
                break;

            case EventState.MingiMessage:
                TickMingiMessage();
                break;

            case EventState.DelayBeforeCall:
                if (Time.unscaledTime >= delayBeforeRingEndTime && IsPlayerReadyToStart())
                {
                    BeginRinging();
                }
                break;

            case EventState.Ringing:
                TickRinging();
                break;

            case EventState.InCall:
                TickInCall();
                break;

            case EventState.Completed:
                UpdateMeetingHallUI();
                break;
        }
    }

    private void LateUpdate()
    {
        // 수신 중/통화 중에는 핸드폰이 M키로 닫히지 않고 화면 오버레이가 최상단에 유지되도록 보장
        if (state == EventState.Ringing || state == EventState.InCall)
        {
            if (phoneController != null && !phoneController.IsPhoneOpen())
            {
                phoneController.OpenPhone();
            }

            if (phoneScreenOverlay != null)
            {
                if (!phoneScreenOverlay.gameObject.activeSelf)
                {
                    phoneScreenOverlay.gameObject.SetActive(true);
                }
                phoneScreenOverlay.SetAsLastSibling();
            }
        }
    }

    #region Phase Transitions

    private void BeginMingiMessage()
    {
        ResolveSceneReferences();
        if (overlayCanvasRoot == null) BuildOverlayUI();

        state = EventState.MingiMessage;
        AnySequenceActive = true;
        stateEnteredFrame = Time.frameCount;
        inputBlockedUntil = Time.unscaledTime + 0.25f;

        // 문자 확인용 F 입력이 주변 오브젝트/오토바이 상호작용과 겹치지 않도록 일시 잠금
        LockMessageInteractions();
        SuppressStoryInboxAutoOpen();

        if (mingiCardGroup != null)
        {
            mingiCardGroup.gameObject.SetActive(true);
            mingiCardGroup.alpha = 1f;
        }

        if (mingiHeaderText != null) mingiHeaderText.text = mingiMessageHeader;
        if (mingiBodyText != null) mingiBodyText.text = "";
        if (mingiHintText != null) mingiHintText.text = "F [확인]";

        PlaySfx(messageChimeClip != null ? messageChimeClip : generatedChimeClip, 0.85f);
        StartTypingText(mingiMessageText);
        messageCompletedTime = -1f;
    }

    private void TickMingiMessage()
    {
        if (isTyping)
        {
            AdvanceTyping(SpeakerType.Mingi, mingiBodyText);
            if (!isTyping)
            {
                messageCompletedTime = Time.unscaledTime;
            }
        }

        if (Time.frameCount > stateEnteredFrame && Time.unscaledTime >= inputBlockedUntil)
        {
            if (IsKeyPressedThisFrame(KeyCode.F, Key.F))
            {
                if (isTyping)
                {
                    CompleteCurrentTyping(mingiBodyText);
                    messageCompletedTime = Time.unscaledTime;
                    inputBlockedUntil = Time.unscaledTime + 0.18f;
                }
                else
                {
                    EndMingiMessageAndQueueCall();
                }
                return;
            }
        }

        if (!isTyping && autoAdvanceMingiMessage && messageCompletedTime > 0f &&
            Time.unscaledTime >= messageCompletedTime + mingiMessageHoldSeconds)
        {
            EndMingiMessageAndQueueCall();
        }
    }

    private void EndMingiMessageAndQueueCall()
    {
        UnlockMessageInteractions();
        if (mingiCardGroup != null)
        {
            mingiCardGroup.alpha = 0f;
            mingiCardGroup.gameObject.SetActive(false);
        }

        state = EventState.DelayBeforeCall;
        delayBeforeRingEndTime = Time.unscaledTime + Mathf.Max(0.1f, delayBeforeRingSeconds);
    }

    private void BeginRinging()
    {
        ResolveSceneReferences();
        if (phoneScreenOverlay == null) BuildPhoneScreenOverlay();

        state = EventState.Ringing;
        AnySequenceActive = true;
        stateEnteredFrame = Time.frameCount;
        inputBlockedUntil = Time.unscaledTime + 0.25f;
        ringStartedTime = Time.unscaledTime;
        nextRingtoneTime = Time.unscaledTime;

        SuspendNavigationPlanner();

        if (phoneController != null)
        {
            if (phoneController.VisualRoot != null && !hasSavedPhoneRotation)
            {
                savedPhoneLocalRotation = phoneController.VisualRoot.localRotation;
                hasSavedPhoneRotation = true;
            }
            // 기존 PhoneController의 보간을 이용해 화면 아래에서 위로 핸드폰이 떠오르게 함
            phoneController.OpenPhone();
        }

        if (phoneScreenOverlay != null)
        {
            phoneScreenOverlay.gameObject.SetActive(true);
            phoneScreenOverlay.SetAsLastSibling();
            SetPhoneOverlayVisual(isInCall: false);
        }

        if (ringPromptRoot != null)
        {
            ringPromptRoot.SetActive(true);
            if (ringPromptText != null)
            {
                ringPromptText.text = $"[F]  {callerDisplayName}의 전화 받기";
            }
        }
    }

    private void TickRinging()
    {
        // 벨소리 반복 재생
        if (Time.unscaledTime >= nextRingtoneTime)
        {
            AudioClip clip = ringtoneClip != null ? ringtoneClip : generatedRingtoneClip;
            PlaySfx(clip, 0.9f);
            float clipLen = clip != null ? clip.length : 1.1f;
            nextRingtoneTime = Time.unscaledTime + Mathf.Max(1.8f, clipLen + 0.9f);
        }

        // 전화 진동 연출 (1.1초 진동, 1.1초 멈춤 패턴)
        if (phoneController != null && phoneController.VisualRoot != null && hasSavedPhoneRotation)
        {
            float cycle = (Time.unscaledTime - ringStartedTime) % 2.2f;
            if (cycle < 1.0f)
            {
                float shake = Mathf.Sin(Time.unscaledTime * 52f) * 2.2f;
                phoneController.VisualRoot.localRotation = savedPhoneLocalRotation * Quaternion.Euler(0f, 0f, shake);
            }
            else
            {
                phoneController.VisualRoot.localRotation = savedPhoneLocalRotation;
            }
        }

        // 화면 수신 버튼 미세 깜빡임
        if (phoneScreenButtonBg != null)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
            phoneScreenButtonBg.color = new Color(0.16f * pulse, 0.68f * pulse, 0.42f * pulse, 1f);
        }

        if (Time.frameCount > stateEnteredFrame && Time.unscaledTime >= inputBlockedUntil)
        {
            if (IsKeyPressedThisFrame(KeyCode.F, Key.F))
            {
                BeginInCall();
            }
        }
    }

    private void BeginInCall()
    {
        RestorePhoneRotation();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
        PlaySfx(pickupClip != null ? pickupClip : generatedPickupClip, 0.9f);

        state = EventState.InCall;
        AnySequenceActive = true;
        stateEnteredFrame = Time.frameCount;
        inputBlockedUntil = Time.unscaledTime + 0.22f;
        callStartedTime = Time.unscaledTime;

        if (ringPromptRoot != null) ringPromptRoot.SetActive(false);

        LockCallControls();
        SetPhoneOverlayVisual(isInCall: true);

        // 위/아래 검은 레터박스 및 우하단 안내 표시 (프로젝트 자막 기준 준수)
        letterboxTarget = 1f;
        if (topLetterboxBar != null) topLetterboxBar.gameObject.SetActive(true);
        if (bottomLetterboxBar != null) bottomLetterboxBar.gameObject.SetActive(true);
        if (subtitleAreaRoot != null) subtitleAreaRoot.SetActive(true);
        if (bottomRightHintText != null)
        {
            bottomRightHintText.text = "F [다음 대사] / Space [건너뛰기]";
            bottomRightHintText.gameObject.SetActive(true);
        }

        currentLineIndex = 0;
        if (callDialogue == null || callDialogue.Length == 0)
        {
            CompleteCallSequence();
            return;
        }

        ShowDialogueLine(currentLineIndex);
    }

    private void TickInCall()
    {
        // 통화 시간 표시 및 음성 파형 애니메이션
        if (phoneScreenSubText != null)
        {
            int elapsed = Mathf.FloorToInt(Time.unscaledTime - callStartedTime);
            phoneScreenSubText.text = $"통화 중  {elapsed / 60:00}:{elapsed % 60:00}";
        }

        UpdatePhoneVoiceBars();

        DialogueLine line = callDialogue[Mathf.Clamp(currentLineIndex, 0, callDialogue.Length - 1)];
        if (isTyping)
        {
            AdvanceTyping(line.speaker, subtitleBodyText);
        }

        if (Time.frameCount <= stateEnteredFrame || Time.unscaledTime < inputBlockedUntil)
        {
            return;
        }

        // Space: 전체 통화 대사 건너뛰기
        if (IsKeyPressedThisFrame(KeyCode.Space, Key.Space))
        {
            CompleteCallSequence();
            return;
        }

        // F: 타이핑 중이면 즉시 완성, 완성 상태면 다음 대사
        if (IsKeyPressedThisFrame(KeyCode.F, Key.F))
        {
            if (isTyping)
            {
                CompleteCurrentTyping(subtitleBodyText);
                inputBlockedUntil = Time.unscaledTime + 0.15f;
            }
            else
            {
                currentLineIndex++;
                if (currentLineIndex >= callDialogue.Length)
                {
                    CompleteCallSequence();
                }
                else
                {
                    ShowDialogueLine(currentLineIndex);
                    inputBlockedUntil = Time.unscaledTime + 0.18f;
                }
            }
        }
    }

    private void CompleteCallSequence()
    {
        isTyping = false;
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        PlaySfx(hangupClip != null ? hangupClip : generatedHangupClip, 0.8f);

        // 레터박스, 자막, 우하단 힌트 숨김
        letterboxTarget = 0f;
        if (subtitleAreaRoot != null) subtitleAreaRoot.SetActive(false);
        if (bottomRightHintText != null) bottomRightHintText.gameObject.SetActive(false);
        if (ringPromptRoot != null) ringPromptRoot.SetActive(false);

        // 핸드폰 화면 오버레이 제거 & 부드럽게 아래로 내림
        if (phoneScreenOverlay != null) phoneScreenOverlay.gameObject.SetActive(false);
        RestorePhoneRotation();
        RestoreNavigationPlanner();
        RestoreStoryInboxAutoOpen();

        if (navigationPlanner != null)
        {
            navigationPlanner.Show(NavigationPlanner.Screen.Map);
        }

        if (phoneController != null && phoneController.IsPhoneOpen())
        {
            // TogglePhone은 localPosition을 순간이동하지 않고 Update 보간으로 부드럽게 내려줍니다.
            phoneController.TogglePhone();
        }

        state = EventState.Completed;
        AnySequenceActive = false;

        // F/Space를 뗄 때까지 기다린 뒤 조작 복원 (점프/상호작용 오발 방지)
        if (releaseControlRoutine != null) StopCoroutine(releaseControlRoutine);
        releaseControlRoutine = StartCoroutine(ReleaseCallControlsAfterKeyUp());

        // 회담장 표시 및 기존 내비게이션 활성화
        ActivateMeetingHallNavigation();

        onCallCompleted?.Invoke();
    }

    private void AbortToCompletedWithoutSkipLoss()
    {
        CleanupActiveSequence();
        state = EventState.Completed;
        ActivateMeetingHallNavigation();
    }

    #endregion

    #region Typing & Undertale-style Voice Blips

    private void ShowDialogueLine(int index)
    {
        DialogueLine line = callDialogue[index];
        if (subtitleSpeakerText != null)
        {
            string label = string.IsNullOrWhiteSpace(line.speakerLabel) ? DefaultSpeakerLabel(line.speaker) : line.speakerLabel;
            subtitleSpeakerText.text = label;
            subtitleSpeakerText.color = SpeakerColor(line.speaker, label);
        }

        if (subtitleBodyText != null)
        {
            subtitleBodyText.text = "";
            subtitleBodyText.color = new Color(1f, 0.96f, 0.90f, 1f);
        }

        StartTypingText(line.text ?? "");
    }

    private void StartTypingText(string text)
    {
        fullLineText = text ?? "";
        revealedCharCount = 0;
        charTimer = 0f;
        isTyping = fullLineText.Length > 0;
    }

    private void AdvanceTyping(SpeakerType speaker, Text targetText)
    {
        if (!isTyping || targetText == null) return;

        charTimer -= Time.deltaTime;
        if (charTimer > 0f) return;

        revealedCharCount++;
        if (revealedCharCount >= fullLineText.Length)
        {
            CompleteCurrentTyping(targetText);
            return;
        }

        targetText.text = fullLineText.Substring(0, revealedCharCount);

        char currentChar = fullLineText[revealedCharCount - 1];
        bool isSpace = char.IsWhiteSpace(currentChar);
        bool isPunctuation = currentChar == '.' || currentChar == ',' || currentChar == '?' || currentChar == '!' || currentChar == '…' || currentChar == '~';

        if (!isSpace && !isPunctuation)
        {
            int interval = Mathf.Max(1, voiceBlipEveryNChars);
            if (revealedCharCount % interval == 0)
            {
                PlayVoiceBlip(speaker);
            }
        }

        float baseDelay = 1f / Mathf.Max(5f, charactersPerSecond);
        charTimer = baseDelay + (isPunctuation ? punctuationPauseSeconds : 0f);
    }

    private void CompleteCurrentTyping(Text targetText)
    {
        isTyping = false;
        revealedCharCount = fullLineText.Length;
        if (targetText != null) targetText.text = fullLineText;
    }

    private void PlayVoiceBlip(SpeakerType speaker)
    {
        if (voiceAudioSource == null || Time.unscaledTime < nextBlipAllowedTime) return;

        AudioClip clip = speaker == SpeakerType.Chief
            ? (chiefVoiceClip != null ? chiefVoiceClip : generatedChiefClip)
            : speaker == SpeakerType.Mingi
                ? (mingiVoiceClip != null ? mingiVoiceClip : generatedMingiClip)
                : (playerVoiceClip != null ? playerVoiceClip : generatedPlayerClip);

        if (clip == null) return;

        float basePitch = speaker == SpeakerType.Chief ? 0.94f : speaker == SpeakerType.Mingi ? 1.04f : 1.0f;
        voiceAudioSource.pitch = basePitch + UnityEngine.Random.Range(-voicePitchJitter, voicePitchJitter);
        float sfxScale = PlayerPrefs.GetFloat("Yun.Settings.SfxVolume", 1f);
        voiceAudioSource.PlayOneShot(clip, voiceBlipVolume * sfxScale);
        nextBlipAllowedTime = Time.unscaledTime + 0.042f;
    }

    private void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxAudioSource == null || clip == null) return;
        float sfxScale = PlayerPrefs.GetFloat("Yun.Settings.SfxVolume", 1f);
        sfxAudioSource.pitch = 1f;
        sfxAudioSource.PlayOneShot(clip, phoneSfxVolume * volumeScale * sfxScale);
    }

    #endregion

    #region Navigation & Meeting Hall Display

    private void ActivateMeetingHallNavigation()
    {
        ResolveSceneReferences();
        meetingHallGuidanceActive = true;

        if (meetingHallTarget != null)
        {
            meetingHallPlace = meetingHallTarget.GetComponent<NavigationPlace>();
            if (meetingHallPlace == null)
            {
                meetingHallPlace = meetingHallTarget.gameObject.AddComponent<NavigationPlace>();
            }

            meetingHallPlace.placeId = "meeting_hall";
            meetingHallPlace.displayName = string.IsNullOrWhiteSpace(meetingHallName) ? "회담장" : meetingHallName;
            meetingHallPlace.kind = NavigationPlaceKind.Other;
            meetingHallPlace.mapAnchor = meetingHallTarget;
            meetingHallPlace.entrance = meetingHallRoadEntrance != null ? meetingHallRoadEntrance : meetingHallTarget;

            // 지도 핀에 회담장 마커가 즉시 반영되도록 갱신
            var allPins = FindObjectsByType<NavigationMapPins>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allPins.Length; i++)
            {
                if (allPins[i] != null) allPins[i].Rebuild();
            }

            if (navigationManager != null)
            {
                navigationManager.SelectPlace(meetingHallPlace);
            }
        }
        else
        {
            Debug.LogWarning("[ChiefPhoneCallEvent] meetingHallTarget이 연결되지 않아 내비게이션 목적지를 설정할 수 없습니다.", this);
        }

        if (showMeetingHallHudAndMarker)
        {
            if (destinationBannerRoot != null) destinationBannerRoot.SetActive(true);
            if (targetMarkerRect != null) targetMarkerRect.gameObject.SetActive(meetingHallTarget != null);
            UpdateMeetingHallUI();
        }
    }

    private void UpdateMeetingHallUI()
    {
        if (!meetingHallGuidanceActive || !showMeetingHallHudAndMarker) return;

        bool hideDuringOtherUi = (phoneController != null && phoneController.IsPhoneOpen())
            || (dayNightCycle != null && dayNightCycle.pcUIPanel != null && dayNightCycle.pcUIPanel.activeInHierarchy)
            || BuildingPortal.IsTeleporting;

        if (destinationBannerRoot != null)
        {
            destinationBannerRoot.SetActive(!hideDuringOtherUi);
        }

        string label = string.IsNullOrWhiteSpace(meetingHallName) ? "회담장" : meetingHallName;
        Vector3 observerPos = GetObserverPosition();
        float distanceMeters = meetingHallTarget != null
            ? (navigationManager != null && navigationManager.SelectedPlace == meetingHallPlace && navigationManager.RemainingDistance > 0.1f
                ? navigationManager.RemainingDistance
                : Vector3.Distance(observerPos, meetingHallTarget.position))
            : 0f;

        string distText = distanceMeters >= 1000f
            ? $"{distanceMeters / 1000f:F1} km"
            : $"{Mathf.CeilToInt(distanceMeters)} m";

        if (destinationBannerTitle != null) destinationBannerTitle.text = label;
        if (destinationBannerDetail != null)
        {
            destinationBannerDetail.text = meetingHallTarget != null
                ? $"내비게이션 안내 중 · {distText}"
                : "목적지로 이동하세요";
        }

        // 3D 화면 마커 갱신
        if (targetMarkerRect == null || targetMarkerLabel == null) return;

        Camera activeCam = GetActiveCamera();
        if (hideDuringOtherUi || meetingHallTarget == null || activeCam == null)
        {
            targetMarkerRect.gameObject.SetActive(false);
            return;
        }

        targetMarkerRect.gameObject.SetActive(true);
        Vector3 worldPoint = meetingHallTarget.position + Vector3.up * 1.6f;
        Vector3 vp = activeCam.WorldToViewportPoint(worldPoint);
        bool behind = vp.z <= 0f;
        Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        if (behind) dir = -dir;
        if (behind && dir.sqrMagnitude < 0.001f) dir = Vector2.down;

        bool outside = behind || vp.x < 0.08f || vp.x > 0.92f || vp.y < 0.16f || vp.y > 0.82f;
        if (outside)
        {
            float edge = Mathf.Min(0.42f / Mathf.Max(0.001f, Mathf.Abs(dir.x)), 0.30f / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            vp.x = 0.5f + dir.x * edge;
            vp.y = 0.48f + dir.y * edge;
        }

        Vector2 anchor = new Vector2(Mathf.Clamp(vp.x, 0.08f, 0.92f), Mathf.Clamp(vp.y, 0.18f, 0.82f));
        targetMarkerRect.anchorMin = anchor;
        targetMarkerRect.anchorMax = anchor;
        targetMarkerRect.anchoredPosition = Vector2.zero;
        targetMarkerLabel.text = $"{label} · {distText}";
    }

    #endregion

    #region Control Locking & Scene Reference Helpers

    private void ResolveSceneReferences()
    {
        if (phoneController == null) phoneController = FindFirstObjectByType<PhoneController>(FindObjectsInactive.Include);
        if (navigationManager == null) navigationManager = FindFirstObjectByType<NavigationManager>(FindObjectsInactive.Include);
        if (navigationPlanner == null) navigationPlanner = FindFirstObjectByType<NavigationPlanner>(FindObjectsInactive.Include);
        if (playerCombat == null) playerCombat = FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
        if (dayNightCycle == null) dayNightCycle = FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        if (bikes == null || bikes.Length == 0) bikes = FindObjectsByType<MotorcycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private bool IsPlayerReadyToStart()
    {
        if (Time.timeScale <= 0f) return false;
        if (PrisonFeaturePolicy.Active) return false;
        if (BuildingPortal.IsTeleporting) return false;
        if (IsRidingAnyBike()) return false;
        if (IsPlayerInterrupted()) return false;
        if (dayNightCycle != null && dayNightCycle.pcUIPanel != null && dayNightCycle.pcUIPanel.activeInHierarchy) return false;
        var camLook = FindFirstObjectByType<CameraMove>();
        if (camLook != null && Cursor.lockState != CursorLockMode.Locked) return false;
        return true;
    }

    private bool IsPlayerInterrupted()
    {
        if (PoliceControl.IsArrestInProgress) return true;
        if (playerCombat != null && (playerCombat.IsDown || playerCombat.WaterRescueActive)) return true;
        return false;
    }

    private bool IsRidingAnyBike()
    {
        if (bikes == null) return false;
        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] != null && bikes[i].isDriven) return true;
        }
        return false;
    }

    private bool IsSequenceBusy()
    {
        return state == EventState.MingiMessage
            || state == EventState.DelayBeforeCall
            || state == EventState.Ringing
            || state == EventState.InCall;
    }

    private void SuspendNavigationPlanner()
    {
        if (plannerSuspended || navigationPlanner == null) return;
        plannerWasEnabled = navigationPlanner.enabled;
        navigationPlanner.enabled = false;
        plannerSuspended = true;
    }

    private void RestoreNavigationPlanner()
    {
        if (!plannerSuspended) return;
        if (navigationPlanner != null) navigationPlanner.enabled = plannerWasEnabled;
        plannerSuspended = false;
    }

    private void SuppressStoryInboxAutoOpen()
    {
        if (inboxAutoOpenSaved) return;
        var inbox = StoryPhoneInbox.Current;
        if (inbox == null) return;
        savedInboxAutoOpen = inbox.AutoOpenEnabled;
        inbox.AutoOpenEnabled = false;
        inboxAutoOpenSaved = true;
    }

    private void RestoreStoryInboxAutoOpen()
    {
        if (!inboxAutoOpenSaved) return;
        var inbox = StoryPhoneInbox.Current;
        if (inbox != null) inbox.AutoOpenEnabled = savedInboxAutoOpen;
        inboxAutoOpenSaved = false;
    }

    private void RestorePhoneRotation()
    {
        if (hasSavedPhoneRotation && phoneController != null && phoneController.VisualRoot != null)
        {
            phoneController.VisualRoot.localRotation = savedPhoneLocalRotation;
        }
        hasSavedPhoneRotation = false;
    }

    private void LockMessageInteractions()
    {
        if (messageInteractionsLocked) return;
        GameObject playerObj = FindPlayerGameObject();
        if (playerObj == null) return;

        messageInteractionsLocked = true;
        suspendedMessageInteractions.Clear();
        foreach (var behaviour in playerObj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is PlayerInteraction || behaviour is PlayerInteract)
            {
                suspendedMessageInteractions[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }
    }

    private void UnlockMessageInteractions()
    {
        if (!messageInteractionsLocked) return;
        foreach (var pair in suspendedMessageInteractions)
        {
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        }
        suspendedMessageInteractions.Clear();
        messageInteractionsLocked = false;
    }

    private void LockCallControls()
    {
        if (callControlsLocked) return;
        GameObject playerObj = FindPlayerGameObject();
        if (playerObj == null) return;

        callControlsLocked = true;
        suspendedBehaviours.Clear();

        foreach (var behaviour in playerObj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is PlayerMove
                || behaviour is PlayerMovement
                || behaviour is CameraMove
                || behaviour is PlayerInteraction
                || behaviour is PlayerInteract
                || behaviour is PlayerWeapon)
            {
                suspendedBehaviours[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }
    }

    private IEnumerator ReleaseCallControlsAfterKeyUp()
    {
        yield return null;
        while (IsKeyHeld(KeyCode.F, Key.F) || IsKeyHeld(KeyCode.Space, Key.Space))
        {
            yield return null;
        }
        UnlockCallControls();
        releaseControlRoutine = null;
    }

    private void UnlockCallControls()
    {
        if (!callControlsLocked) return;
        foreach (var pair in suspendedBehaviours)
        {
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        }
        suspendedBehaviours.Clear();
        callControlsLocked = false;
    }

    private GameObject FindPlayerGameObject()
    {
        if (playerCombat != null) return playerCombat.gameObject;
        var pm = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
        if (pm != null) return pm.gameObject;
        return GameObject.FindGameObjectWithTag("Player");
    }

    private Vector3 GetObserverPosition()
    {
        if (bikes != null)
        {
            for (int i = 0; i < bikes.Length; i++)
            {
                if (bikes[i] != null && bikes[i].isDriven) return bikes[i].transform.position;
            }
        }
        GameObject playerObj = FindPlayerGameObject();
        return playerObj != null ? playerObj.transform.position : Vector3.zero;
    }

    private Camera GetActiveCamera()
    {
        if (playerCombat != null && playerCombat.View != null && playerCombat.View.isActiveAndEnabled)
        {
            return playerCombat.View;
        }
        return Camera.main;
    }

    private void CleanupActiveSequence()
    {
        if (releaseControlRoutine != null)
        {
            StopCoroutine(releaseControlRoutine);
            releaseControlRoutine = null;
        }

        AnySequenceActive = false;
        isTyping = false;
        letterboxTarget = 0f;
        letterboxBlend = 0f;

        if (topLetterboxBar != null) topLetterboxBar.gameObject.SetActive(false);
        if (bottomLetterboxBar != null) bottomLetterboxBar.gameObject.SetActive(false);
        if (subtitleAreaRoot != null) subtitleAreaRoot.SetActive(false);
        if (bottomRightHintText != null) bottomRightHintText.gameObject.SetActive(false);
        if (mingiCardGroup != null) mingiCardGroup.gameObject.SetActive(false);
        if (ringPromptRoot != null) ringPromptRoot.SetActive(false);
        if (phoneScreenOverlay != null) phoneScreenOverlay.gameObject.SetActive(false);

        RestorePhoneRotation();
        RestoreNavigationPlanner();
        RestoreStoryInboxAutoOpen();
        UnlockMessageInteractions();
        UnlockCallControls();
    }

    private void OnDisable()
    {
        if (IsSequenceBusy())
        {
            CleanupActiveSequence();
        }
    }

    private void OnDestroy()
    {
        CleanupActiveSequence();
        if (overlayCanvasRoot != null) Destroy(overlayCanvasRoot);
        if (phoneScreenOverlay != null) Destroy(phoneScreenOverlay.gameObject);
        DestroyGeneratedClip(ref generatedChiefClip);
        DestroyGeneratedClip(ref generatedPlayerClip);
        DestroyGeneratedClip(ref generatedMingiClip);
        DestroyGeneratedClip(ref generatedChimeClip);
        DestroyGeneratedClip(ref generatedRingtoneClip);
        DestroyGeneratedClip(ref generatedPickupClip);
        DestroyGeneratedClip(ref generatedHangupClip);
    }

    private static void DestroyGeneratedClip(ref AudioClip clip)
    {
        if (clip != null)
        {
            Destroy(clip);
            clip = null;
        }
    }

    #endregion

    #region UI Construction & Animation

    private void UpdateLetterboxAnimation()
    {
        if (topLetterboxBar == null || bottomLetterboxBar == null) return;

        float speed = 6f;
        letterboxBlend = Mathf.MoveTowards(letterboxBlend, letterboxTarget, Time.unscaledDeltaTime * speed);
        float eased = Mathf.SmoothStep(0f, 1f, letterboxBlend);

        topLetterboxBar.sizeDelta = new Vector2(0f, 118f * eased);
        bottomLetterboxBar.sizeDelta = new Vector2(0f, 172f * eased);

        bool visible = eased > 0.005f;
        if (topLetterboxBar.gameObject.activeSelf != visible) topLetterboxBar.gameObject.SetActive(visible);
        if (bottomLetterboxBar.gameObject.activeSelf != visible) bottomLetterboxBar.gameObject.SetActive(visible);
    }

    private void UpdatePhoneVoiceBars()
    {
        if (phoneVoiceBars == null) return;
        wavePhase += Time.unscaledDeltaTime * (isTyping ? 16f : 3f);

        for (int i = 0; i < phoneVoiceBars.Length; i++)
        {
            if (phoneVoiceBars[i] == null) continue;
            float amp = isTyping
                ? 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(wavePhase + i * 0.9f))
                : 0.16f + 0.08f * Mathf.Abs(Mathf.Sin(wavePhase + i * 0.6f));
            RectTransform rt = phoneVoiceBars[i].rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 12f + 68f * amp);
        }
    }

    private void SetPhoneOverlayVisual(bool isInCall)
    {
        if (phoneScreenOverlay == null) return;

        if (phoneScreenStatusText != null) phoneScreenStatusText.text = isInCall ? "음성 통화" : "전화 수신 중";
        if (phoneScreenCallerText != null) phoneScreenCallerText.text = callerDisplayName;
        if (phoneScreenSubText != null) phoneScreenSubText.text = isInCall ? "통화 중  00:00" : "발신자 확인됨";

        if (phoneScreenButtonBg != null)
        {
            phoneScreenButtonBg.color = isInCall
                ? new Color(0.14f, 0.30f, 0.36f, 1f)
                : new Color(0.16f, 0.68f, 0.42f, 1f);
        }

        if (phoneScreenButtonText != null)
        {
            phoneScreenButtonText.text = isInCall ? "통화 연결됨" : "[F] 전화 받기";
        }

        if (phoneVoiceBars != null)
        {
            for (int i = 0; i < phoneVoiceBars.Length; i++)
            {
                if (phoneVoiceBars[i] != null) phoneVoiceBars[i].gameObject.SetActive(isInCall);
            }
        }
    }

    private void BuildOverlayUI()
    {
        if (overlayCanvasRoot != null) return;

        Font font = ResolveFont(uiFont);

        overlayCanvasRoot = new GameObject("ChiefPhoneCall_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = overlayCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = overlayCanvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = overlayCanvasRoot.transform;

        // 1. 위/아래 검은 레터박스 바
        topLetterboxBar = CreatePanel(root, "Top Letterbox", new Color(0f, 0f, 0f, 0.96f));
        topLetterboxBar.anchorMin = new Vector2(0f, 1f);
        topLetterboxBar.anchorMax = new Vector2(1f, 1f);
        topLetterboxBar.pivot = new Vector2(0.5f, 1f);
        topLetterboxBar.anchoredPosition = Vector2.zero;
        topLetterboxBar.sizeDelta = Vector2.zero;
        topLetterboxBar.gameObject.SetActive(false);

        bottomLetterboxBar = CreatePanel(root, "Bottom Letterbox", new Color(0f, 0f, 0f, 0.96f));
        bottomLetterboxBar.anchorMin = new Vector2(0f, 0f);
        bottomLetterboxBar.anchorMax = new Vector2(1f, 0f);
        bottomLetterboxBar.pivot = new Vector2(0.5f, 0f);
        bottomLetterboxBar.anchoredPosition = Vector2.zero;
        bottomLetterboxBar.sizeDelta = Vector2.zero;
        bottomLetterboxBar.gameObject.SetActive(false);

        // 2. 통화 자막 영역 (하단 레터박스 위 중앙)
        var subRect = CreateRect(root, "Subtitle Area");
        subRect.anchorMin = new Vector2(0.5f, 0f);
        subRect.anchorMax = new Vector2(0.5f, 0f);
        subRect.pivot = new Vector2(0.5f, 0f);
        subRect.anchoredPosition = new Vector2(0f, 24f);
        subRect.sizeDelta = new Vector2(1380f, 134f);
        subtitleAreaRoot = subRect.gameObject;

        subtitleSpeakerText = CreateLabel(subRect, "Speaker", font, 28, TextAnchor.UpperCenter, new Color(0.75f, 0.90f, 0.71f));
        subtitleSpeakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitleSpeakerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleSpeakerText.rectTransform.pivot = new Vector2(0.5f, 1f);
        subtitleSpeakerText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        subtitleSpeakerText.rectTransform.sizeDelta = new Vector2(0f, 38f);
        AddShadow(subtitleSpeakerText);

        subtitleBodyText = CreateLabel(subRect, "Body", font, 34, TextAnchor.UpperCenter, new Color(1f, 0.96f, 0.90f));
        subtitleBodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
        subtitleBodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleBodyText.rectTransform.offsetMin = new Vector2(20f, 6f);
        subtitleBodyText.rectTransform.offsetMax = new Vector2(-20f, -42f);
        subtitleBodyText.lineSpacing = 1.18f;
        AddShadow(subtitleBodyText);
        subtitleAreaRoot.SetActive(false);

        // 3. 우하단 조작 안내 (프로젝트 Subtitle baseline 준수)
        bottomRightHintText = CreateLabel(root, "Subtitle Controls Hint", font, 22, TextAnchor.LowerRight, new Color(0.86f, 0.89f, 0.92f, 0.92f));
        bottomRightHintText.rectTransform.anchorMin = new Vector2(1f, 0f);
        bottomRightHintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottomRightHintText.rectTransform.pivot = new Vector2(1f, 0f);
        bottomRightHintText.rectTransform.anchoredPosition = new Vector2(-36f, 20f);
        bottomRightHintText.rectTransform.sizeDelta = new Vector2(520f, 38f);
        bottomRightHintText.text = "F [다음 대사] / Space [건너뛰기]";
        AddShadow(bottomRightHintText);
        bottomRightHintText.gameObject.SetActive(false);

        // 4. 민기 문자 카드 (하단 중앙 살짝 위)
        var cardRect = CreatePanel(root, "Mingi Message Card", new Color(0.05f, 0.08f, 0.12f, 0.92f));
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 96f);
        cardRect.sizeDelta = new Vector2(920f, 174f);
        mingiCardGroup = cardRect.gameObject.AddComponent<CanvasGroup>();
        mingiCardGroup.blocksRaycasts = false;
        mingiCardGroup.interactable = false;

        var accent = CreatePanel(cardRect, "Accent Bar", new Color(1f, 0.82f, 0.55f, 1f));
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(8f, 0f);

        mingiHeaderText = CreateLabel(cardRect, "Header", font, 24, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.55f));
        mingiHeaderText.rectTransform.anchorMin = new Vector2(0f, 1f);
        mingiHeaderText.rectTransform.anchorMax = new Vector2(1f, 1f);
        mingiHeaderText.rectTransform.pivot = new Vector2(0f, 1f);
        mingiHeaderText.rectTransform.anchoredPosition = new Vector2(28f, -18f);
        mingiHeaderText.rectTransform.sizeDelta = new Vector2(-56f, 34f);

        mingiBodyText = CreateLabel(cardRect, "Body", font, 30, TextAnchor.UpperLeft, new Color(1f, 0.97f, 0.90f));
        mingiBodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
        mingiBodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        mingiBodyText.rectTransform.offsetMin = new Vector2(28f, 40f);
        mingiBodyText.rectTransform.offsetMax = new Vector2(-28f, -56f);
        mingiBodyText.lineSpacing = 1.15f;

        mingiHintText = CreateLabel(cardRect, "Hint", font, 20, TextAnchor.LowerRight, new Color(0.72f, 0.78f, 0.84f));
        mingiHintText.rectTransform.anchorMin = new Vector2(0f, 0f);
        mingiHintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        mingiHintText.rectTransform.pivot = new Vector2(1f, 0f);
        mingiHintText.rectTransform.anchoredPosition = new Vector2(-24f, 12f);
        mingiHintText.rectTransform.sizeDelta = new Vector2(-48f, 28f);

        mingiCardGroup.gameObject.SetActive(false);

        // 5. 전화 수신 프롬프트 (하단 중앙)
        var ringRect = CreatePanel(root, "Incoming Call Prompt", new Color(0.05f, 0.10f, 0.14f, 0.90f));
        ringRect.anchorMin = new Vector2(0.5f, 0f);
        ringRect.anchorMax = new Vector2(0.5f, 0f);
        ringRect.pivot = new Vector2(0.5f, 0f);
        ringRect.anchoredPosition = new Vector2(0f, 110f);
        ringRect.sizeDelta = new Vector2(460f, 64f);
        ringPromptRoot = ringRect.gameObject;

        ringPromptText = CreateLabel(ringRect, "Prompt", font, 28, TextAnchor.MiddleCenter, new Color(0.75f, 0.95f, 0.78f));
        StretchFull(ringPromptText.rectTransform);
        AddShadow(ringPromptText);
        ringPromptRoot.SetActive(false);

        // 6. 회담장 목적지 상단 중앙 배너
        var bannerRect = CreatePanel(root, "Meeting Hall Banner", new Color(0.04f, 0.08f, 0.12f, 0.88f));
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -24f);
        bannerRect.sizeDelta = new Vector2(400f, 84f);
        destinationBannerRoot = bannerRect.gameObject;

        var bannerAccent = CreatePanel(bannerRect, "Top Accent", new Color(1f, 0.52f, 0.18f, 1f));
        bannerAccent.anchorMin = new Vector2(0f, 1f);
        bannerAccent.anchorMax = new Vector2(1f, 1f);
        bannerAccent.pivot = new Vector2(0.5f, 1f);
        bannerAccent.anchoredPosition = Vector2.zero;
        bannerAccent.sizeDelta = new Vector2(0f, 4f);

        destinationBannerTitle = CreateLabel(bannerRect, "Title", font, 30, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.88f));
        destinationBannerTitle.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        destinationBannerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        destinationBannerTitle.rectTransform.offsetMin = new Vector2(12f, 0f);
        destinationBannerTitle.rectTransform.offsetMax = new Vector2(-12f, -6f);

        destinationBannerDetail = CreateLabel(bannerRect, "Detail", font, 20, TextAnchor.MiddleCenter, new Color(1f, 0.64f, 0.30f));
        destinationBannerDetail.rectTransform.anchorMin = new Vector2(0f, 0f);
        destinationBannerDetail.rectTransform.anchorMax = new Vector2(1f, 0.48f);
        destinationBannerDetail.rectTransform.offsetMin = new Vector2(12f, 6f);
        destinationBannerDetail.rectTransform.offsetMax = new Vector2(-12f, 0f);
        destinationBannerRoot.SetActive(false);

        // 7. 회담장 3D 위치 마커 (주황 다이아몬드 + 회담장 · 거리)
        targetMarkerRect = CreateRect(root, "Meeting Hall Marker");
        targetMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        targetMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        targetMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        targetMarkerRect.sizeDelta = new Vector2(260f, 86f);

        var diamondRect = CreateRect(targetMarkerRect, "Diamond");
        diamondRect.anchorMin = new Vector2(0.5f, 0.72f);
        diamondRect.anchorMax = new Vector2(0.5f, 0.72f);
        diamondRect.pivot = new Vector2(0.5f, 0.5f);
        diamondRect.sizeDelta = new Vector2(30f, 30f);
        var diamond = diamondRect.gameObject.AddComponent<StoryTargetDiamond>();
        diamond.color = new Color(1f, 0.47f, 0.12f, 1f);
        diamond.raycastTarget = false;
        AddShadow(diamond);

        targetMarkerLabel = CreateLabel(targetMarkerRect, "Label", font, 22, TextAnchor.LowerCenter, new Color(1f, 0.97f, 0.90f));
        StretchFull(targetMarkerLabel.rectTransform);
        AddShadow(targetMarkerLabel);
        targetMarkerRect.gameObject.SetActive(false);
    }

    private void BuildPhoneScreenOverlay()
    {
        if (phoneScreenOverlay != null) return;

        RawImage display = null;
        var rawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i] != null && rawImages[i].name == "NavigationFinalDisplay")
            {
                display = rawImages[i];
                break;
            }
        }

        if (display == null) return;

        Font font = ResolveFont(uiFont);
        int layer = display.gameObject.layer;

        var rootObj = new GameObject("ChiefCall_PhoneScreen", typeof(RectTransform), typeof(Image));
        rootObj.layer = layer;
        rootObj.transform.SetParent(display.rectTransform, false);

        phoneScreenOverlay = (RectTransform)rootObj.transform;
        phoneScreenOverlay.anchorMin = new Vector2(0f, 1f);
        phoneScreenOverlay.anchorMax = new Vector2(0f, 1f);
        phoneScreenOverlay.pivot = new Vector2(0f, 1f);
        phoneScreenOverlay.anchoredPosition = Vector2.zero;
        phoneScreenOverlay.sizeDelta = new Vector2(540f, 960f);

        float scaleX = display.rectTransform.rect.width > 1f ? display.rectTransform.rect.width / 540f : 1f;
        float scaleY = display.rectTransform.rect.height > 1f ? display.rectTransform.rect.height / 960f : 1f;
        phoneScreenOverlay.localScale = new Vector3(scaleX, scaleY, 1f);

        var bg = rootObj.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.10f, 0.15f, 1f);
        bg.raycastTarget = false;

        phoneScreenStatusText = CreatePhoneLabel(phoneScreenOverlay, "Status", font, "전화 수신 중", 40f, 110f, 460f, 46f, 30, new Color(0.64f, 0.78f, 0.82f), layer);
        phoneScreenStatusText.alignment = TextAnchor.MiddleCenter;

        // 발신자 배지
        var badge = CreatePhoneBox(phoneScreenOverlay, "Caller Badge", 180f, 200f, 180f, 180f, new Color(0.14f, 0.25f, 0.31f, 1f), layer);
        var badgeText = CreatePhoneLabel(badge, "Initial", font, "실", 0f, 0f, 180f, 180f, 82, new Color(0.75f, 0.90f, 0.71f), layer);
        badgeText.alignment = TextAnchor.MiddleCenter;

        phoneScreenCallerText = CreatePhoneLabel(phoneScreenOverlay, "Caller", font, callerDisplayName, 40f, 415f, 460f, 76f, 56, Color.white, layer);
        phoneScreenCallerText.alignment = TextAnchor.MiddleCenter;

        phoneScreenSubText = CreatePhoneLabel(phoneScreenOverlay, "Sub", font, "발신자 확인됨", 40f, 500f, 460f, 46f, 28, new Color(0.75f, 0.84f, 0.86f), layer);
        phoneScreenSubText.alignment = TextAnchor.MiddleCenter;

        // 통화 중 음성 파형 바
        phoneVoiceBars = new Image[7];
        for (int i = 0; i < phoneVoiceBars.Length; i++)
        {
            var barObj = new GameObject("WaveBar_" + i, typeof(RectTransform), typeof(Image));
            barObj.layer = layer;
            barObj.transform.SetParent(phoneScreenOverlay, false);
            var barRect = (RectTransform)barObj.transform;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(150f + i * 40f, -620f);
            barRect.sizeDelta = new Vector2(18f, 28f);

            var barImg = barObj.GetComponent<Image>();
            barImg.color = new Color(0.45f, 0.84f, 0.68f, 0.95f);
            barImg.raycastTarget = false;
            barObj.SetActive(false);
            phoneVoiceBars[i] = barImg;
        }

        // 하단 수락/상태 버튼
        var btnRect = CreatePhoneBox(phoneScreenOverlay, "Answer Button", 64f, 730f, 412f, 92f, new Color(0.16f, 0.68f, 0.42f, 1f), layer);
        phoneScreenButtonBg = btnRect.GetComponent<Image>();
        phoneScreenButtonText = CreatePhoneLabel(btnRect, "Answer Text", font, "[F] 전화 받기", 0f, 0f, 412f, 92f, 36, Color.white, layer);
        phoneScreenButtonText.alignment = TextAnchor.MiddleCenter;

        phoneScreenOverlay.gameObject.SetActive(false);
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var rt = CreateRect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private static Text CreateLabel(Transform parent, string name, Font font, int size, TextAnchor alignment, Color color)
    {
        var rt = CreateRect(parent, name);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static RectTransform CreatePhoneBox(Transform parent, string name, float x, float y, float w, float h, Color color, int layer)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private static Text CreatePhoneLabel(Transform parent, string name, Font font, string text, float x, float y, float w, float h, int size, Color color, int layer)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddShadow(Graphic target)
    {
        var shadow = target.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static Font ResolveFont(Font overrideFont)
    {
        if (overrideFont != null) return overrideFont;
        try
        {
            var jua = MokUIFont.Font;
            if (jua != null) return jua;
        }
        catch { }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static string DefaultSpeakerLabel(SpeakerType speaker)
    {
        switch (speaker)
        {
            case SpeakerType.Chief: return "실장";
            case SpeakerType.Mingi: return "민기";
            default: return "플레이어";
        }
    }

    private static Color SpeakerColor(SpeakerType speaker, string label)
    {
        if (speaker == SpeakerType.Chief || (!string.IsNullOrEmpty(label) && label.StartsWith("실장")))
            return new Color(0.749f, 0.898f, 0.710f); // #BFE5B5
        if (speaker == SpeakerType.Mingi || (!string.IsNullOrEmpty(label) && label.StartsWith("민기")))
            return new Color(1.000f, 0.820f, 0.545f); // #FFD18B
        return new Color(0.616f, 0.855f, 1.000f);     // #9DDAFF
    }

    #endregion

    #region Procedural Audio Synthesis (Soft Voice Blips & Phone SFX)

    private void SetupAudioSources()
    {
        voiceAudioSource = gameObject.AddComponent<AudioSource>();
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.spatialBlend = 0f;

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.spatialBlend = 0f;
    }

    private void GenerateFallbackAudioClips()
    {
        // 날카로운 전자음 대신 말소리 리듬감이 느껴지는 부드러운 보이스 블립
        generatedChiefClip = CreateVoiceBlipClip("ChiefVoiceBlip", 172f, 0.058f, 0.42f, 0.16f, -14f);
        generatedPlayerClip = CreateVoiceBlipClip("PlayerVoiceBlip", 228f, 0.052f, 0.30f, 0.10f, -8f);
        generatedMingiClip = CreateVoiceBlipClip("MingiVoiceBlip", 278f, 0.050f, 0.24f, 0.07f, 10f);

        generatedChimeClip = CreateMessageChimeClip();
        generatedRingtoneClip = CreateRingtoneClip();
        generatedPickupClip = CreateToneBurstClip("PhonePickup", 520f, 760f, 0.09f);
        generatedHangupClip = CreateToneBurstClip("PhoneHangup", 460f, 290f, 0.16f);
    }

    private static AudioClip CreateVoiceBlipClip(string clipName, float baseFreq, float duration, float harmonic2, float harmonic3, float freqDrift)
    {
        const int sampleRate = 44100;
        int count = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float progress = (float)i / count;
            float freq = baseFreq + freqDrift * progress;
            phase += 2f * Mathf.PI * freq / sampleRate;

            // 부드러운 어택과 자연스러운 감쇠
            float attack = Mathf.Clamp01(progress / 0.14f);
            float decay = Mathf.Pow(1f - progress, 1.8f);
            float env = attack * decay;

            float wave = Mathf.Sin(phase)
                + harmonic2 * Mathf.Sin(phase * 2f)
                + harmonic3 * Mathf.Sin(phase * 3f);

            // 소프트 클리핑으로 따뜻한 톤 유지
            samples[i] = Mathf.Clamp((float)Math.Tanh(wave * 0.65f) * env * 0.55f, -0.9f, 0.9f);
        }

        var clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMessageChimeClip()
    {
        const int sampleRate = 22050;
        float duration = 0.42f;
        int count = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;
            if (t < 0.22f)
            {
                float e = Mathf.Clamp01(t / 0.015f) * Mathf.Pow(1f - t / 0.22f, 2f);
                val += Mathf.Sin(2f * Mathf.PI * 660f * t) * e * 0.35f;
            }
            if (t >= 0.13f)
            {
                float nt = t - 0.13f;
                float e = Mathf.Clamp01(nt / 0.015f) * Mathf.Pow(1f - nt / 0.29f, 2f);
                val += Mathf.Sin(2f * Mathf.PI * 880f * nt) * e * 0.38f;
            }
            samples[i] = Mathf.Clamp(val, -0.8f, 0.8f);
        }

        var clip = AudioClip.Create("MingiMessageChime", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateRingtoneClip()
    {
        const int sampleRate = 22050;
        float duration = 1.05f;
        int count = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float pulse = (t < 0.42f) ? t / 0.42f : (t >= 0.56f && t < 0.98f) ? (t - 0.56f) / 0.42f : -1f;
            if (pulse < 0f) continue;

            float env = Mathf.Sin(pulse * Mathf.PI);
            float tremolo = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 16f * t);
            float tone = (Mathf.Sin(2f * Mathf.PI * 440f * t) + 0.85f * Mathf.Sin(2f * Mathf.PI * 480f * t)) * 0.24f;
            samples[i] = tone * env * tremolo;
        }

        var clip = AudioClip.Create("ChiefPhoneRingtone", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateToneBurstClip(string name, float startFreq, float endFreq, float duration)
    {
        const int sampleRate = 22050;
        int count = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float p = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, p);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float env = Mathf.Sin(p * Mathf.PI);
            samples[i] = Mathf.Sin(phase) * env * 0.32f;
        }

        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion

    #region Input Helpers

    private static bool IsKeyPressedThisFrame(KeyCode legacyKey, Key inputSystemKey)
    {
        var kb = Keyboard.current;
        if (kb != null && kb[inputSystemKey] != null && kb[inputSystemKey].wasPressedThisFrame) return true;
        try { return Input.GetKeyDown(legacyKey); } catch { return false; }
    }

    private static bool IsKeyHeld(KeyCode legacyKey, Key inputSystemKey)
    {
        var kb = Keyboard.current;
        if (kb != null && kb[inputSystemKey] != null && kb[inputSystemKey].isPressed) return true;
        try { return Input.GetKey(legacyKey); } catch { return false; }
    }

    #endregion
}
