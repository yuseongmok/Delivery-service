using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeliveryService.Mok;

public class OrderManager : MonoBehaviour {
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private CurrentOrderUI currentOrderUI;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private CameraMove cameraMove;
    [SerializeField] private OrderGenerator orderGenerator;
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private int basePizzaPrice=10000;
    [SerializeField] private List<PizzaToppingData> toppingDataList=new();
    [SerializeField] private List<DeliveryPoint> deliveryPoints=new();
    private List<PizzaOrder> pendingOrders=new();
    private PizzaOrder activeOrder;
    private string currentTargetDeliveryID;
    private int generatedToday,nextOrderNumber=1,orderNumberDay=-1;
    private bool prevShopOpenState;
    private double deliveryDeadlineMinutes;
    // 하루 길이와 무관하게 배달은 기존 20분 하루(54배속)의 실제 여유를 유지한다.
    public const float DeliveryTimeMultiplier=54f;
    private double deliveryClockMinutes;
    private readonly List<double> refillAt=new();
    public static int CapacityForRating(float rating)=>Mathf.RoundToInt(rating*100)>=500?5:rating>=4?4:3;
    public static int PendingCapacity=>CapacityForRating(ShopReputation.Instance.Rating);
    public const double RefillGameSeconds=5;
    public float RecommendedDeliveryMinutes {get;private set;}=90f;
    public float DeliveryRouteMeters {get;private set;}
    private float deliveryTimeLimitMinutes=>RecommendedDeliveryMinutes;
    private double GameMinutes=>dayNightCycle==null?0:dayNightCycle.currentDay*1440d+dayNightCycle.currentTime*60d;
    public int OrdersGeneratedToday=>generatedToday;
    public int DailyOrderLimit=>0; // 0은 무제한이다. 기존 참조와 호환하기 위해 남긴다.
    public bool DailyOrdersExhausted=>false;
    public bool HasActiveOrder=>activeOrder!=null;
    public PizzaOrder ActiveOrder=>activeOrder;
    public string CurrentTargetDeliveryID=>currentTargetDeliveryID;
    public DeliveryPoint CurrentTargetDeliveryPoint=>deliveryPoints.Find(p=>p!=null&&p.DeliveryPointID==currentTargetDeliveryID);
    public System.Collections.ObjectModel.ReadOnlyCollection<PizzaOrder> PendingOrders=>pendingOrders.AsReadOnly();
    public float RemainingDeliveryMinutes=>!HasActiveOrder?0:(float)System.Math.Max(0,deliveryDeadlineMinutes-deliveryClockMinutes);
    public float ElapsedDeliveryMinutes=>HasActiveOrder?(float)System.Math.Max(0,deliveryClockMinutes-(deliveryDeadlineMinutes-deliveryTimeLimitMinutes)):0;
    public float SignedDeliveryMinutes=>HasActiveOrder?(float)(deliveryDeadlineMinutes-deliveryClockMinutes):0;
    public string LastDeliveryFailure {get;private set;}
    public int LastDeliveredPizzaIndex {get;private set;}=-1;
    public event System.Action OrdersChanged;
    public bool StoryLesson {get;private set;}
    DeliveryPoint lessonDestination;
    bool lessonIssued;
    public void BeginStoryLesson(DeliveryPoint destination){StoryLesson=true;lessonDestination=destination;lessonIssued=false;pendingOrders.Clear();refillAt.Clear();Changed();}
    public void EndStoryLesson(){StoryLesson=false;lessonDestination=null;refillAt.Clear();SyncPendingCapacity();Changed();}
    void Start(){if(orderGenerator==null)orderGenerator=FindFirstObjectByType<OrderGenerator>();if(dayNightCycle==null)dayNightCycle=FindFirstObjectByType<DayNightCycle>();}
    void OnEnable(){ShopReputation.Changed+=RefreshPrices;if(orderUI!=null){orderUI.OnOrderAccepted+=HandleAccept;orderUI.OnOrderDeclined+=HandleDecline;}}
    void OnDisable(){ShopReputation.Changed-=RefreshPrices;if(orderUI!=null){orderUI.OnOrderAccepted-=HandleAccept;orderUI.OnOrderDeclined-=HandleDecline;}}
    void Update(){
        if(dayNightCycle==null)dayNightCycle=FindFirstObjectByType<DayNightCycle>();
        if(dayNightCycle==null)return;
        if(dayNightCycle.isShopOpen)deliveryClockMinutes+=Time.deltaTime*DeliveryTimeMultiplier/60d;
        if(dayNightCycle.isShopOpen&&!prevShopOpenState)OnShopOpened();
        prevShopOpenState=dayNightCycle.isShopOpen;
        if(dayNightCycle.isShopOpen){
            for(int i=refillAt.Count-1;i>=0;i--)if(GameMinutes+0.000001>=refillAt[i]){refillAt.RemoveAt(i);GenerateSingleOrder();}
        }
        if(dayNightCycle.isShopOpen)SyncPendingCapacity();
        UpdateDeliveryDeadline();
    }
    void RefreshPrices(){SyncPendingCapacity();foreach(var order in pendingOrders){var point=deliveryPoints.Find(p=>p!=null&&p.DeliveryPointID==order.destinationID);order.quotedPrice=Quote(order,point);}Changed();}
    void Changed(){if(orderUI!=null)orderUI.RefreshOrderList(pendingOrders);OrdersChanged?.Invoke();}
    void UpdateOrderNumberDay(){if(dayNightCycle==null)return;if(orderNumberDay==dayNightCycle.currentDay)return;orderNumberDay=dayNightCycle.currentDay;nextOrderNumber=1;generatedToday=0;refillAt.Clear();}
    public void OnShopOpened(){
        if(dayNightCycle==null)dayNightCycle=FindFirstObjectByType<DayNightCycle>();
        if(orderGenerator==null)orderGenerator=FindFirstObjectByType<OrderGenerator>();
        UpdateOrderNumberDay();int missing=PendingCapacity-pendingOrders.Count-refillAt.Count;
        for(int i=0;i<missing;i++)GenerateSingleOrder();
    }
    // 평점 하락 시 기존 주문은 유지하고 예약된 보충만 현재 한도에 맞춘다.
    void SyncPendingCapacity(){
        if(StoryLesson)return;
        if(dayNightCycle==null||!dayNightCycle.isShopOpen)return;
        int available=Mathf.Max(0,PendingCapacity-pendingOrders.Count);
        while(refillAt.Count>available)refillAt.RemoveAt(refillAt.Count-1);
        while(refillAt.Count<available)refillAt.Add(GameMinutes+RefillGameSeconds/60d);
    }
    void ScheduleNextOrder(){if(!StoryLesson&&pendingOrders.Count+refillAt.Count<PendingCapacity)refillAt.Add(GameMinutes+RefillGameSeconds/60d);}
    void GenerateSingleOrder(){
        if(dayNightCycle==null||!dayNightCycle.isShopOpen||pendingOrders.Count>=PendingCapacity||orderGenerator==null)return;
        var policy=GetComponent<DistrictDeliveryPolicy>();
        if(StoryLesson&&lessonIssued)return;
        var destination=StoryLesson?lessonDestination:policy!=null?policy.Choose(deliveryPoints):deliveryPoints.FirstOrDefault(p=>p!=null&&p.isActiveAndEnabled);
        if(destination==null){ScheduleNextOrder();return;}
        var order=StoryLesson?new PizzaOrder{recipeName="치즈피자",toppings=new List<string>{"도우","소스","치즈"}}:PizzaRecipes.Create(toppingDataList,ShopReputation.Instance.Rating);
        order.orderName=$"주문 {nextOrderNumber++:D2}";
        order.destinationID=destination.DeliveryPointID;
        order.districtName=policy!=null?policy.Region(destination):"기존 도시";
        order.quotedPrice=Quote(order,destination);
        pendingOrders.Add(order);if(StoryLesson)lessonIssued=true;generatedToday++;Changed();
    }
    public int Quote(PizzaOrder order,DeliveryPoint destination){
        int price=basePizzaPrice;
        foreach(var type in PizzaRecipes.Types(order.toppings).Where(t=>!PizzaRecipes.IsBase(t))){var data=toppingDataList.Find(d=>d!=null&&d.toppingType==type);if(data!=null)price+=Mathf.Max(0,data.sellingPrice);}
        var policy=GetComponent<DistrictDeliveryPolicy>();return Mathf.RoundToInt(price*(policy!=null?policy.Multiplier(destination):1)*ShopReputation.Instance.Multiplier);
    }
    public bool TryAcceptOrder(PizzaOrder order){if(order==null||HasActiveOrder||!pendingOrders.Contains(order)||dayNightCycle==null||!dayNightCycle.isShopOpen)return false;HandleAccept(order);return activeOrder==order;}
    public bool TryDeclineOrder(PizzaOrder order){if(order==null||!pendingOrders.Contains(order)||dayNightCycle==null||!dayNightCycle.isShopOpen)return false;HandleDecline(order);return true;}
    void HandleAccept(PizzaOrder order){
        if(order==null||HasActiveOrder||!pendingOrders.Contains(order)||dayNightCycle==null||!dayNightCycle.isShopOpen)return;
        if(!deliveryPoints.Any(p=>p!=null&&p.isActiveAndEnabled&&p.DeliveryPointID==order.destinationID))return;
        activeOrder=order;currentTargetDeliveryID=order.destinationID;RecommendedDeliveryMinutes=DeliveryTravelTime.Recommend(CurrentTargetDeliveryPoint,out float routeMeters);DeliveryRouteMeters=routeMeters;deliveryDeadlineMinutes=deliveryClockMinutes+deliveryTimeLimitMinutes;pendingOrders.Remove(order);ScheduleNextOrder();
        if(currentOrderUI!=null)currentOrderUI.DisplayElapsedOrder(activeOrder,currentTargetDeliveryID,ElapsedDeliveryMinutes,RecommendedDeliveryMinutes);Changed();
    }
    void HandleDecline(PizzaOrder order){if(StoryLesson){DeliveryRequestNotice.Show("민기: 첫 손님이야. 이번 주문은 수락해 보자.");return;}if(dayNightCycle==null||!dayNightCycle.isShopOpen||!pendingOrders.Remove(order))return;ShopReputation.Instance.Declined();ScheduleNextOrder();Changed();}
    void UpdateDeliveryDeadline(){
        if(!HasActiveOrder||dayNightCycle==null)return;
        if(currentOrderUI!=null)currentOrderUI.DisplayElapsedOrder(activeOrder,currentTargetDeliveryID,ElapsedDeliveryMinutes,RecommendedDeliveryMinutes);
    }
    public void ResetOrdersForNewDay(){refillAt.Clear();prevShopOpenState=false;UpdateOrderNumberDay();pendingOrders.Clear();activeOrder=null;currentTargetDeliveryID="";deliveryDeadlineMinutes=0;LastDeliveredPizzaIndex=-1;if(currentOrderUI!=null)currentOrderUI.ClearOrder();FindFirstObjectByType<NavigationManager>()?.ResetNavigationForNewDay();Changed();}
    public bool TryDeliverPackagedStack(List<PizzaData> pizzas,string destination){
        UpdateDeliveryDeadline();LastDeliveredPizzaIndex=-1;LastDeliveryFailure="";
        if(!HasActiveOrder){LastDeliveryFailure="진행 중인 주문이 없습니다.";return false;}
        if(destination!=currentTargetDeliveryID){LastDeliveryFailure="주문한 배달지가 아닙니다.";return false;}
        if(pizzas==null||pizzas.Count==0){LastDeliveryFailure="포장한 피자가 필요합니다.";return false;}
        int match=pizzas.FindIndex(p=>p!=null&&p.isBaked&&!p.isRuined&&PizzaRecipes.Matches(p.toppings,activeOrder.toppings));
        if(match<0)match=pizzas.FindIndex(p=>p!=null);
        if(match<0){LastDeliveryFailure="전달할 피자가 없습니다.";return false;}
        if(MoneyManager.Instance==null){LastDeliveryFailure="정산을 준비 중입니다.";return false;}
        var delivered=pizzas[match];bool correct=PizzaRecipes.Matches(delivered.toppings,activeOrder.toppings);
        if(StoryLesson&&(!correct||!delivered.isBaked||delivered.isRuined)){LastDeliveryFailure="민기: 첫 손님한테는 제대로 만든 치즈피자를 드리자. 가게에서 다시 만들어 보자.";return false;}
        // 이번 리뷰 직전 평점으로 정산하고 다음 배달부터 새 평점을 반영한다.
        int price=StoryLesson?Quote(activeOrder,CurrentTargetDeliveryPoint):DeliveryPricing.ApplyDelay(Quote(activeOrder,CurrentTargetDeliveryPoint),ElapsedDeliveryMinutes,RecommendedDeliveryMinutes);
        if(!correct)price=Mathf.RoundToInt(price*.25f);if(!delivered.isBaked||delivered.isRuined)price=0;
        MoneyManager.Instance.AddMoney(price);LastDeliveredPizzaIndex=match;
        var review=ShopReputation.Instance.Record(currentTargetDeliveryID,PizzaRecipes.Title(activeOrder),StoryLesson?0:ElapsedDeliveryMinutes,correct,delivered.isBaked,delivered.isRuined,price,dayNightCycle.currentDay,RecommendedDeliveryMinutes);
        if(StoryLesson){review.text="오랜만에 다시 주문했어요. 앞으로도 자주 시킬게요.";DeliveryService.Mok.StoryTutorial.Current?.DeliveryCompleted();}
        activeOrder=null;currentTargetDeliveryID="";deliveryDeadlineMinutes=0;if(currentOrderUI!=null)currentOrderUI.ClearOrder();
        RobberyManager.NotifyDeliveryCompleted();DeliveryService.Mok.Stocks.StockMarket.Instance?.DeliveryClue();Changed();return true;
    }
}
