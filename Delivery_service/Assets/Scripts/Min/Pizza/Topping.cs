using UnityEngine;

public interface IInteractable
{
    void Interact(ToppingInventory inventory);
}

public class Topping : MonoBehaviour, IInteractable
{
    [SerializeField] private PizzaToppingData toppingData;
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private GameObject lockVisual;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material lockedMaterial;           // 잠금 상태
    [SerializeField] private Material unlockedMaterial;         // 해금 상태

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }

    private void Start()
    {
        if (toppingData == null) return;

        if (toppingData.toppingType == ToppingType.Dough ||
            toppingData.toppingType == ToppingType.Sauce ||
            toppingData.toppingType == ToppingType.Cheese)
        {
            isUnlocked = true;
        }
        else
        {
            // 0 - 잠금, 1 - 해금
            isUnlocked = PlayerPrefs.GetInt(toppingData.toppingName + "_Unlocked", 0) == 1;
        }

        UpdateVisuals();
    }

    public void Interact(ToppingInventory inventory)
    {
        if (toppingData == null) return;

        // 잠겨있으면 차단
        if (!isUnlocked)
        {
            return;
        }

        // 손에 아이템이 있으면 차단
        if (inventory.HasItem())
        {
            return;
        }

        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(toppingData.cost, ExpenseType.PizzaTopping))// 피자 토핑 사용해도 돈이 차감이 안되길래 제가 추가했습니다 // 웅
        {
            // 해금된 상태라면 집을 때마다 돈을 쓰지 않고 바로 집기 
            inventory.AddItem(toppingData);  // 
        }
           
    }

    // 재료 최초 해금 함수
    public bool TryUnlock()
    {
        if (isUnlocked) return true;

        // MoneyManager를 통해 최초 1회만 해금 비용 차감
        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(toppingData.cost, ExpenseType.PizzaTopping))
        {
            MoneyManager.Instance.SpendMoney(toppingData.cost);

            isUnlocked = true;
            PlayerPrefs.SetInt(toppingData.toppingName + "_Unlocked", 1);
            PlayerPrefs.Save();

            UpdateVisuals();
            Debug.Log($"{toppingData.toppingName} 해금");
            return true;
        }

        Debug.Log("돈이 부족하여 해금할 수 없습니다.");
        return false;
    }

    private void UpdateVisuals()
    {
        if (lockVisual != null)
        {
            lockVisual.SetActive(!isUnlocked);
        }

        if (targetRenderer != null)
        {
            if (isUnlocked && unlockedMaterial != null)
            {
                targetRenderer.material = unlockedMaterial;
            }
            else if (!isUnlocked && lockedMaterial != null)
            {
                targetRenderer.material = lockedMaterial;
            }
        }
    }
}