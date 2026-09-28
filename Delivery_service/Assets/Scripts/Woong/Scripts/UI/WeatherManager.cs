using UnityEngine;
using UnityEngine.UI;

public class WeatherManager : MonoBehaviour
{
    [Header("날씨 설정")]
    public bool isRaining = false;
    public int rainStartDay = 3;

    [Header("시야 방해")]
    public Image waterDropletUI;
    public float dropletFillRate = 0.5f;
    public float wipeSpeed = 5f;

    [Header("비 이펙트 및 사운드")]
    public ParticleSystem rainParticle;
    public AudioSource rainAudioSource;

    [Header("참조 스크립트")]
    public DayNightCycle dayNightCycle;
    public MotorcycleController motorcycle;

    private float currentDropletAlpha = 0f;
    private bool isWiping = false;

    private void Start()
    {
        if (waterDropletUI != null)
        {
            Color c = waterDropletUI.color;
            c.a = 0f;
            waterDropletUI.color = c;
            waterDropletUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (dayNightCycle != null && dayNightCycle.currentDay < rainStartDay)
        {
            isRaining = false;
        }

        HandleRainState();
        HandleWaterDroplets();
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

            if (isWiping)
            {
                currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0f, Time.deltaTime * wipeSpeed);
                if (currentDropletAlpha <= 0f) isWiping = false;
            }
            else if (isRaining)
            {
                currentDropletAlpha = Mathf.MoveTowards(currentDropletAlpha, 0.95f, Time.deltaTime * dropletFillRate);
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