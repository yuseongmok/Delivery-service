using UnityEngine;

public class ShopPC : MonoBehaviour
{
    public GameObject pcUIPanel;
    public float interactRange = 3f;
    public Transform player;

    private void Update()
    {
        if (player == null || pcUIPanel == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool isPanelOpen = pcUIPanel.activeSelf;

        if (isPanelOpen)
        {
            if (distance > interactRange || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                ClosePanel();
            }
        }
        else
        {
            if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
            {
                OpenPanel();
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