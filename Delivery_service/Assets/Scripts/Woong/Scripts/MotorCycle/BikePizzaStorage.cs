using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BikePizzaStorage : MonoBehaviour, IInteractable
{
    [Header("피자 적재 설정")]
    public GameObject pizzaBoxVisual;
    private List<PizzaData> storedPizzas = new List<PizzaData>();

    [Header("피자 흔들림 게이지")]
    public GameObject pizzaUIGroup;
    public Image shakeGaugeUI;
    public float currentShake = 0f;
    public float maxShake = 100f;
    public float safeSpeed = 10f;

    public MotorcycleController bikeController;

    private void Start()
    {
        if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
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
                currentShake += (turnShake + (speed * 0.1f)) * Time.deltaTime;
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

    public void AddCollisionShock(float impactForce)
    {
        if (storedPizzas.Count > 0)
        {
            currentShake += impactForce * 3f;
            if (currentShake >= maxShake) RuinPizza();
        }
    }

    private void RuinPizza()
    {
        Debug.Log("운전이 그따구라서 피자가 이렇게 망가져버렸습니다");
        storedPizzas.Clear();
        currentShake = 0f;
        if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
        UpdateUI();
    }

    public void Interact(ToppingInventory inventory)
    {
        if (inventory.HasPackagedPizzas())
        {
            storedPizzas = inventory.ClearPackagedPizzas();
            if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(true);
            Debug.Log("오토바이에 피자를 적재했습니다");
        }
        else if (storedPizzas.Count > 0 && !inventory.HasItem())
        {
            inventory.AddPackagedDataStack(new List<PizzaData>(storedPizzas));
            storedPizzas.Clear();
            if (pizzaBoxVisual != null) pizzaBoxVisual.SetActive(false);
            Debug.Log("오토바이에서 피자를 꺼냈습니다");
        }
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