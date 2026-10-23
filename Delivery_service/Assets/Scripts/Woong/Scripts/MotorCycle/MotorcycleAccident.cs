using UnityEngine;

public class MotorcycleAccident : MonoBehaviour
{
    [SerializeField] private float minImpactSpeed = 2f;             // 사고 발생 최소 속도
    [SerializeField] private float speedMultiplierOnImpact = 0.2f;  // 사고 발생 시 감속 비율
    [SerializeField] private LayerMask npcLayer;

    [Header("사고 처리 설정")]
    [SerializeField] private int accidentFine = 3000;               // 합의금
    [Range(0f, 1f)]
    [SerializeField] private float policeSpawnChance = 0.7f;        // 경찰 출동 확률

    private MotorcycleController controller;

    private void Awake()
    {
        controller = GetComponentInParent<MotorcycleController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null) return;

        // 강도 충돌 처리
        var robber = other.GetComponentInParent<RobberAI>();
        if (robber != null)
        {
            if (controller.isDriven && robber.ReceiveBikeImpact(controller.transform.forward * Mathf.Sign(controller.CurrentSpeed), Mathf.Abs(controller.CurrentSpeed)))
                controller.ApplyImpactDeceleration(speedMultiplierOnImpact);
            return;
        }

        // 일반 NPC 사고 처리
        if (((1 << other.gameObject.layer) & npcLayer) != 0)
        {
            float absoluteSpeed = Mathf.Abs(controller.CurrentSpeed);

            if (absoluteSpeed < minImpactSpeed) return;

            NPCControl npc = other.GetComponentInParent<NPCControl>();
            if (npc != null)
            {
                // NPC 사망 처리 및 신고 접수
                Vector3 hitDirection = controller.transform.forward;
                float hitForce = absoluteSpeed * 30f;
                npc.Die(hitDirection, Mathf.Clamp(hitForce, 15f, 60f));
                DeliveryService.Mok.CitizenCrime.ReportPlayerKill(npc);

                // 오토바이 충격 감속
                controller.ApplyImpactDeceleration(speedMultiplierOnImpact);

                // 돈 차감
                // No automatic collision fee in the story campaign.

                // 70% 확률 검사 후 경찰 출동
                if (Random.value <= policeSpawnChance)
                {
                    PoliceManager policeManager = FindObjectOfType<PoliceManager>();
                    if (policeManager != null)
                    {
                        policeManager.SpawnPoliceOnAccidentRefused();
                    }
                }
            }
        }
    }
}