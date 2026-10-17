using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;

    public Camera InteractionCamera => playerCamera;

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
        var combat = GetComponent<DeliveryService.Mok.PlayerCombat>();
        if (combat != null && !combat.CanAct) return;
        if (combat != null && combat.TryPickpocket()) return;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            return;

        // Pizza인지 확인

        Pizza pizza = hit.collider.GetComponentInParent<Pizza>();

        if (pizza != null && pizza.IsPlaced)
        {
            // 제작대에서 토핑 배치와 피자 집기를 함께 처리한다.
            // 피자나 토핑 콜라이더를 눌러도 해당 제작대 상호작용으로 처리한다.
            PizzaMaker maker = pizza.GetComponentInParent<PizzaMaker>();
            if (maker != null) maker.Interact(inventory);
            else pizza.Interact(inventory);
            return;
        }

        // 그 외 상호작용 오브젝트

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        interactable?.Interact(inventory);
    }
}