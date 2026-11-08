using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    [Header("소음 설정")]
    public float noiseRadius = 15f; // 오브젝트 충돌시 소리가 퍼지는 반경
    public int noisePriority = 2;   // 1=발소리, 2=투척물

    private bool hasMadeNoise = false;

    private void OnCollisionEnter(Collision collision)
    {
        // 바닥이나 벽에 처음 부딪혔을 때 한 번만 소음 발생
        if (!hasMadeNoise && collision.relativeVelocity.magnitude > 2f)
        {
            hasMadeNoise = true;
            MakeNoise();
        }
    }

    private void MakeNoise()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, noiseRadius);
        foreach (Collider col in colliders)
        {
            StealthCitizenAI npc = col.GetComponentInParent<StealthCitizenAI>();
            if (npc != null)
            {
                npc.HearSound(transform.position, noisePriority);
            }
        }
    }
}