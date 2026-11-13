using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonCookingIngredient : MonoBehaviour, IInteractable
    {
        public ChickenIngredientType ingredientType;
        public bool IsPlaced { get; private set; }

        private Vector3 startPos;
        private Quaternion startRot;
        private Collider[] cols;

        private void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            cols = GetComponentsInChildren<Collider>();
        }

        public void Interact(ToppingInventory inventory)
        {
            if (PrisonChickenQuest.Current != null && !IsPlaced)
                PrisonChickenQuest.Current.PickUpIngredient(this);
        }

        public void OnPickedUp()
        {
            foreach (var c in cols) if (c) c.enabled = false;
        }

        public void PlaceOnBoard(Transform board, int index)
        {
            IsPlaced = true;
            transform.position = board.position + Vector3.up * 0.15f + new Vector3((index % 3 - 1) * 0.18f, 0, (index / 3) * 0.18f);
            transform.rotation = Quaternion.identity;
        }

        public void ResetToStart()
        {
            IsPlaced = false;
            transform.SetPositionAndRotation(startPos, startRot);
            foreach (var c in cols) if (c) c.enabled = true;
        }

        public string GetKoreanName() => ingredientType switch
        {
            ChickenIngredientType.Chicken => "생닭",
            ChickenIngredientType.Flour => "밀가루",
            ChickenIngredientType.Water => "물",
            ChickenIngredientType.Salt => "소금",
            ChickenIngredientType.Pepper => "후추",
            ChickenIngredientType.Oil => "식용유",
            _ => "요리 재료"
        };
    }
}