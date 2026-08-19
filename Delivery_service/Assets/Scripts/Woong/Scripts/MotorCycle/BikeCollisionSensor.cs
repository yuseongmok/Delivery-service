using UnityEngine;

public class BikeCollisionSensor : MonoBehaviour
{
    public MotorcycleDurability durabilitySystem;  
    public string obstacleTag = "Obstacle";
    public float collisionDamage = 15f;

    private void OnCollisionEnter(Collision collision)
    {
        if (durabilitySystem == null) return;
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            durabilitySystem.TakeDamage(collisionDamage, collision.contacts[0].point);
        }
    }
}