using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Settings
{
    // Capture before legacy UI Update handlers; apply after all Update handlers finish.
    public sealed class SettingsEscapeGate
    {
        private bool requested;
        private bool wasOpen;
        private bool blocked;

        public void Capture(GameSettingsPanel panel, GameObject[] extraPanels)
        {
            requested = panel != null;
            if (!requested) return;
            wasOpen = panel.gameObject.activeInHierarchy;
            blocked = HasOtherWindow(panel, extraPanels);
        }

        public void Apply(GameSettingsPanel panel, GameObject[] extraPanels)
        {
            if (!requested) return;
            requested = false;
            if (panel == null) return;
            if (wasOpen) panel.Close();
            else if (!blocked && !HasOtherWindow(panel, extraPanels)) panel.gameObject.SetActive(true);
        }

        public static bool HasOtherWindow(GameSettingsPanel settings, GameObject[] extraPanels)
        {
            if (extraPanels != null)
                foreach (var panel in extraPanels)
                    if (panel != null && panel != settings.gameObject && Visible(panel.transform)) return true;

            // Buttons, sliders, input fields and dropdowns identify interactive windows;
            // text-only HUD, minimap, clock and stock indicators do not block ESC.
            foreach (var selectable in Selectable.allSelectablesArray)
            {
                if (selectable == null || selectable.transform.IsChildOf(settings.transform)) continue;
                var canvas = selectable.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.isActiveAndEnabled && Visible(selectable.transform)) return true;
            }
            foreach (var pc in Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (pc.pcUIPanel != null && Visible(pc.pcUIPanel.transform)) return true;
            foreach (var cycle in Object.FindObjectsByType<DayNightCycle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cycle.summaryPanel != null && Visible(cycle.summaryPanel.transform)) return true;
                if (cycle.fadeScreen != null && cycle.fadeScreen.isActiveAndEnabled && cycle.fadeScreen.color.a > .001f) return true;
            }
            foreach (var settlement in Object.FindObjectsByType<SettlementUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if ((settlement.confirmationPanel != null && Visible(settlement.confirmationPanel.transform))
                    || (settlement.resultPanel != null && Visible(settlement.resultPanel.transform))) return true;
            return false;
        }

        private static bool Visible(Transform target)
        {
            if (!target.gameObject.activeInHierarchy) return false;
            for (Transform current = target; current != null; current = current.parent)
                foreach (var group in current.GetComponents<CanvasGroup>())
                    if (group.alpha <= .001f) return false;
            return true;
        }
    }
}
