#if UNITY_EDITOR
using System;
using System.Linq;
using DeliveryService.Yun.ShopUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    public static class ShopPcUiVerifier
    {
        [MenuItem("Tools/Delivery Service/Yun/Verify Shop PC UI")]
        public static void Run()
        {
            GameObject root = null;
            try
            {
                root = ShopPcUiSceneSetup.InstantiateForVerification();
                var view = root.GetComponent<ShopPcUiView>();
                view.Initialize(null); // Repeated initialization must not duplicate callbacks.
                var buttons = root.GetComponentsInChildren<Button>(true);
                Require(buttons.Length == 20, "Expected 3 tabs, 4 actions, 12 ingredient controls, 1 close");
                for (int i = 0; i < 3; i++)
                {
                    buttons.Single(b => b.name == "Tab_" + i).onClick.Invoke();
                    Require(view.SelectedTab == i, "Tab click must change selection");
                    for (int j = 0; j < 3; j++)
                        Require(root.transform.Find("Background/Page_" + j).gameObject.activeSelf == (i == j), "Exactly one page visible");
                }
                for (int i = 0; i < view.ingredients.Count; i++)
                {
                    var row = root.transform.Find("Background/Page_2/IngredientsScroll/Content/Ingredient_" + i);
                    row.Find("Card/Plus").GetComponent<Button>().onClick.Invoke();
                    Require(view.ingredients[i].quantity == 2, "Plus click");
                    Require(view.ingredients[i].TotalUnits == 20, "Two bundles contain twenty ingredients");
                    row.Find("Card/Minus").GetComponent<Button>().onClick.Invoke();
                    Require(view.ingredients[i].quantity == 1, "Minus click");
                    view.ChangeQuantity(i, int.MinValue);
                    Require(view.ingredients[i].quantity == 1, "Minimum clamp");
                    view.ChangeQuantity(i, int.MaxValue);
                    Require(view.ingredients[i].quantity == 999, "Maximum clamp and overflow protection");
                    row.Find("Card/Purchase").GetComponent<Button>().onClick.Invoke();
                    Require(view.LastClickedAction.Contains("999묶음 (9990개)"), "Purchase feedback uses bundle count and total units");
                }
                foreach (string name in new[] { "Accept", "Reject", "CloseBusiness", "OpenBusiness" })
                    buttons.Single(b => b.name == name).onClick.Invoke();
                Require(view.ClickCount == 8, "Four purchase and four action callbacks");
                Require(root.GetComponentsInChildren<Text>(true).All(t => t.font != null), "Font available");
                Require(root.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Overlay canvas");
                Debug.Log("SHOP_UI_VERIFICATION_PASSED: 20 buttons, three tabs, four quantity bounds, purchase and action callbacks, font and canvas.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private const string PlayCheckKey = "Yun.ShopUi.PlayCheck";
        private static double playCheckStarted;

        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool(PlayCheckKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayCheck()
        {
            if (!SessionState.GetBool(PlayCheckKey, false)) return;
            playCheckStarted = EditorApplication.timeSinceStartup;
            EditorApplication.update -= CheckPlayScene;
            EditorApplication.update += CheckPlayScene;
        }

        private static void CheckPlayScene()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < 5)
            {
                if (EditorApplication.timeSinceStartup - playCheckStarted > 180)
                {
                    SessionState.SetBool(PlayCheckKey, false);
                    EditorApplication.Exit(1);
                }
                return;
            }
            EditorApplication.update -= CheckPlayScene;
            SessionState.SetBool(PlayCheckKey, false);
            try
            {
                var pc = UnityEngine.Object.FindFirstObjectByType<ShopPC>();
                Require(pc != null, "Scene contains ShopPC");
                Require(pc.playerCamera != null && pc.GetComponent<Collider>() != null, "PC raycast target configured");
                Require(pc.GetComponent<ShopPcUiHost>() != null, "Runtime bootstrap installed host");
                var view = pc.pcUIPanel.GetComponent<ShopPcUiView>();
                Require(view != null && !pc.pcUIPanel.activeSelf, "New panel assigned and initially closed");
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ShopPC).GetMethod("OpenPanel", flags).Invoke(pc, null);
                Require(pc.pcUIPanel.activeSelf, "Existing PC open method opens new UI");
                Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Cursor unlocked");
                Require(pc.cameraLookScript == null || !pc.cameraLookScript.enabled, "Look suspended");
                var buttons = view.GetComponentsInChildren<Button>(true);
                buttons.Single(b => b.name == "Tab_2").onClick.Invoke();
                Require(view.SelectedTab == 2, "Runtime tab switching");
                int moneyBefore = MoneyManager.Instance != null ? MoneyManager.Instance.currentMoney : 0;
                // A deliberately over-capacity order must not spend real saved money.
                view.ChangeQuantity(0, int.MaxValue);
                buttons.First(b => b.name == "Purchase").onClick.Invoke();
                Require(MoneyManager.Instance == null || moneyBefore == MoneyManager.Instance.currentMoney, "Over-capacity purchase does not spend money");
                buttons.Single(b => b.name == "CloseWindow").onClick.Invoke();
                Require(!pc.pcUIPanel.activeSelf, "Close button works");
                Require(pc.cameraLookScript == null || pc.cameraLookScript.enabled, "Look restored");
                typeof(ShopPC).GetMethod("OpenPanel", flags).Invoke(pc, null);
                typeof(ShopPC).GetMethod("ClosePanel", flags).Invoke(pc, null);
                Require(!pc.pcUIPanel.activeSelf, "Existing E/Escape close path works");
                var cycle = UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsSortMode.None)
                    .Single(c => c.pcUIPanel == view.gameObject);
                int dayBefore = cycle.currentDay;
                typeof(ShopPC).GetMethod("OpenPanel", flags).Invoke(pc, null);
                buttons.Single(b => b.name == "OpenBusiness").onClick.Invoke();
                Require(cycle.isShopOpen && !view.gameObject.activeSelf, "Open button runs existing opening function");
                typeof(ShopPC).GetMethod("OpenPanel", flags).Invoke(pc, null);
                buttons.Single(b => b.name == "CloseBusiness").onClick.Invoke();
                Require(!cycle.isShopOpen && cycle.summaryPanel.activeInHierarchy && !view.gameObject.activeSelf, "Close shows existing summary");
                Require(Cursor.visible && Cursor.lockState == CursorLockMode.None && !pc.enabled, "Summary owns input");
                var cancel = cycle.summaryPanel.GetComponentsInChildren<Button>(true).Single(b =>
                    Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "OnClick_CancelClose"));
                cancel.onClick.Invoke();
                Require(cycle.isShopOpen && view.gameObject.activeSelf && !cycle.summaryPanel.activeSelf && pc.enabled, "Existing cancel returns to new UI");
                Require(Cursor.visible && (pc.cameraLookScript == null || !pc.cameraLookScript.enabled), "Cancel retains UI input lock");
                buttons.Single(b => b.name == "CloseBusiness").onClick.Invoke();
                var confirm = cycle.summaryPanel.GetComponentsInChildren<Button>(true).Single(b =>
                    Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "OnClick_ConfirmClose"));
                confirm.onClick.Invoke();
                Require(cycle.currentDay == dayBefore + 1 && !cycle.isShopOpen && cycle.currentTime == 6f, "Existing no-fade scene advances to next day");
                Require(!view.gameObject.activeSelf && !cycle.summaryPanel.activeSelf && pc.enabled, "Next day closes modal and restores PC");
                Require(pc.cameraLookScript == null || pc.cameraLookScript.enabled, "Next day restores camera");
                Debug.Log("SHOP_BUSINESS_VERIFICATION_PASSED: open, close, existing summary cancel and confirm, next day, input restoration.");
                Debug.Log("SHOP_UI_PLAY_VERIFICATION_PASSED: JJinCity PC binding, open/close, cursor, camera, tabs, purchase without money changes.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
