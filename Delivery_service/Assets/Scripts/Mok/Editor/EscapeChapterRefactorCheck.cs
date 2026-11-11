#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok.Editor
{
    // 실제 씬에서 미션 연결을 검사한다. 전제 미션과 이동 위치는 검사에서 준비한다.
    [InitializeOnLoad]
    public static class EscapeChapterRefactorCheck
    {
        private const string SessionKey = "Mok.EscapeRefactorCheck";
        private const string ResultPath = "Logs/Escape-refactor-check.txt";
        private const string BackupPath = "Temp/Escape-refactor-save-backup.json";
        private static readonly Stack<IEnumerator> routines = new();
        private static int lastFrame = -1;
        private static double started;
        private static bool failed;

        [Serializable]
        private class SavedPreference
        {
            public string key;
            public bool exists;
            public int value;
        }

        [Serializable]
        private class SaveBackup
        {
            public SavedPreference[] preferences;
            public string pendingList;
            public bool hadPendingList;
            public string economyPath;
            public string economy;
            public bool hadEconomy;
        }

        static EscapeChapterRefactorCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest() {
            const string request = "Temp/Escape-refactor-request";
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(request)) return;
            File.Delete(request); RunBatch();
        }

        [MenuItem("Tools/Delivery Service/검사/매장 습격·공원 전투·체포") ]
        public static void RunBatch()
        {
            if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Save scene edits before verification.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultPath, "Shop escape and park refactor Play Mode checks\nPrerequisites and travel positions are seeded; not a manual end-to-end playthrough.\n");
            string pending = PlayerPrefs.GetString("PendingToppingList", "");
            var names = AssetDatabase.FindAssets("t:PizzaToppingData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(guid)).toppingName)
                .Concat(pending.Split(',')).Where(name => !string.IsNullOrEmpty(name)).Distinct();
            string economyPath = Path.Combine(Application.persistentDataPath, "economySave.json");
            var backup = new SaveBackup
            {
                preferences = names.SelectMany(name => new[] { "Stock_" + name, "Pending_" + name })
                    .Select(key => new SavedPreference { key = key, exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) }).ToArray(),
                pendingList = pending,
                hadPendingList = PlayerPrefs.HasKey("PendingToppingList"),
                economyPath = economyPath,
                hadEconomy = File.Exists(economyPath),
                economy = File.Exists(economyPath) ? File.ReadAllText(economyPath) : null
            };
            File.WriteAllText(BackupPath, JsonUtility.ToJson(backup));
            EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
            SessionState.SetBool(SessionKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                failed = false;
                started = EditorApplication.timeSinceStartup;
                Application.runInBackground = true;
                routines.Clear();
                routines.Push(Verify());
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += RestoreAndExit;
            }
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Assert) return;
            failed = true;
            File.AppendAllText(ResultPath, "RUNTIME ERROR " + message + "\n" + stack + "\n");
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < 5 || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 360) throw new Exception("Test timeout");
                while (routines.Count > 0)
                {
                    IEnumerator current = routines.Peek();
                    if (!current.MoveNext()) { routines.Pop(); continue; }
                    if (current.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception exception)
            {
                failed = true;
                File.AppendAllText(ResultPath, "FAIL " + exception + "\n");
            }
            Time.timeScale = 1;
            DeveloperCheats.SetInvincible(false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(SessionKey + ".failed", failed);
            EditorApplication.ExitPlaymode();
        }

        private static void RestoreAndExit()
        {
            var backup = JsonUtility.FromJson<SaveBackup>(File.ReadAllText(BackupPath));
            foreach (SavedPreference preference in backup.preferences)
            {
                if (preference.exists) PlayerPrefs.SetInt(preference.key, preference.value);
                else PlayerPrefs.DeleteKey(preference.key);
            }
            if (backup.hadPendingList) PlayerPrefs.SetString("PendingToppingList", backup.pendingList);
            else PlayerPrefs.DeleteKey("PendingToppingList");
            PlayerPrefs.Save();
            if (backup.hadEconomy) File.WriteAllText(backup.economyPath, backup.economy);
            else if (File.Exists(backup.economyPath)) File.Delete(backup.economyPath);
            File.AppendAllText(ResultPath, "Save data restored.\n");
            SessionState.SetBool(SessionKey, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(SessionKey + ".failed", true) ? 1 : 0);
        }

        private static void Check(bool success, string label)
        {
            File.AppendAllText(ResultPath, (success ? "PASS " : "FAIL ") + label + "\n");
            if (!success) throw new Exception(label);
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        private static void Complete(object mission)
        {
            PropertyInfo state = mission.GetType().GetProperty("State");
            state.SetValue(mission, Enum.Parse(state.PropertyType, "Complete"));
        }

        private static void Place(VenueFinale mission, Vector3 position)
        {
            var anchor = new GameObject("Refactor check anchor");
            anchor.transform.position = position;
            mission.PlacePlayer(anchor.transform);
            UnityEngine.Object.Destroy(anchor);
            Physics.SyncTransforms();
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static IEnumerator Until(Func<bool> predicate, float seconds, string label)
        {
            float limit = Time.time + seconds;
            while (!predicate() && Time.time < limit) yield return null;
            Check(predicate(), label);
        }

        private static void Capture(VenueFinale mission, string name)
        {
            Camera camera = mission.cinema.enabled ? mission.cinema : mission.Player.View;
            var texture = new RenderTexture(1280, 720, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Logs/Escape-refactor-" + name + ".png", pixels.EncodeToPNG());
            camera.targetTexture = previous;
            RenderTexture.active = active;
            UnityEngine.Object.Destroy(pixels);
            UnityEngine.Object.Destroy(texture);
        }

        private static IEnumerator Pickup(VenueFinale mission, ParkEscapeSequence park)
        {
            bool taken = false;
            for (int i = 0; i < 16 && !taken; i++)
            {
                Vector3 offset = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * 1.5f;
                Vector3 candidate = park.pistol.position + offset;
                candidate.y = park.pistol.position.y - 1.65f;
                if (NavMesh.SamplePosition(candidate, out var floor, 2, NavMesh.AllAreas)) candidate = floor.position;
                Place(mission, candidate);
                mission.Player.View.transform.LookAt(park.pistol.position);
                yield return null;
                Vector3 delta = park.pistol.position - mission.Player.View.transform.position;
                File.AppendAllText(ResultPath, "PICKUP PROBE act=" + mission.Player.CanAct + " receive=" + mission.Player.CanReceiveHit + " control=" + mission.Player.GetComponent<PlayerMove>().isControllable + " phone=" + UnityEngine.Object.FindFirstObjectByType<PhoneController>().IsPhoneOpen() + " cinematic=" + mission.Cinematic + " down=" + mission.Player.IsDown + " cursor=" + Cursor.lockState + " view=" + mission.Player.View.transform.position + " pistol=" + park.pistol.position + " dot=" + Vector3.Dot(mission.Player.View.transform.forward, delta.normalized) + " hits=" + string.Join(",", Physics.RaycastAll(mission.Player.View.transform.position, delta.normalized, delta.magnitude).Select(h=>h.transform.name)) + "\n");
                taken = park.TakePistol();
            }
            Check(taken, "visible wall pistol can be picked up through existing interaction API");
        }

        private static IEnumerator Verify()
        {
            yield return Wait(1);
            float limit = Time.realtimeSinceStartup + 90;
            while (Cursor.lockState != CursorLockMode.Locked && Time.realtimeSinceStartup < limit)
            {
                Cursor.lockState = CursorLockMode.Locked;
                yield return null;
            }
            Check(Cursor.lockState == CursorLockMode.Locked, "focused game accepts interaction");
            var mission = VenueFinale.Current;
            var chapter = mission.aftermath;
            var park = chapter.parkSequence;
            Check(park && park.policeArrival && chapter.mingi && park.pistol, "saved chapter and police arrival references retained");
            StoryPhoneInbox.Current.AutoOpenEnabled = false;
            UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
            StoryTutorial.Requested = false;
            mission.Player.GetComponent<PlayerMove>().enabled = true;
            mission.Player.GetComponent<PlayerMove>().isControllable = true;
            mission.Player.View.GetComponent<CameraMove>().enabled = false;
            typeof(VenueFinale).GetProperty("State").SetValue(mission, VenueFinale.Phase.Aftermath);
            chapter.mingi.gameObject.SetActive(true);
            var citizens = UnityEngine.Object.FindObjectsByType<NPCManager>(FindObjectsSortMode.None)
                .SelectMany(manager => manager.GetComponentsInChildren<NPCControl>()).Distinct().ToArray();
            Check(citizens.Length > 1, "ambient citizens exist before raid");
            var alreadyHidden = citizens[0];
            alreadyHidden.gameObject.SetActive(false);
            int money = MoneyManager.Instance.currentMoney;
            chapter.Begin();
            Check(chapter.State == ShopEscapeChapter.Stage.Packing && park.Current == ParkEscapeSequence.Beat.Pickup, "aftermath begins at wall pistol objective");
            Check(citizens.All(n => !n.gameObject.activeSelf) && chapter.mingi.gameObject.activeInHierarchy, "raid hides citizens but preserves Mingi");
            citizens[1].gameObject.SetActive(true);
            yield return Wait(1.2f);
            Check(!citizens[1].gameObject.activeSelf, "raid suppresses newly active citizens");
            Check(park.pistol.gameObject.activeSelf && !chapter.bag.gameObject.activeSelf, "pistol replaces old bag objective");
            Time.timeScale = 0;
            Check(!park.TakePistol(), "pause blocks pickup");
            yield return null;
            Time.timeScale = 1;
            yield return Pickup(mission, park);
            Check(park.Current == ParkEscapeSequence.Beat.Run && chapter.State == ShopEscapeChapter.Stage.Escape, "pickup starts escape with companion");
            Check(mission.Player.GetComponent<PlayerWeapon>().Equipped == WeaponKind.Pistol, "mission pistol equipped");
            Check(!park.TakePistol(), "pickup cannot repeat");
            chapter.ResetChapter();
            Check(citizens.Skip(1).All(n => !n || n.gameObject.activeSelf) && !alreadyHidden.gameObject.activeSelf, "reset restores citizens; inactive=" + string.Join(",", citizens.Skip(1).Where(n => n && !n.gameObject.activeSelf).Select(n => n.name)) + " originalHidden=" + alreadyHidden.gameObject.activeSelf);
            Check(chapter.State == ShopEscapeChapter.Stage.None && park.Current == ParkEscapeSequence.Beat.None && mission.getaway.HiddenTrafficCount == 0, "reset restores traffic and clears chapter state");
            chapter.Begin();
            yield return Pickup(mission, park);
            yield return Wait(.5f);
            Check(chapter.CompanionFollowSpeed >= 2.2f && chapter.CompanionFollowSpeed <= 7.5f, "companion follow speed retains walking and running bounds");
            Place(mission, park.park.position);
            Check(NavMesh.SamplePosition(park.partnerCover.position + Vector3.right, out var companionFloor, 2, NavMesh.AllAreas), "companion test arrival uses walkable ground");
            chapter.mingi.position = companionFloor.position;
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.LookBack, 3, "arrival with companion turns camera toward threat");
            Check(mission.cinema.enabled && !mission.Player.CanAct, "look-back cinematic owns controls");
            Time.timeScale = 0;
            Quaternion frozen = mission.cinema.transform.rotation;
            yield return null; yield return null;
            Check(Quaternion.Angle(frozen, mission.cinema.transform.rotation) < .01f, "pause freezes look-back camera");
            Time.timeScale = 1;
            park.fightSeconds = 12;
            park.defendSeconds = 8;
            park.lineSeconds = 2;
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Fight, 6, "look-back hands controls to firefight");
            Check(!mission.cinema.enabled && park.ProtectedFirefight, "firefight has player control and survival protection");
                        for (int probe = 0; probe < 15 && park.SupportShots == 0; probe++)
            {
                yield return Wait(5);
                foreach (var actor in mission.Enemies.Select(e => e.GetComponent<ParkCoverActor>()).Append(chapter.mingi.GetComponent<ParkCoverActor>()))
                {
                    if (!actor) continue;
                    var agent = actor.GetComponent<NavMeshAgent>();
                    File.AppendAllText(ResultPath, "COVER active=" + actor.isActiveAndEnabled + " " + actor.name + " pos=" + actor.transform.position + " target=" + actor.cover.position + " at=" + actor.AtCover + " agent=" + (agent ? agent.enabled + "/" + agent.isOnNavMesh + "/" + agent.pathStatus + "/" + agent.velocity : "none") + "\n");
                }
            }
            Check(park.SupportShots > 0, "enemies reach cover and Mingi fires support shots");
            Check(mission.Enemies.Any(e => e && e.GetComponent<ParkCoverActor>()), "spawned enemies use park cover actors");
            Capture(mission, "combat");
            var enemy = mission.Enemies.FirstOrDefault(e => e && e.Health > 0 && !e.Npc.IsKnockedDown);
            Check(enemy && ParkEscapeSequence.Protects(enemy), "park enemies retain nonfatal combat rule");
            Check(enemy.ReceiveBullet(mission.Player, Vector3.forward, 999) && enemy.Health > 0, "heavy hit knocks enemy down without ending encounter");
            Check(mission.Player.ReceiveBullet(Vector3.back), "player bullet hit starts protected reaction");
            yield return Until(() => mission.Player.IsDown, 2, "player still ragdolls during protected encounter");
            yield return Until(() => !mission.Player.IsDown, 10, "player recovers without hospital transfer");
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Wounded, 35, "combat leads to Mingi injury cinematic");
            Check(chapter.mingi.GetComponentInChildren<Animator>().enabled == false && !park.mingiGun.gameObject.activeSelf, "injury pose owns Mingi bones and lowers gun");
            yield return Wait(1);
            Capture(mission, "injury");
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Defend, 5, "injury returns control to defense");
            Place(mission, chapter.mingi.position + Vector3.right * 2);
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Arrival, 20, "defense ends with police arrival");
            Check(chapter.State == ShopEscapeChapter.Stage.Arrest && mission.cinema.enabled, "police arrival starts arrest cinematic");
            Check(mission.PendingSpawnCount == 0, "arrest cancels pending enemy spawns");
            yield return Wait(park.policeArrival.driveSeconds + .8f);
            Check(park.policeArrival.doors.Any(d => Quaternion.Angle(d.localRotation, Quaternion.identity) > 5), "police car doors open during exit");
            Capture(mission, "police-arrival");
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Call, 50, "police exit and approach before phone call");
            yield return Wait(2);
            Check(park.phoneProp.gameObject.activeSelf, "officer holds phone during call");
            Check(!mission.subtitle.text.Contains("박진웅 · 전화:"), "phone scene does not reveal unheard remote dialogue");
            Capture(mission, "phone-call");
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Release, 12, "phone call releases gang");
            Check(park.GangReleased && !park.phoneProp.gameObject.activeSelf, "gang release restores phone pose");
            yield return Until(() => park.Current == ParkEscapeSequence.Beat.Custody, 8, "only player and Mingi enter custody");
            Check(mission.Player.GetComponent<PlayerWeapon>().Equipped == WeaponKind.None, "custody lowers player weapon");
            yield return Until(() => chapter.State == ShopEscapeChapter.Stage.Transfer, 12, "arrest fades into detention transfer");
            Check(park.Current == ParkEscapeSequence.Beat.Done && mission.Enemies.Length == 0 && !park.policeCar.gameObject.activeSelf, "transfer cleans combat and police presentation");
            Check(chapter.mingi.GetComponentInChildren<Animator>().enabled, "transfer restores companion animator");
            Check(mission.getaway.HiddenTrafficCount == 0, "traffic restored after park encounter");
            Check(citizens.Skip(1).All(n => !n || n.gameObject.activeSelf) && !alreadyHidden.gameObject.activeSelf, "arrest restores ambient citizens; inactive=" + string.Join(",", citizens.Skip(1).Where(n => n && !n.gameObject.activeSelf).Select(n => n.name)) + " originalHidden=" + alreadyHidden.gameObject.activeSelf);
            Check(MoneyManager.Instance.currentMoney == money, "story arrest does not charge a fine or hospital fee");
            Capture(mission, "detention");
            yield return Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PrisonChapter", 45, "detention transfers to prison preview scene");
            File.AppendAllText(ResultPath, "ALL CHECKS PASSED\n");
        }

    }
}
#endif






