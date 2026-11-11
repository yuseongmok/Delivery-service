using DeliveryService.Yun.ShopUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun.Settings
{
    [DefaultExecutionOrder(11000)]
    public sealed class GameSettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings audioSettings;
        [SerializeField] private ShopPC shopPc;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Text bgmValue;
        [SerializeField] private Text sfxValue;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValue;
        private const string SensitivityKey = "DeliveryService.MouseSensitivity";
        private static float sensitivity = -1f;
        public static float MouseSensitivity
        {
            get
            {
                if (sensitivity < 0f) sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), .25f, 3f);
                return sensitivity;
            }
        }
        [SerializeField] private Text status;
        [SerializeField] private GameObject settingsWindow;
        [SerializeField] private GameObject achievementsPage;
        [Tooltip("로비 제작 후 Build Profiles의 Scene List에 추가하고 씬 경로를 지정하세요. 예: Assets/Scenes/Lobby.unity")]
        [SerializeField] private string lobbyScenePath;
        private readonly ShopUiInputScope input = new ShopUiInputScope();
        private readonly SettingsPauseScope pause = new SettingsPauseScope();
        private bool loading;

        private void OnEnable()
        {
            BackToSettings();
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(MouseSensitivity);
            UpdateValues();
            loading = false;
            if (status != null) status.text = "";
            if (audioSettings != null)
            {
                audioSettings.Load();
                bgmSlider.SetValueWithoutNotify(audioSettings.BgmVolume);
                sfxSlider.SetValueWithoutNotify(audioSettings.SfxVolume);
                UpdateValues();
            }
            input.Acquire(shopPc, true);
            pause.Acquire();
        }

        private void LateUpdate()
        {
            input.Maintain();
            pause.Maintain();
            FitWindow(settingsWindow);
            FitWindow(achievementsPage);
        }

        // 저장된 UI 오브젝트의 크기만 맞춥니다. 작은 해상도에서도 버튼이 잘리지 않습니다.
        private void FitWindow(GameObject window)
        {
            if (window == null || !window.activeSelf) return;
            var parent = transform as RectTransform;
            var rect = window.transform as RectTransform;
            if (parent == null || rect == null || rect.rect.width <= 0 || rect.rect.height <= 0) return;
            float scale = Mathf.Min(1f, (parent.rect.width - 32f) / rect.rect.width,
                (parent.rect.height - 32f) / rect.rect.height);
            rect.localScale = Vector3.one * Mathf.Max(.01f, scale);
        }
        private void OnDisable()
        {
            PlayerPrefs.Save();
            pause.Release();
            input.Release();
            if (audioSettings != null) audioSettings.Save();
        }

        public void SetBgmVolume(float value)
        {
            if (audioSettings != null) audioSettings.SetBgmVolume(value);
            UpdateValues();
        }
        public void SetSfxVolume(float value)
        {
            if (audioSettings != null) audioSettings.SetSfxVolume(value);
            UpdateValues();
        }
        // 기본 도시 감도를 100%로 두고 모든 플레이어 카메라에 같은 배율을 적용한다.
        public void SetMouseSensitivity(float value)
        {
            sensitivity = Mathf.Clamp(value, .25f, 3f);
            PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
            UpdateValues();
        }
        private void UpdateValues()
        {
            if (sensitivityValue != null) sensitivityValue.text = Mathf.RoundToInt(MouseSensitivity * 100) + "%";
            if (bgmValue != null) bgmValue.text = Mathf.RoundToInt(bgmSlider.value * 100) + "%";
            if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100) + "%";
        }

        public void OpenAchievements()
        {
            if (achievementsPage == null) return;
            achievementsPage.SetActive(true);
            if (settingsWindow != null) settingsWindow.SetActive(false);
        }

        public void BackToSettings()
        {
            if (achievementsPage != null) achievementsPage.SetActive(false);
            if (settingsWindow != null) settingsWindow.SetActive(true);
        }

        public void Close()
        {
            if (achievementsPage != null && achievementsPage.activeSelf) BackToSettings();
            else gameObject.SetActive(false);
        }

        public void ReturnToLobby()
        {
            if (loading) return;
            if (string.IsNullOrWhiteSpace(lobbyScenePath) || !Application.CanStreamedLevelBeLoaded(lobbyScenePath))
            {
                if (status != null) status.text = "로비 씬이 아직 연결되지 않았습니다.";
                return;
            }
            if (audioSettings != null) audioSettings.Save();
            loading = true;
            // Restore the clock before the destination scene's Awake/Start runs.
            gameObject.SetActive(false);
            SceneManager.LoadSceneAsync(lobbyScenePath, LoadSceneMode.Single);
        }

        public void QuitGame()
        {
            if (audioSettings != null) audioSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
