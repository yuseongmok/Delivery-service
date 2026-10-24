using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 프롤로그를 마친 새 게임에서만 실행한다. 실제 제작과 배달 상태를 관찰한다.
[DefaultExecutionOrder(-200)]
public sealed class StoryTutorial : MonoBehaviour {
    public enum Step { Open, Accept, Dough, Sauce, Cheese, Bake, Pack, Load, Navigate, Ride, Deliver, Review, Return, Goodbye, Complete }
    public static bool Requested;
    public static StoryTutorial Current {get;private set;}
    public static bool Active => Current!=null&&Current.running;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetSession(){Requested=false;Current=null;}
    public OrderManager orders;
    public DayNightCycle day;
    public ToppingInventory inventory;
    public PizzaMaker maker;
    public Oven oven;
    public PizzaPacking packing;
    public PizzaPickUpZone pickup;
    public BikePizzaStorage cargo;
    public NavigationManager navigation;
    public DeliveryPoint destination;
    public Transform pc,actor,insideStand,outsideStand;
    public Transform[] guideRoute;
    public Topping dough,sauce,cheese;
    public Text objective,subtitle,marker;
    public GameObject ui;
    [TextArea(2,4)] public string[] dialogue={
        "나: 어디서부터 하는데?\n민기: 앞치마부터. 총은 넣어 두고. 컴퓨터로 개점해.",
        "나: 열자마자 누가 시키겠어?\n민기: …있네. 치즈피자 한 판, 수락해 봐.",
        "민기: 연습할 재료는 내가 준비했어. 도우를 집어서 제작대에 올려.",
        "민기: 소스를 집어서 도우에 발라. 순서대로 하면 돼.",
        "민기: 치즈는 그 위에.\n나: 생각보다 쉬운데?",
        "민기: 그럼 이제 구우면 되겠네. 피자를 집어서 오븐에 넣어.",
        "민기: 다 구워졌으면 꺼내. 상자를 열고 피자를 넣은 뒤 닫아. 옆에서 챙기고.",
        "민기: 상자는 배달통에. 손에 들고 운전할 생각은 아니지?",
        "민기: M으로 폰을 열고, 방향키와 Enter로 지도 → 배달지를 골라.",
        "민기: 오토바이에 타. W/S로 가속·제동, A/D로 방향을 바꿔. 급회전은 조심하고.",
        "나: 금고 털 때보다 잔소리가 많네.\n민기: 이번엔 손님 돈이 걸렸잖아. 도착하면 내려서 상자를 꺼내 드려.",
        "손님: 이얏호 피자? 다시 열었네요. 예전엔 자주 시켰는데. 잘 먹을게요!\n민기: 첫 리뷰가 왔어. 폰의 별점 앱을 봐.",
        "민기: 첫 손님이 별 다섯 개를 줬네. 이제 가게로 돌아와.",
        "민기: 어땠어?\n나: 다시 열어서 좋대. 돈도 받았어. 이번엔 아무도 안 쫓아오더라.",
        "민기: 그럼 한 판 더 팔아 보자. 문 닫으면… 어젯밤 돈 얘기 좀 하자."
    };
    public Step CurrentStep {get;private set;}
    public bool Completed {get;private set;}
    bool running,delivered,loaded,rode,started;
    float oldMultiplier,nextPoll,enteredAt;
    Outline highlight;
    Transform target,lastHighlight;
    NavMeshAgent agent;
    Animator animator;
    CharacterController guideBody;
    int guideIndex=-1,guideDirection=1;
    void MoveGuide(){
        if(!guideBody||!guideBody.enabled)return;
        Vector3 motion=Vector3.zero;
        if(guideIndex>=0&&guideIndex<guideRoute.Length){
            var offset=guideRoute[guideIndex].position-actor.position;offset.y=0;
            if(offset.magnitude<.12f)guideIndex+=guideDirection;
            else motion=offset.normalized*Mathf.Min(1.6f,offset.magnitude/Mathf.Max(.001f,Time.deltaTime));
        }
        var facing=motion.sqrMagnitude>.01f?motion:inventory.transform.position-actor.position;facing.y=0;
        if(facing.sqrMagnitude>.1f&&facing.sqrMagnitude<100)actor.rotation=Quaternion.RotateTowards(actor.rotation,Quaternion.LookRotation(facing),Time.deltaTime*120);
        guideBody.Move((motion+Vector3.down*3)*Time.deltaTime);
        if(animator)animator.SetFloat("MoveSpeed",motion.magnitude,.15f,Time.deltaTime);
    }
    string[] tasks={"PC에서 개점하기","첫 치즈피자 주문 수락하기","도우를 제작대에 놓기","도우에 소스 바르기","치즈 올리기","피자를 오븐에서 굽기","상자에 포장하고 챙기기","오토바이 배달통에 싣기","지도 앱에서 배달지 안내 시작","오토바이에 탑승하기","피자를 꺼내 첫 손님에게 전달","핸드폰 별점 앱에서 리뷰 보기","피자 가게로 돌아오기","민기와 첫 배달 이야기","첫 정당한 수입 · 일반 영업 시작"};
    void Awake(){
#if UNITY_EDITOR
        Requested|=UnityEditor.SessionState.GetBool("Mok.BankPrologue.Check.tutorial",false);
#endif
        if(!Requested){ui.SetActive(false);actor.gameObject.SetActive(false);enabled=false;return;}
        Requested=false;Current=this;running=true;orders.BeginStoryLesson(destination);
    }
    void Start(){
        if(!running)return;
        started=true;oldMultiplier=day.timeMultiplier;day.timeMultiplier=0;
        ui.SetActive(true);actor.gameObject.SetActive(true);agent=actor.GetComponent<NavMeshAgent>();animator=actor.GetComponentInChildren<Animator>();
        // 매장 내부에는 내비메시가 없으므로 저장된 출입 동선과 물리 바닥을 사용한다.
        if(agent)agent.enabled=false;
        guideBody=actor.GetComponent<CharacterController>();
        if(guideBody)guideBody.enabled=false;
        actor.position=insideStand.position;
        if(guideBody)guideBody.enabled=true;
        SetStep(Step.Open);
    }
    public void DeliveryCompleted(){if(running){delivered=true;SetStep(Step.Review);}}
    void Update(){
        if(!running||!started||Time.timeScale==0)return;
        MoveGuide();
        if(Time.time<nextPoll)return;nextPoll=Time.time+.15f;
        if(CurrentStep==Step.Goodbye){
            if(Time.time-enteredAt>7&&Time.time-enteredAt<8)subtitle.text=dialogue[(int)Step.Complete];
            if(Time.time-enteredAt>13)Finish();return;
        }
        if(delivered){
            if(CurrentStep==Step.Review&&Time.time-enteredAt>7&&FindFirstObjectByType<ReviewPhoneApp>()!=null)SetStep(Step.Return);
            else if(CurrentStep==Step.Return&&!cargo.bikeController.isDriven&&Vector3.Distance(inventory.transform.position,insideStand.position)<2.8f&&Vector3.Distance(actor.position,insideStand.position)<1.4f)SetStep(Step.Goodbye);
            return;
        }
        if(!day.isShopOpen){SetStep(Step.Open);return;}
        if(!orders.HasActiveOrder){SetStep(Step.Accept);return;}
        loaded|=cargo.StoredCount>0;rode|=loaded&&cargo.bikeController.isDriven;
        bool packaged=inventory.HasPackagedPizzas()||cargo.StoredCount>0;
        var pizza=FindObjectsByType<Pizza>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(p=>p.gameObject.scene==gameObject.scene).OrderByDescending(p=>p.IsBaked?100:p.currentToppings.Count).FirstOrDefault();
        if(loaded&&!packaged&&!pizza&&pickup.PackedCount==0){loaded=false;rode=false;}
        if(!packaged&&!loaded&&pickup.PackedCount>0){SetStep(Step.Pack);target=pickup.transform;UpdateHighlight();return;}
        if(!packaged&&!loaded){
            if(!pizza){SetStep(Step.Dough);target=inventory.HasTopping()?maker.transform:dough.transform;}
            else {var types=PizzaRecipes.Types(pizza.currentToppings);
                if(!types.Contains(ToppingType.Sauce)){SetStep(Step.Sauce);target=inventory.HasTopping()?pizza.transform:sauce.transform;}
                else if(!types.Contains(ToppingType.Cheese)){SetStep(Step.Cheese);target=inventory.HasTopping()?pizza.transform:cheese.transform;}
                else if(!pizza.IsBaked){SetStep(Step.Bake);target=inventory.HasPizza()||pizza.GetComponentInParent<Oven>()?oven.transform:maker.transform;}
                else {SetStep(Step.Pack);if(!inventory.HasPizza())target=oven.transform;}
            }
        }else if(!loaded)SetStep(Step.Load);
        else if(!rode){
            if(navigation.RouteDestination!=destination.transform||navigation.RouteRevision<=navigationRevision)SetStep(Step.Navigate);
            else SetStep(Step.Ride);
        }else SetStep(Step.Deliver);
        UpdateHighlight();
    }
    int navigationRevision;
    void SetStep(Step step){
        if(CurrentStep==step&&objective.text.Length>0)return;
        CurrentStep=step;enteredAt=Time.time;objective.text="첫 영업일  ·  "+tasks[(int)step];subtitle.text=dialogue[(int)step];
        target=step==Step.Open||step==Step.Accept?pc:step==Step.Dough?dough.transform:step==Step.Sauce?sauce.transform:step==Step.Cheese?cheese.transform:step==Step.Bake?oven.transform:step==Step.Pack?packing.transform:step==Step.Load||step==Step.Ride?cargo.transform:step==Step.Deliver?destination.transform:step==Step.Return?insideStand:null;
        if(step==Step.Load)navigationRevision=navigation.RouteRevision;
        if(step==Step.Load&&guideRoute!=null&&guideRoute.Length>0){guideDirection=1;guideIndex=0;}
        if(step==Step.Review&&guideRoute!=null&&guideRoute.Length>0){guideDirection=-1;guideIndex=Mathf.Clamp(guideIndex,0,guideRoute.Length-1);}
        UpdateHighlight();
    }
    void UpdateHighlight(){
        if(lastHighlight==target)return;
        if(highlight)Destroy(highlight);highlight=null;lastHighlight=target;
        if(target&&!target.GetComponent<Outline>()&&target.GetComponentInChildren<Renderer>()){
            highlight=target.gameObject.AddComponent<Outline>();highlight.OutlineMode=Outline.Mode.OutlineVisible;highlight.OutlineColor=new Color(1,.55f,.18f);highlight.OutlineWidth=4;
        }
    }
    void LateUpdate(){
        if(!running||!marker)return;
        var cam=Camera.main;marker.gameObject.SetActive(target&&cam);
        if(!target||!cam)return;
        StoryTargetDiamond.Place(marker,cam,target.position+Vector3.up,inventory.transform.position,"목표");
    }
    public void DeveloperComplete(){if(!Active)return;orders.ResetOrdersForNewDay();Finish();}
    void Finish(){
        Completed=true;running=false;day.timeMultiplier=oldMultiplier;orders.EndStoryLesson();
        FindFirstObjectByType<DeliveryService.Yun.Achievements.AchievementManager>()?.TryUnlock(DeliveryService.Yun.Achievements.AchievementManager.FirstDeliveryId);
        DeliveryRequestNotice.Show("업적 달성 · 첫 정당한 수입\n이제 스스로 주문을 골라 배달해 보세요.");
        if(highlight)Destroy(highlight);ui.SetActive(false);
    }
    void OnDestroy(){if(Current==this){if(started&&day)day.timeMultiplier=oldMultiplier;Current=null;}if(highlight)Destroy(highlight);}
}
}
