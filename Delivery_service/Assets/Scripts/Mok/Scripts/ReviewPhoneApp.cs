using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
public sealed class ReviewPhoneApp : MonoBehaviour {
 Text score,multiplier,notice,page,empty;Text[] reviews=new Text[3];Image[] cards=new Image[3];int selection,revision=-1;
 public static ReviewPhoneApp Build(Transform parent,Font font){
  var root=Box(parent,"Reviews App",0,0,540,960,new Color(.96f,.95f,.90f));var app=root.gameObject.AddComponent<ReviewPhoneApp>();
  Label(root,font,"우리 가게 리뷰",28,24,480,60,38);var icon=Box(root,"Star icon",28,100,96,96,new Color(0,0,0,0)).gameObject.AddComponent<Navigation.PhoneAppIcon>();icon.App=3;
  app.score=Label(root,font,"",144,100,350,65,48);app.multiplier=Label(root,font,"",28,208,484,44,28);
  Label(root,font,"최근 리뷰와 평판을 반영한 가게 평점",28,259,484,34,23);
  app.notice=Label(root,font,"",28,308,484,50,24);
  for(int i=0;i<3;i++){var card=Box(root,"Review card "+i,24,380+i*168,492,154,Color.white);app.cards[i]=card.GetComponent<Image>();app.reviews[i]=Label(card,font,"",18,12,456,132,26);}
  app.empty=Label(root,font,"배달을 완료하면\n손님들의 리뷰가 여기에 쌓여요.",30,400,480,150,30);
  app.page=Label(root,font,"",28,904,484,36,24);app.page.alignment=TextAnchor.MiddleRight;
  root.gameObject.SetActive(false);return app;
 }
 static RectTransform Box(Transform p,string n,int x,int y,int w,int h,Color color){var go=new GameObject(n,typeof(RectTransform));go.layer=p.gameObject.layer;go.transform.SetParent(p,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);if(color.a>0){go.AddComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;}return r;}
 static Text Label(Transform p,Font font,string text,int x,int y,int w,int h,int size){var r=Box(p,"Review text",x,y,w,h,Color.clear);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=new Color(.1f,.17f,.23f);t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=21;t.resizeTextMaxSize=size;return t;}
 public void Open(){selection=0;gameObject.SetActive(true);Refresh();}
 public void Move(int direction){var count=ShopReputation.Instance.Reviews.Count;if(count>0)selection=(selection+direction+count)%count;Refresh();}
 void Update(){if(revision!=ShopReputation.Instance.Revision)Refresh();}
 public void Refresh(){var shop=ShopReputation.Instance;revision=shop.Revision;score.text=$"{shop.Rating:0.00} / 5";multiplier.text=$"피자 판매가 {shop.Multiplier:0.00}배";notice.text=shop.LastNotice;
 var list=shop.Reviews;selection=Mathf.Clamp(selection,0,Mathf.Max(0,list.Count-1));empty.gameObject.SetActive(list.Count==0);page.text=list.Count==0?"리뷰 0개":$"{selection+1} / {list.Count}";
 for(int i=0;i<3;i++){int n=selection/3*3+i;cards[i].gameObject.SetActive(n<list.Count);if(n>=list.Count)continue;var review=list[n];cards[i].color=n==selection?new Color(1,.88f,.65f):Color.white;reviews[i].text=$"{review.address} · DAY {review.day}\n{review.stars}점 / 5 · {review.pizza}\n{review.text}";}
 }
}
}
