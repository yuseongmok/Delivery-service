using System;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Stocks {
// 한 화면에서 한 가지를 선택하는 초보자용 주식 화면.
public sealed class StockPhoneApp : MonoBehaviour {
    enum Page { Companies, Actions, Amount, Confirm, Chart, Result }
    Page page;
    Text header,balance,name,description,large,summary,footer,notice;
    Text[] choices=new Text[3];Image[] choiceBoxes=new Image[3];
    Text[] companyRows=new Text[5];Image[] companyBoxes=new Image[5];
    StockCompanyLogo companyLogo;
    RawImage chart;Texture2D graph;
    int selection,action,quantity=1,quote;bool buy=true;float refreshAge;string result="";
    static readonly Color Ink=new Color(.10f,.17f,.25f),Teal=new Color(.07f,.46f,.49f);
    readonly string[] descriptions={"오토바이를 만드는 회사","주유소와 연료를 공급하는 회사","물건을 배달하는 회사","치즈와 식재료를 만드는 회사","휴대기기를 만드는 회사"};
    public int Selection=>selection;public bool Detail=>page!=Page.Companies;public bool Confirming=>page==Page.Confirm;
    public int Quantity=>quantity;
    public static StockPhoneApp Build(Transform parent,Font font){
        var root=Box(parent,"Stock App",0,0,540,960,new Color(.91f,.95f,.94f));var app=root.gameObject.AddComponent<StockPhoneApp>();
        app.header=Label(root,font,28,40,484,50,32);app.balance=Label(root,font,28,100,484,48,30);
        var card=Box(root,"Company Card",22,172,496,430,Color.white);
        app.companyLogo=Rect(card,"Company Logo",20,20,72,72).gameObject.AddComponent<StockCompanyLogo>();app.companyLogo.raycastTarget=false;
        app.name=Label(card,font,108,25,368,65,42);app.description=Label(card,font,24,100,448,70,27);
        app.large=Label(card,font,24,190,448,75,44);app.summary=Label(card,font,24,285,448,125,29);
        for(int i=0;i<3;i++){var r=Box(root,"Choice "+i,28,626+i*58,484,50,Teal);app.choiceBoxes[i]=r.GetComponent<Image>();app.choices[i]=Label(r,font,12,0,460,50,29);app.choices[i].alignment=TextAnchor.MiddleCenter;}
        app.notice=Label(root,font,28,805,484,55,25);app.footer=Label(root,font,28,878,484,75,24);app.footer.gameObject.SetActive(false);
        var chartRect=Rect(card,"Optional Chart",24,180,448,150);app.chart=chartRect.gameObject.AddComponent<RawImage>();app.chart.raycastTarget=false;app.graph=new Texture2D(448,150,TextureFormat.RGBA32,false);app.chart.texture=app.graph;
        for(int i=0;i<5;i++){
            var row=Box(card,"Company Row "+i,12,12+i*82,472,76,Color.white);
            app.companyBoxes[i]=row.GetComponent<Image>();app.companyRows[i]=Label(row,font,88,8,370,65,28);
            var logo=Rect(row,"Logo",12,8,60,60).gameObject.AddComponent<StockCompanyLogo>();logo.Company=i;logo.raycastTarget=false;
        }
        root.gameObject.SetActive(false);return app;
    }
    static RectTransform Rect(Transform parent,string name,int x,int y,int w,int h){var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static RectTransform Box(Transform p,string n,int x,int y,int w,int h,Color color){var r=Rect(p,n,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
    static Text Label(Transform p,Font f,int x,int y,int w,int h,int size){var t=Rect(p,"Text",x,y,w,h).gameObject.AddComponent<Text>();t.font=f;t.fontSize=size;t.color=Ink;t.raycastTarget=false;return t;}
    public void Open(){page=Page.Companies;action=0;result="";gameObject.SetActive(true);Refresh();}
    public void Move(int direction,bool horizontal){
        if(page==Page.Companies&&!horizontal)selection=(selection-direction+5)%5;
        else if(page==Page.Actions&&!horizontal)action=(action-direction+2)%2;
        else if(page==Page.Amount&&!horizontal)quantity=Mathf.Clamp(quantity+direction,1,MaximumQuantity());
        result="";Refresh();
    }
    int MaximumQuantity(){var m=StockMarket.Instance;if(m==null)return 1;var c=m.Companies[selection];return Mathf.Clamp(buy?Mathf.Max(0,MoneyManager.Instance?.currentMoney??0)/c.price:c.shares,1,999);}
    bool Allowed(int price,out string reason){
        var m=StockMarket.Instance;reason="";if(m==null)return false;
        if(!m.MarketOpen){reason="가게를 개점하면 사고팔 수 있어요.";return false;}
        if(buy&&(long)price*quantity>(MoneyManager.Instance?.currentMoney??0)){reason="돈이 부족해요. 수량을 줄여주세요.";return false;}
        if(!buy&&m.Companies[selection].shares<quantity){reason="팔 수 있는 주식이 없어요.";return false;}return true;
    }
    public void Confirm(){
        var m=StockMarket.Instance;if(m==null)return;
        switch(page){
            case Page.Companies:page=Page.Actions;action=0;break;
            case Page.Actions:
                buy=action==0;quantity=1;page=Page.Amount;break;
            case Page.Amount:
                quote=m.Companies[selection].price;if(Allowed(quote,out result))page=Page.Confirm;break;
            case Page.Confirm:
                if(m.Trade(selection,quantity,buy,quote,out result)){result=$"{quantity}주 {(buy?"샀어요!":"팔았어요!")}";page=Page.Result;}
                else {result=result.Replace("현금","돈").Replace("보유 수량","가지고 있는 주식");page=Page.Amount;}break;
            case Page.Chart:page=Page.Actions;break;
            case Page.Result:page=Page.Companies;result="";break;
        }
        Refresh();
    }
    public bool Back(){
        if(page==Page.Companies)return false;
        page=page==Page.Confirm?Page.Amount:page==Page.Amount||page==Page.Chart?Page.Actions:Page.Companies;
        result="";Refresh();return true;
    }
    void Update(){refreshAge+=Time.unscaledDeltaTime;if(refreshAge>=.25f){refreshAge=0;Refresh();}}
    void Choice(int i,string text,bool selected,bool visible=true){choiceBoxes[i].gameObject.SetActive(visible);choices[i].text=(selected?">  ":"")+text;choiceBoxes[i].color=selected?Teal:new Color(.81f,.88f,.87f);choices[i].color=selected?Color.white:Ink;}
    public void Refresh(){
        var m=StockMarket.Instance;if(m==null)return;var c=m.Companies[selection];int cash=MoneyManager.Instance?.currentMoney??0;
        header.text=page==Page.Companies?$"회사 고르기    {selection+1} / 5":"나의 주식";balance.text=$"내 돈  {cash:N0}원";
        name.text=StockCatalog.Names[selection];description.text=descriptions[selection];large.color=Ink;
        bool list=page==Page.Companies,details=page==Page.Actions;
        companyLogo.Company=selection;companyLogo.gameObject.SetActive(!list);name.gameObject.SetActive(!list);description.gameObject.SetActive(!list);
        large.gameObject.SetActive(!list&&!details&&page!=Page.Chart);summary.gameObject.SetActive(!list&&page!=Page.Chart);
        chart.gameObject.SetActive(details||page==Page.Chart);
        summary.rectTransform.anchoredPosition=new Vector2(24,details?-345:-285);
        summary.rectTransform.sizeDelta=new Vector2(448,details?80:125);
        for(int i=0;i<5;i++){
            companyBoxes[i].gameObject.SetActive(list);var company=m.Companies[i];
            companyBoxes[i].color=i==selection?Teal:new Color(.94f,.97f,.96f);
            companyRows[i].color=i==selection?Color.white:Ink;
            companyRows[i].text=$"{(i==selection?">  ":"")}{StockCatalog.Names[i]}   {company.price:N0}원\n보유 {company.shares}주";
        }
        notice.text=result;footer.text="";
        for(int i=0;i<3;i++)Choice(i,"",false,false);
        long gain=(long)c.price*c.shares-c.cost;
        string profit=gain>0?$"지금 팔면 {gain:N0}원 이득":gain<0?$"지금 팔면 {-gain:N0}원 손해":"산 가격과 같아요";
        if(page==Page.Companies){
            Choice(0,"회사 선택",true);notice.text="회사 소식은 매장 PC 뉴스에서";footer.text="";
        }else if(page==Page.Actions){
            description.text=$"현재 {c.price:N0}원 · 보유 {c.shares}주";
            summary.text=c.shares>0?profit:"왼쪽은 과거, 오른쪽은 지금";
            DrawGraph(c);
            Choice(0,"사기",action==0);Choice(1,"팔기",action==1);footer.text="";
        }else if(page==Page.Amount||page==Page.Confirm){
            int price=page==Page.Confirm?quote:c.price;long total=(long)price*quantity;
            description.text=page==Page.Amount?(buy?"몇 주 살까요?":"몇 주 팔까요?"):"이대로 거래할까요?";
            large.text=$"{quantity}주";
            summary.text=$"{(buy?"매수":"매도")}  {total:N0}원";
            bool allowed=Allowed(price,out var reason);Choice(0,page==Page.Confirm?$"{total:N0}원에 {(buy?"사기":"팔기")}":"이 수량으로 계속",allowed);
            if(!allowed)notice.text=reason;
            footer.text="";
        }else if(page==Page.Chart){
            description.text=$"현재 1주 {c.price:N0}원\n왼쪽은 과거, 오른쪽은 지금이에요.";DrawGraph(c);Choice(0,"돌아가기",true);notice.text="가격은 오르기도, 내리기도 해요.";
        }else{
            description.text="거래가 끝났어요";large.text=result;summary.text=$"내가 가진 주식 {c.shares}주\n남은 돈 {cash:N0}원";notice.text="";Choice(0,"다른 회사 보기",true);
        }
    }
    void DrawGraph(StockMarket.Company c){var pixels=new Color32[448*150];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(232,242,239,255);int min=c.price,max=c.price;foreach(int v in c.history){min=Math.Min(min,v);max=Math.Max(max,v);}if(min==max){min--;max++;}for(int x=0;x<448;x++){float p=(float)x/447*Math.Max(0,c.history.Count-1);int n=(int)p;float value=Mathf.Lerp(c.history[n],c.history[Math.Min(n+1,c.history.Count-1)],p-n);int y=Mathf.RoundToInt(10+(value-min)/(max-min)*128);for(int d=-1;d<=1;d++)pixels[(y+d)*448+x]=new Color32(18,135,140,255);}graph.SetPixels32(pixels);graph.Apply();}
    void OnDestroy(){if(graph!=null)Destroy(graph);}
}}
