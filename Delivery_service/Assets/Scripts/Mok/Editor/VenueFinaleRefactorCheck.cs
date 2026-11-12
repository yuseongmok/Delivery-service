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
    public static class VenueFinaleRefactorCheck
    {
        private const string SessionKey = "Mok.VenueRefactorCheck";
        private const string ResultPath = "Logs/Venue-refactor-check.txt";
        private const string BackupPath = "Temp/Venue-refactor-save-backup.json";
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

        static VenueFinaleRefactorCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void RunBatch()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultPath, "Venue finale refactor Play Mode checks\nPrerequisites and travel positions are seeded; not a manual end-to-end playthrough.\n");
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
                if (EditorApplication.timeSinceStartup - started > 180) throw new Exception("Test timeout");
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

        private static IEnumerator Verify()
        {
            yield return Wait(.7f);
            File.AppendAllText(ResultPath, "Waiting for focused Game view and locked cursor.\n");
            float focusDeadline = Time.realtimeSinceStartup + 90;
            while (Cursor.lockState != CursorLockMode.Locked && Time.realtimeSinceStartup < focusDeadline)
            {
                Cursor.lockState = CursorLockMode.Locked;
                yield return null;
            }
            Check(Cursor.lockState == CursorLockMode.Locked, "real Game view supports locked input");
            var mission = VenueFinale.Current;
            Check(mission && mission.preparation && mission.stealth && mission.getaway && mission.aftermath,
                "saved scene references retained");
            var recon = mission.preparation.preparation;
            Complete(recon.delivery.broker);
            Complete(recon.delivery);
            Complete(recon);
            Complete(mission.preparation);
            StoryPhoneInbox.Current.AutoOpenEnabled = false;
            DeveloperCheats.SetInvincible(true);
            var move = mission.Player.GetComponent<PlayerMove>();
            var look = mission.Player.View.GetComponent<CameraMove>();
            move.enabled = true;
            move.isControllable = true;
            look.enabled = true;
            int money = MoneyManager.Instance.currentMoney;

            mission.Day.currentTime = 9;
            mission.Day.isShopOpen = false;
            int originalDay = mission.Day.currentDay;
            yield return Wait(.3f);
            Check(mission.CanStart, "completed preparations unlock finale without business or day requirements");
            Check(StoryPhoneInbox.Current.Messages.Count(m => m.title == "작전 준비 완료 · 마지막 배달") == 1, "one finale invitation");
            var phoneApp = UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);
            phoneApp.Open(false);phoneApp.Move(4);phoneApp.Confirm();phoneApp.Refresh();
            Check(phoneApp.action.text == "행사장으로 출발", "phone offers immediate departure");
            phoneApp.Confirm();
            Check(mission.State == VenueFinale.Phase.Opening && mission.Day.currentDay == originalDay, "phone enters opening without advancing day");
            Check(!mission.Begin(), "running finale cannot restart itself");
            Check(mission.cinema.enabled && !move.enabled && !mission.Player.View.enabled, "cutscene owns camera and movement");
            mission.SkipCinematic();
            Check(mission.State == VenueFinale.Phase.Entrance && move.enabled && move.isControllable
                && look.enabled && mission.Player.View.enabled, "opening releases original controls");
            mission.PlacePlayer(mission.entry);
            Time.timeScale = 0;
            Check(!mission.Interact(), "pause blocks interaction");
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Check(mission.Interact(), "entrance admits delivery player; canAct=" + mission.Player.CanAct
                + " cursor=" + Cursor.lockState + " receive=" + mission.Player.CanReceiveHit
                + " distance=" + Vector3.Distance(mission.Player.transform.position, mission.entry.position));
            mission.SkipCinematic();
            Check(mission.InsideVenue && mission.State == VenueFinale.Phase.Deposit, "admission enters venue");
            yield return Wait(.2f);
            mission.EjectFromVenue(mission.stealth.guards[0].transform);
            yield return Wait(.65f);
            Check(mission.State == VenueFinale.Phase.Ejected && mission.fade.color.a > 0, "ejection camera fades");
            mission.SkipCinematic();
            Check(!mission.InsideVenue && mission.State == VenueFinale.Phase.Entrance && move.enabled, "ejection returns outside and unlocks movement");
            Check(mission.Interact(), "readmission remains available");
            mission.SkipCinematic();
            mission.stealth.enabled = false;
            foreach (var guard in mission.stealth.guards)
            {
                guard.enabled = false;
                var agent = guard.GetComponent<NavMeshAgent>();
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            }
            Place(mission, mission.deliveryTable.position + Vector3.back * 2);
            Check(mission.Interact() && mission.State == VenueFinale.Phase.Observe, "pizza deposit advances to observation");
            yield return null;
            Check(mission.objective.text.Contains("박진웅") || mission.objective.text.Contains("관찰"), "objective presentation matches observation");
            // 경비 판단을 제외하고 기존 관찰 위치와 시선 판정을 검사한다.
            foreach (var guard in mission.stealth.guards) guard.gameObject.SetActive(false);
            mission.PlacePlayer(mission.observeStand);
            look.enabled = false;
            mission.Player.View.transform.LookAt(mission.target.position + Vector3.up * 1.4f);
            mission.stealth.Tick(.1f);
            Check(mission.CanObserve(), "listening checks target sightline");
            mission.Observe(true, mission.observeSeconds + 1);
            Check(mission.State == VenueFinale.Phase.Conversation, "listening advances to editable conversation");
            mission.AdvanceConversation(true);
            Check(mission.IntelSecured && mission.State == VenueFinale.Phase.Exposed, "conversation exposes player and retains intel");
            mission.SkipCinematic();
            Check(mission.State == VenueFinale.Phase.InteriorEscape, "exposure starts escape combat");
            // 테스트용으로 적을 제압한다. 실제 위치 이동과 전투 난이도 평가는 별도다.
            yield return Wait(.5f);
            object roster = typeof(VenueFinale).GetField("enemyRoster", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mission);
            foreach (var guard in mission.stealth.guards.Take(2))
            {
                var enemy = guard.Combat;
                enemy.gameObject.SetActive(true);
                if (!mission.Enemies.Contains(enemy)) roster.GetType().GetMethod("Add").Invoke(roster, new object[] { enemy });
                enemy.Npc.KnockDown(Vector3.back, 2);
            }
            yield return null;
            yield return null;
            Check(mission.ExitUnlocked, "two subdued enemies unlock exit");
            Place(mission, mission.interiorExit.position);
            Check(mission.Interact() && mission.State == VenueFinale.Phase.StreetEscape && !mission.InsideVenue, "exit returns to original exterior");
            Check(Vector3.Distance(mission.Player.transform.position, (mission.streetArrival ? mission.streetArrival : mission.entry).position) < 2,
                "exterior position is retained");
            Check(!mission.SpawnOutsideView(mission.Player.View.transform.position + mission.Player.View.transform.forward * 10), "on-screen enemy spawn rejected");
            mission.StopStorySpawning();
            Check(mission.PendingSpawnCount == 0, "pending enemy requests can be cancelled");
            mission.PlacePlayer(mission.carStand);
            yield return null;
            Check(mission.Interact() && mission.State == VenueFinale.Phase.Boarding, "car interaction starts boarding");
            yield return Wait(mission.boardingSeconds * .5f);
            Check(Quaternion.Angle(mission.door.localRotation, Quaternion.identity) > 20, "boarding door opens");
            Check(Vector3.Distance(mission.cinema.transform.position, mission.car.TransformPoint(new Vector3(-1.05f, 1.12f, .16f))) < .5f,
                "boarding camera passes driver doorway");
            BrokerMissionCheck.Capture(null, "Venue-refactor-boarding", mission.cinema);
            Vector3 cameraPosition = mission.cinema.transform.position;
            Time.timeScale = 0;
            yield return null;
            yield return null;
            Check(mission.cinema.transform.position == cameraPosition, "pause freezes boarding camera");
            Time.timeScale = 1;
            yield return Wait(mission.boardingSeconds * .6f);
            Check(mission.State == VenueFinale.Phase.Driving && mission.getaway.Driving, "boarding hands over to manual drive");
            mission.SkipCinematic();
            Check(mission.State == VenueFinale.Phase.Driving, "skip cannot bypass manual driving");
            mission.AbandonCar();
            Check(mission.State == VenueFinale.Phase.Driving, "cannot abandon away from destination");
            mission.getaway.ExternalInput = true;
            Vector3 carStart = mission.car.position;
            mission.getaway.Input(.8f, 0);
            yield return Wait(.8f);
            Check(Vector3.Distance(mission.car.position, carStart) > .1f, "manual drive still moves car");
            mission.getaway.Input(0, 0);
            yield return Wait(3);
            mission.car.position = mission.driveEnd.position;
            yield return null;
            mission.AbandonCar();
            Check(mission.State == VenueFinale.Phase.Abandoning, "destination allows car abandonment");
            yield return Wait(2.1f);
            Check(Vector3.Distance(mission.cinema.transform.position, mission.car.TransformPoint(new Vector3(-1.85f, 1.65f, .16f))) < .15f,
                "exit camera reaches exterior standing height");
            mission.SkipCinematic();
            Check(mission.State == VenueFinale.Phase.Debrief && mission.fade.color.a > .95f, "abandonment fades into homecoming");
            yield return Wait(1.5f);
            var mingi = recon.delivery.kitchen.actor;
            Check(Vector3.Angle(mission.cinema.transform.forward, mingi.position + Vector3.up * 1.45f - mission.cinema.transform.position) < 3,
                "homecoming camera faces Mingi");
            BrokerMissionCheck.Capture(null, "Venue-refactor-homecoming", mission.cinema);
            mission.SkipCinematic();
            Check(mission.State == VenueFinale.Phase.Aftermath, "homecoming connects existing shop escape chapter");
            mission.Cancel();
            yield return null;
            Check(mission.State == VenueFinale.Phase.Failed && !mission.cinema.enabled && mission.PendingSpawnCount == 0
                && mission.Enemies.Length == 0 && mission.getaway.HiddenTrafficCount == 0, "cancel cleans enemies camera and traffic");
            look.enabled = true;
            Check(mission.Begin(), "failed mission can retry without losing preparation");
            mission.DeveloperComplete();
            Check(mission.Completed && !mission.Begin(), "completed mission cannot restart");
            Check(MoneyManager.Instance.currentMoney == money, "refactor does not change mission reward or money");
            File.AppendAllText(ResultPath, "ALL CHECKS PASSED\n");
        }
    }
}
#endif
