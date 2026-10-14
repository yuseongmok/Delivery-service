#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
using DeliveryService.Yun.ShopUI;

namespace DeliveryService.Mok.Editor
{
    public static class ShopPcBusinessInstaller
    {
        static Font font;
        static Sprite rounded;
        static readonly Color Navy = new Color32(25,43,66,255), Teal = new Color32(21,143,164,255), Pale = new Color32(242,246,249,255), Muted = new Color32(102,120,134,255);
        [InitializeOnLoadMethod] static void Schedule()
        {
            if (!File.Exists("Temp/Mok-business-install-request")) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete("Temp/Mok-business-install-request");
                try { Install(); } catch (Exception e) { File.WriteAllText("Temp/Mok-business-install.txt", e.ToString()); Debug.LogException(e); }
            };
        }
        static void Place(RectTransform r, float x0,float y0,float x1,float y1)
        {
            r.anchorMin=new Vector2(x0,y0); r.anchorMax=new Vector2(x1,y1); r.offsetMin=r.offsetMax=Vector2.zero;
        }
        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false); Place(r,x0,y0,x1,y1); return r;
        }
        static RectTransform Panel(string name,Transform parent,Color color,float x0,float y0,float x1,float y1)
        {
            var r=Rect(name,parent,x0,y0,x1,y1); var i=r.gameObject.AddComponent<Image>();
            i.color=color; i.sprite=rounded; i.type=Image.Type.Sliced; return r;
        }
        static Text Label(string name,Transform parent,string value,int size,Color color,float x0,float y0,float x1,float y1,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var r=Rect(name,parent,x0,y0,x1,y1); var t=r.gameObject.AddComponent<Text>();
            t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false; return t;
        }
        static Text Metric(Transform parent,string name,string title,float left,float right,Color ink)
        {
            var card=Panel(name,parent,Pale,left,.60f,right,.79f);
            Label("Title",card,title,25,Muted,.08f,.57f,.92f,.90f);
            return Label("Value",card,"0원",42,ink,.08f,.10f,.94f,.58f);
        }
        static Text Detail(Transform parent,string name,string title,float bottom)
        {
            Label(name+"Title",parent,title,27,Navy,.035f,bottom,.61f,bottom+.17f);
            return Label(name+"Value",parent,"0원",28,Navy,.62f,bottom,.965f,bottom+.17f,TextAnchor.MiddleRight);
        }
        [MenuItem("Tools/Delivery Service/Mok/Install Opening and Closing Dashboard")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            var pc=UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
            var view=pc.pcUIPanel.GetComponent<ShopPcUiView>();
            if(view.GetComponent<ShopPcBusinessDashboard>()!=null)throw new Exception("Dashboard already installed; preserve edits.");
            var cycle=UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.pcUIPanel==pc.pcUIPanel);
            EditorSceneManager.SaveScene(pc.gameObject.scene);
            File.Copy(pc.gameObject.scene.path,"Temp/Mok-business-before.unity",true);
            if (PrefabUtility.IsPartOfPrefabInstance(view.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(view.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
            rounded=view.GetComponentsInChildren<Image>(true).First(i=>i.sprite!=null).sprite;
            var bg=view.transform.Find("Background");var order=bg.Find("Page_0");var closing=bg.Find("Page_1");
            var open=closing.Find("OpenBusiness").GetComponent<Button>();var close=closing.Find("CloseBusiness").GetComponent<Button>();
            var ledger=Undo.AddComponent<ShopDayLedger>(pc.gameObject);ledger.cycle=cycle;
            var ui=Undo.AddComponent<ShopPcBusinessDashboard>(view.gameObject);ui.pc=pc;ui.view=view;ui.cycle=cycle;ui.ledger=ledger;
            ui.openButton=open;ui.closeButton=close;ui.orderList=order.Find("MokOrderQueue").gameObject;
            var tab=bg.Find("Sidebar/Tab_1").GetComponentInChildren<Text>(true);Undo.RecordObject(tab,"Rename closing tab");tab.text="마감";PrefabUtility.RecordPrefabInstancePropertyModifications(tab);

            var gate=Rect("MokOpeningGate",order,0,0,1,1);ui.openingGate=gate.gameObject;
            Undo.RegisterCreatedObjectUndo(gate.gameObject,"Create opening gate");
            Label("Heading",gate,"주문 접수",48,Navy,.055f,.86f,.945f,.98f);
            Label("Subtitle",gate,"개점 후 들어오는 주문을 확인할 수 있습니다.",23,Muted,.057f,.80f,.945f,.87f);
            var card=Panel("OpeningCard",gate,Pale,.16f,.24f,.84f,.69f);
            Label("Title",card,"오늘의 영업을 시작할까요?",38,Navy,.06f,.63f,.94f,.85f,TextAnchor.MiddleCenter);
            Label("Description",card,"개점하면 시간이 흐르고 주문 접수가 시작됩니다.",24,Muted,.05f,.43f,.95f,.62f,TextAnchor.MiddleCenter);
            Undo.SetTransformParent(open.transform,card,"Move opening button to order page");
            Undo.RecordObject(open.transform,"Place opening button");Place((RectTransform)open.transform,.25f,.13f,.75f,.34f);
            var openText=open.GetComponentInChildren<Text>(true);Undo.RecordObject(openText,"Rename opening button");openText.text="개점하기";
            PrefabUtility.RecordPrefabInstancePropertyModifications(open.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(openText);
            var stats=Rect("MokClosingDashboard",closing,0,0,1,1);Undo.RegisterCreatedObjectUndo(stats.gameObject,"Create closing statistics");
            Label("Heading",stats,"마감 정산",48,Navy,.055f,.86f,.63f,.98f);
            ui.periodLabel=Label("Period",stats,"오늘 개점부터 현재까지",23,Muted,.057f,.80f,.945f,.87f);
            ui.balanceLabel=Label("Balance",stats,"현재 잔액  —",25,Navy,.58f,.88f,.945f,.96f,TextAnchor.MiddleRight);
            ui.incomeLabel=Metric(stats,"Income","수입",.055f,.34f,Navy);
            ui.expenseLabel=Metric(stats,"Expense","총지출",.358f,.643f,Navy);
            ui.profitLabel=Metric(stats,"Profit","순수익",.661f,.945f,Teal);
            Label("BreakdownHeading",stats,"지출 상세",30,Navy,.055f,.50f,.945f,.58f);
            var detail=Panel("ExpenseBreakdown",stats,Pale,.055f,.19f,.945f,.50f);
            ui.ingredientLabel=Detail(detail,"Ingredients","재료비",.77f);
            ui.fuelLabel=Detail(detail,"Fuel","주유비",.54f);
            ui.lossLabel=Detail(detail,"Loss","벌금 · 도난",.31f);
            ui.otherLabel=Detail(detail,"Other","기타 지출",.08f);
            ui.statusLabel=Label("Status",stats,"개점 전입니다. 주문 탭에서 개점해 주세요.",22,Muted,.055f,.045f,.65f,.15f);
            Undo.RecordObject(close.transform,"Place closing button");Place((RectTransform)close.transform,.71f,.035f,.945f,.14f);
            close.transform.SetAsLastSibling();PrefabUtility.RecordPrefabInstancePropertyModifications(close.transform);
            ui.Refresh();view.SelectTab(0);
            EditorUtility.SetDirty(ui);EditorUtility.SetDirty(ledger);
            EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);EditorSceneManager.SaveScene(pc.gameObject.scene);
            File.WriteAllText("Temp/Mok-business-install.txt","PASS: saved opening gate and closing dashboard; original buttons and serialized callbacks retained; runtime ledger on PC.");
            Debug.Log("MOK_BUSINESS_DASHBOARD_INSTALLED");
        }
    }
}
#endif
