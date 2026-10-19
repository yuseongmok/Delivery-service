using UnityEngine;
using UnityEngine.UI;

public class WeatherManager : MonoBehaviour
{
    [Header("날씨 설정")]
    public bool isRaining = false;
    public int rainStartDay = 3;

    public bool isSnowing = false;
    public int snowStartDay = 5;

    public bool isTyphoon = false;
    public int typhoonStartDay = 7;

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

    public Image tornadoFadeUI; // 토네이도

    [Header("참조 스크립트")]
    public DayNightCycle dayNightCycle;
    public MotorcycleController motorcycle;


    private float currentDropletAlpha = 0f;
    private bool isWiping = false;

    private float currentDizzyAlpha = 0f;

    private int lastUpdatedDay = -1;


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
        if (dayNightCycle != null && dayNightCycle.currentDay != lastUpdatedDay)
        {
            DetermineDailyWeather();
            lastUpdatedDay = dayNightCycle.currentDay;
        }
        HandleCheatCodes();
        if (motorcycle != null)
        {
            motorcycle.isSnowing = this.isSnowing;
        }

        HandleRainState();
        HandleWaterDroplets();
        HandleDizzyEffect();
    }

    private void HandleCheatCodes()
    {
        if (motorcycle != null && motorcycle.isDriven)
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                isRaining = false;
                isSnowing = false;
                isTyphoon = false;
                Debug.Log("맑음");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                isRaining = true;
                isSnowing = false;
                isTyphoon = false;
                Debug.Log("비");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                isRaining = false;
                isSnowing = true;
                isTyphoon = false;
                Debug.Log("눈");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                isRaining = false;
                isSnowing = false;
                isTyphoon = true;
                Debug.Log("태풍");
            }
        }
    }
    private void DetermineDailyWeather()
    {
        int today = dayNightCycle.currentDay;

        // 일단 모든 날씨를 끄기
        isRaining = false;
        isSnowing = false;
        isTyphoon = false;

        if (today == rainStartDay) { isRaining = true; return; }
        if (today == snowStartDay) { isSnowing = true; return; }
        if (today == typhoonStartDay) { isTyphoon = true; return; }

        // 그 외의 날에는 랜덤으로 결정 (원하는 확률로 조절 가능)
        int randomChance = Random.Range(0, 100);

        if (randomChance < 40)
        {
            // 40% 확률로 맑음 
        }
        else if (randomChance < 60 && today >= rainStartDay)
        {
            // 20% 확률로 비
            isRaining = true;
        }
        else if (randomChance < 80 && today >= snowStartDay)
        {
            // 20% 확률로 눈
            isSnowing = true;
        }
        else if (today >= typhoonStartDay)
        {
            // 20% 확률로 태풍
            isTyphoon = true;
        }
        else if (today >= rainStartDay)
        {
            // 아직 눈/태풍이 해금 안 된 날짜에 걸렸다면 대신 비를 내림
            isRaining = true;
        }
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