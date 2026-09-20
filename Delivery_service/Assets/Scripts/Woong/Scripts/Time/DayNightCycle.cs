using System.Collections;  
using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Game System")]
    public int currentDay = 1;
    public bool isShopOpen = false;

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
    public Text dayText;// 이거 날짜임
    public Text timeText; // 시간 

    [Header("Fade Effect")]
    public Image fadeScreen;             
    public float fadeDuration = 1.5f;    

    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float currentTime = 6f;
    public float timeMultiplier = 1000f;

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

            if (currentTime >= 24f && !isTransitioning)
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
            int hours = Mathf.FloorToInt(currentTime);
            int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);
            timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
    }
    public void OnClick_OpenShop()
    {
        if (isShopOpen || isTransitioning) return;

        isShopOpen = true;
        openShopButton.SetActive(false);
        closeShopButton.SetActive(true);
        ClosePCMenu();
    }

    public void OnClick_CloseShop()
    {
        if (!isShopOpen || isTransitioning) return;

        isShopOpen = false;
        closeShopButton.SetActive(false);
        pcUIPanel.SetActive(false);
        summaryPanel.SetActive(true);

        if (MoneyManager.Instance != null)
        {
            if (summaryIncomeText != null) summaryIncomeText.text = $"오늘의 수익: {MoneyManager.Instance.dailyIncome}원";
            if (summaryExpenseText != null) summaryExpenseText.text = $"오늘의 지출: {MoneyManager.Instance.dailyTotalExpense}원";
        }
    }

    public void OnClick_ConfirmClose()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(TransitionToNextDay());
    }

    public void OnClick_CancelClose()
    {
        summaryPanel.SetActive(false);
        closeShopButton.SetActive(true);
        pcUIPanel.SetActive(true);
        isShopOpen = true;
    }


    private void ClosePCMenu()
    {
        if (pcUIPanel != null) pcUIPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        if (pcUIPanel != null) pcUIPanel.SetActive(false);
        openShopButton.SetActive(true);
        closeShopButton.SetActive(false);
        summaryPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dayText != null) dayText.text = $"DAY {currentDay}";

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