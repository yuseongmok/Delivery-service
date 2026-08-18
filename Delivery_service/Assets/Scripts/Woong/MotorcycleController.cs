using UnityEngine;

public class MotorcycleController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontWheel;
    public WheelCollider rearWheel;

    [Header("Motor Settings (Smooth)")]
    public float maxMotorTorque = 1500f;     
    public float boostMultiplier = 1.5f;     
    public float brakeForce = 4000f;

    [Header("Steering Settings")]
    public float maxLowSpeedSteerAngle = 35f;
    public float maxHighSpeedSteerAngle = 15f;
    public float topSpeedForSteering = 30f;

    [Header("Physics & Anti-Flip")]
    public Transform centerOfMass;
    public float balanceForce = 50f;  
    public float downForce = 50f;     

    [Header("Camera & View")]
    public GameObject bikeCamera;
    public float mouseSensitivity = 2f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Rigidbody rb;
    private float currentAcceleration = 0f;
    private float currentBrakeForce = 0f;
    private float currentTurnAngle = 0f;

    public bool isDriven = false;
    private GameObject rider;

    private MotorcycleFuel fuelSystem;
    private MotorcycleDurability durabilitySystem;
    private float enterTime = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null)
            rb.centerOfMass = centerOfMass.localPosition;
        else
            rb.centerOfMass = new Vector3(0, -1.0f, 0);

        fuelSystem = GetComponent<MotorcycleFuel>();
        durabilitySystem = GetComponent<MotorcycleDurability>();

        if (bikeCamera != null) bikeCamera.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isDriven || fuelSystem.currentFuel <= 0 || durabilitySystem.currentDurability <= 0) return;

        HandleMotor();
        HandleSteering();
        ApplyBrakes();
        ApplyArcadePhysics();
    }

    private void Update()
    {
        if (isDriven)
        {
            HandleCameraLook();

            if (Time.time - enterTime > 0.2f && Input.GetKeyDown(KeyCode.E))
            {
                ExitBike();
            }
        }
    }

    private void HandleCameraLook()
    {
        if (bikeCamera == null) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -90f, 90f);
        bikeCamera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void HandleMotor()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float currentTorque = verticalInput * maxMotorTorque;

       // Shift 키 부스트
        if (Input.GetKey(KeyCode.LeftShift)) currentTorque *= boostMultiplier;

        rearWheel.motorTorque = currentTorque;
        if (Mathf.Abs(currentTorque) > 0.1f) fuelSystem.ConsumeFuel();
    }

    private void HandleSteering()
    {
        float speedFactor = rb.linearVelocity.magnitude / topSpeedForSteering;
        float currentMaxAngle = Mathf.Lerp(maxLowSpeedSteerAngle, maxHighSpeedSteerAngle, speedFactor);
        currentTurnAngle = Input.GetAxis("Horizontal") * currentMaxAngle;
        frontWheel.steerAngle = currentTurnAngle;
    }

    private void ApplyBrakes()
    {
        currentBrakeForce = Input.GetKey(KeyCode.Space) ? brakeForce : 0f;
        frontWheel.brakeTorque = currentBrakeForce;
        rearWheel.brakeTorque = currentBrakeForce;
    }

    private void ApplyArcadePhysics()
    {
        rb.AddForce(-Vector3.up * rb.linearVelocity.magnitude * downForce);
        float rollAngle = transform.eulerAngles.z;
        if (rollAngle > 180f) rollAngle -= 360f;
        Vector3 balanceTorque = transform.forward * (-rollAngle * balanceForce);
        rb.AddTorque(balanceTorque, ForceMode.Acceleration);
        rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, rb.angularVelocity.z * 0.8f);
    }

    public void EnterBike(GameObject playerObject)
    {
        isDriven = true;
        rider = playerObject;
        enterTime = Time.time;
        rider.SetActive(false);

        if (bikeCamera != null)
        {
            bikeCamera.SetActive(true);
            xRotation = 0f;
            yRotation = 0f;
            bikeCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void ExitBike()
    {
        isDriven = false;
        Vector3 exitPosition = transform.position + transform.right * 1.5f + Vector3.up * 1f;

        if (rider != null)
        {
            rider.transform.position = exitPosition;
            rider.SetActive(true);
        }
        if (bikeCamera != null) bikeCamera.SetActive(false);
    }
}