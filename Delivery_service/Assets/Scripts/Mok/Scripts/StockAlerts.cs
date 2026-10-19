using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok.Stocks {
// 한 뉴스 묶음은 한 번만 알리고 급등락은 우선 표시한다.
public sealed class StockAlerts : MonoBehaviour {
    StockMarket market; AudioSource source; AudioClip normal,urgent;
    readonly List<string> emergencies=new List<string>();
    string message;float remaining;bool emergency;
    public string Message=>message;
    public bool Emergency=>emergency;
    void Awake(){
        market=GetComponent<StockMarket>();market.ArticlePublished+=Article;market.NewsPublished+=Round;
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
        normal=Tone(880,1);urgent=Tone(1200,3);
    }
    AudioClip Tone(float hz,int pulses){
        int rate=22050;var data=new float[(int)(rate*.18f*pulses)];
        for(int i=0;i<data.Length;i++){float t=(float)i/rate;float phase=t%.18f;data[i]=phase<.12f?Mathf.Sin(2*Mathf.PI*hz*t)*.16f*Mathf.Sin(Mathf.PI*phase/.12f):0;}
        var clip=AudioClip.Create("Stock notification",data.Length,1,rate,false);clip.SetData(data,0);return clip;
    }
    void Article(StockMarket.Article article){
        if(Mathf.Abs(article.priceChange)+.00001f<StockCatalog.EmergencyThreshold)return;
        emergencies.Add($"{StockCatalog.Names[article.company]} {(article.priceChange>0?"급등":"급락")} {article.priceChange*100:+0.0;-0.0;0.0}%");
    }
    void Round(){
        emergency=emergencies.Count>0;
        message=emergency?"긴급 시세 알림\n"+string.Join(" · ",emergencies):"새 기업 뉴스가 도착했습니다\n핸드폰 · 매장 PC에서 확인";
        emergencies.Clear();remaining=emergency?7:4;
        source.PlayOneShot(emergency?urgent:normal,PlayerPrefs.GetFloat("Yun.Settings.SfxVolume",1));
    }
    void Update(){if(Time.timeScale>0)remaining-=Time.unscaledDeltaTime;}
    void OnGUI(){
        if(remaining<=0||Time.timeScale<=0)return;
        var matrix=GUI.matrix;int depth=GUI.depth;var color=GUI.color;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1920f,Screen.height/1080f,1));GUI.depth=-15000;
        GUI.color=new Color(1,1,1,Mathf.Clamp01(remaining));
        var style=new GUIStyle(GUI.skin.box){font=DeliveryService.Mok.MokUIFont.Font,fontSize=25,wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(16,16,10,10)};
        style.normal.textColor=emergency?new Color(1,.65f,.24f):Color.white;
        GUI.Box(new Rect(1120,180,740,110),message,style);
        GUI.matrix=matrix;GUI.depth=depth;GUI.color=color;
    }
    void OnDestroy(){if(market!=null){market.ArticlePublished-=Article;market.NewsPublished-=Round;}if(normal!=null)Destroy(normal);if(urgent!=null)Destroy(urgent);}
}
}
