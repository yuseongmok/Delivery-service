using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    public sealed class PrisonNightMission : MonoBehaviour
    {
        public enum Stage { Waiting, Evening, Sneak, Retry, Complete }
        public PrisonCctvMission previous;
        public PrisonNightText dialogue;
        public PrisonEvening evening;
        public Transform checkpoint, companionStart, vehiclePoint;
        public Transform[] route;
        public PrisonSearchlight[] searchlights;
        public GameObject coverProps;
        public GameObject[] curfewActors;
        public Camera film;
        public Transform establishingShot;
        public Stage State { get; private set; }
        public bool Active => State != Stage.Waiting;
        public bool Busy => State == Stage.Evening || State == Stage.Retry;
        public bool Complete => State == Stage.Complete;
        public int Attempts { get; private set; }
        public float Exposure { get; private set; }
        public string Subtitle { get; private set; } = "";
        public string Card { get; private set; } = "";
        public Transform Target => State == Stage.Sneak ? (waypoint < route.Length ? route[waypoint] : vehiclePoint) : null;
        public string Objective => Complete ? "야간 잠입 완료 · 호송차량 도착" : (stageIndex==0?"수감동 밖 · 불빛을 살피며 엄폐물로 이동":stageIndex==1?"운동장 · 교차하는 탐조등 통과하기":"창고 골목 · 출발 전 호송차량에 접근하기");
        public PlayerCombat Player => previous.office.previous.previous.prison.player;
        public PrisonCompanion Friend => previous.office.previous.previous.prison.introduction.mingi;
        public int[] stageStarts={0,2,4};
        public Transform[] safeCheckpoints;
        public AudioClip tensionMusic,engineSound;
        public float detectionSeconds=.8f;
        float graceUntil;
        int stageIndex,savedWaypoint;Vector3 savedPosition,savedFriend;
        AudioSource music,engine;bool engineStarted,audioPaused;
        public int StageIndex=>stageIndex;
        int waypoint;
        float fade, subtitleUntil, nextPath;
        bool locked, moveControl, lookControl, combatEnabled, interactionEnabled;
        bool filmWasEnabled, viewWasEnabled;
        Vector3 filmPosition; Quaternion filmRotation;
        void Awake() { previous.legacyEscape.vehicle.gameObject.SetActive(false); foreach (var light in searchlights) light.SetRunning(false); coverProps.SetActive(false); }
        void Update()
        {
            if(Time.timeScale<=0){if(!audioPaused){if(music)music.Pause();if(engine)engine.Pause();audioPaused=true;}return;}
            if(audioPaused){if(music)music.UnPause();if(engine)engine.UnPause();audioPaused=false;}
            if(music){var manager=DeliveryService.Yun.Audio.AudioManager.Instance;float volume=manager?manager.MasterVolume*manager.GetCategoryVolume(DeliveryService.Yun.Audio.SoundCategory.Bgm):PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1);if(manager&&(manager.MasterMuted||manager.GetCategoryMuted(DeliveryService.Yun.Audio.SoundCategory.Bgm)))volume=0;music.volume=(stageIndex==2?.16f:.11f)*volume;}
            if (State == Stage.Waiting)
            {
                if (previous.State == PrisonCctvMission.Stage.Complete) StartCoroutine(Begin());
                return;
            }
            if (Busy || Complete) return;
            if (Time.time > subtitleUntil) Subtitle = "";
            if (!Player.CanAct) return;
            var feet = Player.GetComponent<CharacterController>().bounds.min;
            feet.x = Player.transform.position.x; feet.z = Player.transform.position.z;
            bool lit = Time.time>=graceUntil&&(IsLit(feet + Vector3.up * .15f, Player.transform) || IsLit(feet + Vector3.up * .9f, Player.transform));
            // 가장자리 접촉 한 프레임만으로 실패하지 않도록 아주 짧은 확인 시간만 둔다.
            Exposure = lit ? Exposure + Time.deltaTime : Mathf.MoveTowards(Exposure,0,Time.deltaTime*2);
            foreach(var light in searchlights)light.Hold=lit;
            if (Exposure >= detectionSeconds) { StartCoroutine(Retry()); return; }
            if (waypoint < route.Length && Vector3.Distance(Player.transform.position, route[waypoint].position + Vector3.up) < 3) {waypoint++;UpdateStage();}
            FollowInShadow();
            if (Vector3.Distance(Player.transform.position, vehiclePoint.position + Vector3.up) < 2.4f)
            {
                if (Vector3.Distance(Friend.transform.position, vehiclePoint.position) > 4) { Say(dialogue.waitingForMingi); return; }
                Finish();
            }
        }
        public bool IsLit(Vector3 at, Transform actor = null)
        {
            foreach (var light in searchlights) if (light.Illuminates(at, actor)) return true;
            return false;
        }
        void FollowInShadow()
        {
            Friend.ScriptedTravel = true; Friend.Following = false;
            var agent = Friend.agent;
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.speed = Vector3.Distance(Friend.transform.position, Player.transform.position) > 3.5f ? 5.2f : 3.5f;
            if (Time.time >= nextPath)
            {
                nextPath = Time.time + .25f;
                if (Vector3.Distance(Friend.transform.position, Player.transform.position) > 1.8f) agent.SetDestination(Player.transform.position);
            }
            Vector3 next = Vector3.MoveTowards(Friend.transform.position, agent.steeringTarget, 1.4f);
            bool crossing=IsLit(next+Vector3.up*.7f,Friend.transform);
            // 플레이어와 너무 멀어지면 추적을 우선한다. 민기만 노출되어 실패하지 않는다.
            agent.isStopped = (crossing&&Vector3.Distance(Friend.transform.position,Player.transform.position)<5)||Vector3.Distance(Friend.transform.position,Player.transform.position)<1.6f;
        }
        IEnumerator Begin()
        {
            State = Stage.Evening; Lock();
            // 저녁 전환 전에는 플레이어가 서 있는 자리에서 민기를 바라본다.
            film.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);
            Player.View.enabled=false;film.enabled=true;
            var from=film.transform.rotation;
            var animator=Friend.animator;
            var head=animator&&animator.isHuman?animator.GetBoneTransform(HumanBodyBones.Head):null;
            var target=head?head.position:Friend.transform.position+Vector3.up*1.5f;
            var facing=Player.transform.position-Friend.transform.position;facing.y=0;if(facing.sqrMagnitude>.01f)Friend.transform.rotation=Quaternion.LookRotation(facing);
            var to=Quaternion.LookRotation(target-film.transform.position);
            for(float t=0;t<.7f;t+=Time.deltaTime){film.transform.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,t/.7f));yield return null;}
            Say(dialogue.waiting);
            for(float t=0;t<dialogue.lineSeconds;t+=Time.deltaTime){ if(head)film.transform.rotation=Quaternion.Slerp(film.transform.rotation,Quaternion.LookRotation(head.position-film.transform.position),Time.deltaTime*8); yield return null; }
            yield return Fade(1); SetEvening(); PlaceAtStart();
            Card = dialogue.timeCard; yield return new WaitForSeconds(dialogue.lineSeconds);
            Card = "";
            film.transform.position=Player.View.transform.position;
            film.transform.LookAt(searchlights[0].transform.position);
            foreach(var light in searchlights)light.SetRunning(false);
            Player.View.enabled = false; film.enabled = true;
            yield return Fade(0);
            foreach(var light in searchlights){light.SetRunning(true);yield return new WaitForSeconds(.6f);}
            StartMusic();
            foreach (var line in dialogue.introduction) { Say(line); yield return new WaitForSeconds(dialogue.lineSeconds); }
            yield return Fade(1); RestoreView(); yield return Fade(0); Unlock();
            State = Stage.Sneak; graceUntil=Time.time+3;Say(dialogue.retry);
        }
        void SetEvening()
        {
            previous.legacyEscape.vehicle.gameObject.SetActive(true);
            evening.SetEvening();
            if (RenderSettings.sun) RenderSettings.sun.intensity = .1f;
            coverProps.SetActive(true);
            foreach(var actor in curfewActors)if(actor)actor.SetActive(false);
            foreach (var light in searchlights) { light.ResetSweep(); light.SetRunning(true); }
        }
        void UpdateStage(){
            int nextStage=waypoint>=stageStarts[2]?2:waypoint>=stageStarts[1]?1:0;
            if(nextStage<=stageIndex)return;stageIndex=nextStage;savedWaypoint=waypoint;
            var safe=safeCheckpoints!=null&&safeCheckpoints.Length>stageIndex?safeCheckpoints[stageIndex]:null;
            savedPosition=safe?safe.position:Player.transform.position-Vector3.up*1.1f;savedFriend=savedPosition+Vector3.left*1.5f;
            Say(stageIndex==1?dialogue.crossing:dialogue.departure);
            if(stageIndex==2&&!engineStarted){engineStarted=true;engine=previous.legacyEscape.vehicle.gameObject.AddComponent<AudioSource>();engine.clip=engineSound;engine.loop=true;engine.spatialBlend=1;engine.minDistance=7;engine.maxDistance=50;engine.volume=.22f*SuppliedAudio.Volume;engine.Play();}
        }
        void StartMusic(){if(!music){music=gameObject.AddComponent<AudioSource>();music.clip=tensionMusic;music.loop=true;music.volume=.12f*SuppliedAudio.Volume;}music.Play();}
        void PlaceAtStart()
        {
            var cc = Player.GetComponent<CharacterController>(); cc.enabled = false;
            Player.transform.SetPositionAndRotation((stageIndex==0?checkpoint.position:savedPosition) + Vector3.up * 1.1f, checkpoint.rotation);
            cc.enabled = true; Player.View.transform.localRotation = Quaternion.identity;
            Player.GetComponentInChildren<CameraMove>().SyncViewFromTransform();
            var agent = Friend.agent;
            if (agent.enabled && agent.isOnNavMesh) { agent.Warp(stageIndex==0?companionStart.position:savedFriend); agent.ResetPath(); agent.isStopped = false; }
            Friend.ScriptedTravel = true; Friend.Following = false;
            waypoint = savedWaypoint; Exposure = 0;graceUntil=Time.time+4; Physics.SyncTransforms();
        }
        IEnumerator Retry()
        {
            State = Stage.Retry; Attempts++; Lock(); Say(dialogue.caught);
            yield return new WaitForSeconds(1.1f); yield return Fade(1);
            PlaceAtStart(); foreach (var light in searchlights) light.ResetSweep();
            yield return Fade(0); Unlock(); State = Stage.Sneak; graceUntil=Time.time+3;Say(dialogue.retry);
        }
        void Finish()
        {
            State = Stage.Complete; Exposure = 0;if(music)music.Stop();if(engine)engine.Stop();
            foreach (var light in searchlights) light.SetRunning(false);
            Friend.ScriptedTravel = false; Friend.Following = true;
            if (Friend.agent.enabled && Friend.agent.isOnNavMesh) Friend.agent.isStopped = false;
            Say(dialogue.complete, 12);
        }
        void Say(string line, float duration = 8) { Subtitle = line; subtitleUntil = Time.time + duration; }
        IEnumerator Fade(float to)
        {
            float from = fade;
            for (float t = 0; t < .8f; t += Time.deltaTime) { fade = Mathf.Lerp(from,to,t/.8f); yield return null; }
            fade = to;
        }
        void Lock()
        {
            if (locked) return; locked = true;
            var move = Player.GetComponent<PlayerMove>(); var look = Player.GetComponentInChildren<CameraMove>();
            moveControl = move.isControllable; lookControl = look.isControllable; combatEnabled = Player.enabled;
            interactionEnabled = Player.GetComponent<PlayerInteraction>().enabled;
            viewWasEnabled = Player.View.enabled; filmWasEnabled = film.enabled; filmPosition = film.transform.position; filmRotation = film.transform.rotation;
            move.isControllable = look.isControllable = false; Player.enabled = false; Player.GetComponent<PlayerInteraction>().enabled = false;
            Friend.Following = false; Friend.ScriptedTravel = true;
            if (Friend.agent.enabled && Friend.agent.isOnNavMesh) Friend.agent.ResetPath();
        }
        void RestoreView()
        {
            film.enabled = filmWasEnabled; film.transform.SetPositionAndRotation(filmPosition,filmRotation);
            Player.View.enabled = viewWasEnabled;
            Player.GetComponentInChildren<CameraMove>().SyncViewFromTransform();
        }
        void Unlock()
        {
            if (!locked) return; RestoreView(); locked = false;
            Player.GetComponent<PlayerMove>().isControllable = moveControl;
            Player.GetComponentInChildren<CameraMove>().isControllable = lookControl;
            Player.enabled = combatEnabled; Player.GetComponent<PlayerInteraction>().enabled = interactionEnabled;
        }
        public void DeveloperComplete()
        {
            StopAllCoroutines(); Unlock(); fade = 0; Card = ""; SetEvening(); Finish();
        }
        void OnDisable() { if(music)music.Stop();if(engine)engine.Stop();Unlock(); foreach (var light in searchlights) if(light)light.SetRunning(false); }
        void OnGUI()
        {
            if (!Active || Time.timeScale <= 0) return;
            var old = GUI.color;
            if (Exposure > 0) { GUI.Label(new Rect(0,Screen.height*.22f,Screen.width,50),"탐조등에 노출됨 · 엄폐물 뒤로 숨으세요",new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=24,alignment=TextAnchor.MiddleCenter});GUI.color = new Color(1,.65f,.2f,.12f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
            if (fade > 0) { GUI.color = new Color(0,0,0,fade); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
            GUI.color = Color.white;
            if (Card.Length > 0) GUI.Label(new Rect(0,Screen.height*.42f,Screen.width,70),Card,new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=32,alignment=TextAnchor.MiddleCenter});
            GUI.color = old;
        }
    }
}

