using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask motorcycleLayer;

    public Transform playerCameraTransform;
    private FuelNozzle heldNozzle;

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
            GameObject target = hit.collider.gameObject;

            // 노즐  
            FuelNozzle nozzle = target.GetComponent<FuelNozzle>();
            if (nozzle != null)
            {
                if (heldNozzle == null)
                {
                    nozzle.PickUpNozzle();
                    heldNozzle = nozzle;
                }
                return;
            }

            // 주유구 클릭 
            FuelCap cap = target.GetComponent<FuelCap>();
            if (cap != null)
            {
                if (heldNozzle != null)
                {
                    heldNozzle.AttachToBike(cap);
                    heldNozzle = null;
                }
                return;
            }

            // 주유기계 클릭  
            if (target.CompareTag("GasPump"))
            {
                if (heldNozzle != null)
                {
                    heldNozzle.ReturnPump();
                    heldNozzle = null;
                }
                return;
            }

            MotorcycleController bike = hit.collider.GetComponentInParent<MotorcycleController>();
            if (bike != null && heldNozzle == null && !bike.isRefueling)
            {
                bike.EnterBike(this.gameObject);
                this.gameObject.SetActive(false);
            }
        }
    }
}