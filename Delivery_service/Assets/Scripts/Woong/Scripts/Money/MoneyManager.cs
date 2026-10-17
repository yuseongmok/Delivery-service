using UnityEngine;
using System.IO;
using System;
public enum ExpenseType { None, PizzaTopping, Fuel, Loss }

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("현재 재화")]
    private const int StartingMoney = 20000;
    public int currentMoney = StartingMoney;

    [Header("오늘의 정산 데이터")]
    public int dailyIncome = 0;
    public int dailyTotalExpense = 0;
    public int expensePizza = 0;
    public int expenseFuel = 0;

    public event Action<int> OnMoneyChanged;
    // 수입은 양수, 지출은 음수이며 완료된 거래만 알린다.
    public event Action<int, ExpenseType> TransactionRecorded;
    private string saveFilePath;
    private int lastCheckedMoney;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "economySave.json");
            // 기존 플레이어도 새 게임마다 같은 초기 자금으로 시작한다.
            currentMoney = StartingMoney;
            ResetDailyStats();
            SaveMoney();
            lastCheckedMoney = currentMoney;
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (currentMoney != lastCheckedMoney)
        {
            lastCheckedMoney = currentMoney;
            SaveMoney();
            OnMoneyChanged?.Invoke(currentMoney);
        }
    }
#endif
    public bool SpendMoney(int amount, ExpenseType expenseType = ExpenseType.None)
    {
        if (amount <= 0) return false;
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            lastCheckedMoney = currentMoney;

            // 정산용 지출 누적
            dailyTotalExpense += amount;
            if (expenseType == ExpenseType.PizzaTopping) expensePizza += amount;
            else if (expenseType == ExpenseType.Fuel) expenseFuel += amount;

            SaveMoney();
            OnMoneyChanged?.Invoke(currentMoney);
            TransactionRecorded?.Invoke(-amount, expenseType);
            return true;
        }
        return false;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        lastCheckedMoney = currentMoney;
        dailyIncome += amount; // 정산용 수익 누적

        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
        TransactionRecorded?.Invoke(amount, ExpenseType.None);
    }

    // 투자 원금 이동은 영업 수입/지출 통계에 넣지 않는다.
    public bool TransferInvestmentMoney(int delta)
    {
        long balance=(long)currentMoney+delta;
        if(delta==0 || balance>int.MaxValue || balance<int.MinValue || (delta<0 && balance<0))return false;
        currentMoney=(int)balance;lastCheckedMoney=currentMoney;
        SaveMoney();OnMoneyChanged?.Invoke(currentMoney);return true;
    }

    public void ResetDailyStats()
    {
        dailyIncome = 0;
        dailyTotalExpense = 0;
        expensePizza = 0;
        expenseFuel = 0;
    }

    public void AccidentMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney -= amount;
        lastCheckedMoney = currentMoney;
        dailyTotalExpense += amount; // 벌금과 도난은 매출 감소가 아닌 지출로 기록한다.

        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
        TransactionRecorded?.Invoke(-amount, ExpenseType.Loss);
    }

    private void SaveMoney()
    {
        EconomySaveData data = new EconomySaveData { savedMoney = currentMoney };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
    }


}

[System.Serializable]
public class EconomySaveData
{
    public int savedMoney;
}