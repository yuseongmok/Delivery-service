using UnityEngine;
using UnityEngine.Rendering;

namespace DeliveryService.Mok
{
    // The warehouse lighting is scoped to this set; other cinematic locations retain their own ambience.
    public sealed class AftermathHideoutLighting : MonoBehaviour
    {
        public Color ambient = new Color(.13f, .16f, .21f);
        AmbientMode previousMode;
        Color previousLight;
        bool applied;
        void OnEnable()
        {
            previousMode = RenderSettings.ambientMode;
            previousLight = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            applied = true;
        }
        void OnDisable()
        {
            if (!applied) return;
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousLight;
            applied = false;
        }
    }
}
