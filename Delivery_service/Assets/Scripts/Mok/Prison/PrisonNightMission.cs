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
        public string Objective => Complete ? "야간 잠입 완료 · 호송차량 도착" : "야간 잠입 · 감시탑 불빛을 피해 호송차량으로 이동하기";
        public PlayerCombat Player => previous.office.previous.previous.prison.player;
        public PrisonCompanion Friend => previous.office.previous.previous.prison.introduction.mingi;
        int waypoint;
        float fade, subtitleUntil, nextPath;
        bool locked, moveControl, lookControl, combatEnabled, interactionEnabled;
        bool filmWasEnabled, viewWasEnabled;
        Vector3 filmPosition; Quaternion filmRotation;
        void Awake() { previous.legacyEscape.vehicle.gameObject.SetActive(false); foreach (var light in searchlights) light.SetRunning(false); coverProps.SetActive(false); }
        void Update()
        {
            if (Time.timeScale <= 0) return;
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
            bool lit = IsLit(feet + Vector3.up * .15f, Player.transform) || IsLit(feet + Vector3.up * .9f, Player.transform);
            // 가장자리 접촉 한 프레임만으로 실패하지 않도록 아주 짧은 확인 시간만 둔다.
            Exposure = lit ? Exposure + Time.deltaTime : 0;
            if (Exposure >= .15f) { StartCoroutine(Retry()); return; }
            if (waypoint < route.Length && Vector3.Distance(Player.transform.position, route[waypoint].position + Vector3.up) < 3) waypoint++;
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
            agent.isStopped = IsLit(next + Vector3.up * .7f, Friend.transform) || Vector3.Distance(Friend.transform.position, Player.transform.position) < 1.6f;
        }
        IEnumerator Begin()
        {
            State = Stage.Evening; Lock(); Say(dialogue.waiting);
            yield return new WaitForSeconds(dialogue.lineSeconds);
            yield return Fade(1); SetEvening(); PlaceAtStart();
            Card = dialogue.timeCard; yield return new WaitForSeconds(dialogue.lineSeconds);
            Card = "";
            film.transform.SetPositionAndRotation(establishingShot.position, establishingShot.rotation);
            Player.View.enabled = false; film.enabled = true;
            yield return Fade(0);
            foreach (var line in dialogue.introduction) { Say(line); yield return new WaitForSeconds(dialogue.lineSeconds); }
            yield return Fade(1); RestoreView(); yield return Fade(0); Unlock();
            State = Stage.Sneak; Say(dialogue.retry);
        }
        void SetEvening()
        {
            previous.legacyEscape.vehicle.gameObject.SetActive(true);
            evening.SetEvening();
            if (RenderSettings.sun) RenderSettings.sun.intensity = .22f;
            coverProps.SetActive(true);
            foreach(var actor in curfewActors)if(actor)actor.SetActive(false);
            foreach (var light in searchlights) { light.ResetSweep(); light.SetRunning(true); }
        }
        void PlaceAtStart()
        {
            var cc = Player.GetComponent<CharacterController>(); cc.enabled = false;
            Player.transform.SetPositionAndRotation(checkpoint.position + Vector3.up * 1.1f, checkpoint.rotation);
            cc.enabled = true; Player.View.transform.localRotation = Quaternion.identity;
            Player.GetComponentInChildren<CameraMove>().SyncViewFromTransform();
            var agent = Friend.agent;
            if (agent.enabled && agent.isOnNavMesh) { agent.Warp(companionStart.position); agent.ResetPath(); agent.isStopped = false; }
            Friend.ScriptedTravel = true; Friend.Following = false;
            waypoint = 0; Exposure = 0; Physics.SyncTransforms();
        }
        IEnumerator Retry()
        {
            State = Stage.Retry; Attempts++; Lock(); Say(dialogue.caught);
            yield return new WaitForSeconds(1.1f); yield return Fade(1);
            PlaceAtStart(); foreach (var light in searchlights) light.ResetSweep();
            yield return Fade(0); Unlock(); State = Stage.Sneak; Say(dialogue.retry);
        }
        void Finish()
        {
            State = Stage.Complete; Exposure = 0;
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
        void OnDisable() { Unlock(); foreach (var light in searchlights) if(light)light.SetRunning(false); }
        void OnGUI()
        {
            if (!Active || Time.timeScale <= 0) return;
            var old = GUI.color;
            if (Exposure > 0) { GUI.color = new Color(1,.65f,.2f,.12f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
            if (fade > 0) { GUI.color = new Color(0,0,0,fade); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
            GUI.color = Color.white;
            if (Card.Length > 0) GUI.Label(new Rect(0,Screen.height*.42f,Screen.width,70),Card,new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=32,alignment=TextAnchor.MiddleCenter});
            GUI.color = old;
        }
    }
}
