using UnityEngine;
public class FuelNozzle : MonoBehaviour {
    public enum NozzleState { OnPump, InHand, Refueling }
    public NozzleState currentState=NozzleState.OnPump;
    public Transform pumpResetPoint,playerHandPoint;
    public FuelCap currentTargetCap;
    public LineRenderer hoseRender;
    public Transform hoseStartPoint,hoseEndPoint;
    public int hoseResoulution=20;
    public float hoseSag=2f,refuelSpeed=15f;
    public int costPerSecond=600;
    private float costAccumulator;
    private int paid;
    private float startingFuel;
    void Start(){ReturnPump();}
    void Update(){DrawHose();TickFuel(Time.deltaTime);}
    // 먼저 결제 가능한 양을 계산해 잔액 부족이나 완충 시 초과 결제를 막는다.
    void TickFuel(float seconds){
        if(currentState!=NozzleState.Refueling||currentTargetCap==null||currentTargetCap.bikeFuelSystem==null)return;
        var fuel=currentTargetCap.bikeFuelSystem;var money=MoneyManager.Instance;
        if(fuel.currentFuel>=fuel.maxFuel)return;
        if(money==null||money.currentMoney<=0||refuelSpeed<=0){PickUpNozzle();return;}
        float unitPrice=Mathf.Max(0,costPerSecond)/refuelSpeed;
        float affordable=unitPrice>0?Mathf.Max(0,(money.currentMoney+paid-costAccumulator)/unitPrice):float.PositiveInfinity;
        float amount=Mathf.Min(refuelSpeed*Mathf.Max(0,seconds),fuel.maxFuel-fuel.currentFuel,affordable);
        float total=(Mathf.Min(fuel.maxFuel,fuel.currentFuel+amount)-startingFuel)*unitPrice;
        int charge=Mathf.Max(0,Mathf.CeilToInt(total-.001f)-paid);
        if(charge>0&&!money.SpendMoney(charge,ExpenseType.Fuel)){PickUpNozzle();return;}
        costAccumulator=total;paid+=charge;fuel.AddFuel(amount);
        if(money.currentMoney<=0&&fuel.currentFuel<fuel.maxFuel)PickUpNozzle();
    }
    void DrawHose(){
        if(hoseRender==null||hoseStartPoint==null||hoseEndPoint==null)return;
        int count=Mathf.Max(2,hoseResoulution);hoseRender.positionCount=count;hoseRender.useWorldSpace=true;
        Vector3 start=hoseStartPoint.position,end=hoseEndPoint.position,middle=(start+end)*.5f+Vector3.down*hoseSag;
        for(int i=0;i<count;i++){float t=i/(float)(count-1);hoseRender.SetPosition(i,(1-t)*(1-t)*start+2*(1-t)*t*middle+t*t*end);}
    }
    void Disconnect(){
        if(currentTargetCap!=null&&currentTargetCap.bikeFuelSystem!=null){var bike=currentTargetCap.bikeFuelSystem.GetComponent<MotorcycleController>();if(bike!=null){bike.isRefueling=false;bike.UpdateUIVisibility();}}
        currentTargetCap=null;costAccumulator=0;paid=0;
    }
    void Dock(Transform point,int layer){
        if(point!=null){transform.SetParent(point,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;Vector3 scale=point.lossyScale;transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));}
        foreach(var t in GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
    }
    public void PickUpNozzle(){if(playerHandPoint==null)return;Disconnect();currentState=NozzleState.InHand;Dock(playerHandPoint,2);}
    public void AttachToBike(FuelCap cap){
        if(currentState!=NozzleState.InHand||cap==null||cap.bikeFuelSystem==null||cap.NozzleAttachPoint==null)return;
        var bike=cap.bikeFuelSystem.GetComponent<MotorcycleController>();
        if(bike!=null&&bike.isDriven)return;
        if(MoneyManager.Instance==null||MoneyManager.Instance.currentMoney<=0||cap.bikeFuelSystem.currentFuel>=cap.bikeFuelSystem.maxFuel)return;
        Disconnect();startingFuel=cap.bikeFuelSystem.currentFuel;currentTargetCap=cap;currentState=NozzleState.Refueling;Dock(cap.NozzleAttachPoint,LayerMask.NameToLayer("Motorcycle"));
        if(bike!=null){bike.isRefueling=true;bike.UpdateUIVisibility();}
    }
    public void ReturnPump(){Disconnect();currentState=NozzleState.OnPump;Dock(pumpResetPoint,LayerMask.NameToLayer("Motorcycle"));}
    void OnDisable(){Disconnect();}
}
