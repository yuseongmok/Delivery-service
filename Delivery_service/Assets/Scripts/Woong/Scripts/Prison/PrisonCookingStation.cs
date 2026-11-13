using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonCookingStation : MonoBehaviour, IInteractable
    {
        public CookingStationType stationType;
        public Transform cameraViewPoint;

        private List<GameObject> outlineMeshes = new List<GameObject>();

        private void Awake()
        {
            CreateOutlineMeshes();
            SetHighlight(false);
        }

        private void CreateOutlineMeshes()
        {
            var filters = GetComponentsInChildren<MeshFilter>();
            Shader unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material outlineMat = new Material(unlitShader) { color = new Color(1f, 0.65f, 0.1f) };

            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                GameObject outlineObj = new GameObject("StationOutline");
                outlineObj.transform.SetParent(mf.transform, false);
                outlineObj.transform.localPosition = Vector3.zero;
                outlineObj.transform.localRotation = Quaternion.identity;
                outlineObj.transform.localScale = Vector3.one * 1.06f;

                var nMf = outlineObj.AddComponent<MeshFilter>();
                nMf.sharedMesh = mf.sharedMesh;
                var nMr = outlineObj.AddComponent<MeshRenderer>();
                nMr.sharedMaterial = outlineMat;
                nMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineObj.layer = 2;
                outlineMeshes.Add(outlineObj);
            }
        }

        public void SetHighlight(bool active)
        {
            foreach (var obj in outlineMeshes)
                if (obj) obj.SetActive(active);
        }

        public void Interact(ToppingInventory inventory)
        {
            if (PrisonChickenQuest.Current != null)
                PrisonChickenQuest.Current.InteractWithStation(this);
        }
    }
}