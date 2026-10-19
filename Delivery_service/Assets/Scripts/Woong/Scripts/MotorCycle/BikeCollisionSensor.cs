using UnityEngine;

public class BikeCollisionSensor : MonoBehaviour
{
    [Header("자동 할당됨")]
    public MotorcycleDurability durabilitySystem;
    public MotorcycleController bikeController;
    public BikePizzaStorage pizzaStorage;

    [Header("충돌 설정")]
    public string obstacleTag = "Obstacle";
    public float collisionDamage = 15f;
    public float minimumCollisionSpeed = 3f;
    [Min(0f)] public float collisionCooldown = 0.25f;

    [Header("사고 날아가기 설정")]
    public float ejectionSpeedThreshold = 25f;

    private float lastImpactTime = float.NegativeInfinity;

    private void Awake()
    {
        if (transform.parent == null) return;
        if (bikeController == null) bikeController = transform.parent.GetComponentInParent<MotorcycleController>();
        if (durabilitySystem == null) durabilitySystem = transform.parent.GetComponentInChildren<MotorcycleDurability>();
        if (pizzaStorage == null) pizzaStorage = transform.parent.GetComponentInChildren<BikePizzaStorage>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(obstacleTag)) return;
        if (Time.time - lastImpactTime < collisionCooldown) return;

        // 도로 타일의 바닥 접촉은 장애물 태그가 있어도 사고로 처리하지 않는다.
        float impactSpeed = 0f;
        Vector3 hitPoint = transform.position;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up)) > 0.7f) continue;

            // 벽을 따라 이동하는 속도 대신 벽에 부딪히는 방향의 속도를 사용한다.
            float normalSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
            if (normalSpeed <= impactSpeed) continue;
            impactSpeed = normalSpeed;
            hitPoint = contact.point;
        }
        if (impactSpeed < minimumCollisionSpeed) return;

        // 여러 충돌체에 동시에 닿아도 짧은 순간에 피해가 누적되지 않게 한다.
        lastImpactTime = Time.time;
        if (durabilitySystem != null) durabilitySystem.TakeDamage(collisionDamage, hitPoint);
        if (pizzaStorage != null) pizzaStorage.AddCollisionShock(impactSpeed);
        if (bikeController != null && impactSpeed >= ejectionSpeedThreshold && bikeController.isDriven)
            bikeController.CrashAndEject();
    }
}
