using UnityEngine;

public class MotorcycleAccident : MonoBehaviour
{
    [SerializeField] private float minImpactSpeed = 2f;             // 사고 발생 최소 속도
    [SerializeField] private float speedMultiplierOnImpact = 0.2f;  // 사고 났을 때 속도 줄어드는 퍼센트
    [SerializeField] private GameObject accidentPanel;
    [SerializeField] private LayerMask npcLayer;

    private MotorcycleController controller;

    private void Awake()
    {
        controller = GetComponentInParent<MotorcycleController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null) return;

        if (((1 << other.gameObject.layer) & npcLayer) != 0)
        {
            float absoluteSpeed = Mathf.Abs(controller.CurrentSpeed);

            if (absoluteSpeed < minImpactSpeed) return;

            NPCControl npc = other.GetComponentInParent<NPCControl>();
            if (npc != null)
            {
                Vector3 hitDirection = controller.transform.forward;
                float hitForce = absoluteSpeed * 30f;
                npc.Die(hitDirection, Mathf.Clamp(hitForce, 15f, 60f));

                controller.ApplyImpactDeceleration(speedMultiplierOnImpact);

                if (accidentPanel != null)
                {
                    accidentPanel.SetActive(true);
                    SetUIState(true);
                }
            }
        }
    }

    public void OnClickNoButton()
    {
        if (accidentPanel != null)
        {
            accidentPanel.SetActive(false);
        }

        SetUIState(false);
    }

    public void OnClickYesButton()
    {
        if (accidentPanel != null)
        {
            accidentPanel.SetActive(false);
        }
        SetUIState(false);
    }

    private void SetUIState(bool isUIActive)
    {
        if (isUIActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (controller != null) controller.SetControllable(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (controller != null) controller.SetControllable(true);
        }
    }
}
