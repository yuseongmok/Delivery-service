using UnityEngine;

public class SnowMan : MonoBehaviour
{
    private GameObject destroyEffect;

    private void OnCollisionEnter(Collision collision)
    {
        // 눈사람이랑 사고나면 눈사람 파괴
        if (collision.relativeVelocity.magnitude >= 3f)
        {
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject, 0.05f); // 너무 빠르니까 안부숴지더라
        }
    }
}