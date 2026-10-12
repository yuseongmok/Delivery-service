using UnityEngine;
using UnityEngine.UI;

public class WeatherManager : MonoBehaviour
{
    [Header("날씨 설정")]
    public bool isRaining = false;
    public int rainStartDay = 3;

    public bool isSnowing = false;
    public int snowStartDay = 5;

    [Header("시야 방해 (빗물 UI)")]
    public Image waterDropletUI;
    public float dropletFillRate = 0.5f;
    public float wipeSpeed = 5f;

    [Header("주행할 때 빗물 제거")]
    public float windClearThreshold = 15f;
    public float windClearRate = 1.0f;

    [Header("비 이펙트 및 사운드")]
    public ParticleSystem rainParticle;
    public AudioSource rainAudioSource;

    [Header("어지러움 효과 (눈덩이)")]
    public Image dizzyOverlayUI;

    [Header("참조 스크립트")]
    public DayNightCycle dayNightCycle;
    public MotorcycleController motorcycle;

    private float currentDropletAlpha = 0f;
    private bool isWiping = false;

    private float currentDizzyAlpha = 0f;
    private void Start()
    {
        if (waterDropletUI != null)
        {
            Color c = waterDropletUI.color;
            c.a = 0f;
            waterDropletUI.color = c;
            waterDropletUI.gameObject.SetActive(false);
        }
        if (dizzyOverlayUI != null)
        {
            Color c = dizzyOverlayUI.color;
            c.a = 0f;
            dizzyOverlayUI.color = c;
            dizzyOverlayUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (dayNightCycle != null && dayNightCycle.currentDay < rainStartDay)
        {
            if (dayNightCycle.currentDay < rainStartDay) isRaining = false;
            if (dayNightCycle.currentDay < snowStartDay) isSnowing = false;
        }
        if (motorcycle != null)
        {
            motorcycle.isSnowing = this.isSnowing;
        }

        HandleRainState();
        HandleWaterDroplets();
        HandleDizzyEffect();
    }
    public void TriggerDizzyEffect()
    {
        currentDizzyAlpha = 0.8f; // 화면을 확 흐리게 만듦
        if (dizzyOverlayUI != null) dizzyOverlayUI.gameObject.SetActive(true);
    }

    private void HandleDizzyEffect()
    {
        if (dizzyOverlayUI == null) return;

        if (currentDizzyAlpha > 0)
        {
            currentDizzyAlpha -= Time.deltaTime * 0.25f; // 서서히 회복
            Color c = dizzyOverlayUI.color;
            c.a = currentDizzyAlpha;
            dizzyOverlayUI.color = c;
        }
        else if (dizzyOverlayUI.gameObject.activeSelf)
        {
            dizzyOverlayUI.gameObject.SetActive(false);
        }
    }

    private void HandleRainState()
    {
        if (isRaining)
        {
            if (rainParticle != null && !rainParticle.isPlaying) rainParticle.Play();
            if (rainAudioSource != null && !rainAudioSource.isPlaying) rainAudioSource.Play();
        }
        else
        {
            if (rainParticle != null && rainParticle.isPlaying) rainParticle.Stop();
            if (rainAudioSource != null && rainAudioSource.isPlaying) rainAudioSource.Stop();
        }
    }

    private void HandleWaterDroplets()
    {
        if (waterDropletUI == null) return;

        bool isRiding = (motorcycle != null && motorcycle.isDriven); 
        
        if (isRaining && isRiding)
        {
            if (!waterDropletUI.gameObject.activeSelf)
            {
                waterDropletUI.gameObject.SetActive(true);
            }
        }

        if (isRiding)
        {
            if (Input.GetKeyDown(KeyCode.R) && currentDropletAlpha > 0f)
            {
                isWiping = true;
            }

            float currentBikeSpeed = Mathf.Abs(motorcycle.CurrentSpeed); 

            if (isWiping)
            {
                currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0f, Time.deltaTime * wipeSpeed);
                if (currentDropletAlpha <= 0f) isWiping = false;
            }
            else if (isRaining)
            {
                if (currentBikeSpeed >= windClearThreshold)
                {
                    currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0f, Time.deltaTime * windClearRate);
                }
                else
                {
                    currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0.95f, Time.deltaTime * dropletFillRate);
                }
            }
            else
            {
                currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0f, Time.deltaTime * wipeSpeed);
            }
        }
        else
        {
            currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0f, Time.deltaTime * wipeSpeed);
            isWiping = false;
        }

        Color c = waterDropletUI.color;
        c.a = currentDropletAlpha;
        waterDropletUI.color = c;

        waterDropletUI.rectTransform.localScale = new Vector3(1f, 1f, 1f);  

        if (currentDropletAlpha <= 0f && (!isRaining || !isRiding))
        {
            if (waterDropletUI.gameObject.activeSelf)
            {
                waterDropletUI.gameObject.SetActive(false);
            }
        }
    }
}