using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    // The new PC view releases its input lock when closed. Keep the existing summary modal usable.
    [DefaultExecutionOrder(10000)]
    public sealed class ShopSummaryInputGuard : MonoBehaviour
    {
        private readonly ShopUiInputScope inputScope = new ShopUiInputScope();
        private ShopPC pc;

        public void Configure(ShopPC owner)
        {
            pc = owner;
            if (isActiveAndEnabled) Acquire();
        }

        private void OnEnable() { Acquire(); }
        private void LateUpdate() { inputScope.Maintain(); }

        private void Acquire()
        {
            if (pc != null) inputScope.Acquire(pc, true);
        }

        private void OnDisable()
        {
            inputScope.Release();
        }
    }
}
