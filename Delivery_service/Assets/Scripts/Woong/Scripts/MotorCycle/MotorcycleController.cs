using UnityEngine;

public class MotorcycleController : MonoBehaviour
{
    [Header("Arcade Physics")]
    public Rigidbody sphereRB;
    public Rigidbody bikeBody;
    public Collider sphereCollider;
    public Collider bodyCollider;

    [Header("Motor Settings")]
    public float maxSpeed = 30f;
    public float reverseSpeed = 5f;
    public float acceleration = 15f;
    public float deceleration = 10f;
    public float steerStrength = 80f;
    public float boostMultiplier = 1.5f;

    [Header("Camera and View ")]
    public GameObject bikeCamera;
    public float mouseSensitivity = 2f;
    public float maxLeanAngle = 20f;

    private float currentSpeed = 0f;
    private float currentSteerAngle = 0f;
    private float smoothedSteerInput = 0f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float moveInput = 0f;
    private float steerInput = 0f;

    public bool isDriven = false;
    private GameObject rider;
    private float enterTime = 0f;
    private Vector3 bodyOffset;

    private MotorcycleFuel fuelSystem;
    public MotorcycleDurability durabilitySystem;

    private void Start()
    {
        if (sphereCollider != null && bodyCollider != null)
        {
            Physics.IgnoreCollision(sphereCollider, bodyCollider);
        }
        if (bikeBody != null && sphereRB != null)
        {
            bodyOffset = bikeBody.transform.position - sphereRB.transform.position;
        }
        if (sphereRB != null) sphereRB.transform.parent = null;
        if (bikeBody != null) bikeBody.transform.parent = null;

        fuelSystem = GetComponent<MotorcycleFuel>();

        if (bikeCamera != null) bikeCamera.SetActive(false);
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

            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
            smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, steerInput, Time.deltaTime * 15f);
        }
        else
        {
            moveInput = 0f;
            steerInput = 0f;
            smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, 0f, Time.deltaTime * 15f);
        }
    }

    private void FixedUpdate()
    {
        if (fuelSystem != null && fuelSystem.currentFuel <= 0) moveInput = 0f;
        if (durabilitySystem != null && durabilitySystem.currentDurability <= 0) moveInput = 0f;

        Movement();
        RotationPhysics();

        if (isDriven && Mathf.Abs(moveInput) > 0.1f && fuelSystem != null)
        {
            fuelSystem.ConsumeFuel();
        }
    }
    private void LateUpdate()
    {
        if (sphereRB != null && bikeBody != null)
        {
            transform.position = sphereRB.transform.position;
            bikeBody.position = sphereRB.transform.position + bodyOffset;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float targetLeanAngle = -smoothedSteerInput * maxLeanAngle * speedFactor;

            Quaternion targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, targetLeanAngle);
            bikeBody.rotation = Quaternion.Slerp(bikeBody.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }

    private void Movement()
    {
        if (sphereRB == null) return;

        float targetMaxSpeed = maxSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) targetMaxSpeed *= boostMultiplier;
        if (moveInput > 0.1f)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, moveInput * targetMaxSpeed, Time.fixedDeltaTime * (acceleration / 10f));
        }
        else if (moveInput < -0.1f)
        {
            currentSpeed = -reverseSpeed;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * (deceleration / 10f));
        }

        Vector3 targetVelocity = transform.forward * currentSpeed;
        targetVelocity.y = sphereRB.linearVelocity.y;
        sphereRB.linearVelocity = targetVelocity;
    }
    private void RotationPhysics()
    {
        float speedFactor = Mathf.Clamp(Mathf.Abs(currentSpeed) / maxSpeed, 0.5f, 1f);
        if (Mathf.Abs(currentSpeed) < 0.1f) speedFactor = 0f;

        float rotationAmount = smoothedSteerInput * steerStrength * speedFactor * Time.fixedDeltaTime;
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