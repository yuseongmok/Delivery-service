using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;

    private ToppingInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<ToppingInventory>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            return;

        // Pizza인지 확인

        Pizza pizza = hit.collider.GetComponentInParent<Pizza>();

        if (pizza != null && pizza.IsPlaced)
        {
            pizza.Interact(inventory);
            return;
        }

        // 그 외 상호작용 오브젝트

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        interactable?.Interact(inventory);
    }
}