using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok {
[DefaultExecutionOrder(1000)]
public sealed class CompactHUD : MonoBehaviour {
    DayNightCycle clock;OrderManager orders;IncomeFeedback feedback;
    Text sourceOrder,orderTitle,orderBody,timeLabel,cash,popup;
    RectTransform orderCard;GameObject vehicle;
    MotorcycleController[] bikes;
    readonly Image[] fills=new Image[2];readonly GameObject[] gauges=new GameObject[2];
    static readonly Color Cream=new Color(.98f,.95f,.86f),Orange=new Color(.95f,.57f,.25f),Danger=new Color(.94f,.29f,.22f);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Install();}
    static void Loaded(Scene s,LoadSceneMode m){Install();}
    static void Install(){if(FindFirstObjectByType<DayNightCycle>()==null||FindFirstObjectByType<OrderManager>()==null||FindFirstObjectByType<CompactHUD>()!=null)return;new GameObject("Compact gameplay HUD").AddComponent<CompactHUD>();}
    IEnumerator Start(){
        yield return null;
        clock=FindFirstObjectByType<DayNightCycle>();orders=FindFirstObjectByType<OrderManager>();
        feedback=FindFirstObjectByType<IncomeFeedback>();bikes=FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
        var old=FindFirstObjectByType<CurrentOrderUI>();
        if(old!=null)sourceOrder=typeof(CurrentOrderUI).GetField("currrentOrderText",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(old) as Text;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        orderCard=Card("Order",new Vector2(0,1),new Vector2(28,-28),new Vector2(290,126));
        orderTitle=Label(orderCard,18,12,254,30,25);orderBody=Label(orderCard,18,48,304,124,21);
        var time=Card("Time",Vector2.one,new Vector2(-28,-28),new Vector2(264,58));timeLabel=Label(time,16,12,232,34,26);timeLabel.alignment=TextAnchor.MiddleCenter;
        var balance=Card("Balance",Vector2.one,new Vector2(-28,-94),new Vector2(264,64));
        var currency=Label(balance,16,12,36,40,32);currency.text="$";currency.color=Orange;currency.alignment=TextAnchor.MiddleCenter;cash=Label(balance,56,13,190,38,29);cash.alignment=TextAnchor.MiddleRight;
        popup=Label(balance,-16,72,280,40,26);popup.alignment=TextAnchor.MiddleRight;
        vehicle=Card("Vehicle",Vector2.zero,new Vector2(28,28),new Vector2(270,150)).gameObject;
        string[] labels={"연료","내구도","피자 흔들림"};
        for(int i=0;i<2;i++){
            var row=Rect(vehicle.transform,"Gauge "+i,12,10+i*44,246,40);gauges[i]=row.gameObject;Icon(row,0,3,30,i);
            var label=Label(row,40,0,192,20,18);label.text=labels[i];
            var track=Rect(row,"Empty track",40,25,190,8).gameObject.AddComponent<Image>();track.color=new Color(.33f,.40f,.44f);
            fills[i]=Rect(track.transform,"Fill",0,0,190,8).gameObject.AddComponent<Image>();fills[i].color=Orange;fills[i].raycastTarget=false;
        }
        Hide(sourceOrder);Hide(clock.dayText);Hide(clock.timeText);
        if(feedback!=null){Hide(feedback.balanceText);Hide(feedback.popupText);}
        foreach(var bike in bikes){
            var fuel=bike.GetComponent<MotorcycleFuel>();var durability=Durability(bike);
            if(fuel)HideGauge(fuel.fuelGaugeUI);if(durability)HideGauge(durability.durabilityGaugeUI);
            foreach(var storage in bike.GetComponentsInChildren<BikePizzaStorage>(true))HideGauge(storage.shakeGaugeUI);
        }
    }
    // 본체와 분리된 내구도 오브젝트도 실제 주행 참조로 연결한다.
    static MotorcycleDurability Durability(MotorcycleController bike)=>bike.durabilitySystem!=null?bike.durabilitySystem:bike.GetComponentInChildren<MotorcycleDurability>(true);
    static void Hide(Graphic g){if(g!=null)g.enabled=false;}
    static void HideGauge(Image image){if(image==null)return;image.enabled=false;if(image.transform.parent!=null)foreach(var text in image.transform.parent.GetComponentsInChildren<Text>(true))text.enabled=false;}
    void LateUpdate(){using(DeliveryService.Mok.PerformanceMarkers.HUD.Auto()){
        if(timeLabel==null||clock==null)return;
        int minutes=Mathf.FloorToInt(clock.currentTime*60);timeLabel.text=$"DAY {clock.currentDay}  ·  {minutes/60:00}:{minutes%60:00}";
        string value=sourceOrder!=null?sourceOrder.text:"";var lines=value.Split('\n');
        bool active=orders!=null&&orders.HasActiveOrder;
        bool cancelled=value.Contains("주문 취소");
        orderTitle.text=active?orders.ActiveOrder.orderName:cancelled?"주문 취소":"주문 대기 중";
        orderTitle.color=cancelled?Danger:Orange;
        orderBody.text=active?PizzaRecipes.Title(orders.ActiveOrder)+"\n"+orders.CurrentTargetDeliveryID+"\n"+(lines.Length>3?lines[3]:""):cancelled?"피자를 폐기하세요":"";
        orderBody.color=active&&orders.ElapsedDeliveryMinutes>orders.RecommendedDeliveryMinutes?Orange:Cream;
        orderCard.sizeDelta=new Vector2(340,active?180:cancelled?92:54);
        cash.text=$"{Mathf.RoundToInt(feedback!=null?feedback.DisplayedBalance:MoneyManager.Instance!=null?MoneyManager.Instance.currentMoney:0):N0}원";
        cash.color=MoneyManager.Instance!=null&&MoneyManager.Instance.currentMoney<0?Danger:Cream;
        popup.text=feedback!=null&&feedback.PopupVisible?$"{feedback.PopupAmount:+#,0;-#,0;0}원":"";
        popup.color=feedback!=null&&feedback.PopupAmount<0?Danger:new Color(.57f,.86f,.65f);
        MotorcycleController current=null;foreach(var b in bikes)if(b!=null&&(b.isDriven||b.isRefueling)){current=b;break;}
        vehicle.SetActive(current!=null);if(current==null)return;
        var f=current.GetComponent<MotorcycleFuel>();var d=Durability(current);var cargo=current.GetComponentInChildren<BikePizzaStorage>(true);
        SetGauge(0,f?f.currentFuel/Mathf.Max(.001f,f.maxFuel):0,false);
        SetGauge(1,d?d.currentDurability/Mathf.Max(.001f,d.maxDurability):0,false);
        ((RectTransform)vehicle.transform).sizeDelta=new Vector2(270,106);
    }}
    void SetGauge(int i,float ratio,bool highDanger){ratio=Mathf.Clamp01(ratio);fills[i].rectTransform.sizeDelta=new Vector2(190*ratio,8);fills[i].color=(highDanger?ratio>.7f:ratio<.25f)?Danger:Orange;}
    RectTransform Card(string name,Vector2 anchor,Vector2 pos,Vector2 size){
        var r=Rect(transform,name,0,0,(int)size.x,(int)size.y);r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=pos;
        var bg=r.gameObject.AddComponent<HudShape>();bg.Kind=-1;bg.color=new Color(.055f,.10f,.15f,.86f);return r;
    }
    static RectTransform Rect(Transform p,string n,float x,float y,int w,int h){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Text Label(Transform p,int x,int y,int w,int h,int size){var t=Rect(p,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=MokUIFont.Font;t.fontSize=size;t.color=Cream;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=size;return t;}
    static void Icon(Transform p,int x,int y,int size,int kind){var i=Rect(p,"Icon",x,y,size,size).gameObject.AddComponent<HudShape>();i.Kind=kind;i.color=Orange;}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HudShape : MaskableGraphic {
    public int Kind;VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh){mesh=vh;vh.Clear();raycastTarget=false;
        if(Kind==-1){var r=rectTransform.rect;float rx=Mathf.Min(12/r.width,.2f),ry=Mathf.Min(12/r.height,.25f);var points=new Vector2[28];int k=0;for(int j=0;j<4;j++){float x=j==0||j==3?1-rx:rx,y=j<2?1-ry:ry;for(int i=0;i<=6;i++){float a=(j*90+i*15)*Mathf.Deg2Rad;points[k++]=new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry);}}Poly(points);return;}
        if(Kind==0){Q(.18f,.15f,.61f,.85f);Q(.64f,.20f,.74f,.65f);Q(.56f,.56f,.81f,.67f);Q(.77f,.54f,.86f,.79f);return;}
        if(Kind==1){Poly(new Vector2(.18f,.12f),new Vector2(.31f,.08f),new Vector2(.73f,.61f),new Vector2(.60f,.74f));Poly(new Vector2(.57f,.58f),new Vector2(.83f,.60f),new Vector2(.94f,.82f),new Vector2(.72f,.72f),new Vector2(.69f,.95f),new Vector2(.49f,.79f));return;}
        if(Kind==2){Poly(new Vector2(.15f,.8f),new Vector2(.87f,.8f),new Vector2(.51f,.12f));return;}
        var p=new Vector2[20];for(int i=0;i<20;i++){float a=i*Mathf.PI/10;p[i]=new Vector2(.5f+Mathf.Cos(a)*.39f,.5f+Mathf.Sin(a)*.39f);}Poly(p);
    }
    void Q(float x,float y,float r,float t)=>Poly(new Vector2(x,y),new Vector2(r,y),new Vector2(r,t),new Vector2(x,t));
    void Poly(params Vector2[] p){var r=GetPixelAdjustedRect();int start=mesh.currentVertCount;foreach(var v in p)mesh.AddVert(new Vector3(r.x+v.x*r.width,r.y+v.y*r.height),color,Vector2.zero);for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
}
}
