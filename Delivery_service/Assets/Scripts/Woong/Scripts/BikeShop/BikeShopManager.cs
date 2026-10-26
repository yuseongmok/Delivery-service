using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BikeInfo
{
    public string bikeName = "기본 오토바이";
    public int price = 5000;
    public bool isUnlocked = false;

    [Header("오토바이 오브젝트 할당")]
    public GameObject displayModel;
    public GameObject actualBikePrefab;

    [Header("성능 그래프 표시용 스탯 (0 ~ 100)")]
    [Range(0f, 100f)] public float topSpeedStat = 50f;
    [Range(0f, 100f)] public float accelerationStat = 50f;
    [Range(0f, 100f)] public float handlingStat = 50f;

    [Header("실제 적용될 물리 스탯")]
    public float actualMaxSpeed = 30f;
    public float actualAcceleration = 15f;
    public float actualSteerStrength = 80f;

    [HideInInspector]
    public Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
}

public class BikeShopManager : MonoBehaviour
{
    public static BikeShopManager Instance;

    [Header("카메라 및 출구 위치 설정")]
    public Camera mainPlayerCamera;
    public Camera shopCamera;
    public Transform exitSpawnPoint;

    [Header("UI 패널 및 페이드")]
    public GameObject shopUIPanel;
    public GameObject mainPlayerHUD;
    public Image fadeImage;
    public float fadeDuration = 0.8f;

    [Header("오토바이 등록 리스트")]
    public BikeInfo[] bikeList;
    public Material lockedSilhouetteMaterial;

    private int currentViewIndex = 0;
    private int equippedBikeIndex = 0;

    [Header("수리 설정")]
    public int baseRepairCost = 500;

    [Header("UI 연결 - 텍스트 및 버튼")]
    public Text bikeNameText;
    public Text statusOrPriceText;
    public Text repairButtonText;
    public Text playerMoneyText;
    public Button buyButton;
    public Button selectButton;
    public Button repairButton;

    [Header("UI 연결 - 성능 그래프 (Image Fill)")]
    public Image speedGauge;
    public Image accelGauge;
    public Image handlingGauge;

