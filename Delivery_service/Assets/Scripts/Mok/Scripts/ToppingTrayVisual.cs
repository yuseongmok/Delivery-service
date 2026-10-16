using UnityEngine;

namespace DeliveryService.Mok
{
    // Replaces appearance only. The original interaction collider and stock label remain in place.
    public sealed class ToppingTrayVisual : MonoBehaviour
    {
        [SerializeField] private Transform model;
        public Transform Model => model;
        public void Initialize(PizzaToppingData data)
        {
            if (model == null) model = transform.Find("Mok Ingredient Tray");
            // The legacy dough prefab also carries Topping; it is a pizza, not a supply station.
            if (Model != null || data == null || GetComponentInParent<Pizza>() != null) return;
            var prefab = Resources.Load<GameObject>("MokPizzaArt/Tray" + data.toppingType);
            if (prefab == null) return;
            var renderers = GetComponentsInChildren<MeshRenderer>();
            var bounds = new Bounds(transform.position, Vector3.zero); bool first = true;
            foreach (var r in renderers)
            {
                if (!r.enabled || r.GetComponent<MeshFilter>() == null) continue;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            if (first) return;
            float width = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z), .32f, .7f);
#if UNITY_EDITOR
            var go = !Application.isPlaying ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, gameObject.scene) : Instantiate(prefab);
            if (!Application.isPlaying) UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Replace ingredient tray");
#else
            var go = Instantiate(prefab);
#endif
            go.name = "Mok Ingredient Tray";
            go.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            go.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            go.transform.localScale = Vector3.one * (width / .48f);
            go.transform.SetParent(transform, true);
            model = go.transform;
            foreach (var t in Model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            foreach (var r in renderers) r.enabled = false;
        }
    }
}
