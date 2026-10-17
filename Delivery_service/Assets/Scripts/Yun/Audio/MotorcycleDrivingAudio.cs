using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    [DisallowMultipleComponent]
    public sealed class MotorcycleDrivingAudio : MonoBehaviour
    {
        [SerializeField] private MotorcycleController motorcycle;
        [SerializeField, Min(0)] private float minimumSpeed = .3f;
        [Header("Engine response")]
        [SerializeField] private Vector2 layerPitchRange = new Vector2(.92f, 1.12f);
        [SerializeField, Range(0, 1)] private float lowSpeedVolume = .25f;
        [SerializeField, Min(.01f)] private float fadeInSeconds = .45f;
        [SerializeField, Min(.01f)] private float fadeOutSeconds = .6f;
        [SerializeField, Min(.01f)] private float pitchResponse = 3f;
        [Header("Stopping brake cue")]
        [SerializeField, Min(0)] private float brakeArmSpeed = 6f;
        [SerializeField, Min(0)] private float brakeStartSpeed = 4f;
        [SerializeField, Min(.01f)] private float brakeMaximumSeconds = 1.5f;
        private SoundHandle driving = SoundHandle.Invalid, braking = SoundHandle.Invalid;
        private SoundHandle highEngine = SoundHandle.Invalid;
        private float blend;
        private AudioManager owner;
        private float volume, pitch = .8f, previousSpeed, brakeAge, brakeVolume;
        private bool brakeArmed, sampledSpeed;

        private void Awake() { if (motorcycle == null) motorcycle = GetComponent<MotorcycleController>(); }
        private void LateUpdate() { Tick(Time.unscaledDeltaTime); }

        internal void Tick(float delta)
        {
            var manager = AudioManager.Instance;
            if (owner != manager) StopAll();
            owner = manager;
            if (Time.timeScale <= 0 || motorcycle == null || manager == null) { StopAll(); return; }
            float dt = Mathf.Clamp(delta, 0, .1f);
            bool riding = motorcycle.isActiveAndEnabled && motorcycle.isDriven;
            float speed = riding ? Mathf.Abs(motorcycle.CurrentSpeed) : 0;
            float normalized = Mathf.Clamp01(speed / Mathf.Max(1, motorcycle.maxSpeed));
            bool moving = riding && speed >= minimumSpeed;
            // No tail after stopping, dismounting, or disabling gameplay controls.
            if (!moving) { StopAll(); return; }
            float target = moving ? Mathf.Lerp(lowSpeedVolume, 1, normalized) : 0;
            volume = Mathf.MoveTowards(volume, target, dt / Mathf.Max(.01f, target > volume ? fadeInSeconds : fadeOutSeconds));
            pitch = Mathf.MoveTowards(pitch, Mathf.Lerp(layerPitchRange.x, layerPitchRange.y, normalized), dt * pitchResponse);
            blend = Mathf.MoveTowards(blend, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.15f, .85f, normalized)), dt * 2.5f);
            if (moving && !manager.IsHandleValid(driving))
            {
                driving = manager.PlayFollow(SoundId.MotorcycleEngineLoop, transform);
                manager.SetVolume(driving, 0);
            }
            if (!manager.IsHandleValid(highEngine))
            {
                highEngine = manager.PlayFollow(SoundId.MotorcycleEngineHighLoop, transform);
                manager.SetVolume(highEngine, 0);
            }
            manager.SetVolume(driving, volume * Mathf.Sqrt(1 - blend)); manager.SetPitch(driving, pitch);
            manager.SetVolume(highEngine, volume * Mathf.Sqrt(blend)); manager.SetPitch(highEngine, pitch);
            if (!moving && volume <= .001f) { manager.Stop(driving); driving = SoundHandle.Invalid; }

            // Rearm only after returning to travel speed, avoiding repeated low-speed squeals.
            if (riding && speed >= Mathf.Max(brakeArmSpeed, brakeStartSpeed + 1)) brakeArmed = true;
            if (riding && sampledSpeed && brakeArmed && previousSpeed > brakeStartSpeed && speed <= brakeStartSpeed && speed < previousSpeed)
            {
                manager.Stop(braking);
                braking = manager.PlayFollow(SoundId.MotorcycleBrake, transform);
                manager.SetVolume(braking, 0);
                brakeAge = brakeVolume = 0; brakeArmed = false;
            }
            brakeAge += dt;
            bool release = !riding || (speed < minimumSpeed && brakeAge > .2f) || brakeAge >= brakeMaximumSeconds
                || (sampledSpeed && speed > previousSpeed + .05f);
            brakeVolume = Mathf.MoveTowards(brakeVolume, release ? 0 : .85f, dt / (release ? .3f : .08f));
            manager.SetVolume(braking, brakeVolume);
            if (release && brakeVolume <= .001f) { manager.Stop(braking); braking = SoundHandle.Invalid; }
            if (!riding) { brakeArmed = false; sampledSpeed = false; } else sampledSpeed = true;
            previousSpeed = speed;
        }

        private void StopAll()
        {
            if (owner != null) { owner.Stop(driving); owner.Stop(highEngine); owner.Stop(braking); }
            highEngine = SoundHandle.Invalid; blend = 0;
            driving = braking = SoundHandle.Invalid; owner = null;
            volume = brakeVolume = brakeAge = previousSpeed = 0; pitch = layerPitchRange.x;
            brakeArmed = sampledSpeed = false;
        }
        private void OnDisable() { StopAll(); }
    }
}
