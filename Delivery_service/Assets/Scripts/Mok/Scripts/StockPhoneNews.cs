using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Stocks {
// 기사 데이터와 조작은 유지하고 뉴스 포털 형태로 표시한다.
public sealed class StockPhoneNews : MonoBehaviour {
    readonly List<StockMarket.Article> articles=new List<StockMarket.Article>();
    static readonly Color Ink=new Color(.10f,.17f,.23f),Muted=new Color(.40f,.46f,.49f),Accent=new Color(.08f,.47f,.48f);
    Text count,section,headline,byline,body,badge,impact,empty;
    GameObject detailPanel;
    Image[] cards=new Image[3],bars=new Image[3];
    Text[] titles=new Text[3],meta=new Text[3],tags=new Text[3];
    StockCompanyLogo[] logos=new StockCompanyLogo[3];
    StockCompanyLogo detailLogo;
    int selection; bool detail;
    public bool Detail=>detail;
    public int Selection=>selection;
    public static StockPhoneNews Build(Transform parent,Font font){
        var root=Box(parent,"News App",0,0,540,960,new Color(.94f,.95f,.94f));
        var app=root.gameObject.AddComponent<StockPhoneNews>();
        Box(root,"Address bar",0,0,540,60,new Color(.87f,.90f,.89f));
        Label(root,font,"CITY / news.local",28,16,484,34,23,Muted);
        Box(root,"Masthead",0,60,540,100,Ink);
        Label(root,font,"CITY NEWS",28,78,400,60,43,Color.white);
        Label(root,font,"도시의 소식, 투자의 단서",28,174,484,40,25,Muted);
        app.section=Label(root,font,"기업 · 최신 뉴스",28,226,484,46,32,Ink);
        Box(root,"Section line",28,280,484,4,Accent);
        for(int i=0;i<3;i++){
            var card=Box(root,"Article card "+i,24,304+i*180,492,164,Color.white);
            app.cards[i]=card.GetComponent<Image>();
            app.bars[i]=Box(card,"Selection",0,0,6,164,Accent).GetComponent<Image>();
            app.logos[i]=Rect(card,"Company icon",18,18,58,58).gameObject.AddComponent<StockCompanyLogo>();app.logos[i].raycastTarget=false;
            app.meta[i]=Label(card,font,"",90,18,380,35,24,Muted);
            app.tags[i]=Label(card,font,"",90,53,380,30,22,Accent);
            app.titles[i]=Label(card,font,"",18,91,454,66,29,Ink);
        }
        app.empty=Label(root,font,"아직 도착한 뉴스가 없습니다.",28,330,484,100,29,Muted);
        app.detailPanel=Box(root,"Article page",20,298,500,552,Color.white).gameObject;
        app.detailLogo=Rect(app.detailPanel.transform,"Publisher logo",20,20,60,60).gameObject.AddComponent<StockCompanyLogo>();app.detailLogo.raycastTarget=false;
        app.byline=Label(app.detailPanel.transform,font,"",96,20,384,32,25,Muted);
        app.badge=Label(app.detailPanel.transform,font,"",96,56,384,32,24,Accent);
        app.headline=Label(app.detailPanel.transform,font,"",20,104,460,110,36,Ink);
        Box(app.detailPanel.transform,"Divider",20,229,460,2,new Color(.88f,.91f,.90f));
        app.body=Label(app.detailPanel.transform,font,"",20,252,460,205,29,Ink);
        app.body.lineSpacing=1.15f;
        var quote=Box(app.detailPanel.transform,"Market reaction",20,477,460,58,new Color(.92f,.95f,.94f));
        app.impact=Label(quote,font,"",12,12,436,38,25,Accent);
        Box(root,"Footer divider",28,880,484,2,new Color(.82f,.86f,.85f));
        Label(root,font,"CITY ECONOMY",28,899,300,34,22,Muted);
        app.count=Label(root,font,"",350,899,162,34,24,Muted);app.count.alignment=TextAnchor.MiddleRight;
        root.gameObject.SetActive(false);return app;
    }
    static RectTransform Rect(Transform p,string n,int x,int y,int w,int h){
        var g=new GameObject(n,typeof(RectTransform));g.layer=p.gameObject.layer;g.transform.SetParent(p,false);
        var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static RectTransform Box(Transform p,string n,int x,int y,int w,int h,Color c){
        var r=Rect(p,n,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=c;image.raycastTarget=false;return r;
    }
    static Text Label(Transform p,Font f,string text,int x,int y,int w,int h,int size,Color c){
        var t=Rect(p,"News Text",x,y,w,h).gameObject.AddComponent<Text>();t.font=f;t.text=text;t.fontSize=size;t.color=c;t.raycastTarget=false;t.supportRichText=false;
        t.resizeTextForBestFit=true;t.resizeTextMinSize=22;t.resizeTextMaxSize=size;return t;
    }
    public void Open(){articles.Clear();if(StockMarket.Instance!=null)articles.AddRange(StockMarket.Instance.Articles);selection=0;detail=false;gameObject.SetActive(true);Refresh();}
    public void Move(int direction){if(detail||articles.Count==0)return;selection=(selection+direction+articles.Count)%articles.Count;Refresh();}
    public void Confirm(){if(articles.Count==0)return;detail=true;Refresh();}
    public bool Back(){if(!detail)return false;detail=false;Refresh();return true;}
    static string Category(StockMarket.Article a)=>!a.followup?"진행 중 · 결과 미정":Mathf.Abs(a.priceChange)>=StockCatalog.EmergencyThreshold?"긴급 · 결과 발표":"후속 · 결과 발표";
    void Refresh(){
        section.text=detail?"기업 · 기사 읽기":"기업 · 최신 뉴스";
        empty.gameObject.SetActive(articles.Count==0);detailPanel.SetActive(detail&&articles.Count>0);
        int start=selection/3*3;
        for(int i=0;i<3;i++){
            int index=start+i;bool show=!detail&&index<articles.Count;cards[i].gameObject.SetActive(show);if(!show)continue;
            var item=articles[index];bool selected=index==selection;
            cards[i].color=selected?new Color(.83f,.93f,.91f):Color.white;bars[i].gameObject.SetActive(selected);
            logos[i].Company=item.company;meta[i].text=$"{StockCatalog.Names[item.company]} · DAY {item.day:00}";
            tags[i].text=Category(item);titles[i].text=item.title;
        }
        count.text=articles.Count==0?"0 / 0":$"{selection+1} / {articles.Count}";
        if(!detail||articles.Count==0)return;
        var a=articles[selection];detailLogo.Company=a.company;
        byline.text=$"{StockCatalog.Names[a.company]} · DAY {a.day:00}";badge.text=Category(a);headline.text=a.title;
        // 가격 반응은 본문과 분리해 기사와 현재 시세를 혼동하지 않게 한다.
        int split=a.body.IndexOf("\n\n발표 직후 주가");
        body.text=split<0?a.body:a.body.Substring(0,split);
        impact.text=a.followup?$"발표 직후 {a.priceChange*100:+0.0;-0.0;0.0}%":"후속 보도를 기다리는 사건입니다";
        impact.color=a.followup&&a.priceChange<0?new Color(.28f,.40f,.68f):a.followup&&a.priceChange>0?new Color(.76f,.29f,.20f):Accent;
    }
}
}
