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

    // 연출로 바뀐 시선을 마우스 조작 각도에 반영한다.
    public void SyncViewFromTransform()
    {
        xRotation = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.localEulerAngles.x), minLookAngle, maxLookAngle);
    }

    public void SetControl(bool enable)
    {
        isControllable = enable;
        Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enable;
    }
}
