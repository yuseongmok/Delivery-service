using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DeliveryService.Mok;

/// <summary>
/// 같은 씬 내부의 두 건물/지점(시작지 ↔ 도착지)을 페이드 아웃/인으로 자연스럽게 오가는 양방향 포탈 스크립트.
/// 시작지 포탈과 도착지 포탈에 각각 붙이고 서로 destinationPortal로 연결하면 왕복 이동이 가능합니다.
/// </summary>
public class BuildingPortal : MonoBehaviour
{
    [Header("1. 포탈 목적지 연결 (양방향 왕복)")]
    [Tooltip("이동할 반대편 BuildingPortal을 연결하세요. 서로 연결하면 시작지 ↔ 도착지를 오갈 수 있습니다.")]
    public BuildingPortal destinationPortal;

    [Tooltip("다른 포탈에서 이 포탈로 도착했을 때 플레이어가 서게 될 위치와 방향(Transform). 비우면 포탈 앞쪽을 자동 사용합니다.")]
    public Transform spawnPoint;

    [Tooltip("선택 사항: 반대편 BuildingPortal 없이 특정 좌표로 단방향 이동만 할 때 사용")]
    public Transform directDestination;

    [Header("2. 상호작용 UI 설정")]
    [Tooltip("포탈에 가까이 갔을 때 표시되는 안내 문구 (예: [F] 회담장 들어가기 / [F] 밖으로 돌아가기)")]
    public string promptText = "[F] 회담장 들어가기";

    [Tooltip("전화 완료 전 잠겨 있을 때 띄울 안내 문구 (비워 두면 안내를 숨깁니다)")]
    public string lockedPromptText = "";

    [Tooltip("선택 사항: 이 포탈로 이동 완료 후 화면 상단에 잠깐 표시할 장소 이름 (비우면 미표시)")]
    public string arrivalLocationBanner = "";

    [Min(0.5f)]
    [Tooltip("상호작용 UI가 뜨는 거리 (Trigger Collider가 없어도 이 거리 안에서 작동합니다)")]
    public float interactDistance = 3.0f;

    [Tooltip("체크하면 이 오브젝트에 붙은 IsTrigger Collider 안에 들어왔을 때만 작동합니다.")]
    public bool useTriggerColliderOnly = false;

    [Header("3. 실장 전화 & 내비게이션 연동")]
    [Tooltip("체크하면 실장 전화 이벤트가 끝난 뒤에만 포탈을 사용할 수 있습니다. (돌아가기 포탈은 체크 해제)")]
    public bool requirePhoneCallCompleted = false;

    [Tooltip("체크하면 이 포탈을 탈 때 '회담장' 내비게이션 안내와 목표 UI를 자동으로 종료합니다.")]
    public bool clearMeetingHallNavigationOnUse = false;

    [Tooltip("연동할 ChiefPhoneCallEvent (비워 두면 씬에서 자동으로 찾습니다)")]
    public ChiefPhoneCallEvent phoneCallEvent;

    [Header("4. 페이드 & 텔레포트 연출")]
    [Min(0.05f)] public float fadeOutDuration = 0.45f;
    [Min(0f)] public float blackHoldDuration = 0.15f;
    [Min(0.05f)] public float fadeInDuration = 0.45f;

    [Tooltip("도착 지점의 앞방향(forward)을 바라보도록 플레이어와 카메라 시선을 암전 중에 맞춥니다.")]
    public bool alignLookDirection = true;

    [Tooltip("포탈 이동 시 재생할 효과음 (선택 사항)")]
    public AudioClip teleportSound;

    [Range(0f, 1f)]
    public float teleportSoundVolume = 0.7f;

    [Header("5. 텔레포트 완료 이벤트 (선택 사항)")]
    public UnityEvent onTeleported;

    // 모든 포탈이 공유하는 상태 (동시 실행 / UI 중복 / 즉시 재진입 방지)
    public static bool IsTeleporting => sharedTeleporting;

    private static readonly List<BuildingPortal> allPortals = new List<BuildingPortal>();
    private static bool sharedTeleporting;
    private static float nextTeleportAllowedTime;
    private static BuildingPortal activePromptOwner;
    private static int promptEvaluatedFrame = -1;

    // 공용 UI 오브젝트
    private static GameObject sharedCanvasRoot;
    private static Image sharedFadeImage;
    private static GameObject sharedPromptRoot;
    private static Text sharedPromptText;
    private static CanvasGroup sharedBannerGroup;
    private static Text sharedBannerText;
    private static AudioSource sharedAudioSource;
    private static Coroutine sharedBannerRoutine;
    private static BuildingPortal sharedBannerOwner;

