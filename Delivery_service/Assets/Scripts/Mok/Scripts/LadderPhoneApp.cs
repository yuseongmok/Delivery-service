using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok {
public sealed class LadderPhoneApp : MonoBehaviour {
 public enum Page { Bet, Choose, Reveal, Result }
 public Page CurrentPage {get;private set;}
 public int Bet {get;private set;}=1000;public int Choice {get;private set;}
 Text balance,heading,amount,description,action,message;Text[] tops=new Text[3],ends=new Text[3];Image[] choices=new Image[3];LadderGraphic ladder;GameObject board;int cashCache=int.MinValue;LadderResultFeedback feedback;
 static readonly Color Ink=new(.10f,.17f,.23f),Gold=new(1,.70f,.32f);
 public static LadderPhoneApp Build(Transform parent,Font font){
  var root=Box(parent,"Ladder App",0,0,540,960,new Color(.96f,.95f,.90f));var app=root.gameObject.AddComponent<LadderPhoneApp>();
  var icon=Box(root,"Ladder logo",28,24,74,74,Color.clear).gameObject.AddComponent<Navigation.PhoneAppIcon>();icon.App=5;
  Label(root,font,"사다리",120,30,380,64,42);app.balance=Label(root,font,"",28,110,484,44,28);
  app.heading=Label(root,font,"",28,178,484,52,34);app.amount=Label(root,font,"",28,244,484,68,43);
  app.description=Label(root,font,"",28,326,484,130,28);
  app.board=Box(root,"Ladder board",28,330,484,430,Color.clear).gameObject;
  for(int i=0;i<3;i++){var tile=Box(app.board.transform,"Start "+i,24+i*170,0,96,54,Color.white);app.choices[i]=tile.GetComponent<Image>();app.tops[i]=Label(tile,font,(i+1).ToString(),0,0,96,54,32);app.tops[i].alignment=TextAnchor.MiddleCenter;
   app.ends[i]=Label(app.board.transform,font,"?",18+i*170,374,108,48,28);app.ends[i].alignment=TextAnchor.MiddleCenter;}
  app.ladder=Box(app.board.transform,"Paths",0,66,484,300,Color.clear).gameObject.AddComponent<LadderGraphic>();app.ladder.raycastTarget=false;
  var button=Box(root,"Action",28,790,484,68,Gold);app.action=Label(button,font,"",12,6,460,56,30);app.action.alignment=TextAnchor.MiddleCenter;
  app.message=Label(root,font,"",28,870,484,74,24);app.feedback=root.gameObject.AddComponent<LadderResultFeedback>();app.feedback.Initialize(app.heading,app.amount,app.board.GetComponent<RectTransform>());root.gameObject.SetActive(false);return app;
 }
 static RectTransform Box(Transform p,string n,int x,int y,int w,int h,Color c){var g=new GameObject(n,typeof(RectTransform));g.layer=p.gameObject.layer;g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);if(c.a>0){var im=g.AddComponent<Image>();im.color=c;im.raycastTarget=false;}return r;}
 static Text Label(Transform p,Font f,string s,int x,int y,int w,int h,int size){var t=Box(p,"Text",x,y,w,h,Color.clear).gameObject.AddComponent<Text>();t.font=f;t.text=s;t.color=Ink;t.fontSize=size;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=22;t.resizeTextMaxSize=size;return t;}
 public void Open(){gameObject.SetActive(true);var game=LadderGame.Current;CurrentPage=game!=null&&game.Revealing?Page.Reveal:game!=null&&game.HasResult?Page.Result:Page.Bet;message.text="";Refresh();}
 public void Move(int direction,bool horizontal){
  if(CurrentPage==Page.Bet&&!horizontal){int max=LadderGame.MaxBet(MoneyManager.Instance?.currentMoney??0);Bet=max>=1000?(int)System.Math.Clamp((long)Bet+direction*1000L,1000,max):1000;}
  if(CurrentPage==Page.Choose&&horizontal)Choice=(Choice+direction+3)%3;
  Refresh();
 }
 public bool Back(){if(CurrentPage==Page.Choose){CurrentPage=Page.Bet;Refresh();return true;}return false;}
 public void Confirm(){var game=LadderGame.Current;if(game==null)return;
  if(CurrentPage==Page.Bet){if(Bet>LadderGame.MaxBet(MoneyManager.Instance.currentMoney)){message.text="베팅할 현금이 부족해요.";return;}Choice=0;CurrentPage=Page.Choose;message.text="";}
  else if(CurrentPage==Page.Choose){if(game.StartRound(Bet,Choice,out var text))CurrentPage=Page.Reveal;message.text=text;}
  else if(CurrentPage==Page.Result){CurrentPage=Page.Bet;message.text="";}
  Refresh();
 }
 void Update(){var game=LadderGame.Current;if(game==null)return;
  if(CurrentPage==Page.Reveal){if(!game.Revealing&&game.HasResult){CurrentPage=Page.Result;Refresh();}else{ladder.Progress=game.Progress;ladder.SetVerticesDirty();}}
  if(cashCache!=(MoneyManager.Instance?.currentMoney??0))Refresh();
 }
 public void Refresh(){var game=LadderGame.Current;if(game==null)return;cashCache=MoneyManager.Instance.currentMoney;balance.text=$"내 돈  {cashCache:N0}원";
  if(CurrentPage==Page.Result)feedback.Show(game);else feedback.Clear();
  bool playing=CurrentPage==Page.Reveal||CurrentPage==Page.Result;int stake=playing?game.Bet:Bet;
  heading.text=CurrentPage==Page.Bet?"얼마를 걸까요?":CurrentPage==Page.Choose?"출발선을 선택하세요":CurrentPage==Page.Reveal?"사다리를 따라가는 중":game.Won?"당첨!":"꽝";
  amount.text=CurrentPage==Page.Result?(game.Won?$"{game.Prize:N0}원 지급":$"{stake:N0}원 잃었어요"):$"베팅  {stake:N0}원";
  description.gameObject.SetActive(CurrentPage==Page.Bet);description.text=$"당첨 시 {stake*2L:N0}원 반환\n세 곳 중 한 곳 당첨 · 확률 1/3\n꽝이면 베팅액 전부를 잃어요.\n대출 원금과 이자는 그대로 남아요.";
  board.SetActive(CurrentPage!=Page.Bet);ladder.Game=game;ladder.Reveal=playing;ladder.Progress=game.Progress;ladder.SetVerticesDirty();
  for(int i=0;i<3;i++){choices[i].color=i==(playing?game.Choice:Choice)?Gold:Color.white;ends[i].text=CurrentPage==Page.Result?(i==game.WinningExit?"당첨":"꽝"):"?";ends[i].color=CurrentPage==Page.Result&&i==game.WinningExit?new Color(.16f,.52f,.36f):Ink;}
  action.text=CurrentPage==Page.Bet?"금액 선택":CurrentPage==Page.Choose?"베팅하고 시작":CurrentPage==Page.Reveal?"결과 확인 중":"금액 선택으로 돌아가기";
  if(CurrentPage==Page.Result&&game.PendingCredit>0)message.text=$"잔액 한도 초과 상금 {game.PendingCredit:N0}원은 보관 중";
 }
}
}
