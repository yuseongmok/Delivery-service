using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float currentTime = 12f; // 현재 시간 
    public float timeMultiplier = 1000f; // 시간 흐름 속도  

    [Header("Lights")]
    public Light sun;
    public Light moon;  

    [Header("Intensity Curves (빛의 세기 조절)")]
    [Tooltip("X축: 0~1 (0시~24시), Y축: 빛의 세기 배율")]
    public AnimationCurve sunIntensity;
    public AnimationCurve moonIntensity;

    private float defaultSunIntensity;
    private float defaultMoonIntensity;

    private void Start()
    {
        if (sun != null) defaultSunIntensity = sun.intensity;
        if (moon != null) defaultMoonIntensity = moon.intensity;
    }

    private void Update()
    {
        // 시간 업데이트  
        currentTime += (Time.deltaTime / 3600f) * timeMultiplier;
        if (currentTime >= 24f)
        {
            currentTime %= 24f; // 24시간이 넘어가면 다시 0시로 초기화
        }

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        float sunRotation = ((currentTime / 24f) * 360f) - 90f;

        if (sun != null)
        {
            sun.transform.localRotation = Quaternion.Euler(sunRotation, 170f, 0f);
            sun.intensity = defaultSunIntensity * sunIntensity.Evaluate(currentTime / 24f);
        }

        if (moon != null)
        {
            moon.transform.localRotation = Quaternion.Euler(sunRotation - 180f, 170f, 0f);
            moon.intensity = defaultMoonIntensity * moonIntensity.Evaluate(currentTime / 24f);
        }

        //태양빛이 줄어들면 맵 전체의 숨은 기본 밝기도 같이 줄어들게끔
        if (sun != null && defaultSunIntensity > 0)
        {
            RenderSettings.ambientIntensity = sun.intensity / defaultSunIntensity;
        }
    }
}