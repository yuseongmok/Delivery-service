using UnityEngine;
using UnityEngine.UI;

public class MotorcycleDurability : MonoBehaviour
{
    [Header("Durability System")]
    public float maxDurability = 100f; //[cite: 10]
    public float currentDurability; //[cite: 10]
    public Image durabilityGaugeUI; //[cite: 10]

    [Header("Effects")]
    public GameObject collisionEffectPrefab; //[cite: 10]
    public GameObject smokeEffectObject; //[cite: 10]
    public float smokeWarningThreshold = 30f; //[cite: 10]

    private void Start()
    {
        currentDurability = maxDurability; //[cite: 10]
        if (smokeEffectObject != null) smokeEffectObject.SetActive(false); //[cite: 10]
        UpdateUI(); //[cite: 10]
    }

    // 🛑 [수정됨] OnCollisionEnter 대신 외부에서 데미지를 줄 수 있는 public 함수로 변경
    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        currentDurability -= damage;
        currentDurability = Mathf.Clamp(currentDurability, 0, maxDurability); //[cite: 10]

        // 이펙트 생성
        if (collisionEffectPrefab != null)
        {
            Instantiate(collisionEffectPrefab, hitPoint, Quaternion.identity); //[cite: 10]
        }

        // 연기 효과
        if (currentDurability <= smokeWarningThreshold && smokeEffectObject != null) //[cite: 10]
        {
            smokeEffectObject.SetActive(true); //[cite: 10]
        }

        UpdateUI(); //[cite: 10]
    }

    private void UpdateUI()
    {
        if (durabilityGaugeUI != null) //[cite: 10]
            durabilityGaugeUI.fillAmount = currentDurability / maxDurability; //[cite: 10]
    }
}