    private bool playerInsideTrigger;
    private PlayerMove cachedPlayerMove;
    private PlayerCombat cachedPlayerCombat;
    private PhoneController cachedPhone;
    private MotorcycleController[] cachedBikes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        allPortals.Clear();
        sharedTeleporting = false;
        nextTeleportAllowedTime = 0f;
        activePromptOwner = null;
        promptEvaluatedFrame = -1;
        sharedCanvasRoot = null;
        sharedFadeImage = null;
        sharedPromptRoot = null;
        sharedPromptText = null;
        sharedBannerGroup = null;
        sharedBannerText = null;
        sharedAudioSource = null;
        sharedBannerRoutine = null;
        sharedBannerOwner = null;
    }

    private void OnEnable()
    {
        if (!allPortals.Contains(this)) allPortals.Add(this);
    }

    private void OnDisable()
    {
        allPortals.Remove(this);
        playerInsideTrigger = false;
        if (activePromptOwner == this)
        {
            activePromptOwner = null;
            if (sharedPromptRoot != null) sharedPromptRoot.SetActive(false);
        }
    }

    private void Start()
    {
        EnsureSharedUI();
        ResolveReferences();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other)) playerInsideTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other)) playerInsideTrigger = false;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        ResolveReferences();

        // 프레임당 한 번만 가장 가까운 포탈을 선정하여 프롬프트 UI 충돌 방지
        if (promptEvaluatedFrame != Time.frameCount)
        {
            promptEvaluatedFrame = Time.frameCount;
            EvaluateClosestPortal();
        }

        if (activePromptOwner != this) return;
        if (sharedTeleporting || Time.unscaledTime < nextTeleportAllowedTime) return;
        if (ChiefPhoneCallEvent.AnySequenceActive) return;

        bool unlocked = IsUnlocked();
        bool ridingBike = IsPlayerRidingBike();

        UpdatePromptDisplay(unlocked, ridingBike);

        if (!unlocked || ridingBike) return;
        if (!CanPlayerInteractNow()) return;

        if (IsKeyPressedThisFrame(KeyCode.F, Key.F))
        {
            Transform targetTransform = ResolveDestinationTransform();
            if (targetTransform != null)
            {
                StartCoroutine(TeleportRoutine(targetTransform));
            }
            else
            {
                Debug.LogWarning($"[BuildingPortal] '{name}'에 이동할 목적지(destinationPortal 또는 directDestination)가 연결되지 않았습니다.", this);
            }
        }
    }

    private static void EvaluateClosestPortal()
    {
        activePromptOwner = null;

        if (sharedTeleporting || ChiefPhoneCallEvent.AnySequenceActive || Time.timeScale <= 0f)
        {
            if (sharedPromptRoot != null) sharedPromptRoot.SetActive(false);
            return;
        }

        BuildingPortal best = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < allPortals.Count; i++)
        {
            var portal = allPortals[i];
            if (portal == null || !portal.isActiveAndEnabled) continue;
            if (portal.ResolveDestinationTransform() == null) continue;
            if (!portal.IsUnlocked() && string.IsNullOrWhiteSpace(portal.lockedPromptText)) continue;

            if (portal.TryGetPlayerDistanceSq(out float distSq))
            {
                bool inRange = portal.useTriggerColliderOnly
                    ? portal.playerInsideTrigger
                    : (portal.playerInsideTrigger || distSq <= portal.interactDistance * portal.interactDistance);

                if (inRange && distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    best = portal;
                }
            }
        }

        activePromptOwner = best;
        if (best == null && sharedPromptRoot != null)
        {
            sharedPromptRoot.SetActive(false);
        }
    }

    private void UpdatePromptDisplay(bool unlocked, bool ridingBike)
    {
        EnsureSharedUI();
        if (sharedPromptRoot == null || sharedPromptText == null) return;

        bool show = !sharedTeleporting && !ChiefPhoneCallEvent.AnySequenceActive && CanShowPromptNow();
        if (!show)
        {
            sharedPromptRoot.SetActive(false);
            return;
        }

        if (!unlocked)
        {
            if (string.IsNullOrWhiteSpace(lockedPromptText))
            {
                sharedPromptRoot.SetActive(false);
                return;
            }
            sharedPromptText.text = lockedPromptText;
            sharedPromptText.color = new Color(0.75f, 0.78f, 0.82f, 1f);
        }
        else if (ridingBike)
        {
            sharedPromptText.text = "오토바이에서 내린 뒤 [F]로 이동하세요";
            sharedPromptText.color = new Color(1f, 0.80f, 0.45f, 1f);
        }
        else
        {
            sharedPromptText.text = string.IsNullOrWhiteSpace(promptText) ? "[F] 이동하기" : promptText;
            sharedPromptText.color = new Color(1f, 0.96f, 0.88f, 1f);
        }

        sharedPromptRoot.SetActive(true);
    }

    private IEnumerator TeleportRoutine(Transform target)
    {
        EnsureSharedUI();
        sharedTeleporting = true;
        if (sharedPromptRoot != null) sharedPromptRoot.SetActive(false);

        GameObject playerObj = FindPlayerObject();
        if (playerObj == null)
        {
            sharedTeleporting = false;
            yield break;
        }

        // 1. 이동 중 플레이어 조작 잠금 및 핸드폰 닫기
        var suspended = new Dictionary<Behaviour, bool>();
        foreach (var behaviour in playerObj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is PlayerMove
                || behaviour is PlayerMovement
                || behaviour is CameraMove
                || behaviour is PlayerInteraction
                || behaviour is PlayerInteract
                || behaviour is PlayerWeapon)
            {
                suspended[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }

        if (cachedPhone != null)
        {
            suspended[cachedPhone] = cachedPhone.enabled;
            cachedPhone.ClosePhone();
            cachedPhone.enabled = false;
        }

        if (teleportSound != null && sharedAudioSource != null)
        {
            float sfxScale = PlayerPrefs.GetFloat("Yun.Settings.SfxVolume", 1f);
            sharedAudioSource.PlayOneShot(teleportSound, teleportSoundVolume * sfxScale);
        }

        // 2. 페이드 아웃 (화면 암전)
        yield return FadeOverlay(0f, 1f, fadeOutDuration);

        // 3. 완전 암전 상태에서 좌표·시선 순간이동 (위화감 방지)
        Vector3 destPosition = GetArrivalPosition(target);
        Quaternion destRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

        var cc = playerObj.GetComponent<CharacterController>();
        bool ccWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        var rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (alignLookDirection)
        {
            playerObj.transform.SetPositionAndRotation(destPosition, destRotation);
        }
        else
        {
            playerObj.transform.position = destPosition;
        }

        Physics.SyncTransforms();

        if (alignLookDirection)
        {
            var camMove = playerObj.GetComponentInChildren<CameraMove>(true);
            if (camMove != null)
            {
                camMove.transform.localRotation = Quaternion.identity;
                camMove.SyncViewFromTransform();
            }
        }

        if (cc != null) cc.enabled = ccWasEnabled;

        // 도착지 트리거 상태 초기화
        playerInsideTrigger = false;
        if (destinationPortal != null) destinationPortal.playerInsideTrigger = false;

        // 회담장 입구 포탈을 탔을 때 내비게이션 및 목표 UI 정리
        if (clearMeetingHallNavigationOnUse && phoneCallEvent != null)
        {
            phoneCallEvent.ClearMeetingHallGuidance();
        }

        onTeleported?.Invoke();

        // 내부 라이팅/렌더링 안정화를 위한 짧은 암전 유지
        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
        }

        // 4. 페이드 인 (화면 밝아짐)
        string bannerText = !string.IsNullOrWhiteSpace(arrivalLocationBanner)
            ? arrivalLocationBanner
            : (destinationPortal != null ? destinationPortal.arrivalLocationBanner : "");

        if (!string.IsNullOrWhiteSpace(bannerText))
        {
            ShowArrivalBanner(bannerText);
        }

        yield return FadeOverlay(1f, 0f, fadeInDuration);

        // 5. F 키를 뗄 때까지 기다린 뒤 조작 복원 (즉시 되돌아가기 방지)
        while (IsKeyHeld(KeyCode.F, Key.F))
        {
            yield return null;
        }

        foreach (var pair in suspended)
        {
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        }

        nextTeleportAllowedTime = Time.unscaledTime + 0.6f;
        sharedTeleporting = false;
    }

    private static IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
    {
        if (sharedFadeImage == null) yield break;

        float safeDuration = Mathf.Max(0.05f, duration);
        sharedFadeImage.gameObject.SetActive(true);

        for (float t = 0f; t < safeDuration; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.SmoothStep(fromAlpha, toAlpha, t / safeDuration);
            sharedFadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        sharedFadeImage.color = new Color(0f, 0f, 0f, toAlpha);
        if (toAlpha <= 0.001f)
        {
            sharedFadeImage.gameObject.SetActive(false);
        }
    }

    private void ShowArrivalBanner(string locationName)
    {
        if (sharedBannerGroup == null || sharedBannerText == null) return;
        if (sharedBannerRoutine != null && sharedBannerOwner != null)
        {
            sharedBannerOwner.StopCoroutine(sharedBannerRoutine);
        }
        sharedBannerOwner = this;
        sharedBannerRoutine = StartCoroutine(ArrivalBannerRoutine(locationName));
    }

    private static IEnumerator ArrivalBannerRoutine(string locationName)
    {
        sharedBannerText.text = locationName;
        sharedBannerGroup.gameObject.SetActive(true);

        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            sharedBannerGroup.alpha = Mathf.SmoothStep(0f, 1f, t / 0.35f);
            yield return null;
        }
        sharedBannerGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(1.8f);

        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            sharedBannerGroup.alpha = Mathf.SmoothStep(1f, 0f, t / 0.5f);
            yield return null;
        }

        sharedBannerGroup.alpha = 0f;
        sharedBannerGroup.gameObject.SetActive(false);
        sharedBannerRoutine = null;
        sharedBannerOwner = null;
    }

    #region Helpers & UI

    private void ResolveReferences()
    {
        if (phoneCallEvent == null && (requirePhoneCallCompleted || clearMeetingHallNavigationOnUse))
        {
            phoneCallEvent = FindFirstObjectByType<ChiefPhoneCallEvent>(FindObjectsInactive.Include);
        }

        if (cachedPlayerMove == null) cachedPlayerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
        if (cachedPlayerCombat == null) cachedPlayerCombat = FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
        if (cachedPhone == null) cachedPhone = FindFirstObjectByType<PhoneController>(FindObjectsInactive.Include);
        if (cachedBikes == null || cachedBikes.Length == 0)
        {
            cachedBikes = FindObjectsByType<MotorcycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }

    private bool IsUnlocked()
    {
        if (!requirePhoneCallCompleted) return true;
        return phoneCallEvent != null && phoneCallEvent.IsCallCompleted;
    }

    private Transform ResolveDestinationTransform()
    {
        if (destinationPortal != null)
        {
            return destinationPortal.spawnPoint != null ? destinationPortal.spawnPoint : destinationPortal.transform;
        }
        return directDestination;
    }

    private Vector3 GetArrivalPosition(Transform target)
    {
        // 도착 포탈에 별도 spawnPoint를 지정하지 않았다면 포탈 앞쪽으로 1.2m 띄워 끼임 방지
        if (destinationPortal != null && destinationPortal.spawnPoint == null && target == destinationPortal.transform)
        {
            return target.position + target.forward * 1.2f;
        }
        return target.position;
    }

    private bool TryGetPlayerDistanceSq(out float distSq)
    {
        distSq = float.MaxValue;
        Vector3 pos;

        MotorcycleController riddenBike = GetRiddenBike();
        if (riddenBike != null)
        {
            pos = riddenBike.transform.position;
        }
        else
        {
            GameObject playerObj = FindPlayerObject();
            if (playerObj == null || !playerObj.activeInHierarchy) return false;
            pos = playerObj.transform.position;
        }

        Vector3 delta = pos - transform.position;
        if (Mathf.Abs(delta.y) > 3.2f) return false;
        distSq = delta.sqrMagnitude;
        return true;
    }

    private bool CanShowPromptNow()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return false;
        if (cachedPhone != null && cachedPhone.IsPhoneOpen()) return false;
        if (PoliceControl.IsArrestInProgress) return false;
        if (cachedPlayerCombat != null && (cachedPlayerCombat.IsDown || cachedPlayerCombat.WaterRescueActive)) return false;
        return true;
    }

    private bool CanPlayerInteractNow()
    {
        if (!CanShowPromptNow()) return false;
        if (cachedPlayerMove != null && (!cachedPlayerMove.enabled || !cachedPlayerMove.isControllable)) return false;
        return true;
    }

    private bool IsPlayerRidingBike()
    {
        return GetRiddenBike() != null;
    }

    private MotorcycleController GetRiddenBike()
    {
        if (cachedBikes == null) return null;
        for (int i = 0; i < cachedBikes.Length; i++)
        {
            if (cachedBikes[i] != null && cachedBikes[i].isDriven) return cachedBikes[i];
        }
        return null;
    }

    private GameObject FindPlayerObject()
    {
        if (cachedPlayerMove != null) return cachedPlayerMove.gameObject;
        if (cachedPlayerCombat != null) return cachedPlayerCombat.gameObject;
        return GameObject.FindGameObjectWithTag("Player");
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponentInParent<PlayerMove>() != null) return true;
        var bike = other.GetComponentInParent<MotorcycleController>();
        return bike != null && bike.isDriven;
    }

    private static void EnsureSharedUI()
    {
        if (sharedCanvasRoot != null) return;

        Font font = ResolveFont();

        sharedCanvasRoot = new GameObject("BuildingPortal_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = sharedCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32700;

        var scaler = sharedCanvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        sharedAudioSource = sharedCanvasRoot.AddComponent<AudioSource>();
        sharedAudioSource.playOnAwake = false;
        sharedAudioSource.spatialBlend = 0f;

        Transform root = sharedCanvasRoot.transform;

        // 1. 상호작용 프롬프트 ([F] 회담장 들어가기 / [F] 밖으로 돌아가기)
        var promptGo = new GameObject("Portal Prompt", typeof(RectTransform), typeof(Image));
        promptGo.transform.SetParent(root, false);
        var promptRect = (RectTransform)promptGo.transform;
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 128f);
        promptRect.sizeDelta = new Vector2(460f, 60f);

        var promptBg = promptGo.GetComponent<Image>();
        promptBg.color = new Color(0.05f, 0.09f, 0.13f, 0.88f);
        promptBg.raycastTarget = false;

        var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accentGo.transform.SetParent(promptRect, false);
        var accentRect = (RectTransform)accentGo.transform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(6f, 0f);
        var accentImg = accentGo.GetComponent<Image>();
        accentImg.color = new Color(1f, 0.54f, 0.20f, 1f);
        accentImg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(promptRect, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 0f);
        textRect.offsetMax = new Vector2(-18f, 0f);

        sharedPromptText = textGo.GetComponent<Text>();
        sharedPromptText.font = font;
        sharedPromptText.fontSize = 27;
        sharedPromptText.alignment = TextAnchor.MiddleCenter;
        sharedPromptText.color = new Color(1f, 0.96f, 0.88f, 1f);
        sharedPromptText.raycastTarget = false;

        var shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        sharedPromptRoot = promptGo;
        sharedPromptRoot.SetActive(false);

        // 2. 도착 구역명 배너 (선택 사항)
        var bannerGo = new GameObject("Arrival Banner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        bannerGo.transform.SetParent(root, false);
        var bannerRect = (RectTransform)bannerGo.transform;
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -124f);
        bannerRect.sizeDelta = new Vector2(380f, 64f);
        var bannerBg = bannerGo.GetComponent<Image>();
        bannerBg.color = new Color(0.04f, 0.08f, 0.12f, 0.86f);
        bannerBg.raycastTarget = false;

        sharedBannerGroup = bannerGo.GetComponent<CanvasGroup>();
        sharedBannerGroup.alpha = 0f;
        sharedBannerGroup.blocksRaycasts = false;
        sharedBannerGroup.interactable = false;

        var bannerTextGo = new GameObject("Banner Text", typeof(RectTransform), typeof(Text));
        bannerTextGo.transform.SetParent(bannerRect, false);
        var btRect = (RectTransform)bannerTextGo.transform;
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;
        sharedBannerText = bannerTextGo.GetComponent<Text>();
        sharedBannerText.font = font;
        sharedBannerText.fontSize = 30;
        sharedBannerText.alignment = TextAnchor.MiddleCenter;
        sharedBannerText.color = new Color(1f, 0.96f, 0.88f, 1f);
        sharedBannerText.raycastTarget = false;
        bannerGo.SetActive(false);

        // 3. 페이드 인/아웃용 전체 화면 암전 이미지 (최상단)
        var fadeGo = new GameObject("Fade Curtain", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(root, false);
        var fadeRect = (RectTransform)fadeGo.transform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        sharedFadeImage = fadeGo.GetComponent<Image>();
        sharedFadeImage.color = new Color(0f, 0f, 0f, 0f);
        sharedFadeImage.raycastTarget = true;
        fadeGo.SetActive(false);
    }

    private static Font ResolveFont()
    {
        try
        {
            var jua = MokUIFont.Font;
            if (jua != null) return jua;
        }
        catch { }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

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

    private void OnDrawGizmos()
    {
        Gizmos.color = requirePhoneCallCompleted ? new Color(1f, 0.55f, 0.15f, 0.6f) : new Color(0.2f, 0.85f, 0.75f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);

        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.35f);
            Gizmos.DrawRay(spawnPoint.position + Vector3.up * 0.5f, spawnPoint.forward * 1.4f);
        }

        Transform dest = ResolveDestinationTransform();
        if (dest != null)
        {
            Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.8f, dest.position + Vector3.up * 0.8f);
        }
    }

    #endregion
}
