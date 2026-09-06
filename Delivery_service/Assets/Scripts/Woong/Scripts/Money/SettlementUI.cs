using UnityEngine;
using UnityEngine.UI;

public class SettlementUI : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject confirmationPanel; // "정산하시겠습니까?" 묻는 창
    public GameObject resultPanel;       // 실제 영수증 결과 창

    [Header("결과 텍스트")]
    public Text totalIncomeText;
    public Text totalExpenseText;
    public Text pizzaExpenseText;
    public Text fuelExpenseText;

    private void Start()
    {
        confirmationPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    // 정산하기 
    public void OpenConfirmation()
    {
        confirmationPanel.SetActive(true);
    }

    //아니오 
    public void CancelSettlement()
    {
        confirmationPanel.SetActive(false);
    }

    // 예 
    public void ConfirmSettlement()
    {
        confirmationPanel.SetActive(false);
        ShowResult();
    }

    // 정산 결과 표시
    private void ShowResult()
    {
        MoneyManager mm = MoneyManager.Instance;

        totalIncomeText.text = $"총 수익 : {mm.dailyIncome}";
        totalExpenseText.text = $"총 지출 : {mm.dailyTotalExpense}";
        pizzaExpenseText.text = $"피자토핑 : {mm.expensePizza}";
        fuelExpenseText.text = $"주유비 : {mm.expenseFuel}";

        resultPanel.SetActive(true);

       // 오늘 데이터 초기화 내일 다시 하기 위해서
        mm.ResetDailyStats();
    }
    public void CloseResult()
    {
        resultPanel.SetActive(false);
    }
}