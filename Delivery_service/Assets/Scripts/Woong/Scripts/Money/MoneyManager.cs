using UnityEngine;
using System.IO;
using System;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("현재 재화")]
    public int currentMoney = 1000;

    public event Action<int> OnMoneyChanged;
    private string saveFilePath;
    private int lastCheckedMoney;

    private void Awake()
    {
        // 싱글톤 패턴: 어디서든 접근 가능하게 만듦
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
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
        // 게임 실행 중 인스펙터에서 숫자를 직접 수정했을 때를 감지
        if (currentMoney != lastCheckedMoney)
        {
            lastCheckedMoney = currentMoney;
            SaveMoney();
            OnMoneyChanged?.Invoke(currentMoney);
        }
    }
#endif

    // 돈 쓰기 
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            lastCheckedMoney = currentMoney;
            Debug.Log($"-{amount}원 소모. 남은 돈: {currentMoney}원");
            SaveMoney(); // 돈이 바뀔 때마다 자동 저장
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }
        Debug.Log("잔액이 부족합니다.");
        return false;
    }

    // 돈 벌기
    public void AddMoney(int amount)
    {
        OnMoneyChanged?.Invoke(currentMoney);
        currentMoney += amount;
        lastCheckedMoney = currentMoney;
        Debug.Log($"+{amount}원 획득. 남은 돈: {currentMoney}원");
        SaveMoney();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // JSON 저장하기
    private void SaveMoney()
    {
        EconomySaveData data = new EconomySaveData { savedMoney = currentMoney };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
    }

    // JSON 불러오기
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