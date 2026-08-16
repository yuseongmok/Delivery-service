using UnityEngine;
using UnityEngine.UI;

public class MotorcycleDurability : MonoBehaviour
{
    [Header("Durability System")]
    public float maxDurability = 100f;
    public float currentDurability;
    public float collisionDamage = 15f;
    public Image durabilityGaugeUI;  
    public GameObject damageEffectPrefab;  

    private void Start()
    {
        currentDurability = maxDurability;
        UpdateUI();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            currentDurability -= collisionDamage;
            currentDurability = Mathf.Clamp(currentDurability, 0, maxDurability);
            if (damageEffectPrefab != null)
            {
                Instantiate(damageEffectPrefab, collision.contacts[0].point, Quaternion.identity);
            }
            UpdateUI();
            if (currentDurability <= 0)
            {
                Debug.Log("오토바이가 고장났습니다");
            }
        }
    }

    private void UpdateUI()
    {
        if (durabilityGaugeUI != null)
            durabilityGaugeUI.fillAmount = currentDurability / maxDurability;
    }
}