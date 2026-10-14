#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor {
public static class ShopPcOrdersInstaller {
static Font font;static Sprite rounded;
static readonly Color Navy=new Color32(25,43,66,255),Teal=new Color32(21,143,164,255),Pale=new Color32(242,246,249,255),Muted=new Color32(102,120,134,255);
[InitializeOnLoadMethod]static void Schedule(){if(File.Exists("Temp/Mok-orders-install-request"))EditorApplication.delayCall+=()=>{File.Delete("Temp/Mok-orders-install-request");Install();};}
static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
static Image Panel(string name,Transform parent,Color color,Vector2 min,Vector2 max){var r=Rect(name,parent,min,max);var i=r.gameObject.AddComponent<Image>();i.color=color;i.sprite=rounded;i.type=Image.Type.Sliced;return i;}
static Text Label(string name,Transform parent,string value,int size,Color color,Vector2 min,Vector2 max,TextAnchor align=TextAnchor.MiddleLeft){var r=Rect(name,parent,min,max);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;}
static Button Button(string name,Transform parent,string label,Color color,Color text,Vector2 min,Vector2 max){var p=Panel(name,parent,color,min,max);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;var colors=b.colors;colors.disabledColor=new Color(.65f,.7f,.72f,.5f);b.colors=colors;Label("Label",p.transform,label,26,text,Vector2.zero,Vector2.one,TextAnchor.MiddleCenter);return b;}
[MenuItem("Tools/Delivery Service/Mok/Install PC Order Queue")]
public static void Install(){
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
var pc=UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();var manager=pc.GetComponentInChildren<OrderManager>(true);
if(manager==null)throw new Exception("Existing PC OrderManager missing.");
var page=pc.pcUIPanel.transform.Find("Background/Page_0");
if(page.Find("MokOrderQueue")!=null)throw new Exception("Order queue already installed; preserve edits.");
font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");rounded=pc.pcUIPanel.GetComponentsInChildren<Image>(true).First(i=>i.sprite!=null).sprite;
foreach(Transform child in page){Undo.RecordObject(child.gameObject,"Hide old order placeholder");child.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);}
var root=Rect("MokOrderQueue",page,Vector2.zero,Vector2.one);Undo.RegisterCreatedObjectUndo(root.gameObject,"Install PC order queue");
var queue=root.gameObject.AddComponent<ShopPcOrderQueue>();queue.manager=manager;
Label("Heading",root,"주문 접수",48,Navy,new Vector2(.055f,.86f),new Vector2(.65f,.98f));
Label("Subtitle",root,"들어온 순서대로 확인하고 수락해 주세요",23,Muted,new Vector2(.057f,.80f),new Vector2(.75f,.87f));
var badge=Panel("CountBadge",root,Pale,new Vector2(.79f,.875f),new Vector2(.945f,.955f));queue.countLabel=Label("Count",badge.transform,"대기 00건",26,Teal,Vector2.zero,Vector2.one,TextAnchor.MiddleCenter);
var active=Panel("ActiveOrder",root,Pale,new Vector2(.055f,.70f),new Vector2(.945f,.785f));queue.activeLabel=Label("ActiveLabel",active.transform,"진행 중인 주문 없음",24,Navy,new Vector2(.025f,0),new Vector2(.98f,1));
var scrollRoot=Panel("OrdersScroll",root,Color.white,new Vector2(.055f,.12f),new Vector2(.945f,.675f));
queue.scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();queue.scroll.horizontal=false;queue.scroll.vertical=true;queue.scroll.movementType=ScrollRect.MovementType.Clamped;queue.scroll.scrollSensitivity=38;
var viewport=Rect("Viewport",scrollRoot.transform,Vector2.zero,new Vector2(.98f,1));viewport.gameObject.AddComponent<RectMask2D>();
queue.content=Rect("Content",viewport,new Vector2(0,1),Vector2.one);queue.content.pivot=new Vector2(.5f,1);
var layout=queue.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=14;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.padding=new RectOffset(0,10,0,4);
var fitter=queue.content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
queue.scroll.viewport=viewport;queue.scroll.content=queue.content;
var track=Panel("Scrollbar",scrollRoot.transform,Pale,new Vector2(.985f,0),Vector2.one);var handle=Panel("Handle",track.transform,new Color32(175,191,200,255),Vector2.zero,Vector2.one);var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;queue.scroll.verticalScrollbar=scrollbar;
queue.emptyLabel=Label("Empty",viewport,"대기 중인 주문이 없습니다.\n영업을 시작하면 주문이 순서대로 표시됩니다.",28,Muted,new Vector2(.03f,.2f),new Vector2(.97f,.85f),TextAnchor.MiddleCenter);
queue.hintLabel=Label("Hint",root,"새 주문은 목록 아래에 추가됩니다.",21,Muted,new Vector2(.055f,.025f),new Vector2(.945f,.10f));
var card=Panel("OrderRowTemplate",root,Pale,Vector2.zero,Vector2.one);card.gameObject.SetActive(false);var le=card.gameObject.AddComponent<LayoutElement>();le.preferredHeight=112;
var row=card.gameObject.AddComponent<ShopPcOrderRow>();row.number=Label("OrderNumber",card.transform,"주문 01",32,Navy,new Vector2(.035f,.48f),new Vector2(.56f,.91f));row.description=Label("Description",card.transform,"피자 1판 · 자유 토핑",23,Muted,new Vector2(.035f,.09f),new Vector2(.57f,.48f));
row.decline=Button("Decline",card.transform,"거절",Color.white,Muted,new Vector2(.625f,.23f),new Vector2(.77f,.77f));row.accept=Button("Accept",card.transform,"수락",Teal,Color.white,new Vector2(.795f,.23f),new Vector2(.965f,.77f));queue.template=row;
EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);EditorSceneManager.SaveScene(pc.gameObject.scene);
Debug.Log("MOK_ORDER_QUEUE_INSTALLED: existing OrderManager and timer connected to saved PC order tab.");
}
}}
#endif
