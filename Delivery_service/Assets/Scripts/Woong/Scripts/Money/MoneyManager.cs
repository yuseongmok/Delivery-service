using UnityEngine;
using System.IO;
using System;

// ?? 지출 카테고리 정의
public enum ExpenseType { None, PizzaTopping, Fuel }

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("현재 재화")]
    public int currentMoney = 1000;

    [Header("오늘의 정산 데이터")]
    public int dailyIncome = 0;
    public int dailyTotalExpense = 0;
    public int expensePizza = 0;
    public int expenseFuel = 0;

    public event Action<int> OnMoneyChanged;
    private string saveFilePath;
    private int lastCheckedMoney;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "economySave.json");
            LoadMoney();
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

    // ?? 돈 쓰기 (어디에 썼는지 카테고리 추가)
    public bool SpendMoney(int amount, ExpenseType expenseType = ExpenseType.None)
    {
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
            return true;
        }
        return false;
    }

    // ?? 돈 벌기 (수익 누적)
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        lastCheckedMoney = currentMoney;
        dailyIncome += amount; // 정산용 수익 누적

        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // ?? 정산 완료 후 하루 데이터 초기화
    public void ResetDailyStats()
    {
        dailyIncome = 0;
        dailyTotalExpense = 0;
        expensePizza = 0;
        expenseFuel = 0;
    }

    private void SaveMoney()
    {
        EconomySaveData data = new EconomySaveData { savedMoney = currentMoney };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadMoney()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            EconomySaveData data = JsonUtility.FromJson<EconomySaveData>(json);
            currentMoney = data.savedMoney;
        }
    }
}

[System.Serializable]
public class EconomySaveData
{
    public int savedMoney;
}