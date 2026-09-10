using UnityEngine;

public class MotorcycleController : MonoBehaviour
{
    [Header("Arcade Physics")]
    public Rigidbody sphereRB;
    public Rigidbody bikeBody;
    public Collider sphereCollider;
    public Collider bodyCollider;  
    [Header(" 속도 및 가속 )")]
    public float maxSpeed = 30f;
    public float reverseSpeed = 10f;
    public float acceleration = 15f;
    public float shiftAccelMultiplier = 3f;
    public float deceleration = 15f;
    public float brakePower = 40f;
    public float steerStrength = 80f;

    [Header("Camera and View")]
    public GameObject bikeCamera;
    public float mouseSensitivity = 2f;
    public float maxLeanAngle = 20f;

    [Header("UI System")]
    public GameObject bikeUIPanel;
    public bool isRefueling = false;

    private float currentSpeed = 0f;
    public float CurrentSpeed => currentSpeed;//이거 민기쟝이 작성한거임
    private float currentSteerAngle = 0f; 
    private float smoothedSteerInput = 0f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float moveInput = 0f;
    private float steerInput = 0f;
    private bool isBraking = false;
    private bool isShiftPressed = false;

    public bool isDriven = false;
    private bool isControllable = true;
    private GameObject rider;
    private float enterTime = 0f;

    private Vector3 localOffset;

    private MotorcycleFuel fuelSystem;
    public MotorcycleDurability durabilitySystem;

    private void Start()
    {
        if (sphereCollider != null)
        {
            Collider[] allBikeColliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in allBikeColliders)
            {
                if (col != sphereCollider)
                {
                    Physics.IgnoreCollision(sphereCollider, col);
                }
            }
        }

        if (sphereRB != null)
        {
            localOffset = Quaternion.Inverse(transform.rotation) * (transform.position - sphereRB.transform.position);

            sphereRB.transform.parent = null;
            sphereRB.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (bikeBody != null)
        {
            bikeBody.isKinematic = true;
            bikeBody.interpolation = RigidbodyInterpolation.None;
        }

        fuelSystem = GetComponent<MotorcycleFuel>();
        if (bikeCamera != null) bikeCamera.SetActive(false);

        UpdateUIVisibility();
    }

    private void Update()
    {
        if (isDriven && isControllable)
        {
            HandleCameraLook();
            if (Time.time - enterTime > 0.2f && Input.GetKeyDown(KeyCode.E)) ExitBike();

            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
            isBraking = Input.GetKey(KeyCode.Space);
            isShiftPressed = Input.GetKey(KeyCode.LeftShift);

            smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, steerInput, Time.deltaTime * 15f);
            RotationPhysics();
        }
        else
        {
            moveInput = 0f;
            steerInput = 0f;
            isBraking = false;
            isShiftPressed = false;
            smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, 0f, Time.deltaTime * 15f);
        }
    }

    private void FixedUpdate()
    {
        if (fuelSystem != null && fuelSystem.currentFuel <= 0) moveInput = 0f;
        if (durabilitySystem != null && durabilitySystem.currentDurability <= 0) moveInput = 0f;

        Movement();

        if (isDriven && Mathf.Abs(moveInput) > 0.1f && fuelSystem != null)
        {
            fuelSystem.ConsumeFuel();
        }
    }

    private void LateUpdate()
    {
        if (sphereRB != null)
        {
            transform.position = sphereRB.transform.position + (transform.rotation * localOffset);
        }

        if (bikeBody != null)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float targetLeanAngle = -smoothedSteerInput * maxLeanAngle * speedFactor;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLeanAngle);
            bikeBody.transform.localRotation = Quaternion.Slerp(bikeBody.transform.localRotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    private void Movement()
    {
        if (sphereRB == null) return;
        float currentAccel = isShiftPressed ? acceleration * shiftAccelMultiplier : acceleration;
        if (isBraking)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * (brakePower / 10f));
        }
        else if (moveInput > 0.1f)
        {
            if (currentSpeed < -0.1f) currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * (brakePower / 10f));
            else currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.fixedDeltaTime * (currentAccel / 10f));
        }
        else if (moveInput < -0.1f)
        {
            if (currentSpeed > 0.1f) currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * (brakePower / 10f));
            else currentSpeed = Mathf.Lerp(currentSpeed, -reverseSpeed, Time.fixedDeltaTime * (currentAccel / 10f));
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * (deceleration / 10f));
        }
        if (Mathf.Abs(currentSpeed) < 0.1f && moveInput == 0f && !isBraking) currentSpeed = 0f;

        Vector3 targetVelocity = transform.forward * currentSpeed;
        targetVelocity.y = sphereRB.linearVelocity.y;
        sphereRB.linearVelocity = targetVelocity;
    }

    private void RotationPhysics()
    {
        float speedFactor = Mathf.Clamp(Mathf.Abs(currentSpeed) / maxSpeed, 0.5f, 1f);
        if (Mathf.Abs(currentSpeed) < 0.1f) speedFactor = 0f;
        float reverseMultiplier = (currentSpeed < -0.1f) ? -1f : 1f;
        float rotationAmount = smoothedSteerInput * steerStrength * speedFactor * reverseMultiplier * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0, Space.World);
    }

    private void HandleCameraLook()
    {
        if (bikeCamera == null) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -60f, 60f);
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -90f, 90f);
        bikeCamera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    public void UpdateUIVisibility()
    {
        if (bikeUIPanel != null) bikeUIPanel.SetActive(isDriven || isRefueling);
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
        UpdateUIVisibility();
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
        UpdateUIVisibility();
    }
    // 사고 났을 때 오토바이 속도 대폭 감소
    public void ApplyImpactDeceleration(float ratio = 0.2f)
    {
        currentSpeed *= ratio;

        if (sphereRB != null)
        {
            Vector3 targetVelocity = transform.forward * currentSpeed;
            targetVelocity.y = sphereRB.linearVelocity.y;
            sphereRB.linearVelocity = targetVelocity;
        }
    }

    public void SetControllable(bool controllable)
    {
        isControllable = controllable;

        if (!isControllable)
        {
            moveInput = 0f;
            steerInput = 0f;
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, 0.5f);
        }
    }
}