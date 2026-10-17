using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Stocks {
public static class StockDebtPrompt {
    public static IEnumerator Resolve(){
        var market=StockMarket.Instance;var money=MoneyManager.Instance;
        if(market==null||money==null||money.currentMoney>=0||market.PortfolioValue<=0)yield break;
        bool done=false;
        var root=new GameObject("주식 처분 확인",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
        var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);
        var panel=Element(root.transform,"Panel",0,0,1,1);panel.gameObject.AddComponent<Image>().color=new Color(.02f,.04f,.07f,.99f);
        var text=Element(panel,"Message",.15f,.42f,.85f,.75f).gameObject.AddComponent<Text>();text.font=MokUIFont.Font;text.fontSize=32;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;
        text.text=$"현금이 부족합니다\n현재 현금 {money.currentMoney:N0}원 · 주식 평가 {market.PortfolioValue:N0}원\n\n보유 주식을 팔아 부족분을 메울까요?\n전부 처분해도 부족하면 파산합니다.";
        Button(root.transform,"주식으로 정산",.2f,.28f,.48f,.39f,()=>{market.CoverDebt();done=true;});
        Button(root.transform,"처분하지 않고 파산",.52f,.28f,.8f,.39f,()=>done=true);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        while(!done)yield return null;
        Object.Destroy(root);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
    }
    static RectTransform Element(Transform parent,string name,float x0,float y0,float x1,float y1){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static void Button(Transform parent,string label,float a,float b,float c,float d,UnityEngine.Events.UnityAction action){var r=Element(parent,label,a,b,c,d);r.gameObject.AddComponent<Image>().color=new Color(.08f,.48f,.54f);r.gameObject.AddComponent<Button>().onClick.AddListener(action);var t=Element(r,"Text",0,0,1,1).gameObject.AddComponent<Text>();t.font=MokUIFont.Font;t.fontSize=27;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.text=label;t.raycastTarget=false;}
}}
