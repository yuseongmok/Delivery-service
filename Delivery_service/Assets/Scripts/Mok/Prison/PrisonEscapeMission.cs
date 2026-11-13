using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    // 조사 완료 이후의 변장, 차량 이동, 검문 체크포인트를 관리한다.
    public sealed class PrisonEscapeMission : MonoBehaviour
    {
        public enum Stage { Waiting, Change, Vehicle, Boarding, Checkpoint, Retry, Leaving, Complete }
        public PrisonOfficeSearch previous;
        public PrisonEscapeText dialogue;
        public Transform changePoint, vehicle, boardingPoint, gateLeft, gateRight, gateStop, exitPoint;
        public Camera filmCamera;
        public Material uniform;
        public PrisonEscapePerformance performance;
        public Stage State { get; private set; }
        public bool Active => State != Stage.Waiting;
        public string Subtitle { get; private set; } = "";
        public float Fade { get; private set; }
        public int Selected { get; private set; }
        public int Attempts { get; private set; }
        public bool Disguised { get; private set; }
        public Transform Target => State == Stage.Change ? changePoint : State == Stage.Vehicle ? boardingPoint : null;
        public string Objective => State == Stage.Change ? "탈옥 · 간수복으로 갈아입기" : State == Stage.Vehicle ? "탈옥 · 민기와 호송차량에 탑승하기" : State == Stage.Checkpoint ? "정문 검문 · 오늘의 암구호 선택" : State == Stage.Complete ? "탈옥 성공" : "탈옥 · 정문 통과하기";
        public string[] Choices => new[] { dialogue.decoys.Length > 0 ? dialogue.decoys[0] : "붉은 등대", previous.dialogue.password, dialogue.decoys.Length > 1 ? dialogue.decoys[1] : "검은 파도" };
        PlayerCombat Player => previous.previous.previous.prison.player;
        PrisonCompanion Friend => previous.previous.previous.prison.introduction.mingi;
        bool moveControl, lookControl, combatEnabled, interactionEnabled, phoneEnabled;
        bool locked;
        AudioSource engine;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        void Update()
        {
            if(engine){engine.volume=.16f*SuppliedAudio.Volume;if(Time.timeScale<=0)engine.Pause();else if(!engine.isPlaying&&State!=Stage.Complete)engine.UnPause();}
            if (Time.timeScale <= 0) return;
            if (State == Stage.Waiting)
            {
                if (previous.Complete && !previous.GetComponent<PrisonOfficePickup>().Busy)
                { State = Stage.Change; Subtitle = dialogue.ready; }
                return;
            }
            var keys = Keyboard.current;
            if (State == Stage.Checkpoint && keys != null)
            {
                if (keys.downArrowKey.wasPressedThisFrame) Selected = (Selected + 1) % 3;
                if (keys.upArrowKey.wasPressedThisFrame) Selected = (Selected + 2) % 3;
                if (keys.enterKey.wasPressedThisFrame) Choose(Selected);
            }
            if (keys != null && keys.fKey.wasPressedThisFrame) Interact();
        }
        public bool Interact()
        {
            if (!Target || !Player.CanAct || Vector3.Distance(Player.transform.position, Target.position) > 3) return false;
            if (State == Stage.Change) { StartCoroutine(Change()); return true; }
            if (State == Stage.Vehicle)
            {
                if (Vector3.Distance(Friend.transform.position, boardingPoint.position) > 6)
                { Subtitle = "민기가 도착할 때까지 기다리세요."; return false; }
                StartCoroutine(Board()); return true;
            }
            return false;
        }
        IEnumerator Change()
        {
            State = Stage.Boarding; Lock(true);
            yield return Black(1);
            Dress(Player.transform); Dress(Friend.transform);
            Disguised = true;
            Friend.Following = true;
            Subtitle = dialogue.changed;
            yield return new WaitForSeconds(.6f);
            yield return Black(0);
            Lock(false); State = Stage.Vehicle;
        }
        void Dress(Transform actor)
        {
            foreach (var r in actor.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && (mats[i].name.Contains("주황") || mats[i].name.Contains("죄수복"))) mats[i] = uniform;
                r.sharedMaterials = mats;
            }
        }
        IEnumerator Board()
        {
            State=Stage.Boarding;Lock(true);Subtitle=dialogue.boarding;
            filmCamera.enabled=true;Player.View.enabled=false;
            var oldListener=Player.View.GetComponent<AudioListener>();if(oldListener)oldListener.enabled=false;
            var listener=filmCamera.GetComponent<AudioListener>();if(!listener)listener=filmCamera.gameObject.AddComponent<AudioListener>();listener.enabled=true;
            yield return performance.Board();
            engine=vehicle.gameObject.AddComponent<AudioSource>();engine.clip=SuppliedAudio.Current?.playerEngine;engine.loop=true;engine.spatialBlend=.7f;engine.volume=.16f*SuppliedAudio.Volume;if(engine.clip)engine.Play();
            yield return Drive(gateStop.position,5);
            yield return performance.ApproachInspector();
            State=Stage.Checkpoint;Subtitle=dialogue.checkpoint;Selected=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public bool Choose(int index)
        {
            if (State != Stage.Checkpoint || Time.timeScale <= 0 || index < 0 || index > 2) return false;
            if (Choices[index] == previous.dialogue.password) StartCoroutine(Escape());
            else StartCoroutine(Retry());
            return true;
        }
        IEnumerator Retry()
        {
            State = Stage.Retry; Attempts++; Subtitle = dialogue.wrong;
            yield return new WaitForSeconds(dialogue.lineSeconds);
            yield return Black(1); Subtitle = dialogue.retry;
            vehicle.position = gateStop.position;
            yield return new WaitForSeconds(.6f); yield return Black(0);
            Selected = 0; State = Stage.Checkpoint; Subtitle = dialogue.checkpoint;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        IEnumerator Escape()
        {
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;State = Stage.Leaving; Subtitle = dialogue.accepted;
            Subtitle=dialogue.checking;yield return new WaitForSeconds(dialogue.lineSeconds);Subtitle=dialogue.accepted;yield return performance.ClearGate();
            var left = gateLeft.rotation; var right = gateRight.rotation;
            PrisonAudio.Clink(gateLeft.position,.2f);
            for (float t = 0; t < 2; t += Time.deltaTime)
            {
                float f = Mathf.SmoothStep(0, 1, t / 2);
                gateLeft.rotation = left * Quaternion.Euler(0, -100 * f, 0);
                gateRight.rotation = right * Quaternion.Euler(0, 100 * f, 0);
                yield return null;
            }
            yield return Drive(exitPoint.position, 5);
            var openLeft=gateLeft.rotation;var openRight=gateRight.rotation;
            for(float t=0;t<2;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/2);gateLeft.rotation=Quaternion.Slerp(openLeft,left,f);gateRight.rotation=Quaternion.Slerp(openRight,right,f);yield return null;}
            gateLeft.rotation=left;gateRight.rotation=right;
            Subtitle = dialogue.escaped;
            yield return new WaitForSeconds(dialogue.lineSeconds);
            foreach(var line in dialogue.relief){Subtitle=line;performance.LookAtMingi(line.StartsWith("민기:"));yield return new WaitForSeconds(dialogue.lineSeconds);}
            yield return Black(1);
            // 다음 챕터가 연결되기 전에는 탈옥 완료 화면에서 마무리한다.
            if(engine)engine.Stop();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;State = Stage.Complete;
        }
        IEnumerator Drive(Vector3 target, float seconds)
        {
            var start = vehicle.position;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                vehicle.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t / seconds));
                filmCamera.transform.position = vehicle.position + new Vector3(-6, 4, -7);
                filmCamera.transform.LookAt(vehicle.position + Vector3.up);
                yield return null;
            }
            vehicle.position = target;
        }
        IEnumerator Black(float target)
        {
            float from = Fade;
            for (float t = 0; t < .5f; t += Time.deltaTime) { Fade = Mathf.Lerp(from, target, t / .5f); yield return null; }
            Fade = target;
        }
        void Lock(bool value)
        {
            var move = Player.GetComponent<PlayerMove>();
            var look = Player.GetComponentInChildren<CameraMove>();
            var interaction = Player.GetComponent<PlayerInteraction>();
            var phone = Player.GetComponentInChildren<PhoneController>(true);
            if (value && !locked)
            {
                moveControl = move.isControllable; lookControl = look.isControllable;
                combatEnabled = Player.enabled; interactionEnabled = interaction.enabled; phoneEnabled = phone && phone.enabled;
                cameraPosition = Player.View.transform.position; cameraRotation = Player.View.transform.rotation;
            }
            locked = value; move.isControllable = !value && moveControl; look.isControllable = !value && lookControl;
            Player.enabled = !value && combatEnabled; interaction.enabled = !value && interactionEnabled;
            if (phone) phone.enabled = !value && phoneEnabled;
        }
        void OnGUI()
        {
            if (!Active || Time.timeScale <= 0) return;
            var old = GUI.color;
            if (Fade > 0) { GUI.color = new Color(0, 0, 0, Fade); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); }
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.button) { font = MokUIFont.Font, fontSize = 24 };
            if (State == Stage.Checkpoint)
            {
                var choices = Choices;
                for (int i = 0; i < choices.Length; i++)
                    if (GUI.Button(new Rect(Screen.width * .3f, Screen.height * (.5f + i * .09f), Screen.width * .4f, 55), (Selected == i ? "▶ " : "") + choices[i], style)) Choose(i);
            }
            if (State == Stage.Complete)
                GUI.Label(new Rect(0, Screen.height * .4f, Screen.width, 150), "탈옥 성공\n" + dialogue.escaped, new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 32, alignment = TextAnchor.MiddleCenter });
            GUI.color = old;
        }
    }
}
