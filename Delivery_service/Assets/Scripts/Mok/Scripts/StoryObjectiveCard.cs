using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    // 목표 원문과 미션 상태를 변경하지 않고 화면에 표시하는 방식만 통일한다.
    [DefaultExecutionOrder(12000)]
    public sealed class StoryObjectiveCard : MonoBehaviour
    {
        private static readonly List<StoryObjectiveCard> cards = new List<StoryObjectiveCard>();
        public static bool AnyVisible => cards.Exists(x=>x && x.Visible);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCards()=>cards.Clear();
        private Text source, title, goal;
        private RectTransform card;
        private string missionTitle;
        private BankPrologue prologue;
        private bool originalEnabled;
        public bool Visible => card && card.gameObject.activeInHierarchy;
        public string DisplayedGoal => goal ? goal.text : "";
        public Text Source => source;

        public static void Bind(Text source, string missionTitle, BankPrologue prologue = null)
        {
            if (!source || GameModeSession.IsChallenge || source.GetComponent<StoryObjectiveCard>()) return;
            var presenter = source.gameObject.AddComponent<StoryObjectiveCard>();
            presenter.source = source;
            presenter.missionTitle = missionTitle;
            presenter.prologue = prologue;
            presenter.originalEnabled = source.enabled;
            presenter.Build();
            cards.Add(presenter);
        }

        private void Build()
        {
            card = Rect(source.transform.parent, "Story objective card", 0, 0, 540, 126);
            card.anchoredPosition = new Vector2(32, -32);
            var background = card.gameObject.AddComponent<HudShape>();
            background.Kind = -1; background.color = new Color(.035f,.065f,.09f,.88f);
            var accent = Rect(card, "Accent", 0, 18, 4, 38).gameObject.AddComponent<Image>();
            accent.color = new Color(1,.48f,.16f); accent.raycastTarget = false;
            title = Label(card,"Mission",24,16,488,28,22,new Color(1,.58f,.25f));
            goal = Label(card,"Goal",24,53,488,100,27,new Color(1,.96f,.87f));
            card.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!source || !card) return;
            // 중앙에 표시되는 프롤로그 장면 제목과 페이드는 원래 연출을 유지한다.
            bool original = GameModeSession.IsChallenge || (prologue && prologue.IsCinematic);
            source.enabled = original && originalEnabled;
            string raw = source.text ?? "";
            bool show = !StoryCutsceneHUD.Active && !original && source.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(raw);
            card.gameObject.SetActive(show);
            if (!show) return;
            Split(raw, missionTitle, out string caption, out string instruction);
            instruction = StoryInteractionCue.WithoutDuplicate(source, instruction);
            title.text = caption; goal.text = instruction;
            float height = string.IsNullOrWhiteSpace(instruction)?0:Mathf.Clamp(goal.preferredHeight, 36, 200);
            goal.rectTransform.sizeDelta = new Vector2(488,height);
            card.sizeDelta = new Vector2(540, 53 + height + 20);
        }

        public static void Split(string raw, string fallback, out string caption, out string instruction)
        {
            caption = fallback; instruction = raw.Trim();
            int separator = instruction.IndexOf('·');
            int newline = instruction.IndexOf('\n');
            if (separator > 0 && separator < 28 && (newline < 0 || separator < newline))
            {
                caption = instruction.Substring(0,separator).Trim();
                instruction = instruction.Substring(separator+1).Trim();
                if (instruction.Length == 0) instruction = raw.Trim();
            }
        }

        private void OnDisable()
        {
            if(card)card.gameObject.SetActive(false);
            if(source)source.enabled=originalEnabled;
        }
        private void OnDestroy(){cards.Remove(this);if(source)source.enabled=originalEnabled;if(card)Destroy(card.gameObject);}
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        private static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Color color)
        {
            var text=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            text.font=MokUIFont.Font;text.fontSize=size;text.color=color;text.alignment=TextAnchor.UpperLeft;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            text.supportRichText=true;text.raycastTarget=false;return text;
        }
    }

    public sealed class StoryHudInstaller : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Install();}
        private static void Loaded(Scene scene,LoadSceneMode mode)=>Install();
        private static void Install()
        {
            if(GameModeSession.IsChallenge||FindFirstObjectByType<StoryHudInstaller>())return;
            new GameObject("Story HUD presentation").AddComponent<StoryHudInstaller>();
        }
        private IEnumerator Start()
        {
            // 공용 감옥 UI가 생성되는 프레임까지 기다린다. 매 프레임 씬을 검색하지 않는다.
            yield return null;yield return null;
            BindScene();yield return new WaitForSecondsRealtime(1);BindScene();
        }
        public static void BindScene()
        {
            if(GameModeSession.IsChallenge)return;
            foreach(var x in FindObjectsByType<BankPrologue>(FindObjectsInactive.Include,FindObjectsSortMode.None))StoryObjectiveCard.Bind(x.objective,"회수 작전",x);
            foreach(var x in FindObjectsByType<PrologueObjectiveMarker>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(x.director)StoryInteractionCue.Bind(x.marker,x.director.objective,x.director.prompt);
            foreach(var x in FindObjectsByType<StoryTutorial>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"위장 생활");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<BrokerMission>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"연락이 끊긴 중개인");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<SpecialOrderMission>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"특별 주문");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<BackdoorMission>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"도시의 뒷문");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<MechanicMission>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"빚진 정비사");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<VenueFinale>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"마지막 배달");StoryInteractionCue.Bind(x.marker,x.objective);}
            foreach(var x in FindObjectsByType<PrisonSharedPresentation>(FindObjectsInactive.Include,FindObjectsSortMode.None)){StoryObjectiveCard.Bind(x.objective,"감옥에서 탈출");StoryInteractionCue.Bind(x.marker,x.objective);}
        }
    }
}
