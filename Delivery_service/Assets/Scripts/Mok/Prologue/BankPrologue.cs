using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok {
// 프롤로그는 본편의 잔액·수배·일차를 수정하지 않는 별도 씬이다.
public sealed class BankPrologue : MonoBehaviour {
    public enum Phase { Opening, Robbery, Escape, Getaway, Complete, Failed, Driving, CityReveal }
    public ProloguePlayer player;
    public PrologueAccomplice accomplice;
    public Camera filmCamera;
    public Transform vaultDoor, getawayCar, exitPoint;
    public Transform[] shots;
    public PrologueOfficer[] officers;
    public GameObject[] cash;
    public Light[] alarmLights;
    public Text objective, subtitle, prompt, ammo, health, pauseText;
    public CanvasGroup bars, fade, pausePanel;
    public Image damageFlash, progress;
    public GameObject crosshair;
    public string gameScene = "Assets/Scenes/JJinCity.unity";
    public string lobbyScene = "Assets/Scenes/Lobby.unity";
    public bool autoEnterGame = true;
    public PrologueDrive drive;
    public PrologueShopArrival shopArrival;
    public PrologueOfficer[] roadPolice;
    public GameObject cityVista;
    public Transform cityCarStart, cityCarEnd, cityWideCamera, cityLookTarget;
    public CanvasGroup logo;
    [Header("대사 파일 — Project에서 선택해 문장과 시간을 수정")]
    public PrologueStory story;
    [Header("확장 은행의 탈출 순서")]
    public Transform[] escapeRoute = new Transform[0];
    public Transform[] policeEntryTargets = new Transform[0];
    [Header("경찰 증원")]
    [Min(2)] public int initialPolice = 8;
    [Min(1)] public int waveSize = 4, maxActivePolice = 16;
    [Min(1)] public float waveInterval = 7;
    public int RouteStep { get; private set; }
    public int SpawnedPolice => reinforcements;
    public Phase Current { get; private set; }
    public bool Paused { get; private set; }
    // 자동 검증은 입력 대신 명시적인 동작 호출을 사용한다. 실제 플레이에서는 항상 켜진다.
    public bool AcceptInput { get; set; } = true;
    public int Collected { get; private set; }
    public bool CanPlay => !Paused && (Current == Phase.Robbery || Current == Phase.Escape);
    public bool IsCinematic => Current == Phase.Opening || Current == Phase.Getaway || Current == Phase.CityReveal;
    public bool CinematicSkipRequested => skip;
    public bool Alarm { get; private set; }
    [Header("은행 경보 반복") ]
    [Min(1)] public int alarmRepeats=3;
    [Min(.1f)] public float alarmCycleSeconds=3.1f,alarmRestSeconds=5;
    float alarmPhaseTime;bool alarmResting;
    AudioSource siren;
    AudioClip sirenClip;
    float collecting, waveTime, hitOpacity;
    int reinforcements;
    bool skip, leaving;
    bool temporaryStory;
    public enum CombatBeat { FirstContact, Breather, Reinforcement, FinalPush, Withdrawal }
    public CombatBeat BattleBeat { get; private set; }
    public float SquadShotInterval => BattleBeat==CombatBeat.Breather?2.2f:BattleBeat==CombatBeat.FinalPush?.65f:1.05f;
    float beatStarted;
    bool finalPushStarted;
    void ChangeBeat(CombatBeat beat){BattleBeat=beat;beatStarted=Time.time;}

    void Start() {
        Time.timeScale = 1;
        if(progress)progress.gameObject.SetActive(false);
        gameObject.AddComponent<PrologueMissionMusic>();
        gameObject.AddComponent<StorySoundscape>();
        if(!story){story=ScriptableObject.CreateInstance<PrologueStory>();temporaryStory=true;}
        player.director = this;
        if(logo)logo.alpha=0;
        if(cityVista)cityVista.SetActive(false);
        Current = Phase.Opening;
        pausePanel.alpha = 0;
        pausePanel.blocksRaycasts = false;
        foreach (var officer in officers) officer.director = this;
        MakeSiren();
        StartCoroutine(Opening());
    }
    void StartBankAlarm(){if(Alarm||Collected>=cash.Length)return;Alarm=true;alarmResting=false;alarmPhaseTime=0;siren.time=0;siren.Play();}
    void StopBankAlarm(){Alarm=false;alarmResting=false;alarmPhaseTime=0;if(siren)siren.Stop();foreach(var light in alarmLights)if(light)light.intensity=0;}
    void UpdateBankAlarm(){if(siren)siren.volume=.25f*SuppliedAudio.Volume;if(!Alarm)return;if(Collected>=cash.Length){StopBankAlarm();return;}alarmPhaseTime+=Time.deltaTime;float duration=alarmResting?alarmRestSeconds:alarmCycleSeconds*alarmRepeats;if(alarmPhaseTime<duration)return;alarmPhaseTime=0;alarmResting=!alarmResting;if(alarmResting)siren.Stop();else{siren.time=0;siren.Play();}}
    void MakeSiren() {
        int rate = 22050;
        var samples = new float[rate * 2];
        float angle = 0;
        for (int i = 0; i < samples.Length; i++) {
            float t = i / (float)rate;
            angle += 2 * Mathf.PI * (550 + 180 * Mathf.Sin(t * Mathf.PI * 2)) / rate;
            samples[i] = Mathf.Sin(angle) * .12f;
        }
        sirenClip = AudioClip.Create("은행 경보", samples.Length, 1, rate, false);
        sirenClip.SetData(samples, 0);
        siren = gameObject.AddComponent<AudioSource>();
        siren.clip = SuppliedAudio.Current&&SuppliedAudio.Current.bankAlarm?SuppliedAudio.Current.bankAlarm:sirenClip; siren.loop = true; siren.playOnAwake = false;
        siren.volume = .25f*SuppliedAudio.Volume;
    }
    void SetCinematic(bool value) {
        player.view.enabled = !value;
        player.view.GetComponent<AudioListener>().enabled = !value;
        filmCamera.enabled = value;
        filmCamera.GetComponent<AudioListener>().enabled = value;
        player.weapon.SetActive(!value);
        bars.alpha = value ? 1 : 0;
        crosshair.SetActive(!value);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    IEnumerator Shot(int index, string line, float seconds, Vector3 drift) {
        subtitle.text = line;
        var start = shots[index];
        for (float t = 0; t < seconds && !skip; t += Time.deltaTime) {
            filmCamera.transform.SetPositionAndRotation(start.position + drift * Mathf.SmoothStep(0, 1, t / seconds), start.rotation);
            yield return null;
        }
    }
    IEnumerator Opening() {
        SetCinematic(true); fade.alpha = 1;
        objective.text = story.title;
        prompt.text = "Space  컷신 건너뛰기";
        filmCamera.transform.SetPositionAndRotation(shots[0].position, shots[0].rotation);
        for (float t = 0; t < 1.2f && !skip; t += Time.deltaTime) { fade.alpha = 1 - t / 1.2f; yield return null; }
        fade.alpha = 0;
        yield return Shot(0, story.vault.text, story.vault.seconds, Vector3.forward * .35f);
        if(accomplice&&accomplice.performance)yield return accomplice.performance.OpenVault();
        else for (float t = 0; t < 2.4f && !skip; t += Time.deltaTime) {
            vaultDoor.localRotation = Quaternion.Euler(0, -105 * Mathf.SmoothStep(0, 1, t / 2.4f), 0);
            yield return null;
        }
        vaultDoor.localRotation = Quaternion.Euler(0, -105, 0);
        StartBankAlarm();
        if(accomplice&&accomplice.performance&&!skip)yield return accomplice.performance.ReactToAlarm();
        yield return Shot(1, story.alarm.text, story.alarm.seconds, Vector3.forward * .2f);
        officers[0].StageEntrance(policeEntryTargets.Length>0?policeEntryTargets[0].position:new Vector3(-.7f, 0, -5));
        officers[1].StageEntrance(policeEntryTargets.Length>1?policeEntryTargets[1].position:new Vector3(.7f, 0, -5));
        yield return Shot(2, story.police.text, story.police.seconds, Vector3.left * .3f);
        // 입구를 보여준 뒤 플레이할 시점으로 짧게 연결해 조작 전환을 알린다.
        if(!skip){
            subtitle.text=story.collect;
            for(float t=0;t<.45f&&!skip;t+=Time.deltaTime){
                fade.alpha=t<.2f?t/.2f:1-(t-.2f)/.25f;
                if(t>=.2f)filmCamera.transform.SetPositionAndRotation(player.view.transform.position,player.view.transform.rotation);
                yield return null;
            }
        }
        BeginRobbery();
    }
    public void BeginRobbery() {
        Current = Phase.Robbery; skip = false;
        vaultDoor.localRotation = Quaternion.Euler(0, -105, 0);
        StartBankAlarm();
        SetCinematic(false); fade.alpha = 0;
        subtitle.text = story.collect;
        waveTime = Time.time + waveInterval;
        reinforcements=0;
        finalPushStarted=false;ChangeBeat(CombatBeat.FirstContact);
        while(reinforcements<Mathf.Min(Mathf.Min(initialPolice,4),officers.Length)){if(!DeployNextOfficer())break;}
    }
    bool DeployNextOfficer(){
        // 보이는 후보는 대기시키고 가려진 다른 증원 후보부터 투입한다.
        for(int i=reinforcements;i<officers.Length;i++){
            var officer=officers[i];
            if(!officer.Active&&PrologueEscapePerformance.VisibleSpawn(player.view,officer.transform.position))continue;
            var pending=officers[reinforcements];officers[reinforcements]=officer;officers[i]=pending;
            if(!officer.Active)officer.Activate();reinforcements++;return true;
        }
        return false;
    }
    void Update() {
        if (leaving) return;
        if (AcceptInput && PrologueKeyboard.Pressed(Key.Escape)) SetPaused(!Paused);
        if (Paused) {
            if (AcceptInput && PrologueKeyboard.Pressed(Key.Enter)) SetPaused(false);
            if (AcceptInput && PrologueKeyboard.Pressed(Key.R)) Retry();
            if (AcceptInput && PrologueKeyboard.Pressed(Key.L)) Load(lobbyScene);
            return;
        }
        UpdateBankAlarm();
        if (AcceptInput && IsCinematic && PrologueKeyboard.Pressed(Key.Space)) skip = true;
        if (AcceptInput && Current == Phase.Failed && PrologueKeyboard.Pressed(Key.R)) Retry();
        for (int i = 0; i < alarmLights.Length; i++)
            alarmLights[i].intensity = Alarm ? (Mathf.Sin(Time.time * 7 + i * Mathf.PI) > 0 ? 5 : .15f) : 0;
        hitOpacity = Mathf.MoveTowards(hitOpacity, 0, Time.deltaTime * .7f);
        damageFlash.color = new Color(.8f, .02f, .01f, hitOpacity);
        if(CanPlay)health.gameObject.SetActive(false);
        if (!CanPlay || player.KnockedDown) return;
        if(Current==Phase.Escape && RouteStep<escapeRoute.Length &&
            Vector3.Distance(player.transform.position,escapeRoute[RouteStep].position)<5) {
            RouteStep++;
            if(RouteStep<story.routeLines.Length)subtitle.text=story.routeLines[RouteStep];
        }
        string route=RouteStep<story.routeNames.Length?story.routeNames[RouteStep]:"정문 앞 도주 차량";
        objective.text = Current == Phase.Robbery ? $"공범이 돈을 챙기는 동안 경찰을 막으세요   {Collected} / {cash.Length}" : $"공범과 함께 탈출  /  {route}";
        ammo.text = player.Reloading ? "재장전 중…" : $"{player.Ammo} / {player.magazineSize}\nR  재장전";
        health.gameObject.SetActive(false);
        UpdateBattleRhythm();
        if (BattleBeat!=CombatBeat.Breather && BattleBeat!=CombatBeat.FirstContact && Time.time >= waveTime && reinforcements < officers.Length) {
            int live = 0; foreach (var officer in officers) if (officer.Active && !officer.Dead) live++;
            int cap=BattleBeat==CombatBeat.FinalPush?maxActivePolice:Mathf.Min(maxActivePolice,10);
            int count=Mathf.Min(BattleBeat==CombatBeat.FinalPush?waveSize+2:waveSize,cap-live);
            for(int i=0;i<count&&reinforcements<officers.Length;i++){if(!DeployNextOfficer())break;}
            waveTime=Time.time+(BattleBeat==CombatBeat.FinalPush?4:waveInterval);
        }
        prompt.text = ""; progress.fillAmount = 0;
        if (Current == Phase.Robbery && accomplice) {
            progress.fillAmount = (Collected+accomplice.CollectionProgress)/cash.Length;
        } else {
            collecting = 0;
            if (Current == Phase.Escape && Vector3.Distance(player.transform.position, exitPoint.position) < 3) {
                prompt.text = accomplice&&!accomplice.ReadyToBoard ? "공범이 차량으로 오고 있습니다" : "F  공범과 도주 차량 탑승";
                if (AcceptInput && PrologueKeyboard.Pressed(Key.F)) BoardCar();
            }
        }
    }
    void UpdateBattleRhythm(){
        if(Current==Phase.Escape){
            if(BattleBeat!=CombatBeat.Withdrawal){ChangeBeat(CombatBeat.Withdrawal);waveTime=Mathf.Max(waveTime,Time.time+4);}
            return;
        }
        float elapsed=Time.time-beatStarted;
        float collected=(Collected+(accomplice?accomplice.CollectionProgress:0))/Mathf.Max(1,cash.Length);
        // 마지막 돈을 챙기는 시점에 맞추되, 최소 한 번은 숨 돌릴 틈을 보장한다.
        if(!finalPushStarted&&collected>=.72f&&BattleBeat!=CombatBeat.FirstContact&&BattleBeat!=CombatBeat.Breather){
            finalPushStarted=true;ChangeBeat(CombatBeat.FinalPush);
            subtitle.text=story.finalPressure;
            return;
        }
        if(BattleBeat==CombatBeat.FirstContact&&elapsed>=story.firstContactSeconds){ChangeBeat(CombatBeat.Breather);subtitle.text=story.breather;}
        else if(BattleBeat==CombatBeat.Breather&&elapsed>=story.breatherSeconds){ChangeBeat(CombatBeat.Reinforcement);subtitle.text=story.reinforcement;}
        else if(BattleBeat==CombatBeat.Reinforcement&&!finalPushStarted&&elapsed>=story.supportSeconds){ChangeBeat(CombatBeat.Breather);subtitle.text=story.breather;}
    }
    int NearestCash() {
        for (int i = 0; i < cash.Length; i++)
            if (cash[i].activeSelf && Vector3.Distance(player.transform.position, cash[i].transform.position) < 2.4f) return i;
        return -1;
    }
    public bool Collect(int index) {
        if(accomplice)return false;
        if (!CanPlay || player.KnockedDown || Current != Phase.Robbery || index < 0 || index >= cash.Length ||
            !cash[index].activeSelf || Vector3.Distance(player.transform.position, cash[index].transform.position) >= 2.4f) return false;
        cash[index].SetActive(false); Collected++; collecting = 0;
        if (Collected == cash.Length) { StopBankAlarm();Current = Phase.Escape; subtitle.text = story.escape; }
        return true;
    }
    public bool SecureCash(int index,PrologueAccomplice actor){
        if(!CanPlay||Current!=Phase.Robbery||actor!=accomplice||!actor||index!=Collected||index>=cash.Length||
            !cash[index].activeSelf||actor.CollectionProgress<1||
            Vector3.Distance(actor.transform.position,cash[index].transform.position)>2.6f)return false;
        cash[index].SetActive(false);Collected++;
        if(Collected==cash.Length){StopBankAlarm();StartCoroutine(FinishPacking());}
        return true;
    }
    IEnumerator FinishPacking(){
        if(accomplice&&accomplice.performance)yield return accomplice.performance.PackAndCarry();
        if(Current==Phase.Robbery){Current=Phase.Escape;subtitle.text=story.escape;}
    }
    public bool BoardCar() {
        // 중간 경로는 안내용이다. 차량까지 도착했다면 놓친 지점 때문에 탑승을 막지 않는다.
        if (!CanPlay || player.KnockedDown || Current != Phase.Escape || Vector3.Distance(player.transform.position, exitPoint.position) >= 3) return false;
        if(accomplice&&!accomplice.ReadyToBoard)return false;
        RouteStep=escapeRoute.Length;
        if(accomplice&&!accomplice.performance)accomplice.Board();
        Current = Phase.Getaway; StartCoroutine(Getaway()); return true;
    }
    IEnumerator Getaway() {
        skip = false;
        foreach (var officer in officers) officer.Stop();
        SetCinematic(true); objective.text = ""; ammo.text = ""; health.text = "";
        prompt.text = "Space  컷신 건너뛰기";
        var escapePerformance=GetComponent<PrologueEscapePerformance>();
        if(escapePerformance)yield return escapePerformance.BoardFirstPerson();
        else if(accomplice&&accomplice.performance)yield return accomplice.performance.EnterVehicle();
        subtitle.text=story.boarding.text;
        skip=false;Current=Phase.Driving;bars.alpha=0;
        player.GetComponent<CharacterController>().enabled=false;
        foreach(var officer in roadPolice){officer.gameObject.SetActive(true);}
        subtitle.text=story.driving;
        filmCamera.transform.SetPositionAndRotation(getawayCar.TransformPoint(drive.driverEye),getawayCar.rotation);
        // 운전 구간에서는 뒤따르는 경찰차의 공간 사이렌을 듣게 한다.
        siren.Stop();drive.Begin();
    }
    public bool FinishDriving(){
        if(Paused||Current!=Phase.Driving||Vector3.Distance(getawayCar.position,drive.finish.position)>=9)return false;
        drive.Stop();Current=Phase.CityReveal;skip=false;StartCoroutine(Arrival());return true;
    }
    IEnumerator Arrival(){
        objective.text="";ammo.text="";health.text="";prompt.text="Space  컷신 건너뛰기";subtitle.text="";
        for(float t=0;t<1;t+=Time.deltaTime){fade.alpha=t;yield return null;}
        fade.alpha=1;siren.Stop();Alarm=false;bars.alpha=1;
        foreach(var o in roadPolice)o.Stop();
        // 실제 본편 씬의 건물·도로만 저장한 전경을 사용해 경제와 일차는 아직 시작하지 않는다.
        cityVista.SetActive(true);RenderSettings.fog=false;filmCamera.farClipPlane=3000;
        var carBody=getawayCar.GetComponent<Rigidbody>();carBody.position=cityCarStart.position;carBody.rotation=cityCarStart.rotation;
        Vector3 carA=cityCarStart.position,carB=cityCarEnd.position;
        for(float t=0;t<story.cityArrivalSeconds&&!skip;t+=Time.deltaTime){
            float p=t/Mathf.Max(.1f,story.cityArrivalSeconds);
            var moving=Vector3.Lerp(carA,carB,p);carBody.position=moving;getawayCar.position=moving;
            foreach(var wheel in drive.wheels)wheel.Rotate(Vector3.right,Vector3.Distance(carA,carB)/Mathf.Max(.1f,story.cityArrivalSeconds)*Time.deltaTime*130,Space.Self);
            filmCamera.transform.position=getawayCar.position-getawayCar.forward*12+getawayCar.right*9+Vector3.up*6;
            filmCamera.transform.LookAt(getawayCar.position+getawayCar.forward*4+Vector3.up);
            fade.alpha=1-Mathf.Clamp01(t);yield return null;
        }
        getawayCar.position=carB;carBody.position=carB;fade.alpha=0;
        float citySpeed=Vector3.Distance(carA,carB)/Mathf.Max(.1f,story.cityArrivalSeconds);
        Vector3 cameraStart=filmCamera.transform.position;Quaternion rotationStart=filmCamera.transform.rotation;
        subtitle.text=shopArrival?"":story.getaway.text;
        for(float t=0;t<story.cityPanSeconds&&!skip;t+=Time.deltaTime){
            float p=Mathf.SmoothStep(0,1,t/Mathf.Max(.1f,story.cityPanSeconds));
            float rollingSpeed=Mathf.Lerp(citySpeed,1.2f,p);var moving=getawayCar.position+getawayCar.forward*rollingSpeed*Time.deltaTime;carBody.position=moving;getawayCar.position=moving;
            foreach(var wheel in drive.wheels)wheel.Rotate(Vector3.right,rollingSpeed*Time.deltaTime*130,Space.Self);
            filmCamera.transform.position=Vector3.Lerp(cameraStart,cityWideCamera.position,p);
            filmCamera.transform.rotation=Quaternion.Slerp(rotationStart,cityWideCamera.rotation,p);yield return null;
        }
        filmCamera.transform.SetPositionAndRotation(cityWideCamera.position,cityWideCamera.rotation);
        if(shopArrival)yield return shopArrival.Play();
        subtitle.text="";prompt.text="";logo.alpha=0;
        // 로고는 전경이 완전히 드러난 뒤 등장한다.
        for(float t=0;t<story.logoSeconds;t+=Time.deltaTime){logo.alpha=Mathf.Clamp01(t*2);yield return null;}
        for(float t=0;t<1;t+=Time.deltaTime){fade.alpha=t;logo.alpha=1-t;yield return null;}
        fade.alpha=1;logo.alpha=0;objective.text=shopArrival?"다음 날 아침\n이얏호 피자, 첫 영업일":story.ending.text;
        yield return new WaitForSeconds(Mathf.Max(.1f,story.ending.seconds));
        Current=Phase.Complete;if(autoEnterGame)Load(gameScene);
    }
    public void HitFeedback() { hitOpacity = .26f; }
    public void Fail() {
        if (!CanPlay) return;
        Current = Phase.Failed;
        foreach (var officer in officers) officer.Stop();
        subtitle.text = story.failure;
        objective.text = "R  은행에서 다시 시작";
        prompt.text = "Esc  메뉴";
        crosshair.SetActive(false);
    }
    public void SetPaused(bool value) {
        Paused = value; Time.timeScale = value ? 0 : 1;
        pausePanel.alpha = value ? 1 : 0;
        pauseText.text = "일시정지\n\nEnter  계속하기\nR  프롤로그 다시 시작\nL  로비로 돌아가기";
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
        if (siren) { if (value) siren.Pause(); else if (Alarm) siren.UnPause(); }
    }
    public bool DeveloperComplete(){if(!Application.CanStreamedLevelBeLoaded(gameScene))return false;StopAllCoroutines();Current=Phase.Complete;Load(gameScene);return true;}
    public void Retry() { Load(SceneManager.GetActiveScene().path); }
    void Load(string path) {
        if (!Application.CanStreamedLevelBeLoaded(path)) { objective.text = "씬 연결을 확인해 주세요."; return; }
        if(Current==Phase.Complete&&path==gameScene)StoryTutorial.Requested=true;
        leaving = true; Time.timeScale = 1; SceneManager.LoadSceneAsync(path);
    }
    void OnDestroy() { Time.timeScale = 1; if (sirenClip) Destroy(sirenClip);if(temporaryStory&&story)Destroy(story); }
}
}
