#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Yun.ShopUI;
using DeliveryService.Mok.Stocks;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class StockInstaller {
    static StockInstaller(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-stock-install-request"))return;File.Delete("Temp/Mok-stock-install-request");try{Install();File.WriteAllText("Temp/Mok-stock-install.txt","PASS: stock market and saved PC news tab installed");}catch(Exception e){File.WriteAllText("Temp/Mok-stock-install.txt",e.ToString());Debug.LogException(e);}}
    static Font font;
    static RectTransform Rect(Transform p,string name,float x0,float y0,float x1,float y1){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static Text Text(Transform p,string name,string value,int size,float x0,float y0,float x1,float y1){var t=Rect(p,name,x0,y0,x1,y1).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.1f,.17f,.26f);t.text=value;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string label,float x0,float y0,float x1,float y1){var r=Rect(p,label,x0,y0,x1,y1);r.gameObject.AddComponent<Image>().color=new Color(.08f,.56f,.64f);var b=r.gameObject.AddComponent<Button>();var t=Text(r,"Label",label,26,0,0,1,1);t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;return b;}
    [MenuItem("Tools/Delivery Service/Mok/Install Stock Market")]
    public static void Install(){
        var view=UnityEngine.Object.FindFirstObjectByType<ShopPcUiView>(FindObjectsInactive.Include);if(view==null)throw new Exception("PC UI missing");
        EditorSceneManager.SaveScene(view.gameObject.scene);File.Copy(view.gameObject.scene.path,"Temp/Mok-stock-before.unity",true);
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
        if(UnityEngine.Object.FindFirstObjectByType<StockMarket>()==null)new GameObject("Mok Stock Market").AddComponent<StockMarket>();
        var bg=view.transform.Find("Background");var existing=bg.Find("Page_3");
        if(existing==null){
            var page=Rect(bg,"Page_3",.18f,.075f,1,.94f);page.gameObject.AddComponent<Image>().color=Color.white;
            var news=page.gameObject.AddComponent<StockNewsPage>();
            news.heading=Text(page,"Heading","도시 경제 뉴스",36,.05f,.87f,.95f,.98f);
            news.article=Text(page,"Article","개점 전에도 오늘의 기사를 확인할 수 있습니다.",30,.06f,.35f,.94f,.85f);
            news.clue=Text(page,"Clue","배달 현장 메모",23,.06f,.13f,.94f,.34f);
            news.previous=Button(page,"이전 기사",.05f,.015f,.26f,.095f);news.next=Button(page,"최신 방향",.75f,.015f,.96f,.095f);
            news.position=Text(page,"Position","1 / 3",23,.30f,.015f,.70f,.095f);news.position.alignment=TextAnchor.MiddleCenter;
            var sidebar=bg.Find("Sidebar");var tab=Button(sidebar,"뉴스",.07f,.265f,.93f,.355f);
            var so=new SerializedObject(view);var pages=so.FindProperty("pages");var tabs=so.FindProperty("tabs");pages.arraySize=4;tabs.arraySize=4;pages.GetArrayElementAtIndex(3).objectReferenceValue=page.gameObject;tabs.GetArrayElementAtIndex(3).objectReferenceValue=tab;so.ApplyModifiedPropertiesWithoutUndo();page.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();
    }
}}
#endif
