using System.Collections.Generic;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    [Header("다음 이동 가능한 포인트")]
    public List<WayPoint> nextWaypoints = new List<WayPoint>();

    // 경로 확인 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.4f);

        if (nextWaypoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var next in nextWaypoints)
            {
                if (next != null)
                {
                    Gizmos.DrawLine(transform.position, next.transform.position);
                }
            }
        }
    }
}
