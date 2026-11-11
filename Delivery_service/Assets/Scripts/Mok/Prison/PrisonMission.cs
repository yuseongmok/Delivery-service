using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Mok
{
    // 임무 순서와 재시도만 담당한다. 조리/카트/감시는 별도 컴포넌트다.
    public sealed class PrisonMission : MonoBehaviour
    {
        public enum Phase { Cook, Load, Deliver, Infirmary, Talk, Return, Complete }
        public static PrisonMission Current { get; private set; }
        public PrisonMissionText dialogue;
        public PlayerCombat player;
        public PrisonMealCart cart;
        public PrisonMissionPoint cooking, infirmary, cell;
        public PrisonMissionPoint[] recipients;
        public Transform stealthCheckpoint;
        public PrisonGuard[] guards;
        public PrisonDistraction distraction;
        public bool UsedDistraction { get; private set; }
        public void MarkDistractionUsed() { UsedDistraction=true; }
        public Phase State { get; private set; }
        public int Delivered { get; private set; }
        public bool Busy { get; private set; }
        public bool InConversation => (State == Phase.Talk || (State == Phase.Return && Time.time < subtitleUntil)) && conversation > 0 && Vector3.Distance(player.transform.position,infirmary.transform.position)<3.5f;
        public bool Failing { get; private set; }
        public string Subtitle { get; private set; }
        public float Fade { get; private set; }
        public float Progress { get; private set; }
        float subtitleUntil;
        int conversation;
        public Transform Target => State switch {
            Phase.Cook => cooking.transform, Phase.Load => cart.transform,
            Phase.Deliver => recipients[Mathf.Clamp(Delivered, 0, recipients.Length - 1)].transform,
            Phase.Infirmary => !UsedDistraction && distraction ? distraction.transform : infirmary.transform,
            Phase.Talk => infirmary.transform, Phase.Return => cell.transform, _ => null
        };
        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Say(dialogue.introduction, 10);
        }
        void Update()
        {
            if (!Failing && Time.time > subtitleUntil && State != Phase.Complete) Subtitle = "";
            if (State == Phase.Infirmary && Vector3.Distance(player.transform.position, infirmary.transform.position) < 2.5f)
                State = Phase.Talk;
        }
        public void Say(string line, float seconds = 7) { Subtitle = line; subtitleUntil = Time.time + seconds; }
        public bool Use(PrisonMissionPoint point)
        {
            if (Busy || Failing || !player.CanAct) return false;
            if (point == cooking && State == Phase.Cook) { StartCoroutine(Cook()); return true; }
            if (State == Phase.Deliver && Delivered < recipients.Length && point == recipients[Delivered])
            {
                if (Vector3.Distance(cart.transform.position, point.transform.position) > 4.5f)
                { Say("배식카트를 가까이 가져와야 한다.", 3); return false; }
                if(!cart.controller.cartRigidbody.isKinematic) cart.StopPushing();
                StartCoroutine(Serve(point)); return true;
            }
            if (point == infirmary && (State == Phase.Talk || State == Phase.Infirmary))
            {
                State = Phase.Talk;
                if (Time.time < subtitleUntil && conversation > 0) return false;
                Say(dialogue.mingi[conversation++], 5);
                if (conversation >= dialogue.mingi.Length) State = Phase.Return;
                return true;
            }
            if (point == cell && State == Phase.Return)
            { State = Phase.Complete; Say(dialogue.completed, 9999); return true; }
            return false;
        }
        IEnumerator Cook()
        {
            Busy = true; PrisonAudio.Clink(cooking.transform.position);
            Say("밥을 담고 반찬을 준비하는 중…", 4);
            for (float t = 0; t < 3; t += Time.deltaTime) { Progress = t / 3; yield return null; }
            Progress = 0; cart.ShowPrepared(true); State = Phase.Load; Busy = false;
            Say("식사 준비가 끝났다. 카트에 실어 배식하자.");
        }
        public void LoadCart()
        {
            if (State != Phase.Load || Busy || Failing) return;
            PrisonAudio.Clink(cart.transform.position); cart.ShowPrepared(false); cart.SetMeals(3); State = Phase.Deliver;
            Say("손잡이를 잡고 첫 번째 재소자에게 카트를 밀어 가자.");
        }
        IEnumerator Serve(PrisonMissionPoint point)
        {
            Busy = true;
            yield return cart.DeliverTo(point.transform.position + Vector3.up);
            Say(dialogue.prisoners[Delivered], 9); Delivered++;
            State = Delivered == recipients.Length ? Phase.Infirmary : Phase.Deliver;
            Busy = false;
        }
        public void Fail(bool assault)
        {
            if (Failing || (!assault && State == Phase.Complete)) return;
            StartCoroutine(Retry(assault));
        }
        IEnumerator Retry(bool assault)
        {
            Failing = true; Busy = true; if(!cart.controller.cartRigidbody.isKinematic) cart.StopPushing();
            Say(assault ? dialogue.attacked : dialogue.caught, 20);
            player.GetComponent<PlayerMove>().isControllable = false;
            yield return new WaitForSeconds(1.2f);
            for (float t = 0; t < .8f; t += Time.deltaTime) { Fade = t / .8f; yield return null; }
            if (assault) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); yield break; }
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.SetPositionAndRotation(stealthCheckpoint.position, stealthCheckpoint.rotation);
            cc.enabled = true;
            foreach (var guard in guards) guard.ResetWatch();
            State = Phase.Infirmary; conversation = 0; UsedDistraction=false;
            for (float t = 0; t < .6f; t += Time.deltaTime) { Fade = 1 - t / .6f; yield return null; }
            Fade = 0; Busy = Failing = false; player.GetComponent<PlayerMove>().isControllable = true;
        }
    }
}

