using UnityEngine;

public class ShopPC : MonoBehaviour
{
    public GameObject pcUIPanel;
    public float interactRange = 3f;

    [Header("1인칭 카메라 연결")]
    public Transform playerCamera;  

    private void Update()
    {
        if (playerCamera == null || pcUIPanel == null) return;

        bool isPanelOpen = pcUIPanel.activeSelf;

        if (isPanelOpen)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.position);
            if (distance > interactRange || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                ClosePanel();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, interactRange))
                {
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        OpenPanel();
                    }
                }
            }
        }
    }

    private void OpenPanel()
    {
        pcUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePanel()
    {
        pcUIPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}