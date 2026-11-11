using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Game System")]
    public int currentDay = 1;
    public bool isShopOpen = false;
    public bool storyDayNightEnabled = false;
    public bool IsNightPreparation => !DeliveryService.Mok.StoryTutorial.Active;
    public bool IsTransitioning => isTransitioning;
    public MonoBehaviour cameraLookScript;

    public float dailyIncome = 0f;
    public float dailyExpense = 0f;

    [Header("Player Respawn")]
    public GameObject player;
    public Transform spawnPoint;

    [Header("UI Elements")]
    public GameObject pcUIPanel;
    public GameObject openShopButton;
    public GameObject closeShopButton;
    public GameObject summaryPanel;
    public Text summaryIncomeText;
    public Text summaryExpenseText;
    public Text dayText;
    public Text timeText;

    [Header("Fade Effect")]
    public Image fadeScreen;
    public float fadeDuration = 1.5f;

    [Header("Motorcycle System")]
    public MotorcycleController motorcycle;
    public Transform bikeSpawnPoint;

    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float currentTime = 6f;
    public float timeMultiplier = 108f;

    [Header("Lights")]
    public Light sun;
    public Light moon;

    [Header("Intensity Curves")]
    public AnimationCurve sunIntensity;
    public AnimationCurve moonIntensity;

    [Header("Skydome")]
    public MeshRenderer skydomeRenderer;
    public Color daySkyColor = Color.white;
    public Color nightSkyColor = new Color(0.05f, 0.05f, 0.1f);

    private float defaultSunIntensity;
    private float defaultMoonIntensity;
    private Material skydomeMaterial;

    private bool isTransitioning = false;

    private void Start()
    {
        if (sun != null) defaultSunIntensity = sun.intensity;
        if (moon != null) defaultMoonIntensity = moon.intensity;
        if (skydomeRenderer != null) skydomeMaterial = skydomeRenderer.material;

        InitializeDay();
    }

    // 날짜 대신 스토리 진행으로 미션을 해금한다. 시간 값은 조명 호환용이다.
    private void Update()
    {
        isShopOpen = DeliveryService.Mok.StoryTutorial.Active;
        HideCalendarUI();
        if (PlayerPrefs.HasKey("PendingToppingList")) ToppingStockManager.DeliverAllPendingStock();
        UpdateLighting();
    }

    private void HideCalendarUI()
    {
        if (dayText) dayText.gameObject.SetActive(false);
        if (timeText) timeText.gameObject.SetActive(false);
        if (openShopButton) openShopButton.SetActive(false);
        if (closeShopButton) closeShopButton.SetActive(false);
        if (summaryPanel) summaryPanel.SetActive(false);
    }

    public void OnClick_OpenShop() { isShopOpen = DeliveryService.Mok.StoryTutorial.Active; ClosePCMenu(); }
    public void OnClick_CloseShop() { EnterNightPreparation(); ClosePCMenu(); }
    public void OnClick_ConfirmClose() { EnterNightPreparation(); ClosePCMenu(); }
    public void OnClick_CancelClose() { HideCalendarUI(); ClosePCMenu(); }

    public void EnterNightPreparation()
    {
        if (DeliveryService.Mok.StoryTutorial.Active) return;
        currentTime = 18f;
        isShopOpen = false;
        HideCalendarUI();
        FindFirstObjectByType<OrderManager>()?.CloseOrderReception();
    }

    private void ClosePCMenu()
    {
        if (pcUIPanel != null) pcUIPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = true;
        }
    }

    private void InitializeDay()
    {
        // 초기화 API는 기존 씬 참조를 위해 유지하고 정산/날짜 증가는 실행하지 않는다.
        isTransitioning = false;
        isShopOpen = DeliveryService.Mok.StoryTutorial.Active;
        HideCalendarUI();
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        float sunRotation = ((currentTime / 24f) * 360f) - 90f;
        float currentSunEval = sunIntensity.Evaluate(currentTime / 24f);

        if (sun != null)
        {
            sun.transform.localRotation = Quaternion.Euler(sunRotation, 170f, 0f);
            sun.intensity = defaultSunIntensity * currentSunEval;
        }
        if (moon != null)
        {
            moon.transform.localRotation = Quaternion.Euler(sunRotation - 180f, 170f, 0f);
            moon.intensity = defaultMoonIntensity * moonIntensity.Evaluate(currentTime / 24f);
        }
        if (sun != null && defaultSunIntensity > 0)
        {
            RenderSettings.ambientIntensity = sun.intensity / defaultSunIntensity;
        }
        if (skydomeMaterial != null)
        {
            Color currentColor = Color.Lerp(nightSkyColor, daySkyColor, currentSunEval);
            if (skydomeMaterial.HasProperty("_Color")) skydomeMaterial.color = currentColor;
            if (skydomeMaterial.HasProperty("_BaseColor")) skydomeMaterial.SetColor("_BaseColor", currentColor);
            if (skydomeMaterial.HasProperty("_TintColor")) skydomeMaterial.SetColor("_TintColor", currentColor);
            if (skydomeMaterial.HasProperty("_EmissionColor")) skydomeMaterial.SetColor("_EmissionColor", currentColor);
        }
    }
}