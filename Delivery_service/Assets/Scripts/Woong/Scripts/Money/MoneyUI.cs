using UnityEngine;
using UnityEngine.UI; 

public class MoneyUI : MonoBehaviour
{
    [Header("돈을 표시할 텍스트")]
    public Text moneyText;

    private void Start()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateUI;
            UpdateUI(MoneyManager.Instance.currentMoney);
        }
    }

    private void OnDestroy()
    {
        // UI가 파괴되거나 씬이 넘어갈 때 에러가 나지 않도록 연결 해제
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateUI;
        }
    }

    // 돈이 바뀔 때마다 자동으로 실행되는 함수
    private void UpdateUI(int currentMoney)
    {
        if (moneyText != null)
        {
            // 원하는 형식으로 텍스트 변경
            moneyText.text = $"$ {currentMoney}"; // 원 단위로 했는데 짜쳐서 일단 이걸로..
        }
    }
}