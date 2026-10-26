using System.Reflection;
using DeliveryService.Mok;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Yun.Audio
{
    // 게임 상태와 SoundLibrary를 연결하는 공통 진입점.
    // 새 효과음 연결은 별도 MonoBehaviour 대신 이 클래스에 추가합니다.
    [DisallowMultipleComponent]
    public sealed class GameplayAudio : MonoBehaviour
    {
        private static readonly FieldInfo BakingField = typeof(Oven).GetField("isBaking", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PizzaField = typeof(Oven).GetField("currentPizza", BindingFlags.Instance | BindingFlags.NonPublic);
        private Oven oven;
        private FuelNozzle nozzle;
        private WeatherManager weather;
        private PizzaBoxStation station;
        private Transform hinge;
        private float previousAngle;
        private int direction;
        private AudioManager owner;
        private SoundHandle ovenLoop = SoundHandle.Invalid;
        private SoundHandle rainLoop = SoundHandle.Invalid;
        private SoundHandle fuelLoop = SoundHandle.Invalid;
        private AudioSource originalRainSource;
        private bool originalRainMute;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Oven target in root.GetComponentsInChildren<Oven>(true)) Attach(target);
                foreach (PizzaPacking target in root.GetComponentsInChildren<PizzaPacking>(true)) Attach(target);
                foreach (WeatherManager target in root.GetComponentsInChildren<WeatherManager>(true)) Attach(target);
                foreach (FuelNozzle target in root.GetComponentsInChildren<FuelNozzle>(true)) Attach(target);
            }
        }

        private static void Attach(Component target)
        {
            if (target.GetComponent<GameplayAudio>() == null) target.gameObject.AddComponent<GameplayAudio>();
        }

        private void OnEnable()
        {
            oven = GetComponent<Oven>();
            nozzle = GetComponent<FuelNozzle>();
            weather = GetComponent<WeatherManager>();
            station = GetComponent<PizzaBoxStation>();
            if (oven != null && (BakingField == null || PizzaField == null))
                Debug.LogError("[Yun Audio] Oven 상태 필드가 변경되었습니다. GameplayAudio를 확인하세요.", this);
            originalRainSource = weather != null ? weather.rainAudioSource : null;
            if (originalRainSource != null)
            {
                originalRainMute = originalRainSource.mute;
                originalRainSource.mute = true;
            }
        }

        private void LateUpdate()
        {
            var manager = AudioManager.Instance;
            if (owner != manager) StopLoops();
            owner = manager;
            bool canPlay = manager != null && Time.timeScale > 0f;
            UpdateLoop(ref ovenLoop, SoundId.OvenLoop, canPlay && oven != null && oven.isActiveAndEnabled
                && BakingField != null && PizzaField != null && (bool)BakingField.GetValue(oven)
                && (PizzaField.GetValue(oven) as Pizza) != null, true);
            UpdateLoop(ref rainLoop, SoundId.RainLoop, canPlay && weather != null
                && weather.isActiveAndEnabled && weather.isRaining, false);
            UpdateBox(canPlay);
            var fuel = nozzle != null && nozzle.currentTargetCap != null ? nozzle.currentTargetCap.bikeFuelSystem : null;
            // 완충 뒤에도 노즐 상태는 Refueling이므로 연료량과 결제 가능 여부도 확인합니다.
            UpdateLoop(ref fuelLoop, SoundId.FuelLoop, canPlay && nozzle != null && nozzle.isActiveAndEnabled
                && nozzle.currentState == FuelNozzle.NozzleState.Refueling && nozzle.refuelSpeed > 0f
                && fuel != null && fuel.currentFuel < fuel.maxFuel
                && MoneyManager.Instance != null && MoneyManager.Instance.currentMoney > 0, true);
        }

        private void UpdateLoop(ref SoundHandle handle, SoundId id, bool active, bool spatial)
        {
            if (!active)
            {
                if (owner != null) owner.Stop(handle);
                handle = SoundHandle.Invalid;
            }
            else if (!owner.IsHandleValid(handle))
                handle = spatial ? owner.PlayFollow(id, transform) : owner.Play(id);
        }

        private void UpdateBox(bool canPlay)
        {
            // 비활성 상태로 시작한 박스는 PizzaPacking.Awake 이후에 가져옵니다.
            if (station == null) station = GetComponent<PizzaBoxStation>();
            if (station == null || !station.isActiveAndEnabled) { hinge = null; direction = 0; return; }
            if (hinge == null)
            {
                foreach (Transform child in station.GetComponentsInChildren<Transform>(true))
                    if (child.name == "LidHinge") { hinge = child; break; }
                if (hinge == null) return;
                previousAngle = hinge.localEulerAngles.x;
            }
            float angle = hinge.localEulerAngles.x;
            float delta = Mathf.DeltaAngle(previousAngle, angle);
            previousAngle = angle;
            if (!canPlay) return;
            if (!station.IsBusy) { direction = 0; return; }
            if (Mathf.Abs(delta) > .001f)
            {
                int movement = delta > 0f ? 1 : -1;
                // 직전 애니메이션의 마지막 각도 보정은 새 열기/닫기로 취급하지 않습니다.
                int expectedMovement = station.IsOpen ? -1 : 1;
                if (movement != expectedMovement) return;
                if (movement != direction)
                {
                    direction = movement;
                    owner.PlayAt(movement > 0 ? SoundId.PizzaBoxOpen : SoundId.PizzaBoxClose, hinge.position);
                }
            }
        }

        private void StopLoops()
        {
            if (owner != null) { owner.Stop(ovenLoop); owner.Stop(rainLoop); owner.Stop(fuelLoop); }
            ovenLoop = rainLoop = fuelLoop = SoundHandle.Invalid;
            owner = null;
        }

        private void OnDisable()
        {
            StopLoops();
            hinge = null;
            direction = 0;
            if (originalRainSource != null) originalRainSource.mute = originalRainMute;
        }

        private void OnDestroy() { StopLoops(); }
    }
}
