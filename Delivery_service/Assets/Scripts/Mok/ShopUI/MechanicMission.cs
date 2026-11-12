using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 회수한 부품과 차량 지원은 현재 게임 세션의 준비 성과로 유지한다.
    public sealed class MechanicMission : MonoBehaviour
    {
        public enum Phase
        {
            Available,
            Meet,
            Briefing,
            SpotMove,
            SpotDialogue,
            RacerDeal,
            RacerDialogue,
            Racing,
            Return,
            Handover,
            Debrief,
            Complete,
            Failed
        }

        private Phase state;
        public int CompletedDay { get; private set; }

        public static MechanicMission Current { get; private set; }
        public static bool Active => Current && Current.Running;

        public Phase State
        {
            get => state;
            private set
            {
                if (value == Phase.Complete && state != Phase.Complete)
                    CompletedDay = Day.currentDay;
                state = value;
            }
        }

        public bool Running => State >= Phase.Meet && State <= Phase.Debrief;
        public bool Completed => State == Phase.Complete;
        public bool InDialogue =>
            State == Phase.Briefing ||
            State == Phase.SpotDialogue ||
            State == Phase.RacerDialogue ||
            State == Phase.Handover ||
            State == Phase.Debrief;

        // 레이서 NPC가 평소 대화 대신 미션 대화를 진행해야 하는 구간인지 확인
        public bool IsRacerMissionStep =>
            Running && (State == Phase.RacerDeal || State == Phase.RacerDialogue || State == Phase.Racing);

        public bool PartRecovered { get; private set; }
        public bool VehicleSupportUnlocked => Completed;

        [Header("미션 참조 및 주요 위치")]
        public BackdoorMission preparation;
        public Transform mechanic;
        public Transform approach;
        public Transform pickupStand;
        public Transform caseRest;
        public Transform benchRest;
        public Transform supportVehicle;
        public Transform rendezvous;

        [Header("거래처 NPC(서안골목) 및 레이서 위치 설정")]
        public Transform targetSpot; // 서안골목 거래처 NPC
        public string targetSpotMarkerName = "서안골목 거래처";
        public Transform racerNpc;   // 씬의 레이서 또는 레이서 프리팹
        public Transform racerSpawnPoint; // 프리팹 소환 시 사용할 위치

        [Header("오브젝트 및 UI")]
        public GameObject parts;
        public GameObject keys;
        public GameObject ui;
        public Text objective;
        public Text subtitle;
        public Text marker;

        [Header("적 및 사운드 설정")]
        public Transform[] enemyPosts;
        public CitizenCombat enemyTemplate;
        public AudioSource sound;
        public AudioClip pickupSound;
        public AudioClip repairSound;
        public float handoverSeconds = 2.4f;

        [Header("1. 정비사 브리핑 대사")]
        [TextArea(2, 5)]
        public string[] briefing =
        {
            "정비사: 민기가 보냈지? 행사장 뒤에 준비해 둘 차가 필요하다면서.",
            "나: 눈에 안 띄고, 확실하게 움직이는 차면 돼.\n정비사: 차는 있어. 문제는 시동 제어기야. 부품이 어디 갔는지 도무지 모르겠어.",
            "정비사: 서안골목에 내 거래처가 있어. 거기로 가면 그쪽에서 이걸 찾아줄 거야.",
            "나: 알겠어. 서안골목으로 가볼게."
        };

        [Header("2. 거래처 NPC 상호작용 대사")]
        [TextArea(2, 5)]
        public string[] spotDialogue =
        {
            "거래처: 정비사 소개로 왔군. 그 부품이라면 마을의 레이서가 챙겨갔어.",
            "거래처: 그 녀석은 승부욕이 강해서 레이스 승부를 좋아하지. 찾아가서 거래를 걸어 봐."
        };

        [Header("3. 레이서 거래 대사 (레이스 시작 전)")]
        [TextArea(2, 5)]
        public string[] racerDialogue =
        {
            "정비사 녀석 부품 때문에 찾아온 건가? 공짜로는 못 넘겨.",
            "내 차를 상대로 레이스에서 이긴다면 순순히 내주지. 준비됐나?"
        };

        [Header("3-1. 레이서 레이스 승리 시 대사")]
        [TextArea(2, 5)]
        public string[] racerWinDialogue =
        {
            "이 마을에 이 정도 실력자가 있었다고? 약속대로 부품은 넘겨주지."
        };

        [Header("3-2. 레이서 레이스 패배 시 대사")]
        [TextArea(2, 5)]
        public string[] racerLoseDialogue =
        {
            "역시 나는 못 따라잡네. 부품이 필요하면 다시 도전해 봐."
        };

        [Header("4. 정비사 완료 대사")]
        [TextArea(2, 5)]
        public string[] ending =
        {
            "정비사: 맞아, 이 부품이야. 이제 저 차를 다시 굴릴 수 있겠어.",
            "나: 행사장 골목 끝, 큰길 합류 지점이야. 민기가 위치를 보내 줄 거야.\n정비사: 받았어. 차와 열쇠를 거기 두고 먼저 빠질게. 탈출 후에는 지정한 곳에 차를 버려. 추적될 수 있으니까.",
            "정비사: 이건 예비 열쇠야. 이번 빚은 내가 진 걸로 하자.\n나: 그날 꼭 갚아. 시간이 없을 테니까."
        };

        public string FailureReason { get; private set; }
        public CitizenCombat[] Pursuers => enemies;

        private CitizenCombat[] enemies = new CitizenCombat[0];
        private bool engaged;
        private int line;
        private float entered;
        private float noticeUntil;
        private Vector3 boxStart;
        private Quaternion boxRotation;

        private Transform racerPrefabBackup;
        private GameObject spawnedRacerInstance;
        private float interactionCooldownUntil = 0f;

        public PlayerCombat Player => preparation.Player;
        private DayNightCycle Day => preparation.delivery.broker.day;

        public int EarliestDay => preparation.CompletedDay;
        public bool DayUnlocked => preparation && preparation.Completed;
        public bool CanStart =>
            DayUnlocked &&
            preparation &&
            preparation.Completed &&
            !BrokerMission.Active &&
            !VenueFinale.Active &&
            !SpecialOrderMission.Active &&
            !BackdoorMission.Active &&
            !Running &&
            !Completed &&
            !Day.IsTransitioning &&
            !StoryTutorial.Active &&
            !Player.IsDown &&
            !Player.WaterRescueActive &&
            !PoliceControl.IsArrestInProgress;

        public bool ThreatNearby => enemies.Any(e =>
            e &&
            e.gameObject.activeInHierarchy &&
            !e.GetComponent<NPCControl>().IsDead &&
            Vector3.Distance(e.transform.position, Player.transform.position) < 16);

        private void Awake()
        {
            Current = this;
            ui.SetActive(false);
            parts.SetActive(false);
            keys.SetActive(false);
            mechanic.gameObject.SetActive(false);
            if (targetSpot != null) targetSpot.gameObject.SetActive(false);

            if (racerNpc != null && !racerNpc.gameObject.scene.IsValid())
            {
                racerPrefabBackup = racerNpc;
            }
        }

        public void BlockInteractionForSeconds(float seconds = 0.35f)
        {
            interactionCooldownUntil = Mathf.Max(interactionCooldownUntil, Time.unscaledTime + seconds);
        }

        public string[] GetRacerResultDialogue(bool playerWon)
        {
            return playerWon ? racerWinDialogue : racerLoseDialogue;
        }

        private void ResolveRacerInScene()
        {
            if (racerNpc == null || !racerNpc.gameObject.scene.IsValid())
            {
                if (racerNpc != null && !racerNpc.gameObject.scene.IsValid())
                {
                    racerPrefabBackup = racerNpc;
                }

                var sceneRacer = FindFirstObjectByType<RacerNPCInteraction>(FindObjectsInactive.Include);
                if (sceneRacer != null)
                {
                    racerNpc = sceneRacer.transform;
                }
                else if (racerPrefabBackup != null)
                {
                    Vector3 pos = racerSpawnPoint != null ? racerSpawnPoint.position : racerPrefabBackup.position;
                    Quaternion rot = racerSpawnPoint != null ? racerSpawnPoint.rotation : racerPrefabBackup.rotation;
                    spawnedRacerInstance = Instantiate(racerPrefabBackup.gameObject, pos, rot);
                    racerNpc = spawnedRacerInstance.transform;
                }
            }
        }

        private void CleanupSpawnedRacer()
        {
            if (spawnedRacerInstance != null)
            {
                Destroy(spawnedRacerInstance);
                spawnedRacerInstance = null;
            }
            if (racerPrefabBackup != null)
            {
                racerNpc = racerPrefabBackup;
            }
        }

        private void OnDestroy()
        {
            Clean();
            CleanupSpawnedRacer();
            if (Current == this)
                Current = null;
        }

        public bool Begin()
        {
            if (!CanStart) return false;

            Clean();
            CleanupSpawnedRacer();
            FailureReason = "";
            engaged = false;

            mechanic.gameObject.SetActive(true);
            if (targetSpot != null) targetSpot.gameObject.SetActive(false);
            parts.SetActive(false);
            ui.SetActive(true);
            keys.SetActive(false);

            preparation.ui.SetActive(false);
            preparation.delivery.ui.SetActive(false);

            FindFirstObjectByType<PhoneController>()?.ClosePhone();
            Day.pcUIPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (var pc in FindObjectsByType<ShopPC>(FindObjectsSortMode.None))
            {
                if (pc.pcUIPanel == Day.pcUIPanel && pc.cameraLookScript)
                    pc.cameraLookScript.enabled = true;
            }

            Set(PartRecovered ? Phase.Return : Phase.Meet);
            Notice(PartRecovered
                ? "회수한 부품을 정비사에게 전달하세요."
                : "민기: 정비사에게 연락해 뒀어. 수리점에서 만나 봐. 차량은 그 사람이 잘 알아.");

            return true;
        }

        private void Set(Phase value)
        {
            State = value;
            line = 0;
            entered = Time.time;
        }

        private void Notice(string text)
        {
            subtitle.text = text;
            noticeUntil = Time.time + 8;
        }

        private bool Near(Transform target, float height = 1.1f)
        {
            if (target == null || !target.gameObject.activeInHierarchy || !Player.CanAct)
                return false;

            float dist = Vector3.Distance(Player.transform.position, target.position);

            if (target == racerNpc)
            {
                return dist <= 3.5f;
            }

            if (dist > 3.3f)
                return false;

            var from = Player.View.transform.position;
            var delta = target.position + Vector3.up * height - from;
            var inv = Player.GetComponent<ToppingInventory>();

            return !Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore)
                .Any(h =>
                    !h.transform.IsChildOf(Player.transform) &&
                    !h.transform.IsChildOf(target) &&
                    !target.IsChildOf(h.transform) &&
                    !h.transform.IsChildOf(parts.transform) &&
                    !(inv && inv.HoldPoint && h.transform.IsChildOf(inv.HoldPoint)));
        }

        private string[] GetCurrentDialogueLines()
        {
            switch (State)
            {
                case Phase.Briefing: return briefing;
                case Phase.SpotDialogue: return spotDialogue;
                case Phase.RacerDialogue: return racerDialogue;
                default: return ending;
            }
        }

        private Transform GetCurrentInteractTarget()
        {
            switch (State)
            {
                case Phase.SpotMove:
                case Phase.SpotDialogue:
                    return targetSpot;
                case Phase.RacerDeal:
                case Phase.RacerDialogue:
                    ResolveRacerInScene();
                    return racerNpc;
                default:
                    return mechanic;
            }
        }

        private void ShowLine()
        {
            var lines = GetCurrentDialogueLines();
            if (lines == null || lines.Length == 0) return;
            subtitle.text = lines[line] + "\n[F] 계속 · [Space] 대화 건너뛰기";
        }

        // 레이서 NPC에게 말을 걸었을 때 기존 레이서 대화창(DialoguePanel)으로 대사 출력
        public bool InteractWithRacer(Transform interactedRacer = null)
        {
            if (!Running || Time.timeScale <= 0) return false;
            if (Time.unscaledTime < interactionCooldownUntil) return false;

            if (interactedRacer != null && interactedRacer.gameObject.scene.IsValid())
            {
                racerNpc = interactedRacer;
            }
            else
            {
                ResolveRacerInScene();
            }

            if (State == Phase.RacerDeal)
            {
                BlockInteractionForSeconds(0.25f);
                Set(Phase.RacerDialogue);
                subtitle.text = "";

                RacerNPCInteraction racerUI = racerNpc != null ? racerNpc.GetComponent<RacerNPCInteraction>() : null;
                if (racerUI != null && racerDialogue != null && racerDialogue.Length > 0)
                {
                    racerUI.StartMissionDialogue(racerDialogue);
                }
                else
                {
                    StartRacerMatch();
                }
                return true;
            }

            return false;
        }

        // RacerNPCInteraction에서 미션 대화가 모두 끝났을 때 호출 -> 레이스 시작
        public void OnRacerDialogueFinished()
        {
            if (!Running || State != Phase.RacerDialogue) return;
            BlockInteractionForSeconds(0.3f);
            StartRacerMatch();
        }

        // RacerNPCInteraction에서 ESC 등으로 대화가 중단되었을 때 호출
        public void OnRacerDialogueCancelled()
        {
            if (!Running || State != Phase.RacerDialogue) return;
            BlockInteractionForSeconds(0.3f);
            Set(Phase.RacerDeal);
        }

        public bool Interact()
        {
            if (!Running || Time.timeScale <= 0) return false;
            if (Time.unscaledTime < interactionCooldownUntil) return false;

            // 레이서 대화창(DialoguePanel) 진행 중일 때는 RacerNPCInteraction이 직접 입력을 처리
            if (State == Phase.RacerDialogue)
            {
                return true;
            }

            // 자막 기반 대화 진행 중일 때 F 입력 처리
            if (State == Phase.Briefing || State == Phase.SpotDialogue || State == Phase.Debrief)
            {
                AdvanceDialogue();
                return true;
            }

            // 1. 정비사 첫 대화 시작
            if (State == Phase.Meet && Near(mechanic))
            {
                BlockInteractionForSeconds(0.2f);
                Set(Phase.Briefing);
                ShowLine();
                return true;
            }

            // 2. 서안골목 거래처 NPC에게 말을 걸었을 때 스팟 다이얼로그 시작
            if (State == Phase.SpotMove && Near(targetSpot))
            {
                BlockInteractionForSeconds(0.2f);
                if (spotDialogue != null && spotDialogue.Length > 0)
                {
                    Set(Phase.SpotDialogue);
                    ShowLine();
                }
                else
                {
                    EnterRacerDealPhase();
                }
                return true;
            }

            // 3. 레이서에게 가서 기존 레이서 대화창 열기
            if (State == Phase.RacerDeal)
            {
                ResolveRacerInScene();
                if (Near(racerNpc))
                {
                    return InteractWithRacer(racerNpc);
                }
            }

            // 4. 승리 후 정비사에게 부품 전달
            if (State == Phase.Return && Near(mechanic))
            {
                if (ThreatNearby)
                {
                    Notice("정비사: 뒤에 놈들부터 떼어 내! 여기까지 데려오면 안 돼.");
                    return false;
                }

                BlockInteractionForSeconds(0.2f);
                Clean();
                boxStart = parts.transform.position;
                boxRotation = parts.transform.rotation;
                Set(Phase.Handover);
                Notice("정비사: 잘 가져왔어. 단자부터 확인해 볼게.");
                if (sound && repairSound) sound.PlayOneShot(repairSound, .3f);
                return true;
            }

            return false;
        }

        public void AdvanceDialogue(bool skip = false)
        {
            if (Time.timeScale <= 0) return;
            if (Time.unscaledTime < interactionCooldownUntil) return;
            if (State != Phase.Briefing && State != Phase.SpotDialogue && State != Phase.Debrief)
                return;

            Transform target = GetCurrentInteractTarget();
            if (target != null && !Near(target))
                return;

            BlockInteractionForSeconds(0.2f);
            line++;
            var lines = GetCurrentDialogueLines();

            if (!skip && lines != null && line < lines.Length)
            {
                ShowLine();
                return;
            }

            if (State == Phase.Briefing)
            {
                if (targetSpot != null)
                {
                    targetSpot.gameObject.SetActive(true);
                    if (targetSpot.TryGetComponent<NPCControl>(out var npc))
                    {
                        npc.ExternalControl = true;
                    }
                }

                Set(Phase.SpotMove);
                Notice("서안골목에 있는 거래처로 이동해 대화를 나누세요.");
            }
            else if (State == Phase.SpotDialogue)
            {
                EnterRacerDealPhase();
            }
            else if (State == Phase.Debrief)
            {
                Set(Phase.Complete);
                if (targetSpot != null) targetSpot.gameObject.SetActive(false);
                CleanupSpawnedRacer();
                keys.SetActive(false);
                parts.transform.SetPositionAndRotation(benchRest.position, benchRest.rotation);
                Notice("빚진 정비사 · 준비 완료\n행사장 탈출 차량·차량 폐기 지원 확보");
                StoryPhoneInbox.Current?.Send(
                    "탈출 차량 지원 확보",
                    "정비사가 행사 당일 골목 끝 합류 지점에 차와 열쇠를 두기로 했어. 직접 운전해 탈출한 다음 지정 장소에 차를 버려. 가게까지 추적되면 안 돼. ‘빚진 정비사’ 준비 성과에 기록했어.",
                    true);
            }
        }

        private void EnterRacerDealPhase()
        {
            ResolveRacerInScene();
            if (racerNpc != null && !racerNpc.gameObject.activeSelf)
            {
                racerNpc.gameObject.SetActive(true);
            }

            Set(Phase.RacerDeal);
            Notice("레이서를 찾아가 거래하세요.");
        }

        private void StartRacerMatch()
        {
            ResolveRacerInScene();
            Set(Phase.Racing);
            subtitle.text = "";

            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.StartRace(racerNpc);
            }
        }

        // RaceManager에서 레이스 최종 정산 및 종료 시 호출
        public void OnRaceFinished(bool playerWon)
        {
            if (!Running || State != Phase.Racing) return;

            BlockInteractionForSeconds(0.35f);

            if (playerWon)
            {
                PartRecovered = true;
                parts.SetActive(true);
                Set(Phase.Return);
                if (sound && pickupSound) sound.PlayOneShot(pickupSound, .4f);
                Notice("레이서에게서 부품을 받아냈습니다! 정비사에게 돌아가 전달하세요.");
            }
            else
            {
                Set(Phase.RacerDeal);
                Notice("승부에서 졌습니다. 레이서에게 다시 말을 걸어 재도전하세요.");
            }
        }

        private void Engage()
        {
            if (engaged) return;
            engaged = true;

            foreach (var e in enemies)
            {
                if (e) e.Provoke(Player);
            }
        }

        private void Clean()
        {
            foreach (var e in enemies)
            {
                if (e)
                {
                    e.EndFight();
                    Destroy(e.gameObject);
                }
            }
            enemies = new CitizenCombat[0];
        }

        public void DeveloperComplete()
        {
            if (Completed) return;

            Clean();
            CleanupSpawnedRacer();
            PartRecovered = true;
            Set(Phase.Complete);
            FailureReason = "";
            parts.SetActive(false);
            keys.SetActive(false);
            mechanic.gameObject.SetActive(false);
            if (targetSpot != null) targetSpot.gameObject.SetActive(false);
            supportVehicle.gameObject.SetActive(true);
            ui.SetActive(false);
        }

        public void Cancel()
        {
            if (Running) Fail("미션을 중단했습니다.");
        }

        public void Fail(string reason)
        {
            if (!Running) return;

            Set(Phase.Failed);
            FailureReason = reason;
            Clean();
            CleanupSpawnedRacer();
            parts.SetActive(false);
            keys.SetActive(false);
            mechanic.gameObject.SetActive(false);
            if (targetSpot != null) targetSpot.gameObject.SetActive(false);

            Notice("빚진 정비사 중단 · " + reason + (PartRecovered
                ? "\n회수한 부품은 유지됩니다. 휴대폰에서 다시 시작하세요."
                : "\n휴대폰에서 다시 시작할 수 있습니다."));
        }

        private void Update()
        {
            if (Time.timeScale <= 0) return;

            if (!Running)
            {
                marker.gameObject.SetActive(false);
                if (Time.time > noticeUntil) ui.SetActive(false);
                return;
            }

            if (Player.HitsTaken >= 3 || Player.WaterRescueActive || PoliceControl.IsArrestInProgress)
            {
                Fail("안전하게 복귀하지 못했습니다.");
                return;
            }

            if (PrologueKeyboard.Pressed(Key.F)) Interact();
            if (PrologueKeyboard.Pressed(Key.Space)) AdvanceDialogue(true);

            // 부품 건네기 연출 (기존 기능 유지)
            if (State == Phase.Handover)
            {
                float t = Mathf.Clamp01((Time.time - entered) / handoverSeconds);
                parts.transform.position = Vector3.Lerp(boxStart, benchRest.position, Mathf.SmoothStep(0, 1, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .15f;
                parts.transform.rotation = Quaternion.Slerp(boxRotation, benchRest.rotation, t);

                if (t >= 1)
                {
                    keys.SetActive(true);
                    Set(Phase.Debrief);
                    ShowLine();
                }
            }

            if (!InDialogue && Time.time > noticeUntil) subtitle.text = "";

            // 목표 텍스트(objective) 업데이트
            if (State == Phase.RacerDeal || State == Phase.RacerDialogue || State == Phase.Racing)
            {
                objective.text = "레이서와 거래하기";
            }
            else
            {
                objective.text =
                    State == Phase.Meet ? "빚진 정비사 · 수리점에서 정비사 만나기" :
                    State == Phase.SpotMove || State == Phase.SpotDialogue ? "빚진 정비사 · 서안골목의 거래처와 대화하기" :
                    State == Phase.Return ? "정비사에게 부품 전달" :
                    State == Phase.Handover ? "정비사가 회수한 부품을 확인하는 중" :
                    "빚진 정비사 · 정비사와 대화";
            }

            if (State != Phase.Racing && State != Phase.RacerDialogue)
            {
                if (Player.RiddenBike)
                {
                    objective.text += "\n도착하면 내려서 상호작용하세요";
                }
                else if (State != Phase.Handover)
                {
                    string actionLabel =
                        State == Phase.SpotMove ? "대화" :
                        State == Phase.RacerDeal ? "거래하기" :
                        State == Phase.Return ? "부품 전달" :
                        "대화";
                    objective.text += "\n[F] " + actionLabel;
                }
            }

            // 마커(Marker) 표시
            if (State == Phase.Racing || State == Phase.RacerDialogue)
            {
                marker.gameObject.SetActive(false);
            }
            else
            {
                Transform target = GetCurrentInteractTarget();
                if (target != null && target.gameObject.activeInHierarchy)
                {
                    string label =
                        (State == Phase.SpotMove || State == Phase.SpotDialogue) ? targetSpotMarkerName :
                        (State == Phase.RacerDeal) ? "레이서" :
                        "정비사";

                    marker.gameObject.SetActive(true);
                    StoryTargetDiamond.Place(
                        marker,
                        Player.View,
                        target.position + Vector3.up * 1.8f,
                        Player.transform.position,
                        label);
                }
                else
                {
                    marker.gameObject.SetActive(false);
                }
            }

            // 정비사가 플레이어를 바라보도록 회전
            var direction = Player.transform.position - mechanic.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .1f && direction.sqrMagnitude < 100)
            {
                mechanic.rotation = Quaternion.RotateTowards(mechanic.rotation, Quaternion.LookRotation(direction), 100 * Time.deltaTime);
            }

            // 거래처 NPC가 활성화되어 있을 때 플레이어를 바라보도록 회전
            if (targetSpot != null && targetSpot.gameObject.activeInHierarchy)
            {
                var spotDir = Player.transform.position - targetSpot.position;
                spotDir.y = 0;
                if (spotDir.sqrMagnitude > .1f && spotDir.sqrMagnitude < 100)
                {
                    targetSpot.rotation = Quaternion.RotateTowards(targetSpot.rotation, Quaternion.LookRotation(spotDir), 100 * Time.deltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            if (!Running) return;

            if (State == Phase.Return)
            {
                parts.SetActive(!Player.RiddenBike && !Player.IsDown);
                if (parts.activeSelf)
                {
                    parts.transform.SetPositionAndRotation(
                        Player.View.transform.TransformPoint(new Vector3(-.32f, -.37f, .67f)),
                        Player.View.transform.rotation);
                }
            }
            else if (State == Phase.Handover)
            {
                parts.SetActive(true);
                var a = mechanic.GetComponentInChildren<Animator>();
                if (!a || !a.isHuman) return;

                var upper = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var lower = a.GetBoneTransform(HumanBodyBones.RightLowerArm);
                var hand = a.GetBoneTransform(HumanBodyBones.RightHand);
                if (!upper || !lower || !hand) return;

                float weight = Mathf.Sin(Mathf.Clamp01((Time.time - entered) / handoverSeconds) * Mathf.PI);
                var direction = parts.transform.position + Vector3.up * .1f - upper.position;
                upper.rotation = Quaternion.Slerp(
                    upper.rotation,
                    Quaternion.FromToRotation(hand.position - upper.position, direction) * upper.rotation,
                    weight * .7f);
            }
        }
    }
}