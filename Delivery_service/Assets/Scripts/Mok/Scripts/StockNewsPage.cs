using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Stocks {
public sealed class StockNewsPage : MonoBehaviour {
    public Text heading,article,clue,position;
    public Button previous,next;
    int index;
    void Awake(){previous.onClick.AddListener(()=>{index++;Refresh();});next.onClick.AddListener(()=>{index--;Refresh();});}
    void OnEnable(){Refresh();}
    void Update(){Refresh();}
    public void Refresh(){
        var m=StockMarket.Instance;if(m==null||m.Articles.Count==0)return;
        index=Mathf.Clamp(index,0,m.Articles.Count-1);var a=m.Articles[index];
        heading.text=$"도시 경제 뉴스   /   {StockCatalog.Names[a.company]}";
        article.text=$"<color=#168FA4>DAY {a.day:00}  06:00 · {(a.followup?"후속 보도":"첫 보도")}</color>\n\n<b>{a.title}</b>\n\n{a.body}";
        clue.text="배달 현장 메모\n"+m.LastClue;
        position.text=$"{index+1} / {m.Articles.Count}  ·  최신순";
        previous.interactable=index<m.Articles.Count-1;next.interactable=index>0;
    }
}}
