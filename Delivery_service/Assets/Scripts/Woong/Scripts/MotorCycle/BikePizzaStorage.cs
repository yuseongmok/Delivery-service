using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BikePizzaStorage : MonoBehaviour, IInteractable
{
    [Header("피자 적재 설정")]
    public GameObject pizzaBoxVisual;
    private List<PizzaData> storedPizzas = new List<PizzaData>();
    public int StoredCount => storedPizzas.Count;

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

    private void Start()
    {
        //if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
        UpdateUI();
    }

    private void Update()
    {
        if (storedPizzas.Count > 0 && bikeController != null && bikeController.isDriven)
        {
            float speed = Mathf.Abs(bikeController.CurrentSpeed);
            if (speed > safeSpeed)
            {
                float turnShake = Mathf.Abs(Input.GetAxis("Horizontal")) * speed * 0.8f;

                // 기존 흔들림 좀 줄임
                float baseShakeIncrement = (turnShake + (speed * 0.1f)) * baseShakeScale;

                // 적재된 피자의 총 재료/토핑 개수에 따른 무게 가중치 곱산
                float weightMultiplier = GetIngredientWeightMultiplier();

                currentShake += (baseShakeIncrement * weightMultiplier) * Time.deltaTime;
            }
            else
            {
                currentShake -= 15f * Time.deltaTime;
            }

            currentShake = Mathf.Clamp(currentShake, 0, maxShake);

            if (currentShake >= maxShake)
            {
                RuinPizza();
            }
            UpdateUI();
        }
        else
        {
            currentShake = Mathf.Lerp(currentShake, 0, Time.deltaTime * 5f);
            UpdateUI();
        }
    }

    // 적재된 피자들의 토핑 수를 기반으로 게이지 증가 계산
    private float GetIngredientWeightMultiplier()
    {
        int totalIngredients = 0;

        foreach (var pizza in storedPizzas)
        {
            if (pizza == null) continue;

            // PizzaData 내부 토핑/재료 리스트 개수 합산
            if (pizza.toppings != null)
            {
                totalIngredients += pizza.toppings.Count;
            }
            else
            {
                totalIngredients += 1;
            }
        }

        // 기본 1.0배 + (총 재료 수 * 가중치)
        return 1f + (totalIngredients * weightFactorPerIngredient);
    }

    public void AddCollisionShock(float impactForce)
    {
        if (storedPizzas.Count > 0)
        {
            // 충돌 충격도 재료 무게 가중치를 적용하여 비율 기준 증가
            float weightedShock = impactForce * 3f * baseShakeScale * GetIngredientWeightMultiplier();
            currentShake += weightedShock;

            if (currentShake >= maxShake) RuinPizza();
        }
    }

    private void RuinPizza()
    {
        Debug.Log("운전이 그따구라서 피자가 이렇게 망가져버렸습니다");

        foreach (var pizza in storedPizzas)
        {
            if (pizza != null)
            {
                pizza.isRuined = true;
            }
        }

        currentShake = 0f;
        UpdateUI();
    }

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
            shakeGaugeUI.fillAmount = currentShake / maxShake;
        }
        if (pizzaUIGroup != null)
        {
            pizzaUIGroup.SetActive(storedPizzas.Count > 0 && bikeController != null && bikeController.isDriven);
        }
    }
}