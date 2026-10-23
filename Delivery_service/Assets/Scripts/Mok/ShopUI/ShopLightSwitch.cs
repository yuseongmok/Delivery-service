using UnityEngine;

namespace DeliveryService.Mok.ShopUI
{
    // 물건을 들고 있을 때도 기존 F키와 IInteractable 경로를 사용한다.
    public sealed class ShopLightSwitch : MonoBehaviour, IInteractable
    {
        public Light[] lights;
        public Renderer[] lampSurfaces;
        public TextMesh label;
        public TextMesh[] signText;
        [SerializeField] private bool lightsOn = true;
        public bool LightsOn => lightsOn;
        private MaterialPropertyBlock properties;
        private void Awake() { SetLights(lightsOn); }
        public void Interact(ToppingInventory inventory) { SetLights(!lightsOn); }
        public void SetLights(bool on)
        {
            lightsOn = on;
            if (lights != null) foreach (var light in lights) if (light != null) light.enabled = on;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetColor("_EmissionColor", on ? new Color(1f,.79f,.49f)*2f : Color.black);
            properties.SetColor("_BaseColor", on ? new Color(.94f,.86f,.68f) : new Color(.28f,.30f,.29f));
            if (lampSurfaces != null) foreach (var lamp in lampSurfaces) if (lamp != null) lamp.SetPropertyBlock(properties);
            if (label != null) label.text = on ? "F  조명 끄기" : "F  조명 켜기";
            if (signText != null) foreach (var text in signText) if (text != null) text.color = on ? new Color(.93f,.88f,.73f) : new Color(.36f,.38f,.34f);
        }
    }
}
