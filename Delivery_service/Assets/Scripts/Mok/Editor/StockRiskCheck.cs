#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.Stocks;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class StockRiskCheck {
    const string Key="Mok.Stock.RiskCheck";
    static double start;static int stage;static string log;
    static GameObject root;static StockPhoneNews news;static StockMarket market;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static StockRiskCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    static void Poll(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-stock-risk-request")){
            File.Delete("Temp/Mok-stock-risk-request");
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5)return;
        try{
            if(stage==0){stage=1;Run();start=EditorApplication.timeSinceStartup;}
            else if(stage==1&&EditorApplication.timeSinceStartup-start>2){
                Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Temp/Mok-stock-risk-news.png");stage=2;start=EditorApplication.timeSinceStartup;
            }else if(stage==2&&EditorApplication.timeSinceStartup-start>1){
                news.Confirm();Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Temp/Mok-stock-risk-article.png");stage=3;start=EditorApplication.timeSinceStartup;
            }else if(stage==3&&EditorApplication.timeSinceStartup-start>1)Finish();
        }catch(Exception e){log+="FAIL: "+e+"\n";Finish();}
    }
    static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){stage=0;log="";SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
        if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+".bg",false);}}
    static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);log+="PASS: "+msg+"\n";}
    static void Run(){
        root=new GameObject("Stock verification");market=root.AddComponent<StockMarket>();market.Initialize(42);
        int rounds=0,ups=0,downs=0,emergencies=0;market.NewsPublished+=()=>rounds++;
        market.ArticlePublished+=a=>{if(a.priceChange>.01f)ups++;if(a.priceChange<-.01f)downs++;if(Mathf.Abs(a.priceChange)>=.20f)emergencies++;};
        market.AdvanceDay(1);
        Check(rounds==1&&market.Articles.Count==3,"첫 뉴스 묶음 알림 1회");
        var alerts=root.GetComponent<StockAlerts>();Check(alerts!=null&&!string.IsNullOrEmpty(alerts.Message),"게임 HUD 알림 연결");
        for(int d=2;d<=60;d++){market.AdvanceDay(d);for(int i=0;i<20;i++)market.TickPrices();}
        Check(ups>0&&downs>0&&emergencies>0,"뉴스 결과 급등·급락과 20% 이상 긴급 사건");
        Check(market.Companies.All(c=>c.price>=10&&c.price<=1000000&&c.history.Count<=100),"가격 하한·상한 및 차트 기록 제한");
        for(int i=0;i<5;i++){var c=market.Companies[i];Check(c.price>=Mathf.Max(10,Mathf.RoundToInt(c.openingPrice*(1-StockCatalog.DailyLimit[i])))&&c.price<=Mathf.Min(1000000,Mathf.RoundToInt(c.openingPrice*(1+StockCatalog.DailyLimit[i]))),"기업 "+i+" 일간 제한");}
        // 양방향 긴급 알림이 실제 기사 변동률을 사용한다.
        var publish=typeof(StockMarket).GetMethod("AddArticle",Flags);var round=typeof(StockAlerts).GetMethod("Round",Flags);
        publish.Invoke(market,new object[]{4,"호재 확정","계약 체결",true,.35f});round.Invoke(alerts,null);
        Check(alerts.Emergency&&alerts.Message.Contains("급등"),"급등 긴급 알림");
        publish.Invoke(market,new object[]{2,"협상 결렬","협상이 결렬돼 예상 매출 달성에 차질이 생겼다.",true,-.32f});round.Invoke(alerts,null);
        Check(alerts.Emergency&&alerts.Message.Contains("급락"),"급락 긴급 알림");
        var canvas=new GameObject("News verification canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(root.transform);
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=30000;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
        var holder=new GameObject("Phone",typeof(RectTransform));holder.transform.SetParent(canvas.transform,false);
        var rect=holder.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(540,960);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(-270,480);
        news=StockPhoneNews.Build(holder.transform,DeliveryService.Mok.MokUIFont.Font);news.Open();news.Move(1);Check(news.Selection==1,"방향키 뉴스 선택");news.Confirm();Check(news.Detail,"기사 상세 진입");
        var before=string.Join("|",news.GetComponentsInChildren<Text>().Select(t=>t.text));
        publish.Invoke(market,new object[]{0,"새 소식","새 소식 본문",false,0f});
        Check(before==string.Join("|",news.GetComponentsInChildren<Text>().Select(t=>t.text)),"열람 중 새 뉴스가 기존 기사 변경하지 않음");
        Check(news.Back()&&!news.Detail&&!news.Back(),"기사 목록 복귀와 홈 복귀 구분");
        Check(news.GetComponentsInChildren<Text>().All(t=>t.font==DeliveryService.Mok.MokUIFont.Font),"뉴스 전체 주아체");
        news.Open();news.Move(4);Canvas.ForceUpdateCanvases();
        var navRoot=new GameObject("Phone routing test",typeof(RectTransform));navRoot.transform.SetParent(root.transform);
        var display=(RectTransform)navRoot.transform;display.sizeDelta=new Vector2(540,960);
        var nav=navRoot.AddComponent<DeliveryService.Mok.Navigation.NavigationPlanner>();
        typeof(DeliveryService.Mok.Navigation.NavigationPlanner).GetMethod("Build",Flags).Invoke(nav,new object[]{display});
        nav.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Home);nav.MoveSelection(2);nav.Confirm();
        Check(nav.CurrentScreen==DeliveryService.Mok.Navigation.NavigationPlanner.Screen.News&&nav.GetComponentInChildren<StockPhoneNews>()!=null,"홈에서 뉴스 앱 진입");
        nav.Back();Check(nav.CurrentScreen==DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Home,"뉴스 앱에서 홈 복귀");
        nav.MoveSelection(1);nav.Confirm();Check(nav.CurrentScreen==DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Stocks,"기존 주식 앱 진입 유지");
        UnityEngine.Object.Destroy(navRoot);
    }
    static void Finish(){File.WriteAllText("Temp/Mok-stock-risk-check.txt",log);EditorApplication.ExitPlaymode();}
}
}
#endif
