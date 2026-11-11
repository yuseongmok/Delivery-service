using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 매장 탈출 후 공원 총격전. 동선·소품은 씬에 저장하고 이 컴포넌트는 연출 순서만 제어한다.
    [DefaultExecutionOrder(2100)]
    public sealed class ParkEscapeSequence : MonoBehaviour
    {
        public enum Beat
        {
            None,
            Pickup,
            Run,
            LookBack,
            Fight,
            Wounded,
            Defend,
            Arrival,
            Call,
            Release,
            Custody,
            Done
        }

        public Beat Current { get; private set; }

        public bool Cinematic => Current == Beat.LookBack || Current == Beat.Wounded || Current >= Beat.Arrival && Current <= Beat.Custody;
        public ShopEscapeChapter chapter;
        public Transform pistol;
        public Transform park;
        public Transform mingiStand;
        public Transform threat;
        public Transform officer;
        public Transform officerStand;
        public Transform policeCar;
        public Transform carStart;
        public Transform carStop;
        public Transform arrivalCamera;
        public Transform gangExit;
        public Transform phoneProp;
        public Transform mingiGun;
        public Transform[] approach;
        public Transform[] enemyPosts;
        public Transform[] backupPosts;
        public Transform[] enemyCover;
        public Transform partnerCover;
        public ParkPoliceArrivalBehaviour policeArrival;
        private bool engagementReady;
        private float combatStarted;
        public float fightSeconds = 18;
        public float defendSeconds = 22;
        public float turnSeconds = 2.5f;
        public float arrivalSeconds = 7;
        public float lineSeconds = 4.5f;
        [TextArea(2, 6)]
        public string pickupLine = "민기: 놈들이 곧 올 거야. 벽에 걸린 권총 챙겨. 공원 쪽으로 빠지자!";
        [TextArea(2, 6)]
        public string runLine = "민기: 이쪽이야! 공원까지 가서 뒤를 막자!";
        [TextArea(2, 6)]
        public string lookLine = "민기: 뒤에서 온다! 여기서 막아!";
        [TextArea(2, 6)]
        public string hurtLine = "민기: 윽! 다리를 맞았어…!\n나: 숙이고 있어! 내가 막을게!";
        [TextArea(2, 6)]
        public string arrivalLine = "경찰: 전부 사격 중지! 무기 내려!";
        [TextArea(2, 6)]
        public string[] callLines = {"경찰: 이거 박진웅 부하들이잖아…", "경찰: 박진웅. 도시에서 이런 소란을 일으키면 어떡해! 봐주는 데도 한계가 있지!", "경찰: …", "경찰: 하… 이번만이다."};
        [TextArea(2, 6)]
        public string[] releaseLines = {"경찰: 너네들은 가 봐.", "나: 저놈들이 먼저 쐈는데… 그냥 보내준다고?"};
        [TextArea(2, 6)]
        public string[] custodyLines = {"경찰: 너희 둘은 같이 간다. 무기 내려놓고 손 보여.", "나: 내려놓을게. 민기부터 치료해 줘.", "민기: 경찰들도 한패였어… 그렇다면 교도소도 안전하지 않을 거야…!"};
        public int SupportShots => combat.SupportShots;
        public bool GangReleased { get; private set; }

        private VenueFinale M => chapter.mission;
        private PlayerCombat Player => M.Player;
        private bool ownsTraffic;
        private ParkEscapeCrowd crowd;
        private float nextTraffic;
        readonly EscapeRouteGuide parkRoute = new EscapeRouteGuide();
        public bool ProtectedFirefight => Current >= Beat.LookBack && Current <= Beat.Defend;
        public static bool Protects(PlayerCombat p) => VenueFinale.Active && VenueFinale.Current.Player == p && VenueFinale.Current.aftermath && VenueFinale.Current.aftermath.parkSequence && VenueFinale.Current.aftermath.parkSequence.ProtectedFirefight;
        public static bool Protects(CitizenCombat e) => VenueFinale.Active && VenueFinale.Current.aftermath && VenueFinale.Current.aftermath.parkSequence && VenueFinale.Current.aftermath.parkSequence.ProtectedFirefight && e.GetComponent<ParkCoverActor>();
        private float entered;
        private float nextWave;
        private float nextAmmo;
        private int waypoint;
        private bool injured;
        internal bool Injured => injured;
        private Vector3 viewStart;
        private Quaternion viewRotation;
        private ParkEscapeCombat combat;
        private ParkEscapeCinematics cinematics;
        internal float Elapsed => Time.time - entered;
        private float Duration(string[] lines) => Mathf.Max(1, lines == null ? 0 : lines.Length) * Mathf.Max(2, lineSeconds);
        private string Line(string[] lines) => lines == null || lines.Length == 0 ? "" : lines[Mathf.Clamp((int)(Elapsed / Mathf.Max(2, lineSeconds)), 0, lines.Length - 1)];
        private void Awake()
        {
            combat = new ParkEscapeCombat(this);
            combat.Initialize();
            cinematics = new ParkEscapeCinematics(this);
            cinematics.Initialize();
            ResetSequence();
        }

        private void Set(Beat beat)
        {
            Current = beat;
            entered = Time.time;
        }

        private void Mark(Transform at, string text)
        {
            M.marker.gameObject.SetActive(true);
            StoryTargetDiamond.Place(M.marker, Player.View, at.position + Vector3.up * (at == pistol ? .2f : 1), Player.transform.position, text);
        }

        public void BeginPreparation()
        {
            ResetSequence();
            crowd ??= new ParkEscapeCrowd(chapter);
            crowd.Suppress();
            M.getaway.SuppressTraffic();
            ownsTraffic = true;
            nextTraffic = 0;
            chapter.bag.gameObject.SetActive(false);
            pistol.gameObject.SetActive(true);
            Set(Beat.Pickup);
            M.subtitle.text = pickupLine;
        }

        public bool TakePistol()
        {
            if (Current != Beat.Pickup || !Player.CanAct || Vector3.Distance(Player.View.transform.position, pistol.position) > 3)
            {
                return false;
            }

            var delta = pistol.position - Player.View.transform.position;
            if (Vector3.Dot(Player.View.transform.forward, delta.normalized) < .65f)
            {
                return false;
            }

            if (Physics.RaycastAll(Player.View.transform.position, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(h => !h.transform.IsChildOf(Player.transform) && !h.transform.IsChildOf(pistol) && h.distance < delta.magnitude - .12f))
            {
                return false;
            }

            var weapon = Player.GetComponent<PlayerWeapon>();
            weapon.Equip(WeaponKind.Pistol);
            weapon.GrantMissionAmmo(90);
            pistol.gameObject.SetActive(false);
            chapter.StartParkEscape();
            Set(Beat.Run);
            waypoint = 0;
            M.subtitle.text = runLine;
            return true;
        }

        public bool Tick()
        {
            if (Current == Beat.None || Current == Beat.Done)
            {
                return false;
            }

            if (ownsTraffic && Time.time >= nextTraffic)
            {
                crowd?.Suppress();
                M.getaway.SuppressTraffic();
                nextTraffic = Time.time + 1;
            }

            if (Current == Beat.Pickup)
            {
                TickPickup();
                return true;
            }

            if (Current == Beat.Run)
            {
                TickRun();
                return true;
            }

            if (Current == Beat.LookBack)
            {
                TickLookBack();
                return true;
            }

            if (Current == Beat.Fight || Current == Beat.Defend)
            {
                TickCombat();
                return true;
            }

            if (Current == Beat.Wounded)
            {
                TickWounded();
                return true;
            }

            if (Current == Beat.Arrival)
            {
                TickArrival();
                return true;
            }

            if (Current == Beat.Call)
            {
                TickCall();
                return true;
            }

            if (Current == Beat.Release)
            {
                TickRelease();
                return true;
            }

            if (Current == Beat.Custody)
            {
                TickCustody();
                return true;
            }

            return false;
        }

        private void ReturnControl()
        {
            var rotation = M.cinema.transform.rotation;
            Player.transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            Player.View.transform.rotation = rotation;
            M.StoryControl(false);
        }

        private void BeginLookBack()
        {
            chapter.StopGuide();
            M.marker.gameObject.SetActive(false);
            viewStart = Player.View.transform.position;
            viewRotation = Player.View.transform.rotation;
            M.StoryControl(true);
            M.cinema.transform.SetPositionAndRotation(viewStart, viewRotation);
            Set(Beat.LookBack);
            engagementReady = false;
            combat.BeginCover();
            M.subtitle.text = lookLine;
            combat.SpawnWave();
            mingiGun.gameObject.SetActive(true);
        }

        private void WoundMingi()
        {
            if (Player.IsDown)
            {
                return;
            }

            combat.FreezeEnemies();
            var shooter = M.Enemies.FirstOrDefault(e => e && e.Health > 0 && !e.Npc.IsKnockedDown);
            if (shooter)
            {
                GunshotSound.Play(shooter.gameObject, true);
                PrologueTracer.Spawn(shooter.transform.position + Vector3.up * 1.35f, chapter.mingi.position + Vector3.up * .45f, new Color(1, .55f, .2f));
            }

            injured = true;
            combat.StopPartner();
            chapter.StartInjury();
            mingiGun.gameObject.SetActive(false);
            viewStart = Player.View.transform.position;
            M.StoryControl(true);
            M.cinema.transform.SetPositionAndRotation(viewStart, Player.View.transform.rotation);
            Set(Beat.Wounded);
            M.objective.text = "민기가 다리를 맞았다";
            M.subtitle.text = hurtLine;
        }

        private void BeginArrival()
        {
            combat.FreezeEnemies();
            viewStart = Player.View.transform.position;
            chapter.BeginArrest();
            Set(Beat.Arrival);
            M.objective.text = "경찰 출동 · 사격 중지";
            M.subtitle.text = arrivalLine;
            policeCar.gameObject.SetActive(true);
            policeCar.SetPositionAndRotation(carStart.position, carStart.rotation);
            chapter.policeBlock.SetActive(false);
            if (policeArrival)
            {
                policeArrival.Begin();
            }

            cinematics.PlaySiren();
            foreach (var e in M.Enemies)
            {
                if (e)
                {
                    e.usesFirearm = false;
                }
            }
        }

        private void BeginRelease()
        {
            phoneProp.gameObject.SetActive(false);
            cinematics.RestoreOfficer();
            Set(Beat.Release);
            GangReleased = true;
            foreach (var e in M.Enemies)
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

            M.subtitle.text = Line(releaseLines);
        }

        private void RestoreMissionTraffic()
        {
            crowd?.Restore();
            if (ownsTraffic && M && M.getaway)
            {
                M.getaway.RestoreTraffic();
            }

            ownsTraffic = false;
        }

        public void Finish()
        {
            RestoreMissionTraffic();
            cinematics.RestoreOfficer();
            cinematics.StopSiren();
            phoneProp.gameObject.SetActive(false);
            mingiGun.gameObject.SetActive(false);
            policeCar.gameObject.SetActive(false);
            Set(Beat.Done);
        }

        public void ResetSequence()
        {
            RestoreMissionTraffic();
            combat.Reset();
            engagementReady = false;
            if (policeArrival)
            {
                policeArrival.ResetArrival();
            }

            cinematics.Reset();
            if (pistol)
            {
                pistol.gameObject.SetActive(false);
            }

            if (phoneProp)
            {
                phoneProp.gameObject.SetActive(false);
            }

            if (mingiGun)
            {
                mingiGun.gameObject.SetActive(false);
            }

            if (policeCar)
            {
                policeCar.gameObject.SetActive(false);
            }

            injured = false;
            GangReleased = false;
            nextAmmo = 0;
            parkRoute.Reset();
            Current = Beat.None;
        }

        public void PrepareParkEnemy(CitizenCombat enemy)
        {
            combat.PrepareParkEnemy(enemy);
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0)
            {
                return;
            }

            combat.ApplyPartnerPose();
            cinematics.TickPose();
        }

        private void TickPickup()
        {
            M.objective.text = "매장 탈출 · 벽의 권총 챙기기 [F]";
            Mark(pistol, "권총과 탄약");
            if (PrologueKeyboard.Pressed(Key.F))
            {
                TakePistol();
            }

            return;
        }

        private void TickRun()
        {
            chapter.Follow();
            if (Player.RiddenBike)
            {
                Player.RiddenBike.ExitBike();
            }

            var next = parkRoute.Marker(Player.transform.position, park.position, false);
            M.marker.gameObject.SetActive(true);
            StoryTargetDiamond.Place(M.marker, Player.View, next + Vector3.up, Player.transform.position, "민기와 공원으로");
            M.objective.text = "민기와 공원으로 도망치기";
            if (Vector3.Distance(Player.transform.position, park.position) < 4 && Vector3.Distance(chapter.mingi.position, Player.transform.position) < 4)
            {
                BeginLookBack();
            }

            if (Elapsed > 7)
            {
                M.subtitle.text = "";
            }

            return;
        }

        private void TickLookBack()
        {
            combat.UpdateCover(false);
            cinematics.AimCamera(threat.position + Vector3.up);
            if (Elapsed >= turnSeconds || PrologueKeyboard.Pressed(Key.Space))
            {
                ReturnControl();
                Set(Beat.Fight);
                M.subtitle.text = lookLine;
                nextWave = Time.time + 8;
            }

            return;
        }

        private void TickCombat()
        {
            combat.ArmEnemies();
            if (!engagementReady)
            {
                combat.UpdateCover(false);
                M.objective.text = "공원 안으로 진입하는 조직원에 대비";
                if (M.Enemies.Count(e => e && e.Health > 0 && e.GetComponent<ParkCoverActor>() && e.GetComponent<ParkCoverActor>().AtCover) >= Mathf.Min(2, M.Enemies.Count(e => e && e.Health > 0)) && M.Enemies.Any(e => e && e.Health > 0))
                {
                    engagementReady = true;
                    combatStarted = Time.time;
                    nextWave = Time.time + 8;
                }
                else if (!M.Enemies.Any(e => e && e.Health > 0) && M.PendingSpawnCount == 0)
                {
                    combat.SpawnWave();
                }

                return;
            }

            combat.UpdateCover(true);
            M.objective.text = Current == Beat.Fight ? "민기와 함께 조직원 저지" : "다친 민기를 지키며 조직원 저지";
            M.marker.gameObject.SetActive(false);
            if (Vector3.Distance(Player.transform.position, park.position) > 25)
            {
                M.objective.text = "민기를 두고 갈 수 없습니다 · 공원으로 돌아가기";
                Mark(park, "민기에게 돌아가기");
            }

            if (Time.time >= nextWave && combat.Waves < 3)
            {
                nextWave = Time.time + 10;
                combat.SpawnWave();
            }

            if (Current == Beat.Fight)
            {
                combat.SupportFire();
                if (Time.time - combatStarted >= fightSeconds)
                {
                    if (M.Enemies.Any(e => e && e.Health > 0 && !e.Npc.IsKnockedDown))
                    {
                        WoundMingi();
                    }
                    else if (M.PendingSpawnCount == 0 && Time.time >= nextWave)
                    {
                        combat.SpawnWave();
                        nextWave = Time.time + 5;
                    }
                }
            }
            else if (Elapsed >= defendSeconds && Vector3.Distance(Player.transform.position, chapter.mingi.position) < 20 && !Player.IsDown)
            {
                BeginArrival();
            }

            // 전투 중 탄약이 떨어져 진행이 막히지 않도록 민기 옆에서 예비 탄창을 받는다.
            var weapon = Player.GetComponent<PlayerWeapon>();
            if (weapon.Equipped == WeaponKind.Pistol && weapon.Ammo < 12 && Vector3.Distance(Player.transform.position, chapter.mingi.position) < 4 && Time.time >= nextAmmo)
            {
                weapon.GrantMissionAmmo(30);
                nextAmmo = Time.time + 10;
            }

            return;
        }

        private void TickWounded()
        {
            cinematics.AimCamera(chapter.mingi.position + Vector3.up);
            if (Elapsed >= 2.3f || PrologueKeyboard.Pressed(Key.Space))
            {
                ReturnControl();
                combat.UnfreezeEnemies();
                Set(Beat.Defend);
                combat.SpawnWave();
                nextWave = Time.time + 10;
            }

            return;
        }

        private void TickArrival()
        {
            if (policeArrival)
            {
                chapter.policeBlock.SetActive(true);
                policeArrival.Tick(Elapsed);
            }
            else
            {
                policeCar.position = Vector3.Lerp(carStart.position, carStop.position, Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / 4)));
                if (Elapsed > 4)
                {
                    chapter.policeBlock.SetActive(true);
                    cinematics.MoveOfficer(officerStand.position, 1.8f);
                }
            }

            cinematics.FlashLights();
            M.cinema.transform.SetPositionAndRotation(arrivalCamera.position, arrivalCamera.rotation);
            M.cinema.transform.LookAt((Elapsed > 7 ? officer.position : policeCar.position) + Vector3.up);
            if ((policeArrival ? policeArrival.Ready : Elapsed >= arrivalSeconds) || PrologueKeyboard.Pressed(Key.Space))
            {
                if (policeArrival)
                {
                    policeArrival.Complete();
                }
                else
                {
                    policeCar.SetPositionAndRotation(carStop.position, carStop.rotation);
                    officer.position = officerStand.position;
                }

                chapter.policeBlock.SetActive(true);
                cinematics.SetOfficerSpeed(0);
                cinematics.QuietSiren();
                Set(Beat.Call);
                M.subtitle.text = Line(callLines);
            }

            return;
        }

        private void TickCall()
        {
            M.objective.text = "무기를 내려놓고 기다리기";
            M.subtitle.text = Line(callLines);
            cinematics.CloseOfficerCamera();
            cinematics.FlashLights();
            if (Elapsed >= Duration(callLines) || PrologueKeyboard.Pressed(Key.Space))
            {
                BeginRelease();
            }

            return;
        }

        private void TickRelease()
        {
            M.subtitle.text = Line(releaseLines);
            cinematics.FlashLights();
            combat.RetreatGang();
            var lead = M.Enemies.FirstOrDefault(e => e && e.Health > 0 && !e.Npc.IsKnockedDown);
            if (lead)
            {
                ParkEscapeCinematics.Face(officer, lead.transform.position);
                cinematics.AimCamera(lead.transform.position + Vector3.up);
            }
            else
            {
                cinematics.CloseOfficerCamera();
            }

            if (Elapsed >= Duration(releaseLines) || PrologueKeyboard.Pressed(Key.Space))
            {
                Set(Beat.Custody);
                Player.GetComponent<PlayerWeapon>().Equip(WeaponKind.None);
                M.subtitle.text = Line(custodyLines);
            }

            return;
        }

        private void TickCustody()
        {
            M.objective.text = "민기와 함께 체포";
            M.subtitle.text = Line(custodyLines);
            combat.RetreatGang();
            ParkEscapeCinematics.Face(officer, Player.transform.position);
            if (Elapsed < Duration(custodyLines) - lineSeconds)
            {
                cinematics.CloseOfficerCamera();
            }
            else
            {
                M.cinema.transform.position = viewStart;
                cinematics.AimCamera(chapter.mingi.position + Vector3.up);
            }

            float duration = Duration(custodyLines);
            M.fade.gameObject.SetActive(Elapsed > duration - 1.5f);
            M.fade.color = new Color(0, 0, 0, Mathf.InverseLerp(duration - 1.5f, duration, Elapsed));
            if (Elapsed >= duration || PrologueKeyboard.Pressed(Key.Space))
            {
                chapter.EnterPrison();
            }

            return;
        }

        private void OnDisable()
        {
            RestoreMissionTraffic();
            cinematics?.StopSiren();
        }
    }
}
