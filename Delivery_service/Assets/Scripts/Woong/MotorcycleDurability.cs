using UnityEngine;
using UnityEngine.UI;

public class MotorcycleDurability : MonoBehaviour
{
    [Header("Durability System")]
    public string obstacleTag = "Obstacle";  
    public float maxDurability = 100f;
    public float currentDurability;
    public float collisionDamage = 15f;
    public Image durabilityGaugeUI;
   
    //public LayerMask obstacleTag;

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

    private void OnCollisionEnter(Collision collision)
    {
        // ★ 고정된 글자 대신 위에 만든 obstacleTag 변수를 사용하도록 수정
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            currentDurability -= collisionDamage;
            currentDurability = Mathf.Clamp(currentDurability, 0, maxDurability);

            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, collision.contacts[0].point, Quaternion.identity);
            }

            if (currentDurability <= smokeWarningThreshold && smokeEffectObject != null)
            {
                smokeEffectObject.SetActive(true);
            }

            UpdateUI();

            if (currentDurability <= 0)
            {
                Debug.Log("오토바이가 완전히 고장났습니다!");
            }
        }
    }

    private void UpdateUI()
    {
        if (durabilityGaugeUI != null)
            durabilityGaugeUI.fillAmount = currentDurability / maxDurability;
    }
}