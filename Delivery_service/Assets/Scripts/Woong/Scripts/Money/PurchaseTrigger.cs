using UnityEngine;
using UnityEngine.Events;

public class PurchaseTrigger : MonoBehaviour
{
    [Header("필요한 재화")]
    public int cost = 100;

    [Header("결과 이벤트")]
    public UnityEvent OnPurchaseSuccess; // 결제 성공 시 실행할 동작
    public UnityEvent OnPurchaseFailed;  // 결제 실패 시 실행할 동작
    public void TryPurchase()
    {
        if (MoneyManager.Instance.SpendMoney(cost))
        {
            OnPurchaseSuccess?.Invoke();
        }
        else
        {
            OnPurchaseFailed?.Invoke();
        }
    }
}