using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    // Routes presentation actions to the existing day-cycle implementation.
    public sealed class ShopBusinessUiBridge : MonoBehaviour
    {
        private DayNightCycle cycle;
        private ShopPcUiView view;
        private GameObject originalPanel;
        private ShopSummaryInputGuard guard;

        public void Bind(ShopPC pc, ShopPcUiView ui, GameObject previousPanel)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (DayNightCycle candidate in root.GetComponentsInChildren<DayNightCycle>(true))
            {
                if (previousPanel != null && candidate.pcUIPanel == previousPanel)
                {
                    cycle = candidate;
                    break;
                }
            }
            if (cycle == null) return;
            view = ui;
            originalPanel = previousPanel;
            cycle.pcUIPanel = ui.gameObject;
            // Preserve old button references: DayNightCycle owns their visibility.
            // The new screen keeps its two buttons and uses the existing method guards.
            view.ActionClicked += OnAction;
            if (cycle.summaryPanel != null)
            {
                guard = cycle.summaryPanel.AddComponent<ShopSummaryInputGuard>();
                guard.Configure(pc);
            }
        }

        private void OnAction(string action)
        {
            if (cycle == null) return;
            if (action == "영업하기") cycle.OnClick_OpenShop();
            else if (action == "마감하기") cycle.OnClick_CloseShop();
        }

        private void OnDestroy()
        {
            if (view != null) view.ActionClicked -= OnAction;
            if (cycle != null && view != null && cycle.pcUIPanel == view.gameObject)
                cycle.pcUIPanel = originalPanel;
            if (guard != null) Destroy(guard);
        }
    }
}
