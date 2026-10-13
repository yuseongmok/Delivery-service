#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeliveryService.Yun.Achievements;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    // 빈 Play 씬과 임시 저장 키를 사용해 실제 플레이어의 돈/날짜/업적을 건드리지 않습니다.
    public static class AchievementVerifier
    {
        private const string Session = "Yun.AchievementVerification";
        private static IEnumerator routine;
        private static int lastFrame = -1;
        private static double deadline;
        private static readonly List<string> checks = new List<string>();

        [MenuItem("Tools/Delivery Service/Yun/Verify Achievements %#i")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Run from one saved scene outside Play Mode.");
            AchievementSceneSetup.Validate(SceneManager.GetActiveScene());
            SessionState.SetString(Session + ".scene", SceneManager.GetActiveScene().path);
            SessionState.SetString(Session + ".prefix", "Yun.AchievementTest." + Guid.NewGuid().ToString("N") + ".");
            SessionState.SetBool(Session, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Session, false)) return;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                if (SessionState.GetBool(Session + ".finished", false))
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetBool(Session, false);
                    SessionState.SetBool(Session + ".finished", false);
                    EditorApplication.update -= Tick;
                    EditorSceneManager.OpenScene(SessionState.GetString(Session + ".scene", "Assets/Scenes/JJinCity.unity"));
                    return;
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Achievement tests timed out");
                if (!EditorApplication.isPlaying || Time.frameCount < 3 || lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (routine == null) routine = Verify();
                if (routine.MoveNext()) return;
                Finish(null);
            }
            catch (Exception error) { Finish(error); }
        }

        private static void Finish(Exception error)
        {
            Time.timeScale = 1;
            PlayerPrefs.DeleteKey(SessionState.GetString(Session + ".prefix", "") + AchievementManager.FirstPizzaId);
            PlayerPrefs.Save();
            string result = error == null ? "PASSED\n" + string.Join("\n", checks) : "FAILED\n" + error;
            System.IO.File.WriteAllText("Temp/YunAchievementVerification.txt", result);
            if (error == null) Debug.Log(result); else Debug.LogException(error);
            SessionState.SetBool(Session + ".finished", true);
            EditorApplication.ExitPlaymode();
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); checks.Add(message); }
        private static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        private static void Call(object target, string name)
        { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }

        private static IEnumerator Verify()
        {
            var popup = new GameObject("Popup", typeof(RectTransform)); popup.SetActive(false);
            var text = new GameObject("Message", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(popup.transform, false);
            var host = new GameObject("Achievement fixture"); host.SetActive(false);
            var notification = host.AddComponent<AchievementNotification>();
            Set(notification, "popup", popup); Set(notification, "message", text);
            var manager = host.AddComponent<AchievementManager>();
            string prefix = SessionState.GetString(Session + ".prefix", "");
            Set(manager, "savePrefix", prefix); Set(manager, "notification", notification);
            var clock = new GameObject("Clock").AddComponent<DayNightCycle>(); clock.enabled = false;
            var inventory = new GameObject("Inventory").AddComponent<ToppingInventory>();
            Set(inventory, "holdPoint", inventory.transform);
            var pickup = new GameObject("Pickup").AddComponent<PizzaPickUpZone>();
            var packing = new GameObject("Packing").AddComponent<PizzaPacking>(); Set(packing, "pickupZone", pickup);
            var bike = new GameObject("Bike").AddComponent<BikePizzaStorage>();
            var bridge = host.AddComponent<PizzaAchievementBridge>();
            Set(bridge, "achievements", manager); Set(bridge, "dayNightCycle", clock);
            Set(bridge, "pickupZones", new[] { pickup }); Set(bridge, "inventories", new[] { inventory });
            Set(bridge, "bikeStorages", new[] { bike });
            host.SetActive(true);
            yield return null;

            Pack(packing, inventory, true);
            yield return null;
            Check(!manager.IsUnlocked(AchievementManager.FirstPizzaId), "Closed-shop baked packaging excluded");
            clock.isShopOpen = true;
            pickup.Interact(inventory); bike.Interact(inventory); bike.Interact(inventory);
            yield return null;
            Check(!manager.IsUnlocked(AchievementManager.FirstPizzaId), "Pre-opening packages moved through inventory/bike remain excluded");
            inventory.ClearPackagedPizzas();
            Pack(packing, inventory, false);
            yield return null;
            Check(!manager.IsUnlocked(AchievementManager.FirstPizzaId), "Unbaked package excluded");

            var pizza = new GameObject("Pizza").AddComponent<Pizza>(); inventory.AddPizza(pizza.gameObject);
            var oven = new GameObject("Oven").AddComponent<Oven>(); oven.enabled = false;
            Set(oven, "ovenPlacePoint", oven.transform);
            oven.Interact(inventory); Call(oven, "FinishBaking");
            yield return null;
            Check(pizza.IsBaked && !manager.IsUnlocked(AchievementManager.FirstPizzaId), "Actual oven completion alone does not unlock");
            oven.Interact(inventory);
            packing.Interact(inventory);
            pickup.Interact(inventory); bike.Interact(inventory); // 포장과 픽업/적재가 같은 프레임인 경우도 검사합니다.
            yield return null;
            Check(manager.IsUnlocked(AchievementManager.FirstPizzaId), "Actual baked packaging and same-frame pickup/storage unlock");
            Check(popup.activeSelf && text.text == "업적 1개 달성됨!", "One achievement popup is shown");
            Check(PlayerPrefs.GetInt(prefix + AchievementManager.FirstPizzaId, 0) == 1, "Achievement saved in isolated storage");
            Check(!manager.TryUnlock(AchievementManager.FirstPizzaId) && text.text == "업적 1개 달성됨!", "Duplicate completion does not notify again");
            notification.Show(2);
            Check(text.text == "업적 3개 달성됨!", "Overlapping achievement counts aggregate");
            Time.timeScale = 0;
            float started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < 4.7f) yield return null;
            Check(popup.activeSelf, "Popup still visible before five seconds with timeScale zero");
            while (Time.realtimeSinceStartup - started < 5.2f) yield return null;
            Check(!popup.activeSelf, "Popup hides after five real seconds");
            Time.timeScale = 1;
            notification.Show(1);
            Check(text.text == "업적 1개 달성됨!", "Next popup resets count");
            notification.enabled = false;

            var reload = new GameObject("Reload fixture"); reload.SetActive(false);
            var loaded = reload.AddComponent<AchievementManager>(); Set(loaded, "savePrefix", prefix);
            reload.SetActive(true);
            Check(loaded.IsUnlocked(AchievementManager.FirstPizzaId) && !popup.activeSelf, "Reload keeps completion without repeating notification");

            var panelObject = new GameObject("Settings fixture", typeof(RectTransform)); panelObject.SetActive(false);
            var settings = panelObject.AddComponent<GameSettingsPanel>();
            var window = new GameObject("Window"); window.transform.SetParent(panelObject.transform);
            var page = new GameObject("Achievements Page"); page.transform.SetParent(panelObject.transform); page.SetActive(false);
            Set(settings, "settingsWindow", window); Set(settings, "achievementsPage", page);
            panelObject.SetActive(true); settings.OpenAchievements();
            Check(page.activeSelf && !window.activeSelf, "Settings opens achievement page");
            var gate = new SettingsEscapeGate(); gate.Capture(settings, null); gate.Apply(settings, null);
            Check(panelObject.activeSelf && !page.activeSelf && window.activeSelf, "First ESC returns to settings");
            gate.Capture(settings, null); gate.Apply(settings, null);
            Check(!panelObject.activeSelf, "Second ESC closes settings");
            yield return null;
        }

        private static void Pack(PizzaPacking packing, ToppingInventory inventory, bool baked)
        {
            var pizza = new GameObject("Pizza").AddComponent<Pizza>(); pizza.SetBaked(baked);
            inventory.AddPizza(pizza.gameObject); packing.Interact(inventory);
        }
    }
}
#endif
