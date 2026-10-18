using UnityEngine;

public class SnowMan : MonoBehaviour
{
    [Header("파괴 이펙트 (선택사항)")]
    public GameObject destroyEffect;

    private void OnCollisionEnter(Collision collision)
    {
        MotorcycleController bike = FindObjectOfType<MotorcycleController>();

        if (bike != null)
        {
            if (collision.gameObject == bike.sphereCollider.gameObject ||
                collision.gameObject == bike.bodyCollider.gameObject ||
                collision.gameObject.GetComponentInParent<MotorcycleController>() != null)
            {
                if (bike.isDriven)
                {
                    // 날아가는 거리 줄일려면 3f에서 적절히  변경하면됨 ㄱㄱ
                    bike.CrashAndEject(3f, -collision.contacts[0].normal);
                }

                if (destroyEffect != null)
                {
                    Instantiate(destroyEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject, 0.05f);

                return;
            }
        }
        if (collision.relativeVelocity.magnitude >= 3f)
        {
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject, 0.05f);
        }
    }
}