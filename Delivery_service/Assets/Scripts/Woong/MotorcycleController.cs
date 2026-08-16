using UnityEngine;

public class MotorcycleController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontWheel;
    public WheelCollider rearWheel;

    [Header("Motor Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeForce = 3000f;

    [Header("Physics & Balance")]
    public Transform centerOfMass;

    private Rigidbody rb;
    private float currentAcceleration = 0f;
    private float currentBrakeForce = 0f;
    private float currentTurnAngle = 0f;

    public bool isDriven = false;
    private GameObject rider;

    // 분리된 두 스크립트를 각각 참조
    private MotorcycleFuel fuelSystem;
    private MotorcycleDurability durabilitySystem;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;

        // 두 컴포넌트를 각각 가져옵니다.
        fuelSystem = GetComponent<MotorcycleFuel>();
        durabilitySystem = GetComponent<MotorcycleDurability>();
    }

    private void FixedUpdate()
    {
        // 기름이 0이거나 내구도가 0이면 주행 불가
        if (!isDriven || fuelSystem.currentFuel <= 0 || durabilitySystem.currentDurability <= 0) return;

        HandleMotor();
        HandleSteering();
        ApplyBrakes();
    }

    private void Update()
    {
        if (isDriven && Input.GetKeyDown(KeyCode.E))
        {
            ExitBike();
        }
    }

    private void HandleMotor()
    {
        currentAcceleration = Input.GetAxis("Vertical") * maxMotorTorque;
        rearWheel.motorTorque = currentAcceleration;

        // 움직임이 있을 때 분리된 연료 스크립트의 소모 함수 호출
        if (Mathf.Abs(currentAcceleration) > 0.1f)
        {
            fuelSystem.ConsumeFuel();
        }
    }

    private void HandleSteering()
    {
        currentTurnAngle = Input.GetAxis("Horizontal") * maxSteeringAngle;
        frontWheel.steerAngle = currentTurnAngle;
    }

    private void ApplyBrakes()
    {
        if (Input.GetKey(KeyCode.Space))
            currentBrakeForce = brakeForce;
        else
            currentBrakeForce = 0f;

        frontWheel.brakeTorque = currentBrakeForce;
        rearWheel.brakeTorque = currentBrakeForce;
    }

    public void EnterBike(GameObject playerObject)
    {
        isDriven = true;
        rider = playerObject;
    }

    public void ExitBike()
    {
        isDriven = false;
        rider.SetActive(true);
        rider.transform.position = transform.position + transform.right * 1.5f;
    }
}