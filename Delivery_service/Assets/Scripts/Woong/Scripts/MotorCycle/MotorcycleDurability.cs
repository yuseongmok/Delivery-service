using UnityEngine;
using UnityEngine.UI;

public class MotorcycleDurability : MonoBehaviour
{
    [Header("Durability System")]
    public float maxDurability = 100f;  
    public float currentDurability;  
    public Image durabilityGaugeUI;  

    [Header("Effects")]
    public GameObject collisionEffectPrefab;  
    public GameObject smokeEffectObject;  
    public float smokeWarningThreshold = 30f; 

    private void Start()
    {
        currentDurability = maxDurability;  
        if (smokeEffectObject != null) smokeEffectObject.SetActive(false);  
        UpdateUI();  
    }
    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        currentDurability -= damage;
        currentDurability = Mathf.Clamp(currentDurability, 0, maxDurability);  
        if (collisionEffectPrefab != null)
        {
            Instantiate(collisionEffectPrefab, hitPoint, Quaternion.identity); 
        }
        if (currentDurability <= smokeWarningThreshold && smokeEffectObject != null) 
        {
            smokeEffectObject.SetActive(true);  
        }

        UpdateUI(); 
    }

    private void UpdateUI()
    {
        if (durabilityGaugeUI != null)  
            durabilityGaugeUI.fillAmount = currentDurability / maxDurability; 
    }
}