#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DeliveryService.Yun.ShopUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    // Run in an isolated batch editor: never saves the scene or changes its assets.
    public static class ShopPcDayRegression
    {
        private const string Key = "Yun.ShopPc.DayRegression";
        private static IEnumerator routine;
        private static int lastFrame = -1;
        private static double deadline;

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, "Regression timed out");
                if (!EditorApplication.isPlaying || Time.frameCount < 5 || Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (routine == null) routine = Verify();
                if (routine.MoveNext()) return;
                Finish(0);
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(code);
        }

        private static IEnumerator Verify()
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<ShopPC>();
            Check(pc != null, "ShopPC missing");
            var view = pc.pcUIPanel.GetComponent<ShopPcUiView>();
            var cycle = UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsSortMode.None)
                .Single(c => c.pcUIPanel == view.gameObject);
            cycle.timeMultiplier = 0;
            var buttons = view.GetComponentsInChildren<Button>(true);
            for (int iteration = 0; iteration < 3; iteration++)
            {
                int day = cycle.currentDay;
                Open(pc, cycle);
                yield return null;
                yield return null;
                Check(view.isActiveAndEnabled && pc.enabled, "Day " + day + ": PC did not stay open");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Day " + day + ": cursor locked");
                Click(buttons.Single(b => b.name == "Tab_2"));
                yield return null;
                int oldQuantity = view.ingredients[0].quantity;
                Click(buttons.First(b => b.name == "Plus"));
                Check(view.ingredients[0].quantity == oldQuantity + 1, "Quantity click failed");
                var scroll = view.GetComponentInChildren<ScrollRect>();
                // Force overflow independently of the batch editor's Game View aspect ratio.
                Vector2 originalOffset = scroll.viewport.offsetMax;
                scroll.viewport.offsetMax = originalOffset - new Vector2(0, Mathf.Max(0, scroll.viewport.rect.height - 200));
                yield return null;
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1;
                Canvas.ForceUpdateCanvases();
                yield return null;
                float before = scroll.content.anchoredPosition.y;
                var scrollData = Pointer(scroll.viewport);
                scrollData.scrollDelta = new Vector2(0, -3);
                var hits = Hits(scrollData);
                Check(hits.Count > 0, "No scroll raycast");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, scrollData, ExecuteEvents.scrollHandler);
                yield return null;
                Check(scroll.content.anchoredPosition.y > before, "Supply scrolling blocked: before=" + before + " after=" + scroll.content.anchoredPosition.y + " hit=" + hits[0].gameObject.name);
                scroll.viewport.offsetMax = originalOffset;
                Click(buttons.Single(b => b.name == "Tab_1"));
                yield return null;
                Click(buttons.Single(b => b.name == "OpenBusiness"));
                yield return null;
                Check(cycle.isShopOpen && !view.gameObject.activeSelf, "Business opening failed");
                Open(pc, cycle);
                yield return null;
                Click(buttons.Single(b => b.name == "CloseBusiness"));
                yield return null;
                Check(cycle.summaryPanel.activeInHierarchy && !pc.enabled, "Summary input guard failed");
                Click(SummaryButton(cycle, "OnClick_CancelClose"));
                yield return null;
                Check(view.isActiveAndEnabled && pc.enabled && Cursor.visible, "Cancel did not restore PC input");
                Click(buttons.Single(b => b.name == "CloseBusiness"));
                yield return null;
                Click(SummaryButton(cycle, "OnClick_ConfirmClose"));
                yield return null;
                Check(cycle.currentDay == day + 1 && !cycle.isShopOpen, "Next day failed");
                Check(pc.enabled && !view.gameObject.activeSelf && !cycle.summaryPanel.activeSelf, "Modal input leaked to next day");
                Debug.Log("SHOP_DAY_REGRESSION: day " + day + " passed pointer clicks, scrolling, open, cancel, confirm.");
            }
            Open(pc, cycle);
            yield return null;
            // Reproduce a competing legacy callback restoring gameplay cursor state while UI is open.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (pc.cameraLookScript != null) pc.cameraLookScript.enabled = true;
            yield return null;
            yield return null;
            Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Visible PC loses cursor after competing input restoration");
            Check(pc.cameraLookScript == null || !pc.cameraLookScript.enabled, "Visible PC loses camera input lock");
            Click(buttons.Single(b => b.name == "Tab_2"));
            yield return null;
            Click(buttons.First(b => b.name == "Plus"));

            // Both release orders must preserve the remaining modal's input ownership.
            cycle.summaryPanel.SetActive(true);
            yield return null;
            cycle.summaryPanel.SetActive(false);
            yield return null;
            Check(Cursor.visible && !pc.cameraLookScript.enabled && pc.enabled, "Summary release stole PC input");
            cycle.summaryPanel.SetActive(true);
            view.Close();
            yield return null;
            Check(Cursor.visible && !pc.cameraLookScript.enabled && !pc.enabled, "PC release stole summary input");
            cycle.summaryPanel.SetActive(false);
            yield return null;
            Check(pc.cameraLookScript.enabled && pc.enabled, "Overlapping modals leaked a suspension");

            var move = cycle.player.GetComponent<PlayerMove>();
            if (move != null) move.enabled = false;
            Open(pc, cycle);
            yield return null;
            Click(buttons.Single(b => b.name == "CloseWindow"));
            Check(move == null || !move.enabled, "A previously disabled component was incorrectly enabled");
            if (move != null) move.enabled = true;

            // Cross midnight while the supply UI is open, including an asynchronous fade.
            var fade = new GameObject("Regression fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fade.transform.SetParent(cycle.summaryPanel.transform.parent, false);
            fade.SetActive(false);
            cycle.fadeScreen = fade.GetComponent<Image>();
            cycle.fadeDuration = .1f;
            Open(pc, cycle);
            yield return null;
            Click(buttons.Single(b => b.name == "Tab_2"));
            int beforeMidnight = cycle.currentDay;
            cycle.isShopOpen = true;
            cycle.currentTime = 24;
            do { yield return null; } while (cycle.currentDay == beforeMidnight || fade.activeSelf);
            Check(!view.gameObject.activeSelf && pc.enabled && !cycle.summaryPanel.activeSelf, "Midnight transition left a modal active");
            Open(pc, cycle);
            yield return null;
            yield return null;
            Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Cursor unavailable after midnight");
            Click(buttons.First(b => b.name == "Plus"));
            Click(buttons.Single(b => b.name == "Tab_1"));
            yield return null;
            Click(buttons.Single(b => b.name == "OpenBusiness"));
            Check(cycle.isShopOpen && !view.gameObject.activeSelf, "Opening after midnight failed");
            Debug.Log("SHOP_INPUT_CONFLICT_REGRESSION_PASSED: cursor recovery, overlapping modals, disabled-state preservation, midnight fade and reopening.");
            Debug.Log("SHOP_DAY_REGRESSION_PASSED");
        }

        private static void Open(ShopPC pc, DayNightCycle cycle)
        {
            // Simulate walking to the PC before invoking the existing interaction entry point.
            var controller = cycle.player.GetComponent<CharacterController>();
            bool enabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            cycle.player.transform.position += pc.transform.position + Vector3.up * .5f - pc.playerCamera.position;
            if (controller != null) controller.enabled = enabled;
            typeof(ShopPC).GetMethod("OpenPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pc, null);
        }

        private static Button SummaryButton(DayNightCycle cycle, string method) =>
            cycle.summaryPanel.GetComponentsInChildren<Button>(true).Single(b =>
                Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == method));

        private static PointerEventData Pointer(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            Check(EventSystem.current != null && EventSystem.current.isActiveAndEnabled, "EventSystem unavailable");
            return new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
        }

        private static List<RaycastResult> Hits(PointerEventData data)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            return hits;
        }

        private static void Click(Button button)
        {
            var data = Pointer((RectTransform)button.transform);
            var hits = Hits(data);
            Check(hits.Count > 0, "No raycast for " + button.name);
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Check(target == button.gameObject, "Click on " + button.name + " blocked by " + hits[0].gameObject.name);
            Check(button.IsInteractable(), "Disabled button " + button.name);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
