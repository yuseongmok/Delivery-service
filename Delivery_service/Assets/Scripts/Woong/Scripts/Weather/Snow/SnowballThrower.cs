using UnityEngine;

public class SnowballThrower : MonoBehaviour
{
    [Header("투척 설정")]
    public GameObject snowballPrefab;
    public Transform throwPoint;
    public float throwForce = 18f;
    public float throwInterval = 2.5f;
    public float attackRange = 25f;

    [Header("오브젝트 숨기기 설정")]
    public GameObject throwerModel;

    private WeatherManager weatherManager;
    private Transform targetPlayer;
    private MotorcycleController motorcycle;
    private float timer = 0f;

    private void Start()
    {
        weatherManager = FindObjectOfType<WeatherManager>();
        motorcycle = FindObjectOfType<MotorcycleController>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;
    }

    private void Update()
    {
        if (weatherManager == null) return;
        if (!weatherManager.isSnowing)
        {
            if (throwerModel != null && throwerModel.activeSelf)
            {
                throwerModel.SetActive(false);
            }
            return;
        }

        if (throwerModel != null && !throwerModel.activeSelf)
        {
            throwerModel.SetActive(true);
        }

        if (targetPlayer == null || motorcycle == null) return;

        Transform currentTarget = motorcycle.isDriven ? motorcycle.transform : targetPlayer;
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= attackRange)
        {
            Vector3 lookDir = currentTarget.position - transform.position;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

            timer += Time.deltaTime;
            if (timer >= throwInterval)
            {
                ThrowSnowball(currentTarget);
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    private void ThrowSnowball(Transform target)
    {
        if (snowballPrefab == null || throwPoint == null) return;

        GameObject snowball = Instantiate(snowballPrefab, throwPoint.position, throwPoint.rotation);
        Rigidbody rb = snowball.GetComponent<Rigidbody>();

        if (rb != null)
        {
            float accuracyOffset = 2.0f; // 걸어 다닐 때 기본 오차

            // 오토바이 탑승 및 가속 시 명중률 대폭 하락
            if (motorcycle.isDriven)
            {
                float speed = Mathf.Abs(motorcycle.CurrentSpeed);
                accuracyOffset = 4f + (speed * 0.4f);
            }

            Vector3 targetPos = target.position + (Vector3.up * 1f);
            targetPos += Random.insideUnitSphere * accuracyOffset;

            Vector3 throwDirection = (targetPos - throwPoint.position).normalized;
            Vector3 throwVelocity = throwDirection * throwForce + (Vector3.up * (throwForce * 0.2f));

            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }
    }
}