using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("감도")]
    [SerializeField] private float mouseSensitivity = 200f;

    [Header("각도")]
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    private float xRotation;
    public bool isControllable = true;

    private void Start()
    {
        SetControl(true);
    }

    private void Update()
    {
        if (!isControllable) return;

        Look();
    }

    private void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }

    public void SetControl(bool enable)
    {
        isControllable = enable;
        Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enable;
    }
}
