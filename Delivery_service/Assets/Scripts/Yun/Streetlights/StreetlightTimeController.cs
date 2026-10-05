using UnityEngine;

namespace DeliveryService.Yun.Streetlights
{
    // Explicit scene references only: installing this in YunCity does not affect other scenes.
    [DefaultExecutionOrder(200)]
    public sealed class StreetlightTimeController : MonoBehaviour
    {
        [SerializeField] private DayNightCycle clock;
        [SerializeField, Range(0, 24)] private float turnOnHour = 16f;
        [SerializeField] private Light[] lamps;
        [SerializeField] private GameObject[] glowingSurfaces;
        public bool IsLit { get; private set; }
        public int LampCount => lamps == null ? 0 : lamps.Length;
        private bool applied;

        public void Configure(DayNightCycle source, Light[] lights, GameObject[] surfaces)
        {
            clock = source;
            lamps = lights;
            glowingSurfaces = surfaces;
            Refresh();
        }

        private void OnEnable() { applied = false; Refresh(); }
        private void LateUpdate() { Refresh(); }
        private void OnDisable() { SetLit(false); }

        [ContextMenu("Refresh from game clock")]
        public void Refresh()
        {
            // Closing the shop does not turn off the street: the clock resets to 06:00 next day.
            bool shouldLight = isActiveAndEnabled && clock != null && clock.currentTime >= turnOnHour;
            if (!applied || shouldLight != IsLit) SetLit(shouldLight);
        }

        private void SetLit(bool value)
        {
            if (lamps != null)
                foreach (Light lamp in lamps) if (lamp != null) lamp.enabled = value;
            if (glowingSurfaces != null)
                foreach (GameObject surface in glowingSurfaces) if (surface != null) surface.SetActive(value);
            IsLit = value;
            applied = true;
        }
    }
}
