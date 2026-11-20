#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DeliveryService.Mok;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Yun.Editor
{
    [InitializeOnLoad]
    public static class ChapterThreeArmsSetup
    {
        const string BackupRoot = "C:/Users/q2244/.codex/visualizations/2026/09/09/01a08608-c058-7f91-9893-3ff4324de64c/chapter3-pistol";
        const string Art = "Assets/Scripts/Yun/ChapterThreeArmsArt";
        static Font font;
        static readonly Color Navy = new Color(.055f, .10f, .15f);
        static readonly Color Teal = new Color(.04f, .52f, .58f);
        static readonly Color Cream = new Color(.95f, .94f, .89f);
        static IEnumerator checks;
        static double nextCheck, startedCheck;
        static int nextFrame;
        static ChapterThreeArmsSetup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool("Yun.Arms.Check", false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                { checks = SessionState.GetBool("Yun.Arms.CheatCheck", false) ? CheatChecks() : PlayChecks(); startedCheck = EditorApplication.timeSinceStartup; nextCheck = startedCheck + 4; EditorApplication.update += TickChecks; }
                if (state == PlayModeStateChange.EnteredEditMode)
                { SessionState.SetBool("Yun.Arms.Check", false); EditorApplication.Exit(SessionState.GetInt("Yun.Arms.Result", 1)); }
            };
        }
        public static void RunPlayBatch()
        {
            SessionState.SetBool("Yun.Arms.CheatCheck", false);
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool("Yun.Arms.Check", true); SessionState.SetInt("Yun.Arms.Result", 1);
            EditorApplication.EnterPlaymode();
        }
        public static void RunCheatBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool("Yun.Arms.CheatCheck", true);
            SessionState.SetBool("Yun.Arms.Check", true); SessionState.SetInt("Yun.Arms.Result", 1);
            EditorApplication.EnterPlaymode();
        }
        static IEnumerator CheatChecks()
        {
            var cheat = DeveloperCheats.Current;
            Require(cheat, "Existing developer menu is available");
            FocusPlayer(UnityEngine.Object.FindFirstObjectByType<PlayerCombat>());
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.LeftCtrl, UnityEngine.InputSystem.Key.LeftShift, UnityEngine.InputSystem.Key.F12));
            yield return .3f;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return .3f;
            Require(cheat.IsOpen && Time.timeScale == 0, "Ctrl Shift F12 opens the existing paused cheat menu");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "chapter3-cheat-menu.png")); yield return .7f;
            // Establish old chapter-three progress to verify this is a restart, not only scene travel.
            typeof(ChapterThreeArmsMission).GetField("progress", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, ChapterThreeArmsMission.Stage.Complete);
            typeof(ChapterThreeArmsMission).GetField("granted", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
            typeof(ChapterThreeArmsMission).GetField("credit", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 25000);
            DeveloperCheats.SetInvincible(true);
            // Up wraps to Close (8), then up reaches the new chapter-three entry (7).
            for (int i = 0; i < 2; i++)
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.UpArrow)); yield return .2f;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return .2f;
            }
            Require((int)typeof(DeveloperCheats).GetField("selection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cheat) == 7, "Keyboard navigation reaches the added ninth-menu entry correctly");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter)); yield return .3f;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return 5f;
            Require(SceneManager.GetActiveScene().name == "NewHarborCity", "Chapter three button loads the new-store scene");
            var intro = UnityEngine.Object.FindFirstObjectByType<MissionManager>();
            var relay = UnityEngine.Object.FindFirstObjectByType<ChapterThreeArmsMission>();
            Require(intro && intro.State == MissionManager.MissionState.DeliverPizza, "Chapter three begins at its first delivery");
            Require(relay && relay.State == ChapterThreeArmsMission.Stage.Meet && relay.MissionCredit == 0 && !relay.Available, "Previous weapon quest and credit are cleared");
            Require(!cheat.IsOpen && Time.timeScale == 1 && !GameModeSession.IsChallenge && !DeveloperCheats.Invincible, "Menu, pause, challenge mode and invincibility are reset");
            typeof(MissionManager).GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(intro, MissionManager.MissionState.GoToDestination);
            yield return .3f; Require(relay.Available, "Old scene is unlocked before testing restart");
            cheat.Open(); Require(cheat.StartChapterThree(), "Restart also works while already in chapter three");
            Require(!cheat.StartChapterThree(), "Double activation cannot load the scene twice");
            yield return 5f;
            relay = UnityEngine.Object.FindFirstObjectByType<ChapterThreeArmsMission>();
            intro = UnityEngine.Object.FindFirstObjectByType<MissionManager>();
            Require(relay && !relay.Available && intro.State == MissionManager.MissionState.DeliverPizza, "Old handoff OnDisable cannot unlock a fresh chapter after restart");
            Debug.Log("YUN_CHAPTER3_CHEAT_PASS");
        }
        static void TickChecks()
        {
            if (EditorApplication.timeSinceStartup < nextCheck || Time.frameCount < nextFrame) return;
            try
            {
                if (EditorApplication.timeSinceStartup - startedCheck > 180) throw new TimeoutException("Pistol smoke test timeout.");
                if (checks.MoveNext()) { nextCheck = EditorApplication.timeSinceStartup + (checks.Current is float seconds ? seconds : .3f); nextFrame = Time.frameCount + 2; return; }
                Debug.Log("YUN_ARMS_PLAY_PASS"); SessionState.SetInt("Yun.Arms.Result", 0);
            }
            catch (Exception ex) { Debug.LogException(ex); Debug.LogError("YUN_ARMS_PLAY_FAIL"); }
            EditorApplication.update -= TickChecks; EditorApplication.ExitPlaymode();
        }
        static void Require(bool condition, string description)
        { if (!condition) throw new InvalidOperationException(description); Debug.Log("YUN_ARMS_CHECK: " + description); }
        static void FocusPlayer(PlayerCombat p)
        {
            var gameView = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            if (EditorWindow.focusedWindow != gameView) gameView.Focus();
            if (StoryPhoneInbox.Current) StoryPhoneInbox.Current.AutoOpenEnabled = false;
            UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            Debug.Log($"YUN_ARMS_PLAYER: enabled={p.isActiveAndEnabled} bike={p.RiddenBike} hit={p.CanReceiveHit} down={p.IsDown} physical={(p.avatar && p.avatar.IsPhysical)} time={Time.timeScale} cursor={Cursor.lockState} controllable={p.GetComponent<PlayerMove>().isControllable}");
        }
        static void UseWorldTarget(PlayerCombat p, Component target)
        {
            FocusPlayer(p);
            var position = target is ChapterThreeArmsMission ? target.transform.position + Vector3.up * 1.5f : target.GetComponent<Collider>().bounds.center;
            var horizontal = position - p.transform.position; horizontal.y = 0; p.transform.rotation = Quaternion.LookRotation(horizontal);
            p.View.transform.LookAt(position); p.GetComponentInChildren<CameraMove>()?.SyncViewFromTransform(); Physics.SyncTransforms();
            var interaction = p.GetComponent<PlayerInteraction>();
            bool hit = Physics.Raycast(p.View.transform.position, p.View.transform.forward, out var result, interaction.InteractionDistance, interaction.InteractionLayers);
            Require(hit && result.collider.GetComponentInParent<IInteractable>() == (IInteractable)target, "Saved collider is reachable by player ray: " + target.name);
            typeof(PlayerInteraction).GetMethod("TryInteract", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(interaction, null);
        }
        static IEnumerator PlayChecks()
        {
            var m = UnityEngine.Object.FindObjectsByType<ChapterThreeArmsMission>(FindObjectsSortMode.None).Single(v => !v.handoffOnly);
            var p = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            var inventory = p.GetComponent<ToppingInventory>();
            Require(!m.Available && m.legacyMerchant.enabled && !m.ui.activeSelf, "Chapter one keeps the original merchant and hides new mission UI");
            Require(m.GetComponents<MonoBehaviour>().OfType<IInteractable>().First() == m, "Existing ray interaction resolves the forwarding controller first");
            m.enableStandaloneTest = true;
            var controller = p.GetComponent<CharacterController>(); controller.enabled = false;
            p.transform.position = m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)); controller.enabled = true;
            Vector3 facing = m.transform.position - p.transform.position; facing.y = 0; p.transform.rotation = Quaternion.LookRotation(facing);
            p.View.transform.LookAt(m.transform.position + Vector3.up * 1.25f);
            p.GetComponentInChildren<CameraMove>()?.SyncViewFromTransform();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            yield return .5f;
            FocusPlayer(p);
            Require(p.CanAct, "Player can interact at the saved weapon shop");
            UseWorldTarget(p, m); yield return .4f;
            Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Dialogue, "Merchant greeting opens scene subtitle panel");
            m.nextButton.onClick.Invoke(); yield return .3f;
            Require(m.subtitle.text.Contains("나보고 일하라고"), "Next advances exactly one line");
            m.skipButton.onClick.Invoke(); yield return .4f;
            Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Candidates, "Skipping greeting still opens weapon choices");
            m.gatlingButton.onClick.Invoke();
            Require(m.notice.text.Contains("게틀링건") && m.State == ChapterThreeArmsMission.Stage.Choose, "Gatling preview does not accept an unfinished request");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "weapon-choices.png")); yield return .7f;
            m.pistolButton.onClick.Invoke(); yield return .3f; m.skipButton.onClick.Invoke(); yield return .5f;
            FocusPlayer(p);
            Require(m.State == ChapterThreeArmsMission.Stage.ReadSlip && !m.ModalOpen && p.CanAct, "Briefing skip unlocks slip task and restores control");
            UseWorldTarget(p, m.boxes[1]); yield return .3f;
            Require(m.State == ChapterThreeArmsMission.Stage.ReadSlip, "Cannot collect a box before reading the slip");
            m.Skip(); yield return .4f;
            FocusPlayer(p);
            UseWorldTarget(p, m.slip); yield return .3f;
            Require(m.State == ChapterThreeArmsMission.Stage.Compare && m.detailBody.text.Contains(m.slipCode), "Slip displays required full code");
            m.Close(); yield return .4f;
            FocusPlayer(p);
            UseWorldTarget(p, m.boxes[0]); yield return .3f; m.takeButton.onClick.Invoke(); yield return .3f;
            Require(m.State == ChapterThreeArmsMission.Stage.Compare && m.subtitle.text.Contains("끝에 거"), "Wrong box gives screenplay hint and leaves the task retryable");
            m.Skip(); yield return .4f;
            FocusPlayer(p);
            UseWorldTarget(p, m.boxes[1]); yield return .3f;
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "box-inspection.png")); yield return .7f;
            m.takeButton.onClick.Invoke(); yield return .4f;
            FocusPlayer(p);
            Require(m.State == ChapterThreeArmsMission.Stage.Carry && p.CanAct, "Matching box becomes carried cargo");
            UseWorldTarget(p, m); yield return .25f;
            Time.timeScale = 0; var atPause = m.boxes[1].transform.position; yield return .35f;
            Require(Vector3.Distance(atPause, m.boxes[1].transform.position) < .001f, "Handover animation respects game pause");
            Time.timeScale = 1; yield return 1.4f;
            Require(m.State == ChapterThreeArmsMission.Stage.Purchase && m.MissionCredit == m.pistolPrice, "Accepted handover credits exactly the weapon price");
            m.Skip(); yield return .4f;
            int moneyBefore = MoneyManager.Instance ? MoneyManager.Instance.currentMoney : 0;
            m.buyButton.onClick.Invoke(); yield return .3f;
            Require(m.State == ChapterThreeArmsMission.Stage.Complete && m.MissionCredit == 0 && p.GetComponent<PlayerWeapon>().Equipped == WeaponKind.Pistol, "Earned credit purchases and equips the existing pistol");
            m.BuyPistol();
            Require(m.MissionCredit == 0 && (!MoneyManager.Instance || MoneyManager.Instance.currentMoney == moneyBefore), "Duplicate purchase cannot charge money or duplicate credit");
            m.Skip(); yield return .5f;
            FocusPlayer(p);
            Require(!m.ModalOpen && p.CanAct && p.GetComponent<PlayerInteraction>().enabled, "Settlement skip restores normal player interaction");
            // Check the actual saved NewHarborCity relay after switching scenes; no modifications are saved by this test.
            UnityEngine.SceneManagement.SceneManager.LoadScene("NewHarborCity"); yield return 4f;
            var relay = UnityEngine.Object.FindObjectsByType<ChapterThreeArmsMission>(FindObjectsSortMode.None).Single(v => v.handoffOnly);
            Require(relay.introduction != null, "Saved new-store mission reference is present");
            typeof(MissionManager).GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(relay.introduction, MissionManager.MissionState.GoToDestination);
            yield return .4f;
            Require(relay.Available, "Mingi completion opens the chapter-three handoff");
            UnityEngine.SceneManagement.SceneManager.LoadScene("JJinCity"); yield return 4f;
            m = UnityEngine.Object.FindObjectsByType<ChapterThreeArmsMission>(FindObjectsSortMode.None).Single(v => !v.handoffOnly);
            Require(m.Available && !m.enableStandaloneTest && m.State == ChapterThreeArmsMission.Stage.Complete, "Tunnel scene travel preserves chapter-three state without editor test flag");
            File.WriteAllText(Path.Combine(BackupRoot, "play-test.txt"), "PASS: merchant compatibility, next/skip, candidates, slip prerequisite, wrong box retry, correct box carry, paused handover, credit, single purchase, control restoration, saved handoff and scene travel.\nFull playthrough navigation and subjective visual polish still require human review.");
        }

        public static void RunBatch()
        {
            try { Install(); Debug.Log("YUN_ARMS_SETUP_PASS"); EditorApplication.Exit(0); }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        [MenuItem("Tools/Yun/Chapter 3/Install pistol mission in saved scenes")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            Directory.CreateDirectory(BackupRoot);
            string backup = Path.Combine(BackupRoot, "backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(backup);
            // Include current unsaved content in the backup before opening either scene.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isLoaded && open.isDirty && !EditorSceneManager.SaveScene(open, Path.Combine(backup, open.name + "-unsaved.unity"), true))
                    throw new IOException("Could not back up unsaved scene.");
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string name in new[] { "JJinCity", "NewHarborCity" })
                File.Copy("Assets/Scenes/" + name + ".unity", Path.Combine(backup, name + ".unity"));
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
            if (!font) throw new InvalidOperationException("Jua font missing.");
            Directory.CreateDirectory(Art); AssetDatabase.Refresh();
            InstallCity();
            InstallHandoff();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            File.WriteAllText(Path.Combine(BackupRoot, "setup.txt"), "Saved JJinCity pistol mission and NewHarborCity handoff.\nBackup: " + backup);
        }
        static void InstallHandoff()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/NewHarborCity.unity");
            var intro = UnityEngine.Object.FindObjectsByType<MissionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Yun Chapter 3 Arms Handoff") ?? new GameObject("Yun Chapter 3 Arms Handoff");
            var relay = go.GetComponent<ChapterThreeArmsMission>() ?? go.AddComponent<ChapterThreeArmsMission>();
            relay.handoffOnly = true; relay.introduction = intro; relay.enableStandaloneTest = false;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static void InstallCity()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            var merchant = UnityEngine.Object.FindObjectsByType<WeaponMerchant>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var mission = merchant.GetComponent<ChapterThreeArmsMission>() ?? merchant.gameObject.AddComponent<ChapterThreeArmsMission>();
            mission.handoffOnly = false; mission.enableStandaloneTest = false; mission.legacyMerchant = merchant;
            // PlayerInteraction uses the first IInteractable. Non-chapter-three calls are forwarded to the original shop.
            while (merchant.GetComponents<MonoBehaviour>().OfType<IInteractable>().First() != mission)
                if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(mission)) throw new InvalidOperationException("Could not order merchant interaction.");
            var parent = merchant.transform.parent;
            var counter = parent.Find("Countertop");
            if (!counter) throw new InvalidOperationException("Saved weapon shop countertop not found.");
            var oldProps = parent.Find("Yun Changed Invoice Props"); if (oldProps) UnityEngine.Object.DestroyImmediate(oldProps.gameObject);
            var props = new GameObject("Yun Changed Invoice Props"); props.transform.SetParent(parent, false); mission.props = props;
            float top = counter.localPosition.y + counter.localScale.y * .5f;
            mission.slip = Prop(props.transform, mission, "입고 전표", "", "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Papers_01.prefab", new Vector3(.75f, top + .015f, -.45f), new Vector3(.38f, .025f, .28f));
            mission.boxes = new[] {
                Prop(props.transform, mission, "상자 DS-318-A", "DS-318-A", "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Cardboard_Box_01.prefab", new Vector3(-1.35f, top, -.24f), new Vector3(.48f, .34f, .44f)),
                Prop(props.transform, mission, "상자 DS-318-B", "DS-318-B", "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Cardboard_Box_01.prefab", new Vector3(-.55f, top, -.24f), new Vector3(.48f, .34f, .44f))
            };
            var rest = new GameObject("Handover rest"); rest.transform.SetParent(props.transform, false); rest.transform.localPosition = new Vector3(.25f, top, -.2f); mission.handoverRest = rest.transform;
            props.SetActive(false);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Yun Chapter 3 Arms Canvas")) UnityEngine.Object.DestroyImmediate(old);
            BuildUI(mission);
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCombat>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!player.GetComponent<PlayerWeapon>()) throw new InvalidOperationException("Existing PlayerWeapon required; no teammate rig changed.");
            EditorUtility.SetDirty(mission);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Arms mission merchant world position: " + merchant.transform.position + "; props top: " + top);
        }
        static ChapterThreeArmsPoint Prop(Transform parent, ChapterThreeArmsMission mission, string name, string code, string path, Vector3 position, Vector3 size)
        {
            var root = new GameObject(name); root.layer = 13; root.transform.SetParent(parent, false); root.transform.localPosition = position;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Missing prop: " + path);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab); visual.transform.SetParent(root.transform, false);
            var renders = visual.GetComponentsInChildren<Renderer>();
            var bounds = renders[0].bounds; foreach (var r in renders) bounds.Encapsulate(r.bounds);
            visual.transform.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            bounds = renders[0].bounds; foreach (var r in renders) bounds.Encapsulate(r.bounds);
            visual.transform.position += root.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var c in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var t in visual.GetComponentsInChildren<Transform>()) t.gameObject.layer = 13;
            var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0, size.y / 2, 0); collider.size = new Vector3(size.x, Mathf.Max(.045f, size.y), size.z);
            var point = root.AddComponent<ChapterThreeArmsPoint>(); point.mission = mission; point.code = code;
            var label = new GameObject("Label", typeof(TextMesh)); label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0, size.y * .6f, -size.z * .5f - .006f);
            var tm = label.GetComponent<TextMesh>(); tm.text = string.IsNullOrEmpty(code) ? "입고 전표" : code; tm.font = font; tm.fontSize = 48; tm.characterSize = .027f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
            label.GetComponent<Renderer>().sharedMaterial = font.material;
            return point;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = pos; return r;
        }
        static GameObject Panel(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
        {
            var r = Rect(name, parent, size, pos); var image = r.gameObject.AddComponent<Image>(); image.color = color; return r.gameObject;
        }
        static Text Label(string name, Transform parent, string text, Vector2 size, Vector2 pos, int fontSize = 25, Color? color = null)
        {
            var r = Rect(name, parent, size, pos); var t = r.gameObject.AddComponent<Text>(); t.text = text; t.font = font; t.fontSize = fontSize; t.color = color ?? Cream; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
        }
        static Button Button(string name, Transform parent, string text, Vector2 size, Vector2 pos, Color? color = null)
        {
            var p = Panel(name, parent, size, pos, color ?? Teal); var b = p.AddComponent<Button>(); b.targetGraphic = p.GetComponent<Image>();
            var colors = b.colors; colors.highlightedColor = new Color(.8f, 1, 1); colors.pressedColor = new Color(.6f, .8f, .8f); b.colors = colors;
            Label("Label", p.transform, text, size - new Vector2(12, 4), Vector2.zero, 24); return b;
        }
        static void BuildUI(ChapterThreeArmsMission m)
        {
            var canvasGo = new GameObject("Yun Chapter 3 Arms Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 160;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            m.objectivePanel = Panel("Mission status", canvasGo.transform, new Vector2(510, 98), new Vector2(-365, 287), new Color(.035f, .065f, .09f, .92f));
            Label("Chapter", m.objectivePanel.transform, "대비 / 무기상 의뢰", new Vector2(470, 25), new Vector2(0, 27), 18, new Color(1, .58f, .25f)).alignment = TextAnchor.MiddleLeft;
            m.objective = Label("Objective", m.objectivePanel.transform, "무기상과 이야기하기", new Vector2(470, 50), new Vector2(0, -13), 22);
            m.objective.alignment = TextAnchor.MiddleLeft; m.objectivePanel.SetActive(false);
            m.prompt = Label("Interaction hint", canvasGo.transform, "", new Vector2(650, 45), new Vector2(0, -265), 24);
            m.ui = Rect("Modal", canvasGo.transform, new Vector2(1280, 720), Vector2.zero).gameObject;
            var shade = Panel("Dim", m.ui.transform, new Vector2(6000, 4000), Vector2.zero, new Color(0, 0, 0, .55f));
            m.dialoguePanel = Panel("Dialogue", m.ui.transform, new Vector2(1120, 205), new Vector2(0, -205), new Color(.025f, .035f, .05f, .94f));
            m.subtitle = Label("Subtitle", m.dialoguePanel.transform, "", new Vector2(1020, 115), new Vector2(0, 20), 29);
            m.subtitle.alignment = TextAnchor.MiddleLeft; m.subtitle.gameObject.AddComponent<StorySpeakerColors>();
            m.nextButton = Button("Next", m.dialoguePanel.transform, "[F] 계속", new Vector2(180, 40), new Vector2(210, -70));
            m.skipButton = Button("Skip", m.dialoguePanel.transform, "[Space] 대화 건너뛰기", new Vector2(250, 40), new Vector2(440, -70), new Color(.18f, .23f, .28f));
            m.candidatesPanel = Panel("Weapon candidates", m.ui.transform, new Vector2(1080, 580), Vector2.zero, Navy);
            Label("Title", m.candidatesPanel.transform, "골목 무기점 · 의뢰 선택", new Vector2(900, 60), new Vector2(0, 235), 36);
            var titles = new[] { "권총", "게틀링건", "기관단총" }; var requests = new[] { "바뀐 전표", "반송된 상자 · 준비 중", "거래는 취소야 · 준비 중" };
            var buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var card = Panel(titles[i], m.candidatesPanel.transform, new Vector2(310, 330), new Vector2((i - 1) * 335, 0), new Color(.1f, .17f, .23f));
                var icon = Rect("Placeholder image", card.transform, new Vector2(240, 115), new Vector2(0, 85)).gameObject.AddComponent<Image>(); icon.sprite = Icon(i); icon.preserveAspect = true; icon.raycastTarget = false;
                Label("Name", card.transform, titles[i], new Vector2(280, 45), new Vector2(0, 0), 32);
                Label("Request", card.transform, requests[i], new Vector2(290, 45), new Vector2(0, -48), 22);
                buttons[i] = Button("Select", card.transform, i == 0 ? "의뢰 시작" : "준비 중 · 확인", new Vector2(260, 52), new Vector2(0, -115), i == 0 ? Teal : new Color(.23f, .29f, .34f));
            }
            m.pistolButton = buttons[0]; m.gatlingButton = buttons[1]; m.smgButton = buttons[2];
            m.notice = Label("Notice", m.candidatesPanel.transform, "", new Vector2(990, 65), new Vector2(0, -225), 22);
            m.detailPanel = Panel("Invoice and box inspection", m.ui.transform, new Vector2(750, 470), Vector2.zero, Navy);
            m.detailTitle = Label("Title", m.detailPanel.transform, "", new Vector2(650, 55), new Vector2(0, 170), 35);
            m.detailBody = Label("Body", m.detailPanel.transform, "", new Vector2(650, 230), new Vector2(0, 10), 29);
            m.takeButton = Button("Take box", m.detailPanel.transform, "이 상자 가져가기", new Vector2(320, 55), new Vector2(0, -170));
            m.purchasePanel = Panel("Settlement", m.ui.transform, new Vector2(760, 510), Vector2.zero, Navy);
            m.purchaseSummary = Label("Summary", m.purchasePanel.transform, "", new Vector2(660, 335), new Vector2(0, 35), 28);
            m.buyButton = Button("Purchase", m.purchasePanel.transform, "의뢰 대금으로 권총 구매", new Vector2(500, 60), new Vector2(0, -180));
            m.closeButton = Button("Close", m.ui.transform, "닫기 · ESC", new Vector2(160, 45), new Vector2(535, 325), new Color(.16f, .22f, .28f));
            m.ui.SetActive(false);
        }
        static Sprite Icon(int kind)
        {
            string path = Art + "/" + new[] { "Pistol", "Gatling", "SMG" }[kind] + "-placeholder.png";
            var tex = new Texture2D(256, 128, TextureFormat.RGBA32, false); var pixels = new Color[256 * 128];
            void Fill(int x, int y, int w, int h) { for (int py = y; py < y + h; py++) for (int px = x; px < x + w; px++) pixels[py * 256 + px] = new Color(.74f, .86f, .88f); }
            if (kind == 0) { Fill(48, 63, 160, 25); Fill(48, 25, 32, 45); Fill(80, 48, 35, 8); Fill(108, 48, 8, 25); Fill(54, 88, 10, 5); }
            else if (kind == 1) { Fill(48, 40, 62, 57); for (int j = 0; j < 3; j++) Fill(110, 44 + j * 18, 108, 11); Fill(186, 38, 10, 64); Fill(68, 20, 20, 20); }
            else { Fill(40, 60, 135, 28); Fill(175, 66, 48, 13); Fill(75, 23, 22, 39); Fill(130, 20, 18, 40); Fill(26, 50, 18, 45); }
            tex.SetPixels(pixels); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
