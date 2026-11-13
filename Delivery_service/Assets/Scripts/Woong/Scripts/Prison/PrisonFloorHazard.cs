using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonFloorHazard : MonoBehaviour
    {
        [Tooltip("플레이어가 이 반경(m) 안으로 밟고 지나가면 소음이 발생합니다.")]
        public float stepTriggerRadius = 0.75f;
        private float nextTriggerTime = 0f;

        private void Update()
        {
            var quest = PrisonChickenQuest.Current;
            if (quest == null || !quest.IsPlayingState() || quest.InMinigameUI || Time.time < nextTriggerTime) return;
            if (quest.player == null) return;

            // 물리 충돌이 누락되더라도 플레이어의 발 위치가 함정에 닿으면 즉시 깨움 카운트 증가!
            Vector3 playerPos = quest.player.transform.position;
            Vector2 pFlat = new Vector2(playerPos.x, playerPos.z);
            Vector2 hFlat = new Vector2(transform.position.x, transform.position.z);

            if (Vector2.Distance(pFlat, hFlat) <= stepTriggerRadius && Mathf.Abs(playerPos.y - transform.position.y) < 1.6f)
            {
                TriggerNoise();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsPlayer(collision.transform)) TriggerNoise();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other.transform)) TriggerNoise();
        }

        private bool IsPlayer(Transform t)
        {
            return t.CompareTag("Player") || t.GetComponentInParent<PlayerMove>() != null || t.GetComponentInParent<PlayerCombat>() != null;
        }

        public void TriggerNoise()
        {
            if (Time.time < nextTriggerTime || PrisonChickenQuest.Current == null) return;
            nextTriggerTime = Time.time + 1.8f; // 연속 중복 카운트 방지 쿨타임
            PrisonChickenQuest.Current.AddWakeUpNoise(transform.position);
        }
    }

    public sealed class PlayerHazardSensor : MonoBehaviour
    {
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var hazard = hit.collider.GetComponentInParent<PrisonFloorHazard>();
            if (hazard != null) hazard.TriggerNoise();
        }
    }
}