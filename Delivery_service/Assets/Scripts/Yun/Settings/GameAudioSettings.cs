using DeliveryService.Yun.Audio;
using UnityEngine;

namespace DeliveryService.Yun.Settings
{
    // Lives on the saved, active settings container, not the hidden panel.
    [DefaultExecutionOrder(-20000)]
    public sealed class GameAudioSettings : MonoBehaviour
    {
        [Tooltip("버튼이 없는 별도 팝업은 여기에 추가하면 표시 중 ESC 설정 열기를 막습니다. HUD는 넣지 마세요.")]
        [SerializeField] private GameObject[] additionalBlockingPanels;
        private GameSettingsPanel settingsPanel;
        private readonly SettingsEscapeGate escapeGate = new SettingsEscapeGate();
        [Tooltip("AudioManager 외부에서 재생하는 효과음. 예: WeatherManager의 빗소리")]
        [SerializeField] private AudioSource[] sceneEffects;
        private float[] originalVolumes;
        private const string BgmKey = "Yun.Settings.BgmVolume";
        private const string SfxKey = "Yun.Settings.SfxVolume";
        public float BgmVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        private AudioManager appliedManager;
        private bool loaded;
        private bool dirty;

        public void Load()
        {
            if (loaded) return;
            loaded = true;
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
        }

        private void OnEnable() { Load(); Apply(); }
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            bool pressed = UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            bool pressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (!pressed) return;
            if (settingsPanel == null) settingsPanel = GetComponentInChildren<GameSettingsPanel>(true);
            escapeGate.Capture(settingsPanel, additionalBlockingPanels);
        }
        private void LateUpdate()
        {
            escapeGate.Apply(settingsPanel, additionalBlockingPanels);
            if (appliedManager != AudioManager.Instance) Apply();
        }

        public void SetBgmVolume(float value)
        {
            Load();
            BgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmKey, BgmVolume);
            dirty = true;
            Apply();
        }

        public void SetSfxVolume(float value)
        {
            Load();
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            dirty = true;
            Apply();
        }

        private void Apply()
        {
            if (sceneEffects != null)
            {
                if (originalVolumes == null)
                {
                    originalVolumes = new float[sceneEffects.Length];
                    for (int i = 0; i < sceneEffects.Length; i++)
                        if (sceneEffects[i] != null) originalVolumes[i] = sceneEffects[i].volume;
                }
                for (int i = 0; i < sceneEffects.Length; i++)
                    if (sceneEffects[i] != null) sceneEffects[i].volume = originalVolumes[i] * SfxVolume;
            }
            appliedManager = AudioManager.Instance;
            if (appliedManager == null) return;
            appliedManager.SetCategoryVolume(SoundCategory.Bgm, BgmVolume);
            appliedManager.SetCategoryVolume(SoundCategory.Sfx, SfxVolume);
            appliedManager.SetCategoryVolume(SoundCategory.Ui, SfxVolume);
            appliedManager.SetCategoryVolume(SoundCategory.Ambience, SfxVolume);
        }

        public void Save()
        {
            if (!dirty) return;
            PlayerPrefs.Save();
            dirty = false;
        }
        private void OnDisable()
        {
            Save();
            if (originalVolumes != null)
                for (int i = 0; i < sceneEffects.Length; i++)
                    if (sceneEffects[i] != null) sceneEffects[i].volume = originalVolumes[i];
            originalVolumes = null;
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
