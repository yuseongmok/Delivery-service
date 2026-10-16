using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeliveryService.Mok
{
    // Renderer-only previews. Never instantiate ingredient gameplay scripts in the player's hand.
    public sealed class CarriedItemVisual : MonoBehaviour
    {
        // Reserved for this first-person display; contains no colliders or gameplay objects.
        public const int DisplayLayer = 22;
        ToppingInventory inventory;
        Camera viewCamera;
        Camera overlayCamera;
        bool baseIncludedDisplayLayer;
        Transform anchor;
        GameObject display;
        GameObject hiddenPizza;
        readonly Dictionary<Renderer, bool> savedVisibility = new Dictionary<Renderer, bool>();
        public GameObject Display => display;
        public Camera ViewCamera => viewCamera;
        public Camera OverlayCamera => overlayCamera;

        void Awake()
        {
            inventory = GetComponent<ToppingInventory>();
            inventory.ContentsChanged += Refresh;
        }
        void Start() { Refresh(); }
        void OnEnable() { if (anchor != null) anchor.gameObject.SetActive(true); if (overlayCamera != null) overlayCamera.enabled = true; }
        void OnDisable() { if (anchor != null) anchor.gameObject.SetActive(false); if (overlayCamera != null) overlayCamera.enabled = false; }
        void LateUpdate() { if (overlayCamera != null && viewCamera != null) overlayCamera.fieldOfView = viewCamera.fieldOfView; }
        void OnDestroy()
        {
            if (inventory != null) inventory.ContentsChanged -= Refresh;
            RestorePizza();
            if (anchor != null) Destroy(anchor.gameObject);
            if (viewCamera != null && overlayCamera != null)
            {
                viewCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(overlayCamera);
                if (baseIncludedDisplayLayer) viewCamera.cullingMask |= 1 << DisplayLayer;
            }
            if (overlayCamera != null) Destroy(overlayCamera.gameObject);
        }
        bool EnsureAnchor()
        {
            if (anchor != null) return true;
            var interaction = GetComponent<PlayerInteraction>();
            viewCamera = interaction != null ? interaction.InteractionCamera : null;
            if (viewCamera == null) return false;
            anchor = new GameObject("Mok Carried Item").transform;
            anchor.SetParent(viewCamera.transform, false);
            anchor.localPosition = new Vector3(.23f, -.245f, .7f);
            anchor.localRotation = Quaternion.Euler(-18, -12, 0);
            overlayCamera = new GameObject("Mok Carried Item Camera").AddComponent<Camera>();
            overlayCamera.transform.SetParent(viewCamera.transform, false);
            overlayCamera.fieldOfView = viewCamera.fieldOfView; overlayCamera.nearClipPlane = .01f; overlayCamera.farClipPlane = 5;
            overlayCamera.cullingMask = 1 << DisplayLayer; overlayCamera.clearFlags = CameraClearFlags.Depth;
            var data = overlayCamera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Overlay; data.volumeLayerMask = 0;
            viewCamera.GetUniversalAdditionalCameraData().cameraStack.Add(overlayCamera);
            baseIncludedDisplayLayer = (viewCamera.cullingMask & (1 << DisplayLayer)) != 0;
            viewCamera.cullingMask &= ~(1 << DisplayLayer);
            return true;
        }
        void RestorePizza()
        {
            foreach (var entry in savedVisibility) if (entry.Key != null) entry.Key.enabled = entry.Value;
            savedVisibility.Clear(); hiddenPizza = null;
        }
        public void Refresh()
        {
            RestorePizza();
            if (display != null) { display.SetActive(false); Destroy(display); display = null; }
            if (!EnsureAnchor() || !inventory.HasItem()) return;
            display = new GameObject("Carried Visual"); display.transform.SetParent(anchor, false);
            if (inventory.HasPizza())
            {
                hiddenPizza = inventory.HeldPizza;
                CopyVisuals(hiddenPizza.transform, display.transform, true);
                Fit(display, .44f);
                foreach (var renderer in hiddenPizza.GetComponentsInChildren<Renderer>(true))
                { savedVisibility[renderer] = renderer.enabled; renderer.enabled = false; }
            }
            else if (inventory.HasPackagedPizzas())
            {
                var prefab = Resources.Load<GameObject>("MokPizzaArt/Box");
                if (prefab != null)
                    for (int i = 0; i < Mathf.Min(3, inventory.GetHeldPackagedPizzas().Count); i++)
                    {
                        var model = CopyVisuals(prefab.transform, display.transform, true);
                        model.localScale = Vector3.one * .42f;
                        model.localPosition = Vector3.up * (.05f * i);
                    }
            }
            else
            {
                var ingredient = inventory.CurrentItem;
                if (ingredient.toppingType == ToppingType.Dough)
                {
                    var prefab = Resources.Load<GameObject>("MokPizzaArt/PizzaStages");
                    if (prefab != null) CopyVisuals(prefab.transform, display.transform, true);
                    Fit(display, .38f);
                }
                else
                {
                    var prefab = Resources.Load<GameObject>("MokPizzaArt/Held" + ingredient.toppingType);
                    if (prefab != null) CopyVisuals(prefab.transform, display.transform, true);
                    else if (ingredient.toppingPrefab != null)
                    {
                        CopyVisuals(ingredient.toppingPrefab.transform, display.transform, true);
                        Fit(display, .28f);
                    }
                }
            }
            foreach (var t in display.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = DisplayLayer;
            foreach (var renderer in display.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        static Transform CopyVisuals(Transform source, Transform parent, bool root)
        {
            var target = new GameObject(source.name).transform; target.SetParent(parent, false);
            if (!root) { target.localPosition = source.localPosition; target.localRotation = source.localRotation; target.localScale = source.localScale; }
            var filter = source.GetComponent<MeshFilter>(); var renderer = source.GetComponent<MeshRenderer>();
            if (filter != null && filter.sharedMesh != null && renderer != null && renderer.enabled)
            {
                target.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                target.gameObject.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            foreach (Transform child in source) if (child.gameObject.activeSelf) CopyVisuals(child, target, false);
            return target;
        }
        static void Fit(GameObject model, float width)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            // Measure in anchor space, so looking up/down never changes the apparent item size.
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in renderers)
            {
                var local = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var point = model.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            float scale = width / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.z));
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = -bounds.center * scale;
        }
    }
}
