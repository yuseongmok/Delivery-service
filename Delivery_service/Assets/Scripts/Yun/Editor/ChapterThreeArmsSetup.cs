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
using UnityEngine.AI;

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
                { checks = SessionState.GetBool("Yun.Arms.GatlingCheck", false) ? GatlingChecks() : SessionState.GetBool("Yun.Arms.CheatCheck", false) ? CheatChecks() : PlayChecks(); startedCheck = EditorApplication.timeSinceStartup; nextCheck = startedCheck + 4; EditorApplication.update += TickChecks; }
                if (state == PlayModeStateChange.EnteredEditMode)
                { SessionState.SetBool("Yun.Arms.Check", false); EditorApplication.Exit(SessionState.GetInt("Yun.Arms.Result", 1)); }
            };
        }
        public static void RunPlayBatch()
        {
            SessionState.SetBool("Yun.Arms.GatlingCheck", false);
            SessionState.SetBool("Yun.Arms.CheatCheck", false);
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool("Yun.Arms.Check", true); SessionState.SetInt("Yun.Arms.Result", 1);
            EditorApplication.EnterPlaymode();
        }
        public static void RunCheatBatch()
        {
            SessionState.SetBool("Yun.Arms.GatlingCheck", false);
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool("Yun.Arms.CheatCheck", true);
            SessionState.SetBool("Yun.Arms.Check", true); SessionState.SetInt("Yun.Arms.Result", 1);
            EditorApplication.EnterPlaymode();
        }
        public static void RunGatlingBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool("Yun.Arms.GatlingCheck", true);
            SessionState.SetBool("Yun.Arms.Check", true); SessionState.SetInt("Yun.Arms.Result", 1);
            EditorApplication.EnterPlaymode();
        }
        static IEnumerator GatlingChecks()
        {
            // Isolate the scripted route test from unrelated ambient NPC combat.
            bool previousInvincibility = DeveloperCheats.Invincible;
            DeveloperCheats.SetInvincible(true);
            var m = UnityEngine.Object.FindObjectsByType<ChapterThreeArmsMission>(FindObjectsSortMode.None).Single(v => !v.handoffOnly);
            var p = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            m.enableStandaloneTest = true;
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)), Vector3.zero);
            UseWorldTarget(p, m); yield return .4f; m.Skip(); yield return .4f;
            m.gatlingButton.onClick.Invoke(); yield return .4f;
            Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Dialogue && m.dialoguePanel.transform.Find("Top letterbox").gameObject.activeSelf, "Gatling briefing uses the merchant's letterboxed dialogue");
            m.Skip(); yield return .6f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingTravel && !m.props.activeSelf && !m.ModalOpen, "Gatling acceptance starts travel without starting the pistol puzzle");
            var navigation = UnityEngine.Object.FindFirstObjectByType<NavigationManager>();
            Require(navigation && navigation.SelectedPlace == m.gatlingParking && m.destinationMarker.gameObject.activeSelf, "Parking navigation and distance marker are connected");
            Require(m.gatlingParking.transform.position.x < -150 && m.gatlingDestination.transform.position.x < -195, "Destination references the existing Seoan district");
            Time.timeScale = 0;
            StandNear(p, m.gatlingParking.transform.position, Vector3.zero); yield return .4f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingTravel, "Paused arrival cannot advance the mission");
            Time.timeScale = 1; yield return .5f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingDelivery && navigation.SelectedPlace == m.gatlingDestination, "Parking arrival changes guidance to the pedestrian delivery address");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "gatling-parking.png")); yield return .8f;
            var rewardBefore = m.MissionCredit;
            StandNear(p, m.gatlingDestination.transform.position, Vector3.zero); yield return .6f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingArrived && m.ModalOpen && m.chaseCamera.enabled, "Arrival shows the thief carrying the parcel before gameplay");
            Require(m.gatlingBlockers.Length == 0 && m.thiefActor.animator.isHuman, "Old obstacle puzzle is removed and thief uses an existing humanoid rig");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "chase-introduction.png")); yield return .6f;
            m.Skip(); yield return .5f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingChase && !m.chaseCamera.enabled && !m.ModalOpen, "Skipping intro hands off correctly to chase gameplay");
            Require(!m.TryCatchThief(), "Cannot catch the thief remotely");
            Time.timeScale = 0; yield return .2f; var pausedPosition = m.thief.transform.position; yield return .5f;
            Require(Vector3.Distance(pausedPosition, m.thief.transform.position) < .02f && m.State == ChapterThreeArmsMission.Stage.GatlingChase, "Pause stops thief movement and chase progression");
            Time.timeScale = 1;
            int furthest = 0;
            for (int i = 0; i < 200 && m.State == ChapterThreeArmsMission.Stage.GatlingChase; i++)
            {
                // Follow the actual moving agent; never teleport it past a route segment.
                var behind = m.thief.transform.position - m.thief.transform.forward * 5;
                if (NavMesh.SamplePosition(behind, out var follow, 2f, NavMesh.AllAreas)) StandNear(p, follow.position, Vector3.zero);
                furthest = Mathf.Max(furthest, m.ChaseWaypoint);
                if (i == 10) ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "chase-running.png"));
                yield return .4f;
            }
            Require(furthest >= m.chaseRoute.Length - 1 && m.State == ChapterThreeArmsMission.Stage.GatlingRetry, $"Thief traverses the full saved route and escaping triggers retry: furthest={furthest}, state={m.State}, position={m.thief.transform.position}, remaining={m.thiefAgent.remainingDistance}, path={m.thiefAgent.pathStatus}, down={p.IsDown}");
            yield return .4f; m.Skip(); yield return .4f;
            Require(Vector3.Distance(p.transform.position, m.chaseRestart.position) < 2 && m.State == ChapterThreeArmsMission.Stage.GatlingArrived, $"Retry returns to chase start without repeating delivery travel: {m.State}, distance={Vector3.Distance(p.transform.position, m.chaseRestart.position)}, down={p.IsDown}, screen={m.CurrentScreen}");
            m.Skip(); yield return .5f;
            StandNear(p, m.thief.transform.position - m.thief.transform.forward * 1.4f, Vector3.zero);
            Require(m.TryCatchThief(), "Nearby unobstructed thief can be caught"); yield return 1f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingCargoReady && m.gatlingCargo.GetComponent<Collider>().enabled, "Catch drops a recoverable parcel");
            m.Skip(); yield return .4f;
            StandNear(p, m.gatlingCargo.transform.position, new Vector3(1, .1f, 0));
            UseWorldTarget(p, m.gatlingCargo); yield return .5f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingReturn && navigation.SelectedPlace == m.gatlingMerchant, "Recovered parcel enables merchant return guidance");
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)), Vector3.zero);
            UseWorldTarget(p, m); yield return 1.2f; m.Skip(); yield return .5f;
            Require(m.GatlingOwned && !m.gatlingCargoRoot.activeSelf && !m.ModalOpen, "Handover completes the draft and records the gatling reward");
            m.ChoosePistol(); m.ChooseGatling(); yield return .2f;
            Require(m.State == ChapterThreeArmsMission.Stage.GatlingComplete, "Completed reward cannot be claimed by stale selection callbacks");
            ChapterThreeArmsMission.PrepareChapterRestart(); yield return .3f;
            Require(m.State == ChapterThreeArmsMission.Stage.Meet, "Chapter restart resets gatling travel");
            DeveloperCheats.SetInvincible(previousInvincibility);
            Debug.Log("YUN_GATLING_MISSION_PASS");
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
            // Invoke the same dispatch used by the mouse button, independently of desktop focus.
            typeof(DeveloperCheats).GetMethod("Activate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cheat, new object[] { 7 });
            yield return 5f;
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
        static void StandNear(PlayerCombat p, Vector3 target, Vector3 offset)
        {
            var cc = p.GetComponent<CharacterController>();
            float footOffset = p.transform.position.y - p.transform.TransformPoint(cc.center - Vector3.up * cc.height * .5f).y;
            cc.enabled = false;
            p.transform.position = target + offset + Vector3.up * footOffset; cc.enabled = true; FocusPlayer(p);
        }
        static IEnumerator PlayChecks()
        {
            var m = UnityEngine.Object.FindObjectsByType<ChapterThreeArmsMission>(FindObjectsSortMode.None).Single(v => !v.handoffOnly);
            var p = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            var inventory = p.GetComponent<ToppingInventory>();
            Require(!m.Available && m.legacyMerchant.enabled, "Original shop is preserved before chapter three");
            m.enableStandaloneTest = true;
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)), Vector3.zero);
            UseWorldTarget(p, m); yield return .4f;
            Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Dialogue && !m.shade.activeSelf && !m.dialoguePanel.GetComponent<Image>() && !m.nextButton && !m.skipButton, "Dialogue uses plain white subtitles with no panel or buttons");
            Require(m.subtitle.font.name.Contains("NanumGothic") && m.subtitle.fontSize == 34 && Mathf.Approximately(m.subtitle.lineSpacing, 1.15f), "Subtitle uses saved story typography");
            Require(m.subtitle.rectTransform.anchorMin == new Vector2(.1f, .09f) && m.subtitle.rectTransform.anchorMax == new Vector2(.9f, .19f) && m.dialoguePanel.GetComponent<CanvasScaler>().referenceResolution == new Vector2(1920, 1080), "Subtitle uses saved story layout and resolution");
            Require(m.dialoguePanel.GetComponentsInChildren<Image>().Count(v => v.color == Color.black && !v.raycastTarget) == 2, "Dialogue displays two non-interactive letterbox bars");
            Require(m.boxes.Select(v => v.markEuler).Distinct().Count() == 6 && m.boxes.Select(v => v.markPosition).Distinct().Count() == 12, "Twelve saved markings vary across all six box faces");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "puzzle-subtitles.png")); yield return .8f;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var firstLine = m.subtitle.text;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F)); yield return .3f;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return .3f;
            Require(m.subtitle.text != firstLine, "F advances dialogue");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space)); yield return .3f;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return .3f;
            Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Candidates && !m.dialoguePanel.activeSelf, "Space skips dialogue and retains its required mission transition");
            m.ChoosePistol(); yield return .3f; m.Skip(); yield return .5f;
            Require(m.boxes.Length == 12 && m.FragmentCount == 0 && !m.InvoiceAssembled, "Twelve boxes start with no invoice knowledge");
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(-1.6f, .1f, -6.2f)), Vector3.zero);
            var look = m.boxes[4].transform.position;
            var horizontal = look - p.transform.position; horizontal.y = 0; p.transform.rotation = Quaternion.LookRotation(horizontal);
            p.View.transform.LookAt(look); p.GetComponentInChildren<CameraMove>()?.SyncViewFromTransform();
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "puzzle-boxes-world.png")); yield return .8f;
            FocusPlayer(p); m.slip.Interact(inventory); yield return .3f;
            Require(!m.InvoiceAssembled && m.State == ChapterThreeArmsMission.Stage.ReadSlip, "Counter cannot reveal the full code before all fragments");
            m.Skip(); yield return .5f;
            foreach (int index in new[] { 0, 1, 6, 10 })
            {
                StandNear(p, m.boxes[index].transform.position, m.transform.parent.TransformVector(new Vector3(.55f, .08f, -.55f)));
                UseWorldTarget(p, m.boxes[index]); yield return 1.4f;
                Require(m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Dialogue, "Shaking a box ends in a search result");
                m.Skip(); yield return .5f;
                if (index == 1)
                {
                    FocusPlayer(p); m.boxes[index].Interact(inventory); yield return .3f;
                    Require(m.FragmentCount == 1, "Rechecking a box cannot duplicate its fragment");
                    m.Skip(); yield return .5f;
                }
            }
            Require(m.FragmentCount == 3 && m.CurrentScreen == ChapterThreeArmsMission.ScreenKind.Slip && !m.InvoiceAssembled, "Three fragments unlock manual assembly, not automatic completion");
            m.PlaceFragment(0); m.PlaceFragment(1); m.PlaceFragment(2);
            Require(!m.InvoiceAssembled, "Wrong piece order is rejected without losing pieces");
            m.ClearAssembly(); m.PlaceFragment(2); m.PlaceFragment(0); m.PlaceFragment(1);
            Require(m.InvoiceAssembled && m.State == ChapterThreeArmsMission.Stage.Compare, "Correct assembly unlocks box comparison");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "puzzle-invoice.png")); yield return .8f;
            m.Close(); yield return .5f;
            StandNear(p, m.boxes[0].transform.position, m.transform.parent.TransformVector(new Vector3(.55f, .08f, -.55f)));
            UseWorldTarget(p, m.boxes[0]); yield return .3f;
            Require(m.previewCode.transform.localPosition == m.boxes[0].markPosition, "Inspection uses the selected box's saved marking position");
            var rotation = m.previewPivot.rotation; m.RotateInspection(new Vector2(1, 1));
            Require(Quaternion.Angle(rotation, m.previewPivot.rotation) > .01f && m.previewStage.activeSelf, "Inspection rotates the saved rendered box");
            Require(m.previewCode.GetComponent<Renderer>().sharedMaterial.shader.name == "DeliveryService/Yun/FadedBoxText", "Faded label uses depth-tested one-sided text");
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "puzzle-box-front.png")); yield return .8f;
            m.previewPivot.localRotation = Quaternion.Euler(12, 190, 0);
            ScreenCapture.CaptureScreenshot(Path.Combine(BackupRoot, "puzzle-box-inspection.png")); yield return .8f;
            m.takeButton.onClick.Invoke(); yield return .5f;
            Require(m.State == ChapterThreeArmsMission.Stage.Carry, "Left click selects even an incorrect box for physical handover");
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)), Vector3.zero);
            UseWorldTarget(p, m); yield return 1.2f;
            Require(m.State == ChapterThreeArmsMission.Stage.Compare && m.MissionCredit == 0 && m.InvoiceAssembled, "Merchant rejects wrong box and preserves assembled invoice for retry");
            m.Skip(); yield return .5f;
            StandNear(p, m.boxes[4].transform.position, m.transform.parent.TransformVector(new Vector3(.55f, .08f, -.55f)));
            UseWorldTarget(p, m.boxes[4]); yield return .4f; m.takeButton.onClick.Invoke(); yield return .5f;
            StandNear(p, m.transform.parent.TransformPoint(new Vector3(.5f, .1f, -1.9f)), Vector3.zero);
            UseWorldTarget(p, m); yield return 1.2f;
            Require(m.State == ChapterThreeArmsMission.Stage.Purchase && m.MissionCredit == m.pistolPrice, "Only the matching box earns payment");
            m.Skip(); yield return .4f; m.buyButton.onClick.Invoke(); yield return .4f; m.Skip(); yield return .5f;
            Require(m.State == ChapterThreeArmsMission.Stage.Complete && !m.ModalOpen && p.GetComponent<PlayerInteraction>().enabled, "Correct delivery and purchase finish the mission and restore control");
            ChapterThreeArmsMission.PrepareChapterRestart();
            Require(m.FragmentCount == 0 && !m.InvoiceAssembled, "Chapter restart clears fragment and assembly progress");
            Debug.Log("YUN_INVOICE_PUZZLE_PASS");
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
            var previous = intro.deliveryCutscene.lines;
            var brokerCamera = previous.First(v => v.cameraPoint && v.text.StartsWith("실장:")).cameraPoint;
            var playerCamera = previous.First(v => v.cameraPoint && !v.text.StartsWith("실장:")).cameraPoint;
            string[] brokerDialogue = {
                "플레이어: 실장?? 살아있던거냐",
                "실장: 어 조직에서 겨우 도망쳐서 아직도 도피 생활중이야 그러던중 너네가 탈옥했다는 뉴스를 보고 놀랐잖아",
                "플레이어: 좀 많은 일이 있었지 그래도 지금은 범죄생활 청산하고 민기랑 새 신원으로 큰 매장을 운영중이야",
                "실장: 알고있어 너네가 어디로간지 찾느라 엄청 힘들었어",
                "플레이어: 왜? 우린 범죄 관련해선 손 땠어 더이상 안해",
                "실장: 그런게 아니야 지금 너네가 위험하단걸 경고하러 왔어",
                "실장: 야마자키파와 박진웅이 엮어져있는 돈을 썼잖아",
                "실장: 지금 야마자키파와 박진웅 두 조직이 너네를 찾겠다고 혈안이 되어있어",
                "실장: 새로운 신분에 지역도 이동했으니 시간이 걸리겠지만 언젠가 들킬거야!",
                "플레이어: ...우린 평범하게 살 수 없는건가",
                "실장: 최소한 대비라도 해놔 내가 잘 알고있는 무기상이 있어 연결해줄게",
                "플레이어: ...알겠어 고마워"
            };
            intro.deliveryCutscene.lines = brokerDialogue.Select(text => new MissionManager.CutsceneLine { text = text, cameraPoint = text.StartsWith("실장:") ? brokerCamera : playerCamera }).ToArray();
            EditorUtility.SetDirty(intro);
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
            string[] codes = { "DS-318-8", "DS-381-B", "DS-318-P", "DS-813-B", "DS-318-B", "D5-318-B", "DS-313-B", "DS-318-R", "DS-319-B", "DS-388-B", "DS-318-D", "D8-318-B" };
            mission.boxes = new ChapterThreeArmsPoint[codes.Length];
            for (int i = 0; i < codes.Length; i++)
            {
                var box = Prop(props.transform, mission, "배송 상자 " + (i + 1), codes[i], "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Cardboard_Box_01.prefab", new Vector3(-3.2f + (i % 3) * .8f, .08f, -2.35f - (i / 3) * .8f), new Vector3(.48f, .34f, .44f));
                box.fragment = i == 1 ? 0 : i == 6 ? 1 : i == 10 ? 2 : -1;
                box.transform.localRotation = Quaternion.Euler(0, (i * 37) % 65 - 32, 0);
                box.transform.Find("Label").GetComponent<TextMesh>().text = "배송 상자";
                box.transform.Find("Label").gameObject.SetActive(false);
                mission.boxes[i] = box;
            }
            mission.briefing = new[] { "무기상: 입고 전표가 찢어져서 상자들 사이로 흩어졌어. 옆에 있는 상자를 하나씩 흔들어 봐.", "플레이어: 조각부터 찾으라는 거지?", "무기상: 세 조각이야. 다 찾으면 카운터에서 이어 붙여. 상자 표식도 지워져서 돌려 가며 봐야 할 거야.", "플레이어: 이런 건 우리 가게서도 맨날 하는데.", "무기상: 그럼 오늘은 틀리지 마." };
            mission.wrongBox = new[] { "무기상: 이 상자가 아니야. 돌려줄 테니 다시 확인해.", "직원: 앞번호는 같아요. 끝에 거 보세요." };
            var rest = new GameObject("Handover rest"); rest.transform.SetParent(props.transform, false); rest.transform.localPosition = new Vector3(.25f, top, -.2f); mission.handoverRest = rest.transform;
            props.SetActive(false);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Yun Chapter 3 Arms Canvas")) UnityEngine.Object.DestroyImmediate(old);
            BuildUI(mission);
            BuildPreview(mission);
            BuildGatlingDestination(mission);
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
            m.shade = Panel("Dim", m.ui.transform, new Vector2(6000, 4000), Vector2.zero, new Color(0, 0, 0, .55f));
            BuildStorySubtitle(m);
            m.nextButton = null; m.skipButton = null;
            m.candidatesPanel = Panel("Weapon candidates", m.ui.transform, new Vector2(1080, 580), Vector2.zero, Navy);
            Label("Title", m.candidatesPanel.transform, "골목 무기점 · 의뢰 선택", new Vector2(900, 60), new Vector2(0, 235), 36);
            var titles = new[] { "권총", "게틀링건", "기관단총" }; var requests = new[] { "바뀐 전표", "반송된 상자", "거래는 취소야 · 준비 중" };
            var buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var card = Panel(titles[i], m.candidatesPanel.transform, new Vector2(310, 330), new Vector2((i - 1) * 335, 0), new Color(.1f, .17f, .23f));
                var icon = Rect("Placeholder image", card.transform, new Vector2(240, 115), new Vector2(0, 85)).gameObject.AddComponent<Image>(); icon.sprite = Icon(i); icon.preserveAspect = true; icon.raycastTarget = false;
                Label("Name", card.transform, titles[i], new Vector2(280, 45), new Vector2(0, 0), 32);
                Label("Request", card.transform, requests[i], new Vector2(290, 45), new Vector2(0, -48), 22);
                buttons[i] = Button("Select", card.transform, i < 2 ? "의뢰 시작" : "준비 중 · 확인", new Vector2(260, 52), new Vector2(0, -115), i < 2 ? Teal : new Color(.23f, .29f, .34f));
            }
            m.pistolButton = buttons[0]; m.gatlingButton = buttons[1]; m.smgButton = buttons[2];
            m.notice = Label("Notice", m.candidatesPanel.transform, "", new Vector2(990, 65), new Vector2(0, -225), 22);
            m.detailPanel = Panel("Invoice and box inspection", m.ui.transform, new Vector2(750, 470), Vector2.zero, Navy);
            m.detailTitle = Label("Title", m.detailPanel.transform, "", new Vector2(650, 55), new Vector2(0, 170), 35);
            m.detailBody = Label("Body", m.detailPanel.transform, "", new Vector2(650, 90), new Vector2(0, 85), 25);
            m.fragmentButtons = new Button[3];
            for (int i = 0; i < 3; i++) m.fragmentButtons[i] = Button("Fragment " + i, m.detailPanel.transform, m.fragmentTexts[i], new Vector2(180, 75), new Vector2((i - 1) * 210, 0), new Color(.42f, .35f, .22f));
            m.assemblyStatus = Label("Assembly result", m.detailPanel.transform, "", new Vector2(680, 60), new Vector2(0, -90), 23);
            m.clearAssemblyButton = Button("Reset pieces", m.detailPanel.transform, "다시 놓기", new Vector2(230, 45), new Vector2(0, -175));
            m.inspectionPanel = Rect("Rotate box inspection", m.ui.transform, new Vector2(1000, 620), Vector2.zero).gameObject;
            var viewport = Rect("Box image - left click to choose", m.inspectionPanel.transform, new Vector2(670, 460), new Vector2(0, 45));
            var raw = viewport.gameObject.AddComponent<RawImage>(); raw.color = Color.white;
            m.takeButton = viewport.gameObject.AddComponent<Button>(); m.takeButton.targetGraphic = raw;
            m.takeButton.navigation = new Navigation { mode = Navigation.Mode.None };
            m.inspectionHint = Label("Inspection controls", m.inspectionPanel.transform, "", new Vector2(1000, 85), new Vector2(0, -245), 23);
            m.purchasePanel = Panel("Settlement", m.ui.transform, new Vector2(760, 510), Vector2.zero, Navy);
            m.purchaseSummary = Label("Summary", m.purchasePanel.transform, "", new Vector2(660, 335), new Vector2(0, 35), 28);
            m.buyButton = Button("Purchase", m.purchasePanel.transform, "의뢰 대금으로 권총 구매", new Vector2(500, 60), new Vector2(0, -180));
            m.closeButton = Button("Close", m.ui.transform, "닫기 · ESC", new Vector2(160, 45), new Vector2(535, 325), new Color(.16f, .22f, .28f));
            m.ui.SetActive(false);
        }
        static void BuildGatlingDestination(ChapterThreeArmsMission m)
        {
            var scene = m.gameObject.scene;
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var parking = transforms.FirstOrDefault(t => t.name == "Motorcycle parking");
            var entrance = transforms.FirstOrDefault(t => t.name == "West entrance 01");
            if (!parking || !entrance) throw new InvalidOperationException("Existing Seoan parking/delivery entrance missing; do not invent a world position.");
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Yun Gatling Delivery Destination");
            if (old) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Yun Gatling Delivery Destination");
            DeliveryService.Mok.Navigation.NavigationPlace Place(string name, Transform anchor, string id)
            {
                var go = new GameObject(name); go.transform.SetParent(root.transform, false); go.transform.position = anchor.position;
                var place = go.AddComponent<DeliveryService.Mok.Navigation.NavigationPlace>();
                place.placeId = id; place.displayName = name; place.mapAnchor = place.entrance = go.transform;
                place.kind = DeliveryService.Mok.Navigation.NavigationPlaceKind.Other;
                return place;
            }
            m.gatlingParking = Place("서안골목 입구 주차장", parking, "yun-gatling-parking");
            m.gatlingDestination = Place("반송 배송지 · 서안골목 01", entrance, "yun-gatling-delivery");
            m.gatlingMerchant = Place("무기상", m.transform, "yun-gatling-return");
            var cargo = new GameObject("Thief chase actors"); cargo.transform.SetParent(root.transform, false);
            m.gatlingCargoRoot = cargo;
            m.gatlingBlockers = Array.Empty<ChapterThreeArmsPoint>(); m.blockerStart = m.blockerEnd = Array.Empty<Vector3>();
            BuildThiefChase(m, root.transform, cargo.transform);
            cargo.SetActive(false);
            m.destinationMarker = Label("Gatling destination marker", m.objectivePanel.transform.parent, "", new Vector2(420, 40), Vector2.zero, 18, new Color(1, .97f, .9f));
            m.destinationMarker.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic-Regular.ttf");
            m.destinationMarker.raycastTarget = false;
            var diamondRect = Rect("Target diamond", m.destinationMarker.transform, new Vector2(22, 22), new Vector2(0, 36));
            var diamond = diamondRect.gameObject.AddComponent<StoryTargetDiamond>(); diamond.color = new Color(1, .43f, .05f); diamond.raycastTarget = false;
            m.destinationMarker.gameObject.SetActive(false);
        }
        static void BuildThiefChase(ChapterThreeArmsMission m, Transform root, Transform actors)
        {
            // The east edge and central market lane are open pedestrian corridors.
            Vector3 Snap(Vector3 position)
            {
                if (!NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) throw new InvalidOperationException("No walkable chase position: " + position);
                return hit.position;
            }
            Transform Anchor(string name, Vector3 position)
            {
                var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.position = Snap(position); return go.transform;
            }
            var points = new[] { new Vector3(-199, 0, -48), new Vector3(-199, 0, -5), new Vector3(-256, 0, -5), new Vector3(-256, 0, 5), new Vector3(-294, 0, -5) };
            m.chaseRoute = points.Select((p, i) => Anchor("Chase waypoint " + i, p)).ToArray();
            m.chaseRestart = Anchor("Chase retry start", new Vector3(-199, 0, -56));
            m.chaseRestart.rotation = Quaternion.LookRotation(m.chaseRoute[0].position - m.chaseRestart.position);
            m.gatlingDestination.transform.position = m.chaseRestart.position;
            float length = 0;
            for (int i = 1; i < m.chaseRoute.Length; i++)
            {
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(m.chaseRoute[i - 1].position, m.chaseRoute[i].position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) throw new InvalidOperationException("Disconnected chase segment " + i);
                for (int j = 1; j < path.corners.Length; j++) length += Vector3.Distance(path.corners[j - 1], path.corners[j]);
            }
            Debug.Log("YUN_CHASE_ROUTE_METRES: " + length);
            var thiefObject = new GameObject("Seoan cargo thief"); thiefObject.transform.SetParent(actors, false); thiefObject.transform.position = m.chaseRoute[0].position; thiefObject.layer = 13;
            m.thief = thiefObject.AddComponent<ChapterThreeArmsPoint>(); m.thief.mission = m; m.thief.code = "THIEF";
            var capsule = thiefObject.AddComponent<CapsuleCollider>(); capsule.center = Vector3.up * .9f; capsule.height = 1.8f; capsule.radius = .3f;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Characters/Character_BusinessMan_Shirt.prefab"));
            visual.transform.SetParent(thiefObject.transform, false);
            foreach (var c in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var t in visual.GetComponentsInChildren<Transform>()) t.gameObject.layer = 13;
            var animator = visual.GetComponentInChildren<Animator>();
            if (!animator || !animator.avatar || !animator.avatar.isHuman) throw new InvalidOperationException("Thief needs an existing humanoid rig.");
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/Robber/Robber.controller"); animator.applyRootMotion = false;
            m.thiefActor = thiefObject.AddComponent<AftermathActor>(); m.thiefActor.animator = animator;
            var mount = new GameObject("Carried parcel mount").transform; mount.SetParent(thiefObject.transform, false); mount.localPosition = new Vector3(0, 1.05f, .4f); m.thiefCarry = mount;
            Transform Contact(string name, float side) { var t = new GameObject(name).transform; t.SetParent(mount, false); t.localPosition = new Vector3(side, .08f, 0); return t; }
            m.thiefActor.leftContact = Contact("Left parcel grip", -.22f); m.thiefActor.rightContact = Contact("Right parcel grip", .22f); m.thiefActor.reach = 1;
            m.thiefAgent = thiefObject.AddComponent<NavMeshAgent>(); m.thiefAgent.radius = .32f; m.thiefAgent.height = 1.8f; m.thiefAgent.speed = m.thiefSpeed; m.thiefAgent.acceleration = 16; m.thiefAgent.angularSpeed = 540; m.thiefAgent.stoppingDistance = .2f;
            m.gatlingCargo = Prop(actors, m, "Stolen return parcel", "RETURN", "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Cardboard_Box_01.prefab", mount.position, new Vector3(.45f, .3f, .35f));
            m.gatlingCargo.transform.Find("Label").gameObject.SetActive(false);
            var camera = new GameObject("Chase introduction camera", typeof(Camera)); camera.transform.SetParent(root, false);
            camera.transform.position = m.chaseRoute[0].position + new Vector3(2.2f, 1.7f, 3.2f); camera.transform.LookAt(m.chaseRoute[0].position + Vector3.up * 1.1f);
            m.chaseCamera = camera.GetComponent<Camera>(); m.chaseCamera.depth = 100; m.chaseCamera.fieldOfView = 55; m.chaseCamera.enabled = false;
        }
        static void BuildStorySubtitle(ChapterThreeArmsMission m)
        {
            // Copy the actual saved story subtitle, including its resolution and anchors.
            // This runs only in the editor; no dialogue UI is generated during Play.
            var destination = m.gameObject.scene;
            var old = destination.GetRootGameObjects().FirstOrDefault(g => g.name == "Yun Chapter 3 Dialogue Canvas");
            if (old) UnityEngine.Object.DestroyImmediate(old);
            var sourceScene = EditorSceneManager.OpenScene("Assets/Scenes/NewHarborCity.unity", OpenSceneMode.Additive);
            try
            {
                var source = sourceScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MissionManager>(true)).Single().deliveryCutscene.dialogueText;
                var sourceCanvas = source.GetComponentInParent<Canvas>(true);
                var go = new GameObject("Yun Chapter 3 Dialogue Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                SceneManager.MoveGameObjectToScene(go, destination);
                EditorUtility.CopySerialized(sourceCanvas, go.GetComponent<Canvas>());
                EditorUtility.CopySerialized(sourceCanvas.GetComponent<CanvasScaler>(), go.GetComponent<CanvasScaler>());
                go.GetComponent<Canvas>().sortingOrder = 161;
                foreach (bool top in new[] { true, false })
                {
                    var bar = new GameObject(top ? "Top letterbox" : "Bottom letterbox", typeof(RectTransform), typeof(Image));
                    bar.transform.SetParent(go.transform, false);
                    var rect = (RectTransform)bar.transform;
                    rect.anchorMin = new Vector2(0, top ? .92f : 0);
                    rect.anchorMax = new Vector2(1, top ? 1 : .08f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = bar.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
                }
                m.subtitle = UnityEngine.Object.Instantiate(source, go.transform, false);
                m.subtitle.name = "Subtitle";
                m.subtitle.text = "";
                m.subtitle.gameObject.SetActive(true);
                var controls = Label("Dialogue controls", go.transform, "F [다음 대사] / Space [건너뛰기]", new Vector2(620, 50), Vector2.zero, 24, new Color(.86f, .77f, .53f));
                controls.font = source.font; controls.alignment = TextAnchor.MiddleRight; controls.raycastTarget = false;
                controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = new Vector2(1, 0);
                controls.rectTransform.pivot = new Vector2(1, 0);
                controls.rectTransform.anchoredPosition = new Vector2(-32, 20);
                m.dialoguePanel = go;
                go.SetActive(false);
            }
            finally { EditorSceneManager.CloseScene(sourceScene, true); }
        }
        static void BuildPreview(ChapterThreeArmsMission m)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Yun Arms Box Inspection Studio")) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Yun Arms Box Inspection Studio"); root.transform.position = new Vector3(0, -2000, 0); m.previewStage = root;
            var pivot = new GameObject("Rotate this box"); pivot.transform.SetParent(root.transform, false); m.previewPivot = pivot.transform;
            var source = m.boxes[0].transform.GetChild(0);
            var visual = UnityEngine.Object.Instantiate(source.gameObject); visual.name = "Synty cardboard box"; visual.transform.SetParent(pivot.transform, false);
            visual.transform.localPosition = source.localPosition - Vector3.up * .17f; visual.transform.localRotation = source.localRotation; visual.transform.localScale = source.localScale;
            pivot.transform.localScale = Vector3.one * 2.7f;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            pivot.transform.localScale *= 1.2f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.position += pivot.transform.position - bounds.center;
            var label = new GameObject("Faded mark on back", typeof(TextMesh)); label.transform.SetParent(pivot.transform, false); label.transform.localPosition = new Vector3(0, .02f, bounds.extents.z / pivot.transform.lossyScale.z + .002f); label.transform.localRotation = Quaternion.Euler(0, 180, 0);
            m.previewCode = label.GetComponent<TextMesh>(); m.previewCode.font = font; m.previewCode.fontSize = 64; m.previewCode.characterSize = .008f; m.previewCode.anchor = TextAnchor.MiddleCenter; m.previewCode.color = new Color(.18f, .15f, .12f); m.previewCode.text = "DS-318-B";
            // Stable, saved placements: each box has one marking on a different face/offset.
            var extent = bounds.extents / pivot.transform.lossyScale.x;
            var rotations = new[] { new Vector3(0, 180, 0), new Vector3(0, 90, 0), new Vector3(90, 0, 0), Vector3.zero, new Vector3(0, -90, 0), new Vector3(-90, 0, 0) };
            for (int i = 0; i < m.boxes.Length; i++)
            {
                var rotation = Quaternion.Euler(rotations[i % rotations.Length]);
                var normal = rotation * Vector3.back;
                float distance = Mathf.Abs(normal.x) * extent.x + Mathf.Abs(normal.y) * extent.y + Mathf.Abs(normal.z) * extent.z;
                var offset = new Vector3(i < 6 ? -.018f : .018f, i % 2 == 0 ? .025f : -.025f, 0);
                m.boxes[i].markPosition = normal * (distance + .002f) + rotation * offset;
                m.boxes[i].markEuler = rotations[i % rotations.Length];
                EditorUtility.SetDirty(m.boxes[i]);
            }
            m.previewCode.characterSize = .006f;
            label.name = "Faded box mark";
            label.transform.localPosition = m.boxes[0].markPosition;
            label.transform.localRotation = Quaternion.Euler(m.boxes[0].markEuler);
            string materialPath = Art + "/FadedBoxText.mat";
            var textMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!textMaterial) { textMaterial = new Material(Shader.Find("DeliveryService/Yun/FadedBoxText")); AssetDatabase.CreateAsset(textMaterial, materialPath); }
            label.GetComponent<Renderer>().sharedMaterial = textMaterial;
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
            var camera = new GameObject("Inspection camera", typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(root.transform, false); camera.transform.localPosition = new Vector3(0, .08f, -2.7f); camera.transform.LookAt(root.transform.position);
            camera.fieldOfView = 36; camera.nearClipPlane = .05f; camera.farClipPlane = 6; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.10f, .13f, .15f); camera.allowHDR = false; camera.allowMSAA = false;
            string path = Art + "/BoxInspection.renderTexture";
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (!texture) { texture = new RenderTexture(1024, 768, 24) { name = "Box inspection" }; AssetDatabase.CreateAsset(texture, path); }
            camera.targetTexture = texture; m.previewCamera = camera; m.takeButton.GetComponent<RawImage>().texture = texture;
            var light = new GameObject("Inspection light", typeof(Light)).GetComponent<Light>(); light.transform.SetParent(root.transform, false); light.transform.localPosition = new Vector3(-1, 1.5f, -1.8f); light.type = LightType.Point; light.intensity = 3; light.range = 6; light.cullingMask = 1 << 31;
            root.SetActive(false);
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
