using System.Collections;  
using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Game System")]
    public int currentDay = 1;
    public bool isShopOpen = false;
    public bool storyDayNightEnabled = false;
    public bool IsNightPreparation => storyDayNightEnabled && currentTime >= 18f;
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
    public Text dayText;// ¿Ã∞≈ ≥Ø¬•¿”
    public Text timeText; // Ω√∞£ 

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

    private void Update()
    {
        if (isShopOpen)
        {
            currentTime += (Time.deltaTime / 3600f) * timeMultiplier;

            if (storyDayNightEnabled && currentTime >= 18f && !DeliveryService.Mok.StoryTutorial.Active)
            {
                EnterNightPreparation();
            }
            else if (!storyDayNightEnabled && currentTime >= 24f && !isTransitioning)
            {
                isTransitioning = true;
                StartCoroutine(TransitionToNextDay());
            }
        }

        UpdateLighting();
        UpdateTimeUI();
    }

    private void UpdateTimeUI()
    {
        
        if (timeText != null)
        {
            if (currentTime >= 24f)
            {
                timeText.text = "00:00";
            }
            else
            {
                int hours = Mathf.FloorToInt(currentTime);
                int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);
                timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
            }
               
        }
    }
    public void OnClick_OpenShop()
    {
        if (isShopOpen || isTransitioning || IsNightPreparation) return;

        isShopOpen = true;
        openShopButton.SetActive(false);
        closeShopButton.SetActive(true);
        ClosePCMenu();
    }

    public void OnClick_CloseShop()
    {
        if (DeliveryService.Mok.StoryTutorial.Active) { DeliveryService.Mok.DeliveryRequestNotice.Show("ÎØºÍ∏∞: Ï≤´ Î∞∞Îã¨Î∂ÄÌÑ∞ ÎßàÏπòÏûê. ÎßàÍ∞êÏùÄ Í∑∏ Îã§ÏùåÏù¥Ïïº."); return; }
        if (!isShopOpen || isTransitioning) return;

        isShopOpen = false;
        closeShopButton.SetActive(false);
        pcUIPanel.SetActive(false);
        summaryPanel.SetActive(true);
        if (storyDayNightEnabled) DeliveryService.Mok.NightPreparationView.SetClosingLabels(this);

        if (MoneyManager.Instance != null)
        {
            if (summaryIncomeText != null) summaryIncomeText.text = $"ø¿¥√¿« ºˆ¿Õ: {MoneyManager.Instance.dailyIncome}ø¯";
            if (summaryExpenseText != null) summaryExpenseText.text = $"ø¿¥√¿« ¡ˆ√‚: {MoneyManager.Instance.dailyTotalExpense:N0}ø¯\n{DeliveryService.Mok.DailyRent.Preview(currentDay)}";
        }
    }

    public void OnClick_ConfirmClose()
    {
        if (DeliveryService.Mok.BrokerMission.Active) return;
        if (isTransitioning || DeliveryService.Mok.StoryTutorial.Active) return;
        if (storyDayNightEnabled && !IsNightPreparation) { EnterNightPreparation(); ClosePCMenu(); return; }

        isTransitioning = true;
        StartCoroutine(TransitionToNextDay());
    }

    public void OnClick_CancelClose()
    {
        if (IsNightPreparation || isTransitioning) return;
        summaryPanel.SetActive(false);
        closeShopButton.SetActive(true);
        pcUIPanel.SetActive(true);
        isShopOpen = true;
    }


    // ÎßàÍ∞êÏùÄ Ï†ïÏÇ∞Í≥º Îã§Ïùå ÎÇ† Ïù¥Îèô ÎåÄÏã† Î∞§ Ï§ÄÎπÑÎ•º Ïó∞Îã§. ÎπÑÏö©ÏùÄ Î∞§ÏùÑ ÎßàÏπ† Îïå Ìïú Î≤àÎßå Ï†ïÏÇ∞ÌïúÎã§.
    public void EnterNightPreparation()
    {
        if (!storyDayNightEnabled || isTransitioning || DeliveryService.Mok.StoryTutorial.Active) return;
        bool announce = isShopOpen || currentTime < 18f;
        currentTime = 18f;
        isShopOpen = false;
        if (openShopButton) openShopButton.SetActive(false);
        if (closeShopButton) closeShopButton.SetActive(false);
        if (summaryPanel) summaryPanel.SetActive(false);
        FindFirstObjectByType<OrderManager>()?.CloseOrderReception();
        if (announce) DeliveryService.Mok.DeliveryRequestNotice.Show("\ub0ae \uc601\uc5c5 \uc885\ub8cc \u00b7 \ud734\ub300\ud3f0 \ubb38\uc790\uc640 \ubbf8\uc158 \uc571\uc744 \ud655\uc778\ud558\uc138\uc694.");
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

    private IEnumerator TransitionToNextDay()
    {
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
        }

        // µŒ ∏∂∞® ∞Ê∑Œ ∏µŒ ¥Ÿ¿Ω ≥Ø √ ±‚»≠°§≥≥«∞°§∏ÆΩ∫∆˘ ¿¸ø° ∞ÀªÁ«—¥Ÿ.
        DeliveryService.Mok.LadderGame.Current?.CompleteRound();
        DeliveryService.Mok.LoanAccount.Current?.SettleDay(currentDay);
        DeliveryService.Mok.DailyRent.Current?.SettleDay(currentDay);
        yield return DeliveryService.Mok.Stocks.StockDebtPrompt.Resolve();
        if (DeliveryService.Mok.BankruptcyGameOver.TryEndDay(this)) yield break;

        currentDay++;
        dailyIncome = 0f;
        dailyExpense = 0f;
        InitializeDay();

        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
            fadeScreen.gameObject.SetActive(false);
        }
    }

    private void InitializeDay()
    {
        currentTime = 6f;
        isShopOpen = false;
        isTransitioning = false;

        ToppingStockManager.DeliverAllPendingStock();

        OrderManager orderManager = FindObjectOfType<OrderManager>();
        if (orderManager != null) orderManager.ResetOrdersForNewDay();

        NavigationManager navManager = FindObjectOfType<NavigationManager>();
        if (navManager != null) navManager.ResetNavigationForNewDay();

        if (pcUIPanel != null) pcUIPanel.SetActive(false);
        openShopButton.SetActive(true);
        closeShopButton.SetActive(false);
        summaryPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dayText != null) dayText.text = $"DAY {currentDay}";
        if (motorcycle != null)
        {
            if (motorcycle.isDriven)
            {
                motorcycle.ExitBike();
            }
            if (bikeSpawnPoint != null)
            {
                motorcycle.RespawnBike(bikeSpawnPoint);
            }
        }


        if (player != null && spawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

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