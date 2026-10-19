using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BikeShopManager : MonoBehaviour
{
    public static BikeShopManager Instance;

    [Header("카메라 설정")]
    public Camera mainPlayerCamera;  
    public Camera garageCamera;      

    [Header("UI 및 페이드 효과")]
    public GameObject shopUIPanel; 
    public Image fadeImage;        
    public float fadeDuration = 1f;

    [Header("오토바이 모델링 배열")]
    public GameObject[] bikeModels; // 차고지 중앙에 띄워둘 오토바이 모델들
    private int currentIndex = 0;

    private MotorcycleDurability playerBikeDurability; // 플레이어의 실제 오토바이 내구도 스크립트
    private GameObject playerObj;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
        if (garageCamera != null) garageCamera.gameObject.SetActive(false);

        //  모든 모델을 숨기고 첫 번째 모델만 
        UpdateModelVisibility();
    }

    public void EnterShop(GameObject player, MotorcycleDurability bike)
    {
        playerObj = player;
        playerBikeDurability = bike;
        StartCoroutine(TransitionRoutine(true));
    }

    public void ExitShop()
    {
        StartCoroutine(TransitionRoutine(false));
    }

    private IEnumerator TransitionRoutine(bool isEntering)
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

        if (isEntering)
        {
            playerObj.GetComponent<CharacterController>().enabled = false;

            mainPlayerCamera.gameObject.SetActive(false); // 1인칭 끄기
            garageCamera.gameObject.SetActive(true);      // 차고지 켜기
            shopUIPanel.SetActive(true);                  // 차고지 UI 켜기

            Cursor.lockState = CursorLockMode.None;       // 마우스 커서 보이게
            Cursor.visible = true;
        }
        else
        {
            mainPlayerCamera.gameObject.SetActive(true);  // 1인칭 켜기
            garageCamera.gameObject.SetActive(false);     // 차고지 끄기
            shopUIPanel.SetActive(false);                 // 차고지 UI 끄기

            Cursor.lockState = CursorLockMode.Locked;     // 마우스 커서 숨기기
            Cursor.visible = false;

            // 플레이어 조작 복구
            playerObj.GetComponent<CharacterController>().enabled = true;
        }

        t = 0f;
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

    public void OnClick_NextModel()
    {
        if (bikeModels.Length == 0) return;
        currentIndex++;
        if (currentIndex >= bikeModels.Length) currentIndex = 0; // 끝에 도달하면 처음으로
        UpdateModelVisibility();
    }

    public void OnClick_PrevModel()
    {
        if (bikeModels.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = bikeModels.Length - 1; // 처음에 도달하면 끝으로
        UpdateModelVisibility();
    }

    public void OnClick_Repair()
    {
        if (playerBikeDurability != null)
        {
            playerBikeDurability.RepairFull();
        }
    }

    private void UpdateModelVisibility()
    {
        for (int i = 0; i < bikeModels.Length; i++)
        {
            if (bikeModels[i] != null)
                bikeModels[i].SetActive(i == currentIndex);
        }
    }
}