    private GameObject currentPlayer;
    private MotorcycleController currentSceneBike;
    private bool isBikeChanged = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        SaveOriginalMaterials();
    }

    private void Start()
    {
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
        if (shopCamera != null) shopCamera.gameObject.SetActive(false);

        if (bikeList.Length > 0) bikeList[0].isUnlocked = true;
    }

    private void Update()
    {
        if (shopUIPanel != null && shopUIPanel.activeSelf && bikeList.Length > 0)
        {
            BikeInfo current = bikeList[currentViewIndex];

            if (speedGauge != null)
                speedGauge.fillAmount = Mathf.Lerp(speedGauge.fillAmount, current.topSpeedStat / 100f, Time.deltaTime * 8f);
            if (accelGauge != null)
                accelGauge.fillAmount = Mathf.Lerp(accelGauge.fillAmount, current.accelerationStat / 100f, Time.deltaTime * 8f);
            if (handlingGauge != null)
                handlingGauge.fillAmount = Mathf.Lerp(handlingGauge.fillAmount, current.handlingStat / 100f, Time.deltaTime * 8f);
        }
    }

    private void SaveOriginalMaterials()
    {
        foreach (var bike in bikeList)
        {
            if (bike.displayModel != null)
            {
                Renderer[] renderers = bike.displayModel.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rnd in renderers)
                {
                    bike.originalMaterials[rnd] = rnd.sharedMaterials;
                }
                bike.displayModel.SetActive(false);
            }
        }
    }

    public void EnterShop(GameObject player, MotorcycleController broughtBike)
    {
        if (broughtBike == null) return;
        currentPlayer = player;
        currentSceneBike = broughtBike;
        isBikeChanged = false;
        currentViewIndex = equippedBikeIndex;

        StartCoroutine(ShopTransitionRoutine(true));
    }

    public void OnClick_ExitShop()
    {
        StartCoroutine(ShopTransitionRoutine(false));
    }

    private IEnumerator ShopTransitionRoutine(bool isEntering)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                Color c = fadeImage.color;
                c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        if (isEntering)
        {
            if (currentSceneBike.isDriven) currentSceneBike.ExitBike();
            currentSceneBike.SetControllable(false);

            if (currentPlayer != null)
            {
                CharacterController cc = currentPlayer.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
            }

            if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(false);
            if (shopCamera != null) shopCamera.gameObject.SetActive(true);
            if (mainPlayerHUD != null) mainPlayerHUD.SetActive(false);
            if (shopUIPanel != null) shopUIPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshShopView();
        }
        else
        {
            if (isBikeChanged) SpawnAndReplaceBike(equippedBikeIndex);
            else if (currentSceneBike != null && exitSpawnPoint != null) currentSceneBike.RespawnBike(exitSpawnPoint);

            if (currentSceneBike != null) currentSceneBike.SetControllable(true);

            if (currentPlayer != null)
            {
                if (exitSpawnPoint != null)
                {
                    currentPlayer.transform.position = exitSpawnPoint.position + exitSpawnPoint.right * 1.8f;
                    currentPlayer.transform.rotation = exitSpawnPoint.rotation;
                }
                CharacterController cc = currentPlayer.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }

            if (shopCamera != null) shopCamera.gameObject.SetActive(false);
            if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);
            if (shopUIPanel != null) shopUIPanel.SetActive(false);
            if (mainPlayerHUD != null) mainPlayerHUD.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (fadeImage != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                Color c = fadeImage.color;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void OnClick_NextBike()
    {
        if (bikeList.Length == 0) return;
        currentViewIndex = (currentViewIndex + 1) % bikeList.Length;
        RefreshShopView();
    }

    public void OnClick_PrevBike()
    {
        if (bikeList.Length == 0) return;
        currentViewIndex--;
        if (currentViewIndex < 0) currentViewIndex = bikeList.Length - 1;
        RefreshShopView();
    }

    public void OnClick_BuyBike()
    {
        BikeInfo targetBike = bikeList[currentViewIndex];
        if (targetBike.isUnlocked) return;

        if (MoneyManager.Instance != null)
        {
            if (MoneyManager.Instance.SpendMoney(targetBike.price, ExpenseType.None))
            {
                targetBike.isUnlocked = true;
                equippedBikeIndex = currentViewIndex;
                isBikeChanged = true;
                RefreshShopView();
                Debug.Log($"{targetBike.bikeName} 해금 및 선택 완료!");
            }
        }
    }

    public void OnClick_SelectBike()
    {
        BikeInfo targetBike = bikeList[currentViewIndex];
        if (!targetBike.isUnlocked) return;

        if (equippedBikeIndex != currentViewIndex)
        {
            equippedBikeIndex = currentViewIndex;
            isBikeChanged = true;
            RefreshShopView();
            Debug.Log($"{targetBike.bikeName} 장착 완료! 나가기 시 적용됩니다.");
        }
    }

    public void OnClick_RepairBike()
    {
        if (currentSceneBike == null || currentSceneBike.durabilitySystem == null) return;
        MotorcycleDurability dur = currentSceneBike.durabilitySystem;
        if (dur.currentDurability >= dur.maxDurability) return;

        if (MoneyManager.Instance != null)
        {
            if (MoneyManager.Instance.SpendMoney(baseRepairCost, ExpenseType.None))
            {
                dur.RepairFull();
                RefreshShopView();
            }
        }
    }

    private void RefreshShopView()
    {
        if (playerMoneyText != null && MoneyManager.Instance != null)
            playerMoneyText.text = $"보유 자금: {MoneyManager.Instance.currentMoney:N0}원";

        for (int i = 0; i < bikeList.Length; i++)
        {
            BikeInfo bike = bikeList[i];
            bool isSelectedView = (i == currentViewIndex);

            if (bike.displayModel != null)
                bike.displayModel.SetActive(isSelectedView);

            if (isSelectedView)
            {
                if (bikeNameText != null) bikeNameText.text = bike.bikeName;

                if (bike.isUnlocked)
                {
                    ApplyOriginalMaterials(bike);
                    if (buyButton != null) buyButton.gameObject.SetActive(false);
                    if (selectButton != null) selectButton.gameObject.SetActive(true);

                    if (i == equippedBikeIndex)
                    {
                        if (statusOrPriceText != null) statusOrPriceText.text = "현재 장착 중";
                        if (selectButton != null) selectButton.interactable = false;
                    }
                    else
                    {
                        if (statusOrPriceText != null) statusOrPriceText.text = "보유 중 (교체 가능)";
                        if (selectButton != null) selectButton.interactable = true;
                    }
                }
                else
                {
                    ApplyLockedSilhouette(bike);
                    if (statusOrPriceText != null) statusOrPriceText.text = $"해금 비용: {bike.price:N0}원";
                    if (buyButton != null) buyButton.gameObject.SetActive(true);
                    if (selectButton != null) selectButton.gameObject.SetActive(false);
                }
            }
        }

        if (repairButton != null && currentSceneBike != null && currentSceneBike.durabilitySystem != null)
        {
            MotorcycleDurability dur = currentSceneBike.durabilitySystem;
            bool needsRepair = dur.currentDurability < dur.maxDurability;
            repairButton.interactable = needsRepair;

            if (repairButtonText != null)
            {
                repairButtonText.text = needsRepair
                    ? $"내구도 수리 ({baseRepairCost}원)\n현재: {Mathf.RoundToInt(dur.currentDurability)}%"
                    : "수리 불필요 (100%)";
            }
        }
    }

    private void SpawnAndReplaceBike(int newIndex)
    {
        BikeInfo newBikeInfo = bikeList[newIndex];
        if (newBikeInfo.actualBikePrefab == null) return;

        Vector3 spawnPos = exitSpawnPoint != null ? exitSpawnPoint.position : currentSceneBike.transform.position;
        Quaternion spawnRot = exitSpawnPoint != null ? exitSpawnPoint.rotation : currentSceneBike.transform.rotation;

        GameObject newBikeObj = Instantiate(newBikeInfo.actualBikePrefab, spawnPos, spawnRot);
        MotorcycleController newController = newBikeObj.GetComponent<MotorcycleController>();

        if (newController != null && currentSceneBike != null)
        {
            newController.bikeUIPanel = currentSceneBike.bikeUIPanel;
            newController.phoneController = currentSceneBike.phoneController;
            newController.navigationManager = currentSceneBike.navigationManager;

            if (newController.durabilitySystem != null && currentSceneBike.durabilitySystem != null)
            {
                newController.durabilitySystem.durabilityGaugeUI = currentSceneBike.durabilitySystem.durabilityGaugeUI;
                newController.durabilitySystem.RepairFull();
            }

            MotorcycleFuel newFuel = newBikeObj.GetComponent<MotorcycleFuel>();
            MotorcycleFuel oldFuel = currentSceneBike.GetComponent<MotorcycleFuel>();
            if (newFuel != null && oldFuel != null)
            {
                newFuel.fuelGaugeUI = oldFuel.fuelGaugeUI;
                newFuel.AddFuel(newFuel.maxFuel);
            }

            newController.maxSpeed = newBikeInfo.actualMaxSpeed;
            newController.acceleration = newBikeInfo.actualAcceleration;
            newController.steerStrength = newBikeInfo.actualSteerStrength;

            WeatherManager wm = FindObjectOfType<WeatherManager>();
            if (wm != null) wm.motorcycle = newController;

            DayNightCycle dnc = FindObjectOfType<DayNightCycle>();
            if (dnc != null) dnc.motorcycle = newController;
        }

        if (currentSceneBike != null)
        {
            if (currentSceneBike.sphereRB != null) Destroy(currentSceneBike.sphereRB.gameObject);
            Destroy(currentSceneBike.gameObject);
        }
        currentSceneBike = newController;
    }

    private void ApplyLockedSilhouette(BikeInfo bike)
    {
        if (lockedSilhouetteMaterial == null || bike.displayModel == null) return;
        Renderer[] renderers = bike.displayModel.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rnd in renderers)
        {
            Material[] mats = new Material[rnd.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = lockedSilhouetteMaterial;
            rnd.sharedMaterials = mats;
        }
    }

    private void ApplyOriginalMaterials(BikeInfo bike)
    {
        if (bike.displayModel == null) return;
        Renderer[] renderers = bike.displayModel.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rnd in renderers)
        {
            if (bike.originalMaterials.ContainsKey(rnd))
                rnd.sharedMaterials = bike.originalMaterials[rnd];
        }
    }
}