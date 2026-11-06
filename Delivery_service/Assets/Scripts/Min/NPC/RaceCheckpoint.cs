using UnityEngine;

public class RaceCheckpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 본체 또는 오토바이 탑승 상태 감지
        bool isPlayer = other.CompareTag("Player");
        bool isBike = other.GetComponent<MotorcycleController>() != null || other.GetComponentInParent<MotorcycleController>() != null;

        if (isPlayer || isBike)
        {
            if (RaceManager.Instance != null && RaceManager.Instance.IsRaceActive)
            {
                RaceManager.Instance.OnCheckpointReached(gameObject);
            }
        }
    }
}