using UnityEngine;

public class SnowMan : MonoBehaviour
{
    [Header("ÆÄ±« ÀÌÆåÆ® (¼±ÅÃ»çÇ×)")]
    public GameObject destroyEffect;

    private void OnCollisionEnter(Collision collision)
    {
        MotorcycleController bike = collision.gameObject.GetComponentInParent<MotorcycleController>();

        if (bike != null)
        {
            if (bike.isDriven)
            {
                bike.CrashAndEject(20f, -collision.contacts[0].normal);
            }

            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject, 0.05f);  

            return;  
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