using System.Collections.Generic;
using UnityEngine;

public class InteractHoverManager : MonoBehaviour
{
    [Header("시선 감지 설정")]
    public Camera playerCamera;
    public float interactDistance = 3f;
    [Tooltip("상호작용할 모든 레이어")]
    public LayerMask interactLayers;

    [Header("아웃라인 설정")]
    public Color outlineColor = Color.white;  
    [Range(0f, 10f)]
    public float outlineWidth = 4f;          
    private Transform currentTarget;
    private List<Outline> activeOutlines = new List<Outline>();

    private void Update()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers))
        {
            Transform hitTransform = hit.collider.transform;

            if (currentTarget == hitTransform) return;

            ClearHighlight();
            HighlightObject(hitTransform);
        }
        else
        {
            ClearHighlight();
        }
    }

    private void HighlightObject(Transform target)
    {
        currentTarget = target;

        Renderer[] renderers = currentTarget.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (((1 << rend.gameObject.layer) & interactLayers) != 0)
            {
                Outline outline = rend.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = rend.gameObject.AddComponent<Outline>();
                }
                outline.OutlineMode = Outline.Mode.OutlineAll;
                outline.OutlineColor = outlineColor;
                outline.OutlineWidth = outlineWidth;
                outline.enabled = true;

                activeOutlines.Add(outline);
            }
        }
    }

    private void ClearHighlight()
    {
        foreach (Outline outline in activeOutlines)
        {
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
        activeOutlines.Clear();
        currentTarget = null;
    }
}