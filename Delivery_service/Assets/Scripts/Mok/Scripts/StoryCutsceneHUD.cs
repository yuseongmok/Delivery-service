using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Mok
{
    // 게임 진행/자막 캔버스는 그대로 두고, 플레이 안내만 투명하게 만든다.
    [DefaultExecutionOrder(14000)]
    public sealed class StoryCutsceneHUD : MonoBehaviour
    {
        private static StoryCutsceneHUD current;
        private BankPrologue prologue;
        private PrisonSharedPresentation prison;
        private readonly HashSet<GameObject> targets = new HashSet<GameObject>();
        private readonly List<CanvasGroup> hidden = new List<CanvasGroup>();
        public static bool Active => !GameModeSession.IsChallenge && current && current.InCutscene();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Install();}
        private static void Loaded(Scene scene,LoadSceneMode mode)=>Install();
        private static void Install()
        {
            if(GameModeSession.IsChallenge||FindFirstObjectByType<StoryCutsceneHUD>())return;
            current=new GameObject("Cutscene HUD visibility").AddComponent<StoryCutsceneHUD>();
        }
        private IEnumerator Start()
        {
            yield return null;yield return null;Bind();
            yield return new WaitForSecondsRealtime(1);Bind();
        }
        private void Bind()
        {
            prologue=FindFirstObjectByType<BankPrologue>();
            prison=FindFirstObjectByType<PrisonSharedPresentation>();
            foreach(var cue in FindObjectsByType<StoryInteractionCue>(FindObjectsInactive.Include,FindObjectsSortMode.None))HideDuringCutscene(cue.gameObject);
            foreach(var diamond in FindObjectsByType<StoryTargetDiamond>(FindObjectsInactive.Include,FindObjectsSortMode.None))HideDuringCutscene(diamond.gameObject);
            var compact=FindFirstObjectByType<CompactHUD>();if(compact)HideDuringCutscene(compact.gameObject);
            var inbox=FindFirstObjectByType<StoryPhoneInbox>();if(inbox)HideDuringCutscene(inbox.toast);
            if(prologue){HideDuringCutscene(prologue.ammo?prologue.ammo.gameObject:null);HideDuringCutscene(prologue.health?prologue.health.gameObject:null);HideDuringCutscene(prologue.crosshair);}
        }
        public static void Track(GameObject target){if(current)current.HideDuringCutscene(target);}
        private void HideDuringCutscene(GameObject target)
        {
            if(!target||!targets.Add(target))return;
            // 전용 그룹만 관리하여 기존 페이드를 덮어쓰지 않는다.
            var group=target.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;hidden.Add(group);
        }
        private bool InCutscene()
        {
            if(prologue&&prologue.IsCinematic)return true;
            var finale=VenueFinale.Current;
            if(finale&&(finale.Cinematic||finale.State==VenueFinale.Phase.Conversation||finale.State==VenueFinale.Phase.Scheduling))return true;
            var broker=BrokerMission.Current;
            if(broker&&(broker.State==BrokerMission.Phase.Conversation||broker.State==BrokerMission.Phase.Debrief))return true;
            if(SpecialOrderMission.Current&&SpecialOrderMission.Current.InDialogue)return true;
            if(BackdoorMission.Current&&BackdoorMission.Current.InDialogue)return true;
            if(MechanicMission.Current&&MechanicMission.Current.InDialogue)return true;
            if(StoryTutorial.Active&&StoryTutorial.Current.CurrentStep>=StoryTutorial.Step.Goodbye)return true;
            if(prison)
            {
                if(prison.mission.introduction&&prison.mission.introduction.Cinematic)return true;
                if(prison.subtitle&&!string.IsNullOrWhiteSpace(prison.subtitle.text))return true;
                var escape=prison.escape;
                if(escape&&escape.Active&&escape.State!=PrisonEscapeMission.Stage.Change&&escape.State!=PrisonEscapeMission.Stage.Vehicle&&escape.State!=PrisonEscapeMission.Stage.Complete)return true;
                if(prison.chicken&&prison.chicken.Failing)return true;
                if(prison.keys&&prison.keys.Failing)return true;
                if(prison.medicine&&prison.medicine.Failing)return true;
            }
            return false;
        }
        private void LateUpdate(){float alpha=Active?0:1;foreach(var group in hidden)if(group)group.alpha=alpha;}
        private void OnDisable(){foreach(var group in hidden)if(group)group.alpha=1;}
        private void OnDestroy(){foreach(var group in hidden)if(group)Destroy(group);if(current==this)current=null;}
    }
}
