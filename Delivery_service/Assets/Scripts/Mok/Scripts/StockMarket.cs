using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok.Stocks {
public sealed class StockMarket : MonoBehaviour {
    public sealed class Company {
        public int price,openingPrice,shares;public long cost;
        public readonly List<int> history=new List<int>();
        public float Average => shares>0?(float)cost/shares:0;
    }
    public sealed class Article {public int day,company;public string title,body;public bool followup;}
    sealed class Story {public int eventIndex,due;public bool good;public float pressure;}
    public static StockMarket Instance {get;private set;}
    public readonly Company[] Companies=new Company[5];
    public readonly List<Article> Articles=new List<Article>();
    readonly Story[] stories=new Story[5];
    readonly Dictionary<int,int> lastUsed=new Dictionary<int,int>();
    readonly float[] trends=new float[5];
    System.Random random;
    DayNightCycle clock;
    int day,newsRound;
    float tick,marketTrend;
    public long RealizedProfit {get;private set;}
    public string LastClue {get;private set;}="배달 중 얻은 기업 소식이 아직 없습니다.";
    public int Revision {get;private set;}
    public bool MarketOpen => clock!=null&&clock.isShopOpen&&clock.currentTime<24&&Time.timeScale>0;
    public long PortfolioValue => Companies.Where(c=>c!=null).Sum(c=>(long)c.price*c.shares);
    public long PortfolioProfit => Companies.Where(c=>c!=null).Sum(c=>(long)c.price*c.shares-c.cost);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){Instance=null;}
    void Awake(){
        if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;
        Initialize(Environment.TickCount);
    }
    void Start(){clock=FindFirstObjectByType<DayNightCycle>();if(clock!=null)AdvanceDay(clock.currentDay);}
    public void Initialize(int seed){
        random=new System.Random(seed);Articles.Clear();lastUsed.Clear();day=0;newsRound=0;tick=0;RealizedProfit=0;
        for(int i=0;i<5;i++){Companies[i]=new Company{price=StockCatalog.StartingPrices[i],openingPrice=StockCatalog.StartingPrices[i]};Companies[i].history.Add(Companies[i].price);stories[i]=null;trends[i]=0;}
    }
    void Update(){
        if(clock==null)return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(Time.timeScale>0){
            if(Input.GetKeyDown(KeyCode.F9))TestNewsRound();
            if(Input.GetKeyDown(KeyCode.F10))TestPriceTicks();
        }
#endif
        if(day!=clock.currentDay)AdvanceDay(clock.currentDay);
        if(!MarketOpen)return;
        tick+=Time.deltaTime;
        if(tick>=8){tick%=8;TickPrices();}
    }
    float Noise()=> (float)(random.NextDouble()*2-1);
    public void AdvanceDay(int nextDay){
        if(nextDay<=day)return;
        day=nextDay;tick=0;marketTrend=Noise()*.00012f;
        foreach(var c in Companies)c.openingPrice=c.price;
        PublishRound();
    }
    void PublishRound(){
        newsRound++;
        int count=0;
        // 만기가 된 사건을 먼저 보도하여 기사와 결과가 어긋나지 않게 한다.
        for(int i=0;i<5&&count<3;i++)if(stories[i]!=null&&stories[i].due<=newsRound){
            var story=stories[i];var e=StockCatalog.Events[story.eventIndex];
            AddArticle(i,story.good?e.goodTitle:e.badTitle,story.good?e.goodBody:e.badBody,true);
            trends[i]=(story.good?1:-1)*(.00035f+(float)random.NextDouble()*.0003f);stories[i]=null;count++;
        }
        var candidates=Enumerable.Range(0,StockCatalog.Events.Length).Where(i=>stories[StockCatalog.Events[i].company]==null&&(!lastUsed.ContainsKey(i)||newsRound-lastUsed[i]>=5)).OrderBy(i=>random.Next()).ToList();
        foreach(int index in candidates){
            if(count>=3)break;var e=StockCatalog.Events[index];if(stories[e.company]!=null)continue;
            bool good=random.NextDouble()<.5;
            stories[e.company]=new Story{eventIndex=index,due=newsRound+random.Next(1,4),good=good,pressure=Noise()*.00045f};lastUsed[index]=newsRound;
            string headline=random.Next(2)==0?e.title:e.alternate;
            AddArticle(e.company,headline,e.body,false);count++;
        }
        // 새 사건이 적은 날에는 진행 중인 사건의 중간 보도를 제공한다.
        foreach(int i in Enumerable.Range(0,5).Where(i=>stories[i]!=null).OrderBy(i=>random.Next())){
            if(count>=2)break;var e=StockCatalog.Events[stories[i].eventIndex];
            AddArticle(i,e.title+"…후속 상황 확인 중","앞서 보도한 사안에 대해 회사가 추가 확인을 진행 중이다. "+e.body+" 구체적인 결과는 아직 발표되지 않았다.",false);count++;
        }
        Revision++;
    }
    void AddArticle(int company,string title,string body,bool followup){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(publishingTest)title="[테스트] "+title;
#endif
        Articles.Insert(0,new Article{day=day,company=company,title=title,body=body,followup=followup});
        if(Articles.Count>90)Articles.RemoveAt(Articles.Count-1);
    }
    public void TickPrices(){
        for(int i=0;i<5;i++){
            var c=Companies[i];float pressure=stories[i]!=null?stories[i].pressure:0;
            // 전체 시장, 기업 흐름, 작은 잡음을 합친다. 매수 여부는 가격 계산에 사용하지 않는다.
            float change=marketTrend+trends[i]+pressure+Noise()*StockCatalog.Volatility[i];
            c.price=Mathf.Clamp(Mathf.RoundToInt(c.price*(1+change)),Mathf.Max(10,Mathf.RoundToInt(c.openingPrice*.8f)),Mathf.RoundToInt(c.openingPrice*1.2f));
            trends[i]*=.996f;c.history.Add(c.price);if(c.history.Count>100)c.history.RemoveAt(0);
        }
        Revision++;
    }
    public bool Trade(int index,int quantity,bool buy,int quotedPrice,out string message){
        message="거래할 수 없습니다.";
        if(!MarketOpen){message="개점 후 거래할 수 있습니다.";return false;}
        if(index<0||index>=5||quantity<1||quantity>999)return false;
        var c=Companies[index];var money=MoneyManager.Instance;if(money==null)return false;
        if(c.price!=quotedPrice){message="가격이 바뀌었습니다. 다시 확인해 주세요.";return false;}
        long total=(long)quantity*c.price;
        if(total>int.MaxValue)return false;
        if(buy){
            if(c.shares>99999-quantity){message="보유 한도를 초과합니다.";return false;}
            if(!money.TransferInvestmentMoney(-(int)total)){message="현금이 부족합니다.";return false;}
            c.shares+=quantity;c.cost+=total;
        }else{
            if(c.shares<quantity){message="보유 수량이 부족합니다.";return false;}
            if(!money.TransferInvestmentMoney((int)total))return false;
            long basis=quantity==c.shares?c.cost:(long)Math.Round((double)c.cost*quantity/c.shares);
            c.cost-=basis;c.shares-=quantity;RealizedProfit+=total-basis;
        }
        message=$"{(buy?"매수":"매도")} 완료 · {quantity}주 / {total:N0}원";Revision++;return true;
    }
    public bool CoverDebt(){
        var money=MoneyManager.Instance;if(money==null||money.currentMoney>=0)return true;
        // 파산 직전에는 부족한 현금만큼 보유 주식을 처분한다.
        for(int i=0;i<5&&money.currentMoney<0;i++){
            var c=Companies[i];int quantity=(int)Math.Min(c.shares,(-(long)money.currentMoney+c.price-1)/c.price);if(quantity==0)continue;
            long basis=quantity==c.shares?c.cost:(long)Math.Round((double)c.cost*quantity/c.shares);
            money.TransferInvestmentMoney(quantity*c.price);RealizedProfit+=(long)quantity*c.price-basis;c.cost-=basis;c.shares-=quantity;
        }
        Revision++;return money.currentMoney>=0;
    }
    public void DeliveryClue(){
        if(random.NextDouble()>.35)return;
        var active=Enumerable.Range(0,5).Where(i=>stories[i]!=null).ToArray();if(active.Length==0)return;
        int index=active[random.Next(active.Length)];var e=StockCatalog.Events[stories[index].eventIndex];
        LastClue=$"DAY {day:00} · 배달 중 들은 {StockCatalog.Names[index]} 소식\n“{(stories[index].good?"현장에서는 해결 방법을 찾는 분위기예요. 아직 발표 전이라 확실하진 않아요.":"현장 일정이 자꾸 밀린대요. 실제 손실 규모는 아직 모른다고 해요.")}”\n관련 보도: {e.title}";
        Revision++;
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool publishingTest;
    string testNotice;
    float testNoticeUntil;
    // 게임 일차와 시간 대신 뉴스 회차만 진행한다. 이후 정상 일차 진행도 유지한다.
    public void TestNewsRound(){
        if(Time.timeScale<=0)return;
        if(clock!=null&&day<clock.currentDay)AdvanceDay(clock.currentDay);
        publishingTest=true;
        try{PublishRound();}finally{publishingTest=false;}
        ShowTestNotice("F9 · 새 기사/후속 보도 생성 — PC 뉴스에서 확인");
    }
    public void TestPriceTicks(){
        if(Time.timeScale<=0)return;
        for(int i=0;i<10;i++)TickPrices();
        ShowTestNotice("F10 · 주가 10회 진행 — 핸드폰 차트에서 확인");
    }
    void ShowTestNotice(string text){testNotice=text;testNoticeUntil=Time.unscaledTime+3;Debug.Log("[주식 테스트] "+text);}
    void OnGUI(){
        if(Time.unscaledTime>=testNoticeUntil||Time.timeScale<=0)return;
        var old=GUI.depth;GUI.depth=-20000;
        var style=new GUIStyle(GUI.skin.box){font=DeliveryService.Mok.MokUIFont.Font,fontSize=Mathf.Max(16,Screen.height/45),alignment=TextAnchor.MiddleCenter};
        GUI.Box(new Rect(Screen.width*.15f,Screen.height*.09f,Screen.width*.7f,48),testNotice,style);GUI.depth=old;
    }
#endif
    void OnDestroy(){if(Instance==this)Instance=null;}
}}
