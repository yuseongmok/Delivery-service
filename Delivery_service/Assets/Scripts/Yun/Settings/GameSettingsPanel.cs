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
        [SerializeField] private Text status;
        [Tooltip("로비 제작 후 Build Profiles의 Scene List에 추가하고 씬 경로를 지정하세요. 예: Assets/Scenes/Lobby.unity")]
        [SerializeField] private string lobbyScenePath;
        private readonly ShopUiInputScope input = new ShopUiInputScope();
        private bool loading;

        private void OnEnable()
        {
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
        }

        private void LateUpdate() { input.Maintain(); }
        private void OnDisable()
        {
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
        private void UpdateValues()
        {
            if (bgmValue != null) bgmValue.text = Mathf.RoundToInt(bgmSlider.value * 100) + "%";
            if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100) + "%";
        }

        public void Close() { gameObject.SetActive(false); }

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
