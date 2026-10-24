using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BikePizzaStorage : MonoBehaviour, IInteractable
{
    [Header("피자 적재 설정")]
    public GameObject pizzaBoxVisual;
    private List<PizzaData> storedPizzas = new List<PizzaData>();
    public int StoredCount => storedPizzas.Count;
    // 저장 피자를 외부에 노출하지 않고 가장 많은 추가 토핑 수만 반환한다.
    public int MaxExtraToppingCount {
        get { int count=0; foreach(var pizza in storedPizzas) count=Mathf.Max(count,RobberyManager.CountExtraToppings(pizza)); return count; }
    }

    [Header("피자 흔들림 게이지")]
    public GameObject pizzaUIGroup;
    public Image shakeGaugeUI;
    public float currentShake = 0f;
    public float maxShake = 100f;
    public float safeSpeed = 10f;

    [Header("흔들림 가중치 및 무게 설정")]
    [SerializeField] private float baseShakeScale = 0.3f;

    // 토핑 1개당 증가 수치
    [SerializeField] private float weightFactorPerIngredient = 0.15f;

    public MotorcycleController bikeController;

    void OnDestroy(){DeliveryService.Mok.SceneActors.Remove(this);}
    void Awake(){DeliveryService.Mok.SceneActors.Register(this);}

    private void Start()
    {
        //if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
        UpdateUI();
    }

    // 흔들림 폐기: 기존 외부 호출과 저장 필드는 호환성을 위해 유지한다.
    private void Update() { currentShake = 0f; UpdateUI(); }
    public void AddCollisionShock(float impactForce) { currentShake = 0f; }

    public void Interact(ToppingInventory inventory)
    {
        var visual = GetComponent<DeliveryService.Mok.BikeCargoVisual>();
        if (visual != null && visual.IsBusy) return;
        bool storing = inventory.HasPackagedPizzas();
        if (!storing && (storedPizzas.Count == 0 || inventory.HasItem())) return;
        var expected = new List<PizzaData>(storing ? inventory.GetHeldPackagedPizzas() : storedPizzas);
        bool Commit()
        {
            var current = storing ? inventory.GetHeldPackagedPizzas() : storedPizzas;
            if (current.Count != expected.Count || (!storing && inventory.HasItem())) return false;
            for (int i=0; i<current.Count; i++) if (!ReferenceEquals(current[i], expected[i])) return false;
            if (storing) storedPizzas.AddRange(inventory.ClearPackagedPizzas());
            else { inventory.AddPackagedDataStack(new List<PizzaData>(storedPizzas)); storedPizzas.Clear(); }
            UpdateUI();
            return true;
        }
        if (visual != null) visual.Animate(inventory, storing, expected.Count, Commit);
        else Commit();
    }

    // 적재 피자 스틸
    // 잠금을 가진 강도에게 첫 상자 하나만 넘기고 나머지 상태는 보존한다.
    public PizzaData TakeOneForRobber(UnityEngine.Object owner)
    {
        var cargo=GetComponent<DeliveryService.Mok.BikeCargoVisual>();
        if(cargo==null||!cargo.IsRobberyBy(owner)||storedPizzas.Count==0)return null;
        var pizza=storedPizzas[0];storedPizzas.RemoveAt(0);cargo.Refresh();UpdateUI();return pizza;
    }
    public bool TryStealPizzas()
    {
        if (storedPizzas != null && storedPizzas.Count > 0)
        {
            storedPizzas.Clear();
            var cargo = GetComponent<DeliveryService.Mok.BikeCargoVisual>();
            if (cargo != null) cargo.Refresh();
            currentShake = 0f;
            //if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
            UpdateUI();
            Debug.Log("강도가 적재된 피자를 훔쳐갔습니다");
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        if (shakeGaugeUI != null)
        {
            shakeGaugeUI.fillAmount = 0;
            shakeGaugeUI.enabled = false;
        }
        if (pizzaUIGroup != null)
        {
            pizzaUIGroup.SetActive(false);
        }
    }
}