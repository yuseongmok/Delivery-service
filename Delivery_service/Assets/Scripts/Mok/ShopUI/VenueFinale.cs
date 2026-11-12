using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    // 준비 성과를 소비하지 않는다. 실패해도 승인·동선·차량 지원을 유지해 재시도한다.
    public sealed class VenueFinale : MonoBehaviour
    {
        public enum Phase
        {
            Available,
            Scheduling,
            Opening,
            Entrance,
            Admission,
            Deposit,
            Observe,
            Conversation,
            Ejected,
            Exposed,
            InteriorEscape,
            StreetEscape,
            Boarding,
            Driving,
            Abandoning,
            Debrief,
            Aftermath,
            Complete,
            Failed
        }

        public static VenueFinale Current { get; private set; }

        public static bool Active => Current && Current.Running;
        public Phase State { get; private set; }

        public bool Running => State >= Phase.Scheduling && State <= Phase.Aftermath;
        public bool InsideVenue => insideVenue;
        public bool Completed => State == Phase.Complete || (aftermath && aftermath.InPrison);
        public bool Cinematic => (State == Phase.Aftermath && aftermath && aftermath.Cinematic) || State == Phase.Ejected || State == Phase.Opening || State == Phase.Admission || State == Phase.Exposed || State == Phase.Boarding || State == Phase.Driving || State == Phase.Abandoning || State == Phase.Debrief;
        // 씬과 편집 도구의 연결을 보존하기 위해 기존 필드 이름은 유지한다.
        [Header("미션 연결")]
        public ShopEscapeChapter aftermath;
        public VenueGetawayDrive getaway;
        public VenueStealth stealth;
        public VenueStreetPursuit streetPursuit;
        public MechanicMission preparation;
        public GameObject interior;
        public GameObject ui;
        public GameObject pizza;
        public GameObject approval;
        public GameObject actors;
        public Transform entryGuard;
        public Transform entry;
        public Transform inside;
        public Transform deliveryTable;
        public Transform observeStand;
        public Transform target;
        public Transform contact;
        public Transform interiorExit;
        public Transform escapeTurn;
        public Transform carStand;
        public Transform car;
        public Transform door;
        public Transform seat;
        public Transform driver;
        public Transform passenger;
        public Transform driveEnd;
        public Transform safePoint;
        public Transform[] interiorPosts;
        public Transform[] streetPosts;
        public Transform streetArrival;
        public Transform[] serviceRoute;
        public Transform[] alleyRoute;
        private int serviceStep;
        private int alleyStep;
        private readonly HashSet<CitizenCombat> subdued = new();
        public int SubduedCount => subdued.Count;
        public bool ExitUnlocked => SubduedCount >= 2;
        public Transform EscapeGoal => State == Phase.InteriorEscape ? interiorExit : carStand;
        public Text objective;
        public Text subtitle;
        public Text marker;
        public Image fade;
        public Camera cinema;
        [Header("연출 사운드와 시간")]
        public AudioSource sound;
        public AudioSource engine;
        public AudioClip stamp;
        public AudioClip alarm;
        public AudioClip doorOpen;
        public AudioClip doorClose;
        public float observeSeconds = 3;
        public float openingSeconds = 5;
        public float revealSeconds = 3.5f;
        public float boardingSeconds = 3.2f;
        public float driveSeconds = 6;
        [Header("수정 가능한 대사")]
        [TextArea(2, 6)]
        public string[] opening = {"민기: 오늘은 문 닫아. 납품 승인, 출입구, 탈출 차량까지 준비됐어.", "민기: 박진웅한테 따지러 가는 게 아니야. 왜 우릴 쫓는지, 안 들키고 들어 봐.", "정비사: 약속한 곳에 차와 열쇠를 뒀어. 난 먼저 빠질게."};
        [TextArea(2, 6)]
        public string[] overheard = ConspiracyDialogue.Overheard();
        [TextArea(2, 6)]
        public string[] closing = ConspiracyDialogue.Closing();
        public float ObservationProgress { get; private set; }

        public bool IntelSecured { get; private set; }

        public string FailureReason { get; private set; }

        public int EventDay { get; private set; }

        public CitizenCombat[] Enemies => enemyRoster.Snapshot;
        private bool insideVenue;
        private bool reinforced;
        private bool turned;
        private bool announced;
        private int line;
        private float entered;
        private Vector3 carOrigin;
        private Quaternion carRotation;
        public PlayerCombat Player => preparation.Player;
        public DayNightCycle Day => preparation.preparation.delivery.broker.day;
        public bool Ready => preparation && preparation.Completed && preparation.preparation.Completed && preparation.preparation.delivery.Completed && preparation.preparation.delivery.broker.Completed;
        public bool CanStart => Ready && !Running && !BrokerMission.Active && !SpecialOrderMission.Active && !BackdoorMission.Active && !MechanicMission.Active && !Completed && !Day.IsTransitioning && !StoryTutorial.Active && !Player.IsDown && !Player.RiddenBike && !Player.WaterRescueActive && !PoliceControl.IsArrestInProgress;
        private VenueFinaleEnemies enemyRoster;
        private VenueFinaleCinematics cinematic;
        private VenueFinalePresentation presentation;
        private void Awake()
        {
            enemyRoster = new VenueFinaleEnemies(this);
            cinematic = new VenueFinaleCinematics(this);
            presentation = new VenueFinalePresentation(this);
            gameObject.AddComponent<StorySoundscape>();
            gameObject.AddComponent<PrologueMissionMusic>();
            StorySoundPause.Attach(sound);
            var bank = SuppliedAudio.Current;
            if (bank)
            {
                doorOpen = bank.carOpen;
                doorClose = bank.carClose;
            }

            Current = this;
            ui.SetActive(false);
            cinema.enabled = false;
            actors.SetActive(false);
            pizza.SetActive(false);
            approval.SetActive(false);
            car.gameObject.SetActive(false);
            fade.gameObject.SetActive(false);
            carOrigin = car.position;
            carRotation = car.rotation;
            if (stealth)
            {
                stealth.ResetAttempt();
            }
        }

        private void OnDestroy()
        {
            if (getaway)
            {
                getaway.RestoreTraffic();
            }

            cinematic.Release();
            Clean();
            if (Current == this)
            {
                Current = null;
            }
        }

        private void OnDisable()
        {
            if (getaway)
            {
                getaway.RestoreTraffic();
            }

            if (cinematic.IsLocked)
            {
                cinematic.Release();
            }
        }

        public bool Begin()
        {
            if (!CanStart)
            {
                return false;
            }

            FailureReason = "";
            Clean();
            enemyRoster.ResetSpawnCount();
            getaway.SuppressTraffic();
            IntelSecured = false;
            ObservationProgress = 0;
            reinforced = turned = false;
            serviceStep = alleyStep = 0;
            subdued.Clear();
            if (stealth)
            {
                stealth.ResetAttempt();
            }

            FindFirstObjectByType<PhoneController>()?.ClosePhone();
            Day.pcUIPanel.SetActive(false);
            preparation.ui.SetActive(false);
            preparation.preparation.ui.SetActive(false);
            preparation.preparation.delivery.ui.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            foreach (var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))
            {
                if (pc.pcUIPanel == Day.pcUIPanel && pc.cameraLookScript)
                {
                    pc.cameraLookScript.enabled = true;
                }
            }

            ui.SetActive(true);
            actors.SetActive(true);
            car.gameObject.SetActive(true);
            car.SetPositionAndRotation(carOrigin, carRotation);
            door.localRotation = Quaternion.identity;
            passenger.gameObject.SetActive(false);
            preparation.supportVehicle.gameObject.SetActive(false);
            driver.gameObject.SetActive(false);
            pizza.SetActive(true);
            approval.SetActive(false);
            EventDay = Day.currentDay;
            StartOpening();
            return true;
        }

        private void StartOpening()
        {
            Day.EnterNightPreparation();
            Set(Phase.Opening);
            cinematic.Lock();
            subtitle.text = string.Join("\n", opening.Where(t => !t.StartsWith("정비사:")));
            cinematic.Shot(entry.position + Vector3.up * 3 + Vector3.right * 6, entry.position + Vector3.up);
        }

        private void Set(Phase value)
        {
            presentation.ResetRoute();
            State = value;
            entered = Time.time;
            line = 0;
        }

        private bool Near(Transform t, float range = 3.2f)
        {
            if (!Player.CanAct || Vector3.Distance(Player.transform.position, t.position) > range)
            {
                return false;
            }

            var from = Player.View.transform.position;
            var d = t.position + Vector3.up * .7f - from;
            return !Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(h => !h.transform.IsChildOf(Player.transform) && !h.transform.IsChildOf(t) && !h.transform.IsChildOf(pizza.transform) && !h.transform.IsChildOf(car));
        }

        public bool Interact()
        {
            if (!Running || Time.timeScale <= 0)
            {
                return false;
            }

            if (State == Phase.Entrance && Near(entry))
            {
                Set(Phase.Admission);
                cinematic.Lock();
                approval.SetActive(true);
                approval.transform.position = entryGuard.TransformPoint(new Vector3(0, 1.25f, .3f));
                approval.transform.rotation = entryGuard.rotation;
                cinematic.Shot(entryGuard.position + entryGuard.forward * 2 + Vector3.up * 1.7f, entryGuard.position + Vector3.up * 1.2f);
                presentation.Notice("경비: 납품 명단 확인했습니다. 피자는 서비스 테이블에 놓고 바로 나오세요.");
                if (stamp)
                {
                    StorySoundMix.Play(sound, stamp, .4f);
                }

                return true;
            }

            if (State == Phase.Deposit && Near(deliveryTable))
            {
                pizza.SetActive(true);
                pizza.transform.SetPositionAndRotation(deliveryTable.position, deliveryTable.rotation);
                Set(Phase.Observe);
                StoryPhoneInbox.Current?.SendVehicleReady();
                presentation.Notice("민기: 안쪽이 VIP 행사장이야. 순찰이 지나가면 움직여. 기둥 옆이나 서비스 통로에서 대화를 들어봐.");
                return true;
            }

            if (State == Phase.Conversation)
            {
                AdvanceConversation();
                return true;
            }

            if (State == Phase.InteriorEscape && Near(interiorExit))
            {
                if (!ExitUnlocked)
                {
                    presentation.Notice("출구 봉쇄 · 경비를 2명 제압해 탈출로를 확보하세요. (" + SubduedCount + "/2)");
                    return false;
                }

                Clean();
                insideVenue = false;
                PlacePlayer(streetArrival ? streetArrival : entry);
                Set(Phase.StreetEscape);
                if (streetPursuit)
                {
                    streetPursuit.Begin();
                }
                else
                {
                    Spawn(streetPosts, 2);
                }

                return true;
            }

            if (State == Phase.StreetEscape && Near(carStand))
            {
                enemyRoster.StopStorySpawning();
                if (streetPursuit)
                {
                    streetPursuit.BoardCar();
                }

                cinematic.CaptureBoardingView();
                Set(Phase.Boarding);
                cinematic.Lock();
                cinema.nearClipPlane = .03f;
                TrafficRecovery.Get(Player.gameObject).Protect(boardingSeconds + driveSeconds + 10);
                cinematic.HidePlayer();
                passenger.gameObject.SetActive(false);
                passenger.position = carStand.position;
                passenger.rotation = car.rotation;
                cinematic.PositionBoardingCamera();
                if (doorOpen)
                {
                    StorySoundMix.Play(sound, doorOpen, .55f);
                }

                return true;
            }

            return false;
        }

        public void AbandonCar()
        {
            if (State != Phase.Driving || !getaway.CanAbandon)
            {
                return;
            }

            cinematic.CaptureExitView();
            getaway.Stop();
            cinema.nearClipPlane = .03f;
            door.localRotation = Quaternion.identity;
            passenger.gameObject.SetActive(false);
            Set(Phase.Abandoning);
            if (doorOpen)
            {
                StorySoundMix.Play(sound, doorOpen, .55f);
            }

            presentation.Notice("나: 여기 두고 가자. 이 차의 흔적을 가게까지 끌고 갈 순 없어.");
        }

        public bool CanObserve()
        {
            if (State != Phase.Observe || !Player.CanAct || (stealth && !stealth.CanListen))
            {
                return false;
            }

            var delta = target.position + Vector3.up * 1.4f - Player.View.transform.position;
            return (stealth ? stealth.AtListeningPoint : Vector3.Distance(Player.transform.position, observeStand.position) < 3.5f) && Vector3.Dot(Player.View.transform.forward, delta.normalized) > .9f && !Physics.RaycastAll(Player.View.transform.position, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(h => !h.transform.IsChildOf(target) && !h.transform.IsChildOf(Player.transform));
        }

        public void Observe(bool held, float seconds)
        {
            if (Time.timeScale <= 0)
            {
                return;
            }

            if (!held || !CanObserve())
            {
                return;
            }

            ObservationProgress += seconds / Mathf.Max(.5f, observeSeconds);
            if (ObservationProgress >= 1)
            {
                ObservationProgress = 1;
                Set(Phase.Conversation);
                ShowConversation();
            }
        }

        private void ShowConversation()
        {
            subtitle.text = overheard[line] + "\n[F] 계속 · [Space] 대화 건너뛰기";
        }

        public void AdvanceConversation(bool skip = false)
        {
            if (State != Phase.Conversation || Time.timeScale <= 0 || !Player.CanAct)
            {
                return;
            }

            line++;
            if (!skip && line < overheard.Length)
            {
                ShowConversation();
                return;
            }

            IntelSecured = true;
            Expose(false);
        }

        public void SecurityWarning(string message)
        {
            presentation.Notice(message, 4);
        }

        public float ExitProgress => State == Phase.Abandoning ? Mathf.Clamp01((Time.time - entered) / 2.5f) : 0;
        public float BoardingProgress => State == Phase.Boarding ? Mathf.Clamp01((Time.time - entered) / boardingSeconds) : 0;
        public void EjectFromVenue(Transform caughtBy = null)
        {
            if (State != Phase.Deposit && State != Phase.Observe && State != Phase.Conversation)
            {
                return;
            }

            Set(Phase.Ejected);
            cinematic.BeginEjection(caughtBy);
            pizza.SetActive(false);
            approval.SetActive(false);
            presentation.Notice("경비: 출입 금지 구역입니다. 당장 밖으로 나가세요.");
        }

        public void Expose(bool early)
        {
            if (State != Phase.Deposit && State != Phase.Observe && State != Phase.Conversation)
            {
                return;
            }

            if (State == Phase.Deposit)
            {
                pizza.SetActive(false);
            }

            var patrol = stealth ? stealth.TakeCombatGuards() : new CitizenCombat[0];
            // 일찍 들켜도 핵심 단서를 놓쳐 다음 이야기로 진행하지 못하는 일이 없게 한다.
            if (early)
            {
                IntelSecured = true;
                StoryPhoneInbox.Current?.SendFrom("민기", "급히 잡힌 통신", "네가 빠져나오는 동안 남은 통신을 들었어. 박진웅이 우릴 보내 야마자키와 싸움을 붙였어. 혼란 속에서 우리 보스를 제거하고 두목이 되려는 거야. 우리와 실장도 없애라고 했어. 지금은 살아서 나와!");
            }

            Set(Phase.Exposed);
            cinematic.Lock();
            cinematic.Shot(Player.View.transform.position, target.position + Vector3.up * 1.3f);
            if (patrol.Length > 0)
            {
                foreach (var guard in patrol)
                {
                    guard.Provoke(Player);
                    enemyRoster.Add(guard);
                }
            }
            else
            {
                Spawn(interiorPosts, 2);
            }

            foreach (var e in enemyRoster.ActiveEnemies)
            {
                e.ExternalAttackControl = true;
            }

            cinematic.Shot(Player.View.transform.position, target.position + Vector3.up * 1.3f);
            presentation.Notice(early ? "박진웅: 저 배달원 잡아! 우리 얘기를 들었어. 살려 보내지 마!\n민기: 들켰어! 들어온 문으로 빠져!" : "박진웅: 저 배달원, 언제부터 듣고 있었어? 잡아! 납품 기록 확인하고 가게에도 사람 보내. 둘 다 놓치지 마!\n민기: 뛰어! 들어온 문으로 빠져!");
            if (alarm && (!SuppliedAudio.Current || !SuppliedAudio.Current.escalation))
            {
                StorySoundMix.Play(sound, alarm, .4f);
            }
        }

        private void Clean()
        {
            enemyRoster.StopStorySpawning();
            if (streetPursuit)
            {
                streetPursuit.ResetPursuit();
            }

            if (stealth)
            {
                stealth.EndPatrol();
            }

            enemyRoster.Clear();
        }

        public void SkipCinematic()
        {
            if (State == Phase.Aftermath)
            {
                return;
            }

            if (State == Phase.Driving)
            {
                return;
            }

            if (!Cinematic || Time.timeScale <= 0)
            {
                return;
            }

            FinishCinematic();
        }

        private void FinishCinematic()
        {
            switch (State)
            {
                case Phase.Opening:
                    cinematic.Release();
                    Set(Phase.Entrance);
                    presentation.Notice("마지막 배달 · 납품 승인서를 제시하고 서비스 출입구로 입장하세요.");
                    break;
                case Phase.Ejected:
                    if (stealth)
                    {
                        stealth.ResetAttempt();
                    }

                    ObservationProgress = 0;
                    IntelSecured = false;
                    insideVenue = false;
                    cinematic.Release();
                    PlacePlayer(entry);
                    Set(Phase.Entrance);
                    presentation.Notice("민기: 정면으로 마주치지 마. 뒤에서 제압하고, 다른 경비가 보는지도 살펴. 입구에서 다시 들어가자.", 8);
                    break;
                case Phase.Admission:
                    approval.SetActive(false);
                    insideVenue = true;
                    PlacePlayer(inside);
                    if (stealth)
                    {
                        stealth.BeginPatrol();
                    }

                    cinematic.Release();
                    Set(Phase.Deposit);
                    presentation.Notice("서비스 테이블에 피자를 놓고 박진웅을 찾으세요.");
                    break;
                case Phase.Exposed:
                    cinematic.Release();
                    Set(Phase.InteriorEscape);
                    foreach (var e in enemyRoster.ActiveEnemies)
                    {
                        if (e)
                        {
                            e.ExternalAttackControl = false;
                        }
                    }

                    presentation.Notice("민기: 출구가 봉쇄됐어! 경비 두 명을 제압하고 들어왔던 접수실 입구로 빠져!");
                    break;
                case Phase.Boarding:
                    cinematic.RestoreBoardingClip();
                    door.localRotation = Quaternion.identity;
                    passenger.position = seat.position;
                    passenger.rotation = seat.rotation;
                    if (doorClose)
                    {
                        StorySoundMix.Play(sound, doorClose, .6f);
                    }

                    Set(Phase.Driving);
                    getaway.Begin();
                    presentation.Notice("나: 이 차는 추적될 수 있어. 수리점 인근에 버리고 가게로 돌아가자.", 7);
                    break;
                case Phase.Abandoning:
                    cinematic.RestoreExitClip();
                    door.localRotation = Quaternion.identity;
                    if (doorClose)
                    {
                        StorySoundMix.Play(sound, doorClose, .5f);
                    }

                    getaway.Stop();
                    fade.gameObject.SetActive(false);
                    if (engine)
                    {
                        engine.Stop();
                    }

                    BeginHomecoming();
                    break;
                case Phase.Debrief:
                    cinematic.Release();
                    CleanupProps();
                    if (aftermath)
                    {
                        Set(Phase.Aftermath);
                        aftermath.Begin();
                    }
                    else
                    {
                        Set(Phase.Complete);
                        presentation.Notice("첫 번째 사건 완료 · 박진웅의 권력 탈취 계획 확인", 10);
                    }

                    break;
            }
        }

        public int PendingSpawnCount => enemyRoster.PendingSpawnCount;
        public int HiddenSpawnCount => enemyRoster.HiddenSpawnCount;
        public void StopStorySpawning()
        {
            enemyRoster.StopStorySpawning();
        }

        public void SpawnStreetPursuers(Transform[] points, int count, float speed)
        {
            enemyRoster.QueueSpawns(points, count, speed);
        }

        public bool SpawnOutsideView(Vector3 point)
        {
            return enemyRoster.SpawnOutsideView(point);
        }

        private void Spawn(Transform[] points, int count)
        {
            enemyRoster.QueueSpawns(points, count, 4.2f);
        }

        public void PlacePlayer(Transform anchor)
        {
            cinematic.PlacePlayer(anchor);
        }

        public void StoryControl(bool enabled)
        {
            cinematic.StoryControl(enabled);
        }

        public void ClearStoryEnemies()
        {
            Clean();
        }

        // 차를 버린 뒤 암전 속에서 실제 플레이어를 가게로 옮기고 민기와 마주한다.
        private void BeginHomecoming()
        {
            Set(Phase.Debrief);
            Clean();
            PlacePlayer(safePoint);
            cinematic.PrepareHomecoming();
        }

        private void CleanupProps()
        {
            Clean();
            if (getaway)
            {
                getaway.RestoreTraffic();
            }

            if (getaway)
            {
                getaway.Stop();
            }

            pizza.SetActive(false);
            approval.SetActive(false);
            actors.SetActive(false);
            car.gameObject.SetActive(false);
            preparation.supportVehicle.gameObject.SetActive(true);
            if (engine)
            {
                engine.Stop();
            }
        }

        // 발표용 바로 시작은 날짜 정산 없이 현재 일차에서 도입 장면부터 재생한다.
        public bool DeveloperStart()
        {
            if (aftermath && aftermath.State != ShopEscapeChapter.Stage.None)
            {
                aftermath.ResetChapter();
                PlacePlayer(safePoint);
            }

            if (Day.IsTransitioning || State == Phase.Scheduling)
            {
                return false;
            }

            if (Running)
            {
                Cancel();
            }

            StopAllCoroutines();
            Clean();
            cinematic.Release();
            CleanupProps();
            insideVenue = false;
            State = Phase.Available;
            EventDay = Day.currentDay;
            Day.EnterNightPreparation();
            return Begin();
        }

        public void DeveloperComplete()
        {
            if (Completed || State == Phase.Scheduling)
            {
                return;
            }

            StopAllCoroutines();
            Clean();
            if (Player.RiddenBike)
            {
                Player.RiddenBike.ExitBike();
            }

            cinematic.Release();
            PlacePlayer(safePoint);
            insideVenue = false;
            IntelSecured = true;
            Set(Phase.Complete);
            FailureReason = "";
            CleanupProps();
            ui.SetActive(false);
        }

        public void Cancel()
        {
            if (aftermath && aftermath.InPrison)
            {
                return;
            }

            if (Running)
            {
                Fail("작전을 중단했습니다.");
            }
        }

        public void Fail(string reason)
        {
            if (!Running)
            {
                return;
            }

            if (aftermath)
            {
                aftermath.ResetChapter();
            }

            StopAllCoroutines();
            Clean();
            if (insideVenue && !Player.IsDown && !Player.WaterRescueActive && !PoliceControl.IsArrestInProgress)
            {
                PlacePlayer(entry);
            }

            insideVenue = false;
            cinematic.Release();
            CleanupProps();
            Set(Phase.Failed);
            FailureReason = reason;
            presentation.Notice("마지막 배달 중단 · " + reason + "\n준비 성과는 유지됩니다. 휴대폰에서 재시도하세요.", 10);
        }

        private void Update()
        {
            enemyRoster.Tick();
            if (Time.timeScale <= 0)
            {
                return;
            }

            if (!Running)
            {
                presentation.RefreshIdle();
                return;
            }

            if (State == Phase.Scheduling)
            {
                objective.text = "첫 번째 사건 · 행사 당일로 이동";
                return;
            }

            if (State == Phase.Aftermath && aftermath)
            {
                if (!aftermath.InPrison && (Player.HitsTaken >= 3 || Player.WaterRescueActive))
                {
                    Fail("매장을 탈출하지 못했습니다.");
                    return;
                }

                aftermath.Tick();
                return;
            }

            if (Player.HitsTaken >= 3 || Player.WaterRescueActive || PoliceControl.IsArrestInProgress)
            {
                Fail("작전을 계속할 수 없습니다.");
                return;
            }

            if (State == Phase.Driving)
            {
                presentation.HideExpiredSubtitle();
                getaway.Tick();
                return;
            }

            if (Cinematic)
            {
                float elapsed = Time.time - entered;
                cinematic.Tick(elapsed);
                float duration = State == Phase.Ejected ? 1.1f : State == Phase.Opening ? openingSeconds : State == Phase.Admission ? 2.5f : State == Phase.Exposed ? revealSeconds : State == Phase.Boarding ? boardingSeconds : State == Phase.Abandoning ? 2.5f : State == Phase.Debrief ? 1.3f + closing.Length * 4.8f : 7;
                if (PrologueKeyboard.Pressed(Key.Space) || elapsed >= duration)
                {
                    FinishCinematic();
                }

                objective.text = "첫 번째 사건 · 마지막 배달";
                marker.gameObject.SetActive(false);
                return;
            }

            UpdateGameplay();
        }

        private void UpdateGameplay()
        {
            if (PrologueKeyboard.Pressed(Key.F))
            {
                Interact();
            }

            if (State == Phase.Conversation && PrologueKeyboard.Pressed(Key.Space))
            {
                AdvanceConversation(true);
            }

            if (State == Phase.Observe)
            {
                Observe(Keyboard.current != null && Keyboard.current.fKey.isPressed, Time.deltaTime);
            }

            if (State == Phase.InteriorEscape)
            {
                foreach (var enemy in enemyRoster.ActiveEnemies)
                {
                    if (enemy && (enemy.Health <= 0 || enemy.Npc.IsKnockedDown))
                    {
                        subdued.Add(enemy);
                    }
                }
            }

            if (State == Phase.InteriorEscape && !reinforced && Vector3.Distance(Player.transform.position, inside.position) < 8)
            {
                reinforced = true;
                Spawn(interiorPosts.Skip(2).ToArray(), Mathf.Max(0, 4 - enemyRoster.ActiveEnemies.Count));
                presentation.Notice("조직원: 서비스 통로 쪽이다!\n민기: 출입구 바로 앞이야. 멈추지 마!");
            }

            if (State == Phase.InteriorEscape && serviceRoute != null && serviceStep < serviceRoute.Length && Vector3.Distance(Player.transform.position, serviceRoute[serviceStep].position) < 3)
            {
                serviceStep++;
            }

            if (State == Phase.StreetEscape && alleyRoute != null && alleyStep < alleyRoute.Length && Vector3.Distance(Player.transform.position, alleyRoute[alleyStep].position) < 4)
            {
                alleyStep++;
            }

            if (State == Phase.StreetEscape && !turned && (Vector3.Distance(Player.transform.position, escapeTurn.position) < 5 || Vector3.Distance(Player.transform.position, carStand.position) < 8))
            {
                turned = true;
                StoryPhoneInbox.Current?.SendFrom("정비사", "차량 위치 확인", "표시된 차가 네 차야. 운전석으로 타. 나는 먼저 빠졌어. 가게로 바로 몰고 가지 말고 폐차 대기 지점에 버려.");
            }

            presentation.RefreshGameplay(turned);
        }

        private void LateUpdate()
        {
            cinematic.UpdateActors(Time.time - entered);
        }
    }
}
