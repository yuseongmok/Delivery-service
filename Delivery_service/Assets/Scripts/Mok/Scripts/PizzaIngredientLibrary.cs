using UnityEngine;

namespace DeliveryService.Mok
{
    // 구매 목록에 없는 기본 재료도 빌드에 포함되도록 참조한다.
    public sealed class PizzaIngredientLibrary : ScriptableObject
    {
        public PizzaToppingData[] ingredients;
    }
}
