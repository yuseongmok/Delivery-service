using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    public sealed class ShopSupplyCatalog : ScriptableObject
    {
        public PizzaToppingData[] materials;

        public PizzaToppingData Find(ToppingType type)
        {
            if (materials != null)
                foreach (var material in materials)
                    if (material != null && material.toppingType == type) return material;
            return null;
        }
    }
}
