using UnityEngine;
using UnityEngine.UI;

public class MotorcycleFuel : MonoBehaviour
{
    [Header("Fuel System")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelConsumptionRate = 5f; // 초당 소모량
    public Image fuelGaugeUI; // 기름 UI (Fill Amount)

    private void Start()
    {
        currentFuel = maxFuel;
        UpdateUI();
    }

    public void ConsumeFuel()
    {
        currentFuel -= fuelConsumptionRate * Time.deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fuelGaugeUI != null)
            fuelGaugeUI.fillAmount = currentFuel / maxFuel;
    }
}