using UnityEngine;

namespace DeliveryService.Mok
{
    // Build-time references include basic ingredients that are not sold in the supply catalog.
    public sealed class PizzaIngredientLibrary : ScriptableObject
    {
        public PizzaToppingData[] ingredients;
    }
}
