using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask motorcycleLayer;

    [Header("1인칭 카메라 연결")]
    public Transform playerCameraTransform;  

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector3 origin = playerCameraTransform != null ? playerCameraTransform.position : transform.position;
        Vector3 direction = playerCameraTransform != null ? playerCameraTransform.forward : transform.forward;
        Ray ray = new Ray(origin, direction);
        RaycastHit hit; 
        
        Debug.DrawRay(origin, direction * interactRange, Color.red, 2f);

        if (Physics.Raycast(ray, out hit, interactRange, motorcycleLayer))
        {
            MotorcycleController bike = hit.collider.GetComponentInParent<MotorcycleController>();
            if (bike != null)
            {
                bike.EnterBike(this.gameObject);
                this.gameObject.SetActive(false);  
            }
        }
    }
}