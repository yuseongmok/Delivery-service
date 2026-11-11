using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    // 프롤로그는 본편의 잔액·수배·일차를 수정하지 않는 별도 씬이다.
    public sealed class BankPrologue : MonoBehaviour
    {
        public enum Phase
        {
            Opening,
            Robbery,
            Escape,
            Getaway,
            Complete,
            Failed,
            Driving,
            CityReveal
        }

        public ProloguePlayer player;
        public PrologueAccomplice accomplice;
        public Camera filmCamera;
        public Transform vaultDoor;
        public Transform getawayCar;
        public Transform exitPoint;
        public Transform[] shots;
        public PrologueOfficer[] officers;
        public GameObject[] cash;
        public Light[] alarmLights;
        public Text objective;
        public Text subtitle;
        public Text prompt;
        public Text ammo;
        public Text health;
        public Text pauseText;
        public CanvasGroup bars;
        public CanvasGroup fade;
        public CanvasGroup pausePanel;
        public Image damageFlash;
        public Image progress;
        public GameObject crosshair;
        public string gameScene = "Assets/Scenes/JJinCity.unity";
        public string lobbyScene = "Assets/Scenes/Lobby.unity";
        public bool autoEnterGame = true;
        public PrologueDrive drive;
        public PrologueShopArrival shopArrival;
        public PrologueOfficer[] roadPolice;
        public GameObject cityVista;
        public Transform cityCarStart;
        public Transform cityCarEnd;
        public Transform cityWideCamera;
        public Transform cityLookTarget;
        public CanvasGroup logo;
        [Header("대사 파일 — Project에서 선택해 문장과 시간을 수정")]
        public PrologueStory story;
        [Header("금고 시설의 탈출 순서")]
        public Transform[] escapeRoute = new Transform[0];
        public Transform[] policeEntryTargets = new Transform[0];
        [Header("조직원 증원")]
        [Min(2)]
        public int initialPolice = 8;
        [Min(1)]
        public int waveSize = 4;
        [Min(1)]
        public int maxActivePolice = 16;
        [Min(1)]
        public float waveInterval = 7;
        public int RouteStep { get; private set; }

        public int SpawnedPolice => combat.SpawnedPolice;
        public Phase Current { get; private set; }

        public bool Paused { get; private set; }

        // 자동 검증은 입력 대신 명시적인 동작 호출을 사용한다. 실제 플레이에서는 항상 켜진다.
        public bool AcceptInput { get; set; } = true;
        public int Collected { get; private set; }

        public bool CanPlay => !Paused && (Current == Phase.Robbery || Current == Phase.Escape);
        public bool IsCinematic => Current == Phase.Opening || Current == Phase.Getaway || Current == Phase.CityReveal;
        public bool CinematicSkipRequested => skip;
        public bool Alarm => alarm.Alarm;
        [Header("은행 경보 반복")]
        [Min(1)]
        public int alarmRepeats = 3;
        [Min(.1f)]
        public float alarmCycleSeconds = 3.1f;
        [Min(.1f)]
        public float alarmRestSeconds = 5;
        private float collecting;
        private float waveTime;
        private float hitOpacity;
        private bool skip;
        private bool leaving;
        private bool temporaryStory;
        public enum CombatBeat
        {
            FirstContact,
            Breather,
            Reinforcement,
            FinalPush,
            Withdrawal
        }

        public CombatBeat BattleBeat => combat.BattleBeat;
        public float SquadShotInterval => combat.SquadShotInterval;
        private PrologueAlarm alarm;
        private PrologueCombatDirector combat;
        private PrologueCinematics cinematics;
        private void Awake()
        {
            alarm = new PrologueAlarm(this);
            combat = new PrologueCombatDirector(this);
            cinematics = new PrologueCinematics(this);
        }

        private void Start()
        {
            Time.timeScale = 1;
            if (progress)
            {
                progress.gameObject.SetActive(false);
            }

            gameObject.AddComponent<PrologueMissionMusic>();
            gameObject.AddComponent<StorySoundscape>();
            if (!story)
            {
                story = ScriptableObject.CreateInstance<PrologueStory>();
                temporaryStory = true;
            }

            player.director = this;
            if (logo)
            {
                logo.alpha = 0;
            }

            if (cityVista)
            {
                cityVista.SetActive(false);
            }

            Current = Phase.Opening;
            pausePanel.alpha = 0;
            pausePanel.blocksRaycasts = false;
            foreach (var officer in officers)
            {
                officer.director = this;
            }

            alarm.MakeSiren();
            StartCoroutine(cinematics.Opening());
        }

        public void BeginRobbery()
        {
            Current = Phase.Robbery;
            skip = false;
            vaultDoor.localRotation = Quaternion.Euler(0, -105, 0);
            alarm.StartBankAlarm();
            cinematics.SetCinematic(false);
            fade.alpha = 0;
            subtitle.text = story.collect;
            combat.Begin(ref waveTime);
        }

        private void Update()
        {
            if (leaving)
            {
                return;
            }

            if (AcceptInput && PrologueKeyboard.Pressed(Key.Escape))
            {
                SetPaused(!Paused);
            }

            if (Paused)
            {
                if (AcceptInput && PrologueKeyboard.Pressed(Key.Enter))
                {
                    SetPaused(false);
                }

                if (AcceptInput && PrologueKeyboard.Pressed(Key.R))
                {
                    Retry();
                }

                if (AcceptInput && PrologueKeyboard.Pressed(Key.L))
                {
                    Load(lobbyScene);
                }

                return;
            }

            alarm.UpdateBankAlarm();
            if (AcceptInput && IsCinematic && PrologueKeyboard.Pressed(Key.Space))
            {
                skip = true;
            }

            if (AcceptInput && Current == Phase.Failed && PrologueKeyboard.Pressed(Key.R))
            {
                Retry();
            }

            for (int i = 0; i < alarmLights.Length; i++)
            {
                alarmLights[i].intensity = Alarm ? (Mathf.Sin(Time.time * 7 + i * Mathf.PI) > 0 ? 5 : .15f) : 0;
            }

            hitOpacity = Mathf.MoveTowards(hitOpacity, 0, Time.deltaTime * .7f);
            damageFlash.color = new Color(.8f, .02f, .01f, hitOpacity);
            if (CanPlay)
            {
                health.gameObject.SetActive(false);
            }

            if (!CanPlay || player.KnockedDown)
            {
                return;
            }

            if (Current == Phase.Escape && RouteStep < escapeRoute.Length && Vector3.Distance(player.transform.position, escapeRoute[RouteStep].position) < 5)
            {
                RouteStep++;
                if (RouteStep < story.routeLines.Length)
                {
                    subtitle.text = story.routeLines[RouteStep];
                }
            }

            string route = RouteStep < story.routeNames.Length ? story.routeNames[RouteStep] : "정문 앞 도주 차량";
            objective.text = Current == Phase.Robbery ? $"공범이 돈을 챙기는 동안 조직원들을 막으세요   {Collected} / {cash.Length}" : $"공범과 함께 탈출  /  {route}";
            ammo.text = player.Reloading ? "재장전 중" : $"{player.Ammo} / {player.magazineSize}";
            health.gameObject.SetActive(false);
            combat.Tick(ref waveTime);
            prompt.text = "";
            progress.fillAmount = 0;
            if (Current == Phase.Robbery && accomplice)
            {
                progress.fillAmount = (Collected + accomplice.CollectionProgress) / cash.Length;
            }
            else
            {
                collecting = 0;
                if (Current == Phase.Escape && Vector3.Distance(player.transform.position, exitPoint.position) < 3)
                {
                    prompt.text = accomplice && !accomplice.ReadyToBoard ? "공범이 차량으로 오고 있습니다" : "F  공범과 도주 차량 탑승";
                    if (AcceptInput && PrologueKeyboard.Pressed(Key.F))
                    {
                        BoardCar();
                    }
                }
            }
        }

        private int NearestCash()
        {
            for (int i = 0; i < cash.Length; i++)
            {
                if (cash[i].activeSelf && Vector3.Distance(player.transform.position, cash[i].transform.position) < 2.4f)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Collect(int index)
        {
            if (accomplice)
            {
                return false;
            }

            if (!CanPlay || player.KnockedDown || Current != Phase.Robbery || index < 0 || index >= cash.Length || !cash[index].activeSelf || Vector3.Distance(player.transform.position, cash[index].transform.position) >= 2.4f)
            {
                return false;
            }

            cash[index].SetActive(false);
            Collected++;
            collecting = 0;
            if (Collected == cash.Length)
            {
                alarm.StopBankAlarm();
                Current = Phase.Escape;
                subtitle.text = story.escape;
            }

            return true;
        }

        public bool SecureCash(int index, PrologueAccomplice actor)
        {
            if (!CanPlay || Current != Phase.Robbery || actor != accomplice || !actor || index != Collected || index >= cash.Length || !cash[index].activeSelf || actor.CollectionProgress < 1 || Vector3.Distance(actor.transform.position, cash[index].transform.position) > 2.6f)
            {
                return false;
            }

            cash[index].SetActive(false);
            Collected++;
            if (Collected == cash.Length)
            {
                alarm.StopBankAlarm();
                StartCoroutine(FinishPacking());
            }

            return true;
        }

        private IEnumerator FinishPacking()
        {
            if (accomplice && accomplice.performance)
            {
                yield return accomplice.performance.PackAndCarry();
            }

            if (Current == Phase.Robbery)
            {
                Current = Phase.Escape;
                subtitle.text = story.escape;
            }
        }

        public bool BoardCar()
        {
            // 중간 경로는 안내용이다. 차량까지 도착했다면 놓친 지점 때문에 탑승을 막지 않는다.
            if (!CanPlay || player.KnockedDown || Current != Phase.Escape || Vector3.Distance(player.transform.position, exitPoint.position) >= 3)
            {
                return false;
            }

            if (accomplice && !accomplice.ReadyToBoard)
            {
                return false;
            }

            RouteStep = escapeRoute.Length;
            if (accomplice && !accomplice.performance)
            {
                accomplice.Board();
            }

            Current = Phase.Getaway;
            StartCoroutine(Getaway());
            return true;
        }

        private IEnumerator Getaway()
        {
            skip = false;
            yield return cinematics.Getaway();
            BeginDriving();
        }

        private void BeginDriving()
        {
            skip = false;
            Current = Phase.Driving;
            bars.alpha = 0;
            player.GetComponent<CharacterController>().enabled = false;
            // 차량 진행 구간에는 사람을 배치하지 않는다.
            foreach (var officer in officers)
            {
                officer.Stop();
                officer.gameObject.SetActive(false);
            }

            foreach (var officer in roadPolice)
            {
                officer.gameObject.SetActive(true);
                officer.Stop();
            }

            subtitle.text = story.driving;
            filmCamera.transform.SetPositionAndRotation(getawayCar.TransformPoint(drive.driverEye), getawayCar.rotation);
            // 운전 구간에서는 뒤따르는 경찰차의 공간 사이렌을 듣게 한다.
            alarm.Silence();
            drive.Begin();
        }

        private IEnumerator Arrival()
        {
            yield return cinematics.Arrival();
            Current = Phase.Complete;
            if (autoEnterGame)
            {
                Load(gameScene);
            }
        }

        internal void StartBankAlarm()
        {
            alarm.StartBankAlarm();
        }

        internal void StopBankAlarm()
        {
            alarm.StopBankAlarm();
        }

        public bool FinishDriving()
        {
            if (Paused || Current != Phase.Driving || Vector3.Distance(getawayCar.position, drive.finish.position) >= 9)
            {
                return false;
            }

            drive.Stop();
            Current = Phase.CityReveal;
            skip = false;
            StartCoroutine(Arrival());
            return true;
        }

        public void HitFeedback()
        {
            hitOpacity = .26f;
        }

        public void Fail()
        {
            if (!CanPlay)
            {
                return;
            }

            Current = Phase.Failed;
            foreach (var officer in officers)
            {
                officer.Stop();
            }

            subtitle.text = story.failure;
            objective.text = "R  회수 작전 다시 시작";
            prompt.text = "Esc  메뉴";
            crosshair.SetActive(false);
        }

        public void SetPaused(bool value)
        {
            Paused = value;
            Time.timeScale = value ? 0 : 1;
            pausePanel.alpha = value ? 1 : 0;
            pauseText.text = "일시정지\n\nEnter  계속하기\nR  프롤로그 다시 시작\nL  로비로 돌아가기";
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
            alarm.SetPaused(value);
        }

        public bool DeveloperComplete()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                return false;
            }

            StopAllCoroutines();
            Current = Phase.Complete;
            Load(gameScene);
            return true;
        }

        public void Retry()
        {
            Load(SceneManager.GetActiveScene().path);
        }

        private void Load(string path)
        {
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                objective.text = "씬 연결을 확인해 주세요.";
                return;
            }

            if (Current == Phase.Complete && path == gameScene)
            {
                StoryTutorial.Requested = true;
            }

            leaving = true;
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(path);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
            alarm?.Dispose();
            if (temporaryStory && story)
            {
                Destroy(story);
            }
        }
    }
}
