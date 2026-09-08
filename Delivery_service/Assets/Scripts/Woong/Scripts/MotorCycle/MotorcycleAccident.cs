using UnityEngine;

public class MotorcycleAccident : MonoBehaviour
{
    [Header("사고 발생 최소 속도")]
    [SerializeField] private float minImpactSpeed = 2f;

    private MotorcycleController controller;

    private void Awake()
    {
        controller = GetComponentInParent<MotorcycleController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null) return;

        float absoluteSpeed = Mathf.Abs(controller.CurrentSpeed);

        if (absoluteSpeed < minImpactSpeed) return;

        NPCControl npc = other.GetComponentInParent<NPCControl>();
        if (npc != null)
        {
            Vector3 hitDirection = controller.transform.forward;
            float hitForce = absoluteSpeed * 2.5f;
            npc.Die(hitDirection, Mathf.Clamp(hitForce, 15f, 60f));
        }
    }
}
