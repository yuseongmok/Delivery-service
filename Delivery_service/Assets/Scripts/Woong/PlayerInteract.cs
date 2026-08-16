using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask motorcycleLayer;  

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

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