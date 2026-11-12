using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 첫 사건의 후일담과 감옥 도입을 연결한다. 기존 체포 벌금·병원 정산과는 분리한다.
    public sealed class ShopEscapeChapter : MonoBehaviour
    {
        public enum Stage
        {
            None,
            Packing,
            Escape,
            Arrest,
            Transfer,
            Prison
        }

        public Stage State { get; private set; }

        public bool Cinematic => State == Stage.Arrest || State == Stage.Transfer || (parkSequence && parkSequence.Cinematic);
        public bool InPrison => State == Stage.Prison;
        public ParkEscapeSequence parkSequence;
        public VenueFinale mission;
        public Transform bag;
        public Transform mingi;
        public Transform prisonArrival;
        public Transform prisonMingi;
        public GameObject policeBlock;
        public GameObject prison;
        public Transform[] route;
        public Transform[] ambush;
        public Transform[] doorway;
        public Transform[] police;
        public float arrestSeconds = 9;
        public float transferSeconds = 8;
        [TextArea(2, 6)]
        public string packing = "민기: 납품 기록에 이 가게 주소가 있어. 여긴 이제 안전하지 않아. 가방만 챙겨. 지금 떠나자.";
        [TextArea(2, 6)]
        public string ambushLine = "조직원: 납품 기록에 나온 가게다! 저 둘, 도망친다!\n민기: 벌써 왔어! 길을 뚫어. 같이 나가자!";
        [TextArea(2, 6)]
        public string[] arrestLines = {"경찰: 도심에서 무장 소란을 일으킨 두 사람! 무기 버리고 손 들어!", "나: 뒤에 놈들이 우릴 쫓고 있어!", "민기: 내려놔. 앞뒤가 막혔어. 여기서 더 싸우면 끝이야."};
        [TextArea(2, 6)]
        public string[] prisonLines = {"소란 신고를 받고 출동한 경찰은 두 사람을 현장에서 체포했다.", "이후, 구치소.\n박진웅의 계획은 밖에서 계속되고 있었다.", "민기: 살아서 나가야 해. 우리가 여기 있는 동안 그놈은 아무 일 없었던 척하겠지."};
        [Header("박진웅의 함정 · 대사는 Tools/Delivery Service/대사 편집에서도 수정 가능")]
        [TextArea(2, 6)]
        public string raidOrder = "조직원 무전: 박진웅 형님 지시다. 가게에서 둘 다 처리해. 금고 CCTV랑 신원은 경찰에 넘겼다. 빠져나가도 검문에 걸릴 거다!\n민기: 경찰 신고까지… 우리한테 전부 뒤집어씌우려는 거야. 뒷길로 가자!";
        [TextArea(2, 6)]
        public string pursuitBroadcast = "경찰 무전: 야마자키 금고 침입 사건 용의자 두 명. 제보된 인상착의와 일치한다. 도주로를 봉쇄하라.\n민기: 우리 이름까지 넘겼어… 진웅이야. 처음부터 빠져나갈 길을 막은 거야.";
        [TextArea(2, 6)]
        public string[] loyaltyArrestLines = {"민기: 윽… 아까 가게에서 맞은 데가… 다리가 말을 안 들어.", "민기: 너라도 가. 둘 다 잡히면 끝이야.", "나: 같이 나왔잖아. 널 두고는 안 가.", "경찰: 금고 침입 사건 용의자! 무기를 내려놓고 동료에게서 천천히 물러나라!", "나: 무기 내려놓을게! 이 사람부터 치료해 줘.", "민기: …미안하다.\n나: 살아서 나가자. 둘이 같이."};
        [TextArea(2, 6)]
        public string[] loyaltyPrisonLines = {"박진웅이 넘긴 금고 CCTV와 신원 정보로 경찰은 두 사람의 도주로를 막았다.", "플레이어는 다친 민기를 두고 떠나지 않았다. 두 사람은 함께 체포되었다.", "치료와 조사 이후, 구치소. 박진웅은 사건의 배후를 숨긴 채 두 사람에게 책임을 떠넘겼다.", "민기: 우릴 여기 넣었다고 끝난 줄 알겠지. 나가자. 우리가 이용당했다는 증거를 찾아야 해."};
        [Header("유착 경찰 · 단계별 대사")]
        [TextArea(2, 6)]
        public string gangRaid = "조직원: 진웅 형님 명령이다. 저 둘 놓치지 마!\n민기: 골목으로! 붙잡히면 끝이야!";
        [TextArea(2, 6)]
        public string gunfireDispatch = "경찰 무전: 인근 골목에서 총격 신고 접수. 현장으로 출동한다.";
        [TextArea(2, 6)]
        public string[] woundedLines = {"민기: 윽… 다리가… 더는 못 뛰겠어.", "민기: 너라도 가.\n나: 널 두고는 안 가."};
        [TextArea(2, 6)]
        public string[] corruptCallLines = {"경찰: 총격 신고 받고 왔는데… 당신네 애들이잖아. 박진웅, 지금 통화 좀 하지.", "경찰 · 통화: 도시에서 이런 소란을 일으키면 어떡해! 봐주는 데도 한계가 있지!", "박진웅 · 전화: 챙겨드린 만큼 이번에도 정리해 주셔야죠. 우리 애들은 보내고, 배달원 둘만 데려가세요."};
        [TextArea(2, 6)]
        public string[] releaseGangLines = {"경찰: 당신들은 가. 뒷일은 우리가 처리한다.", "조직원: 수고하십시오.\n나: 저놈들이 먼저 쐈는데… 그냥 보내준다고?"};
        [TextArea(2, 6)]
        public string[] corruptCustodyLines = {"경찰: 너희 둘! 무기 내려놓고 손 보여. 반항하면 동료도 무사하지 못해.", "나: 내려놓을게. 민기부터 치료해 줘.", "민기: 경찰들도 한패였어… 그렇다면 교도소도 안전하지 않을 거야…!"};
        [TextArea(2, 6)]
        public string[] corruptPrisonLines = {"경찰은 박진웅의 조직원들을 보내고, 플레이어와 민기만 체포했다.", "민기의 치료와 형식적인 조사 이후, 두 사람은 구금되었다.", "민기: 여기서도 누굴 믿어야 할지 몰라. 둘이 같이 빠져나갈 방법을 찾자."};
        public string prisonChapterTitle = "챕터 2 · 철창 너머";
        public float dialogueLineSeconds = 4.5f;
        private static int Count(string[] lines) => lines == null ? 0 : lines.Length;
        private float WoundedEnd => Mathf.Max(1, Count(woundedLines)) * Mathf.Max(1, dialogueLineSeconds);
        private float CallEnd => WoundedEnd + Mathf.Max(1, Count(corruptCallLines)) * Mathf.Max(1, dialogueLineSeconds);
        private float ReleaseEnd => CallEnd + Mathf.Max(1, Count(releaseGangLines)) * Mathf.Max(1, dialogueLineSeconds);
        private bool gangReleased;
        private Animator callingAnimator;
        private Transform callUpper;
        private Transform callLower;
        private Transform callHand;
        private Transform callHead;
        private Quaternion callUpperRest;
        private Quaternion callLowerRest;
        private bool callPose;
        private bool weaponLowered;
        private float broadcastUntil;
        public float ArrestDuration => Mathf.Max(arrestSeconds, ReleaseEnd + Mathf.Max(1, Count(corruptCustodyLines)) * Mathf.Max(1, dialogueLineSeconds));
        public float TransferDuration => Mathf.Max(transferSeconds, Mathf.Max(1, Count(corruptPrisonLines)) * Mathf.Max(1, dialogueLineSeconds));
        private static string LineAt(string[] lines, float t, float duration)
        {
            return lines == null || lines.Length == 0 ? "" : lines[Mathf.Clamp((int)(t / Mathf.Max(.01f, duration) * lines.Length), 0, lines.Length - 1)];
        }

        // 동료 이동 상태는 기존 외부 호출에서도 조회할 수 있다.
        public bool CompanionHasPath => companion.HasPath;
        private int step;
        private float entered;
        private bool secondWave;
        private bool policeZoneReached;
        private AudioSource siren;
        private readonly HashSet<CitizenCombat> subdued = new();
        private Vector3 mingiHome;
        private Quaternion mingiRotation;
        private bool oldPhone;
        private bool phoneCaptured;
        private PhoneController phone;
        public int RouteStep => step;
        public int SubduedCount => subdued.Count;
        public Transform Goal => route[Mathf.Clamp(step, 0, route.Length - 1)];
        private void Awake()
        {
            companion = new ShopEscapeCompanion(this);
            companion.Initialize();
            mingiHome = mingi.position;
            mingiRotation = mingi.rotation;
            bag.gameObject.SetActive(false);
            policeBlock.SetActive(false);
            prison.SetActive(false);
            siren = gameObject.AddComponent<AudioSource>();
            siren.playOnAwake = false;
            siren.loop = true;
            siren.volume = .16f;
            siren.spatialBlend = 0;
            siren.clip = SuppliedAudio.Current ? SuppliedAudio.Current.policeSiren : null;
            StorySoundPause.Attach(siren);
        }

        private void Set(Stage stage)
        {
            State = stage;
            entered = Time.time;
        }

        public void Begin()
        {
            ResetChapter();
            mission.Player.ResetAfterWaterRescue();
            mission.Day.EnterNightPreparation();
            Set(Stage.Packing);
            bag.gameObject.SetActive(true);
            mission.subtitle.text = packing;
            mission.ui.SetActive(true);
            if (parkSequence)
            {
                parkSequence.BeginPreparation();
            }
        }

        public bool TakeBag()
        {
            if (parkSequence)
            {
                return parkSequence.TakePistol();
            }

            if (State != Stage.Packing || !mission.Player.CanAct || Vector3.Distance(mission.Player.transform.position, bag.position) > 3)
            {
                return false;
            }

            bag.gameObject.SetActive(false);
            StorySoundMix.Play(mission.sound, SuppliedAudio.Current ? SuppliedAudio.Current.bag : null, .4f);
            Set(Stage.Escape);
            policeBlock.SetActive(true);
            step = 0;
            subdued.Clear();
            secondWave = false;
            policeZoneReached = false;
            mission.subtitle.text = gangRaid;
            broadcastUntil = 0;
            companion.PrepareEscape();
            mission.SpawnStreetPursuers(ambush, 2, 4.8f);
            return true;
        }

        public void Tick()
        {
            if (Time.timeScale <= 0)
            {
                return;
            }

            if (parkSequence && parkSequence.Tick())
            {
                return;
            }

            if (State == Stage.Packing)
            {
                TickPacking();
                return;
            }

            if (State == Stage.Escape)
            {
                TickEscape();
                return;
            }

            if (State == Stage.Arrest)
            {
                TickArrest();
                return;
            }

            if (State == Stage.Transfer)
            {
                TickTransfer();
                return;
            }

            if (State == Stage.Prison)
            {
                TickPrison();
                return;
            }
        }

        private ShopEscapeCompanion companion;
        public float CompanionFollowSpeed => companion.FollowSpeed;
        public void Follow()
        {
            companion.Follow();
        }

        public void StopGuide()
        {
            companion.StopGuide();
        }

        public void StartInjury()
        {
            companion.StartInjury();
        }

        private void LateUpdate()
        {
            if (!parkSequence)
            {
                UpdateCallPose();
            }

            companion.TickInjury();
        }

        private void Mark(Vector3 position, string text)
        {
            mission.marker.gameObject.SetActive(true);
            StoryTargetDiamond.Place(mission.marker, mission.Player.View, position + Vector3.up, mission.Player.transform.position, text);
        }

        public void StartParkEscape()
        {
            companion.BeginParkEscape();
            Set(Stage.Escape);
            policeBlock.SetActive(false);
        }

        public void BeginArrest()
        {
            if (State != Stage.Escape)
            {
                return;
            }

            StopGuide();
            foreach (var enemy in mission.Enemies)
            {
                enemy.ExternalAttackControl = true;
            }

            Set(Stage.Arrest);
            weaponLowered = false;
            gangReleased = false;
            StartInjury();
            if (parkSequence)
            {
                phone = FindFirstObjectByType<PhoneController>();
                if (phone)
                {
                    oldPhone = phone.enabled;
                    phoneCaptured = true;
                    phone.ClosePhone();
                    phone.enabled = false;
                }

                mission.StoryControl(true);
                return;
            }

            // 경찰은 민기와 플레이어 사이를 가리지 않는 측면에서 접근한다.
            var toward = (mingi.position - mission.Player.transform.position);
            toward.y = 0;
            toward.Normalize();
            var side = Vector3.Cross(Vector3.up, toward);
            for (int i = 0; i < police.Length; i++)
            {
                var pos = mission.Player.transform.position + side * (i % 2 == 0 ? 4 : -4) + toward * (i < 2 ? 0 : -2);
                if (NavMesh.SamplePosition(pos, out var hit, 2, NavMesh.AllAreas))
                {
                    police[i].position = hit.position;
                }
            }

            policeBlock.SetActive(true);
            foreach (var cop in police)
            {
                var delta = mission.Player.transform.position - cop.position;
                delta.y = 0;
                if (delta.sqrMagnitude > .01f)
                {
                    cop.rotation = Quaternion.LookRotation(delta);
                }
            }

            phone = FindFirstObjectByType<PhoneController>();
            if (phone)
            {
                oldPhone = phone.enabled;
                phoneCaptured = true;
                phone.ClosePhone();
                phone.enabled = false;
            }

            mission.StoryControl(true);
            mission.cinema.transform.SetPositionAndRotation(mission.Player.View.transform.position, mission.Player.View.transform.rotation);
            if (siren.clip)
            {
                siren.Play();
            }
        }

        public void EnterPrison()
        {
            if (State != Stage.Arrest)
            {
                return;
            }

            RestoreCallPose();
            companion.RestoreInjury();
            if (parkSequence)
            {
                parkSequence.Finish();
            }

            Set(Stage.Transfer);
            mission.ClearStoryEnemies();
            siren.Stop();
            mission.Player.GetComponent<PlayerWeapon>()?.Equip(WeaponKind.None);
            prison.SetActive(true);
            policeBlock.SetActive(false);
            mission.PlacePlayer(prisonArrival);
            mingi.SetPositionAndRotation(prisonMingi.position, prisonMingi.rotation);
            mission.cinema.transform.SetPositionAndRotation(mission.Player.View.transform.position, mission.Player.View.transform.rotation);
            mission.cinema.transform.LookAt(mingi.position + Vector3.up * 1.4f);
            mission.fade.gameObject.SetActive(true);
            mission.fade.color = Color.black;
            StorySoundMix.Play(mission.sound, SuppliedAudio.Current ? SuppliedAudio.Current.metalImpact : null, .35f);
        }

        public void ResetChapter()
        {
            if (parkSequence)
            {
                parkSequence.ResetSequence();
            }

            RestoreCallPose();
            gangReleased = false;
            companion.RestoreInjury();
            weaponLowered = false;
            broadcastUntil = 0;
            if (Cinematic)
            {
                mission.StoryControl(false);
            }

            StopGuide();
            if (siren)
            {
                siren.Stop();
            }

            if (phoneCaptured && phone)
            {
                phone.enabled = oldPhone;
                phoneCaptured = false;
            }

            bag.gameObject.SetActive(false);
            policeBlock.SetActive(false);
            prison.SetActive(false);
            if (State != Stage.None)
            {
                mingi.SetPositionAndRotation(mingiHome, mingiRotation);
            }

            var body = mingi.GetComponent<CharacterController>();
            if (body)
            {
                body.enabled = true;
            }

            State = Stage.None;
        }

        // 통화 중에는 손을 귀에 대고, 통화 종료·스킵 시 원래 자세로 돌린다.
        private void UpdateCallPose()
        {
            bool active = State == Stage.Arrest && Time.time - entered >= WoundedEnd && Time.time - entered < CallEnd;
            if (!active)
            {
                RestoreCallPose();
                return;
            }

            if (police == null || police.Length == 0 || !police[0])
            {
                return;
            }

            if (!callPose)
            {
                callingAnimator = police[0].GetComponentInChildren<Animator>();
                var bones = police[0].GetComponentsInChildren<Transform>();
                bool human = callingAnimator && callingAnimator.isHuman;
                callUpper = human ? callingAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm) : bones.FirstOrDefault(b => b.name == "Upper Arm.R");
                callLower = human ? callingAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm) : bones.FirstOrDefault(b => b.name == "Lower Arm.R");
                callHand = human ? callingAnimator.GetBoneTransform(HumanBodyBones.RightHand) : bones.FirstOrDefault(b => b.name == "Hand.R");
                callHead = human ? callingAnimator.GetBoneTransform(HumanBodyBones.Head) : bones.FirstOrDefault(b => b.name == "Head");
                if (!callUpper || !callLower)
                {
                    return;
                }

                callUpperRest = callUpper.localRotation;
                callLowerRest = callLower.localRotation;
                callPose = true;
            }

            callUpper.localRotation = callUpperRest;
            callLower.localRotation = callLowerRest;
            var hand = callHand;
            var head = callHead;
            if (!head || !hand)
            {
                return;
            }

            var target = head.position + police[0].right * .14f;
            var elbow = callUpper.position + police[0].forward * .3f - police[0].up * .18f;
            callUpper.rotation = Quaternion.FromToRotation(callLower.position - callUpper.position, elbow - callUpper.position) * callUpper.rotation;
            callLower.rotation = Quaternion.FromToRotation(hand.position - callLower.position, target - callLower.position) * callLower.rotation;
        }

        private void RestoreCallPose()
        {
            if (!callPose)
            {
                return;
            }

            if (callUpper)
            {
                callUpper.localRotation = callUpperRest;
            }

            if (callLower)
            {
                callLower.localRotation = callLowerRest;
            }

            callPose = false;
        }

        private void ReleaseGang()
        {
            foreach (var e in mission.Enemies)
            {
                if (!e)
                {
                    continue;
                }

                e.EndFight();
                e.ExternalAttackControl = true;
                e.enabled = false;
                e.Npc.ExternalControl = true;
            }
        }

        private void TickReleasedGang()
        {
            if (!gangReleased)
            {
                return;
            }

            foreach (var e in mission.Enemies)
            {
                if (!e || e.Health <= 0 || e.Npc.IsDead || e.Npc.IsKnockedDown)
                {
                    continue;
                }

                var agent = e.GetComponent<NavMeshAgent>();
                if (!agent || !agent.enabled || !agent.isOnNavMesh)
                {
                    continue;
                }

                agent.isStopped = false;
                agent.speed = 3;
                agent.stoppingDistance = .5f;
                agent.updateRotation = true;
                if (!agent.hasPath && ambush.Length > 0 && NavMesh.SamplePosition(ambush[0].position, out var exit, 8, NavMesh.AllAreas))
                {
                    agent.SetDestination(exit.position);
                }

                var anim = e.GetComponentInChildren<Animator>();
                if (anim)
                {
                    foreach (var parameter in anim.parameters)
                    {
                        if (parameter.name == "MoveSpeed" && parameter.type == AnimatorControllerParameterType.Float)
                        {
                            anim.SetFloat("MoveSpeed", agent.velocity.magnitude);
                        }

                        if (parameter.name == "IsWalking" && parameter.type == AnimatorControllerParameterType.Bool)
                        {
                            anim.SetBool("IsWalking", agent.velocity.sqrMagnitude > .01f);
                        }
                    }
                }
            }
        }

        private void TickPacking()
        {
            mission.objective.text = "매장을 떠날 준비 · 비상 가방 챙기기 [F]";
            Mark(bag.position, "비상 가방");
            if (PrologueKeyboard.Pressed(Key.F))
            {
                TakeBag();
            }

            return;
        }

        private void TickEscape()
        {
            if (broadcastUntil > 0 && Time.time > broadcastUntil)
            {
                mission.subtitle.text = "";
                broadcastUntil = 0;
            }

            Follow();
            foreach (var e in mission.Enemies)
            {
                if (!e)
                {
                    continue;
                }

                if (!e.usesFirearm)
                {
                    e.usesFirearm = true;
                    e.Provoke(mission.Player);
                }

                if (e.Health <= 0 || e.Npc.IsKnockedDown)
                {
                    subdued.Add(e);
                }
            }

            if (mission.Player.RiddenBike)
            {
                mission.Player.RiddenBike.ExitBike();
            }

            var distance = Vector3.Distance(mission.Player.transform.position, Goal.position);
            if (distance < 4 && step < route.Length - 1)
            {
                step++;
                if (!secondWave)
                {
                    secondWave = true;
                    mission.SpawnStreetPursuers(ambush.Skip(2).ToArray(), 2, 5);
                    broadcastUntil = Time.time + 12;
                    mission.subtitle.text = gunfireDispatch;
                }
            }

            distance = Vector3.Distance(mission.Player.transform.position, Goal.position);
            if (step == route.Length - 1 && distance < 4)
            {
                policeZoneReached = true;
            }

            mission.objective.text = "민기와 매장 탈출 · 조직원을 피해 골목으로" + (step == route.Length - 1 && SubduedCount < 2 ? "\n길을 막는 조직원 제압 " + SubduedCount + " / 2" : "");
            Mark(Goal.position, "민기와 탈출");
            if (step == route.Length - 1 && policeZoneReached && distance < 15 && SubduedCount >= 2 && Vector3.Distance(mingi.position, mission.Player.transform.position) < 2.8f)
            {
                BeginArrest();
            }

            return;
        }

        private void TickArrest()
        {
            float t = Time.time - entered, duration = ArrestDuration;
            mission.objective.text = "다친 민기 곁에 남기";
            mission.marker.gameObject.SetActive(false);
            mission.subtitle.text = t < WoundedEnd ? LineAt(woundedLines, t, WoundedEnd) : t < CallEnd ? LineAt(corruptCallLines, t - WoundedEnd, CallEnd - WoundedEnd) : t < ReleaseEnd ? LineAt(releaseGangLines, t - CallEnd, ReleaseEnd - CallEnd) : LineAt(corruptCustodyLines, t - ReleaseEnd, duration - ReleaseEnd);
            if (t >= CallEnd && !gangReleased)
            {
                gangReleased = true;
                ReleaseGang();
            }

            bool surrender = t >= ReleaseEnd;
            TickReleasedGang();
            if (surrender && !weaponLowered)
            {
                mission.Player.GetComponent<PlayerWeapon>()?.Equip(WeaponKind.None);
                weaponLowered = true;
            }

            var officer = police != null && police.Length > 0 ? police[0] : null;
            if (surrender && officer)
            {
                var delta = mission.Player.transform.position - officer.position;
                delta.y = 0;
                if (delta.sqrMagnitude > 5)
                {
                    officer.position += delta.normalized * Time.deltaTime * .9f;
                }

                if (delta.sqrMagnitude > .01f)
                {
                    officer.rotation = Quaternion.LookRotation(delta);
                }
            }

            Vector3 focus = (t < WoundedEnd || t > duration - Mathf.Max(1, dialogueLineSeconds)) || !officer ? mingi.position + Vector3.up * 1.1f : officer.position + Vector3.up * 1.45f;
            var look = focus - mission.cinema.transform.position;
            if (look.sqrMagnitude > .01f)
            {
                mission.cinema.transform.rotation = Quaternion.Slerp(mission.cinema.transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 3);
            }

            mission.fade.gameObject.SetActive(t > duration - 2);
            mission.fade.color = new Color(0, 0, 0, Mathf.InverseLerp(duration - 2, duration, t));
            if (t >= duration || PrologueKeyboard.Pressed(Key.Space))
            {
                EnterPrison();
            }

            return;
        }

        private void TickTransfer()
        {
            float t = Time.time - entered;
            mission.objective.text = GameDialogue.Resolve(prisonChapterTitle);
            mission.subtitle.text = LineAt(corruptPrisonLines, t, TransferDuration);
            mission.fade.gameObject.SetActive(t < 2.5f);
            mission.fade.color = new Color(0, 0, 0, 1 - Mathf.InverseLerp(1, 2.5f, t));
            if (t >= TransferDuration || PrologueKeyboard.Pressed(Key.Space))
            {
                Set(Stage.Prison);
                if (PrisonPreviewTransition.Begin())
                {
                    return;
                }

                mission.StoryControl(false);
                mission.Player.GetComponent<PlayerWeapon>()?.Equip(WeaponKind.None);
                mission.objective.text = GameDialogue.Resolve(prisonChapterTitle);
                mission.subtitle.text = LineAt(corruptPrisonLines, TransferDuration, TransferDuration);
            }

            return;
        }

        private void TickPrison()
        {
            mission.objective.text = GameDialogue.Resolve(prisonChapterTitle);
            mission.marker.gameObject.SetActive(false);
            if (Time.time - entered > 8)
            {
                mission.subtitle.text = "";
            }
        }

        private void OnDisable()
        {
            RestoreCallPose();
            if (siren)
            {
                siren.Stop();
            }
        }
    }
}
