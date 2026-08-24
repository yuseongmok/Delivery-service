using UnityEngine;
using UnityEngine.UI;

public class MotorcycleFuel : MonoBehaviour
{
    [Header("Fuel System")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelConsumptionRate = 5f;
    public Image fuelGaugeUI;

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

    public void AddFuel(float amount)
    {
        currentFuel += amount;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (fuelGaugeUI != null)
            fuelGaugeUI.fillAmount = currentFuel / maxFuel;
    }
}