using UnityEngine;

public class BikeCollisionSensor : MonoBehaviour
{
    [Header("자동 할당됨 (인스펙터에 안 넣어도 됩니다!)")]
    public MotorcycleDurability durabilitySystem;
    public MotorcycleController bikeController;
    public BikePizzaStorage pizzaStorage;

    [Header("충돌 설정")]
    public string obstacleTag = "Obstacle";
    public float collisionDamage = 15f;
    public float minimumCollisionSpeed = 3f;

    [Header("사고 날아가기 설정")]
    public float ejectionSpeedThreshold = 25f;  

    private void Awake()
    {
        if (transform.parent != null)
        {
            if (bikeController == null) bikeController = transform.parent.GetComponentInParent<MotorcycleController>();
            if (durabilitySystem == null) durabilitySystem = transform.parent.GetComponentInChildren<MotorcycleDurability>();
            if (pizzaStorage == null) pizzaStorage = transform.parent.GetComponentInChildren<BikePizzaStorage>();
        }
    }

    private void OnCollisionEnter(Collision collision)  // AI로 버그 테스트중

    {
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            Debug.Log($"벽에 부딪힘! 충돌 속도: {impactSpeed} / 날아가는 기준 속도: {ejectionSpeedThreshold}");

            if (impactSpeed >= minimumCollisionSpeed)
            {
                if (durabilitySystem != null)
                {
                    durabilitySystem.TakeDamage(collisionDamage, collision.contacts[0].point);
                }
                if (pizzaStorage != null)
                {
                    pizzaStorage.AddCollisionShock(impactSpeed);
                }
                if (bikeController != null && impactSpeed >= ejectionSpeedThreshold && bikeController.isDriven)
                {
                    bikeController.CrashAndEject();
                }
            }
        }
    }
}