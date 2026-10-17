using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 표시 연출만 담당하며 잔액과 통계는 MoneyManager가 관리한다.
public sealed class IncomeFeedback : MonoBehaviour {
    public Text balanceText, popupText;
    public AudioClip incomeSound;
    MoneyManager money;AudioSource source;
    float shown,from,target,elapsed=1,noticeAge=10;int amount;
    Vector3 scale;Vector2 popupPosition;
    static bool pickpocket;
    public int PopupAmount => amount;
    public float DisplayedBalance => shown;
    public bool PopupVisible => noticeAge<1.5f;
    public static void AddPickpocketIncome(int value){try{pickpocket=true;MoneyManager.Instance.AddMoney(value);}finally{pickpocket=false;}}
    void Start(){
        money=MoneyManager.Instance;if(money==null||balanceText==null)return;
        shown=from=target=money.currentMoney;scale=balanceText.transform.localScale;
        popupPosition=popupText.rectTransform.anchoredPosition;popupText.text="";
        source=GetComponent<AudioSource>();
        money.OnMoneyChanged+=BalanceChanged;money.TransactionRecorded+=Transaction;
    }
    void BalanceChanged(int value){from=shown;target=value;elapsed=0;}
    void Transaction(int delta,ExpenseType type){if(delta==0)return;
        amount=noticeAge<1.5f&&Mathf.Sign(amount)==Mathf.Sign(delta)?amount+delta:delta;noticeAge=0;
        if(source!=null&&incomeSound!=null){source.pitch=delta<0?.65f:pickpocket?1.12f:1f;source.PlayOneShot(incomeSound,pickpocket?.20f:.36f);}
    }
    void LateUpdate(){
        if(money==null||balanceText==null)return;
        elapsed+=Time.unscaledDeltaTime;noticeAge+=Time.unscaledDeltaTime;
        shown=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,elapsed/.5f));balanceText.text=$"₩{Mathf.RoundToInt(shown):N0}";
        float pulse=PopupVisible?Mathf.Sin(Mathf.Clamp01(noticeAge/.45f)*Mathf.PI)*.12f:0;
        balanceText.transform.localScale=scale*(1+pulse);
        if(popupText!=null){popupText.text=PopupVisible?$"{(amount>0?"+":"")}{amount:N0}원":"";Color tint=amount<0?new Color(1f,.36f,.32f):new Color(.45f,1f,.65f);tint.a=1-Mathf.InverseLerp(.8f,1.5f,noticeAge);popupText.color=tint;popupText.rectTransform.anchoredPosition=popupPosition+Vector2.up*Mathf.Clamp01(noticeAge/1.5f)*20;}
    }
    void OnDestroy(){if(money!=null){money.OnMoneyChanged-=BalanceChanged;money.TransactionRecorded-=Transaction;}if(balanceText!=null)balanceText.transform.localScale=scale;}
}
}
