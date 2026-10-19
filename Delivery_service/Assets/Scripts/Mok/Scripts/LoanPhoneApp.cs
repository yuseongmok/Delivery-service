using System.Linq;
using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok {
public sealed class LoanPhoneApp : MonoBehaviour {
 public enum Page { Home, Borrow, Repay, Confirm }
 public Page CurrentPage {get;private set;}
 public int Selection {get;private set;}
 bool borrowing;int amount;Text summary,heading,details,message;Text[] labels;Image[] cards;int balanceCache=int.MinValue,principalCache=-1;
 static readonly Color Ink=new(.10f,.17f,.23f),Orange=new(.96f,.72f,.40f);
 int Day=>FindFirstObjectByType<DayNightCycle>()?.currentDay??1;
 int[] Choices=>CurrentPage==Page.Repay?new[]{1000,10000,LoanAccount.Current?.Principal??0}.Where(x=>x>0&&x<=(LoanAccount.Current?.Principal??0)).Distinct().ToArray():new[]{10000,30000,50000};
 public static LoanPhoneApp Build(Transform parent,Font font){
  var root=Box(parent,"Loan App",0,0,540,960,new Color(.96f,.95f,.90f));var app=root.gameObject.AddComponent<LoanPhoneApp>();
  var icon=Box(root,"Loan logo",28,24,74,74,Color.clear).gameObject.AddComponent<Navigation.PhoneAppIcon>();icon.App=4;
  TextAt(root,font,"급전",120,28,380,68,42);app.summary=TextAt(root,font,"",28,128,484,166,29);
  app.heading=TextAt(root,font,"",28,324,484,52,34);app.details=TextAt(root,font,"",28,392,484,180,27);
  app.labels=new Text[3];app.cards=new Image[3];
  for(int i=0;i<3;i++){var row=Box(root,"Choice "+i,28,590+i*80,484,66,Color.white);app.cards[i]=row.GetComponent<Image>();app.labels[i]=TextAt(row,font,"",20,8,444,50,30);}
  app.message=TextAt(root,font,"",28,844,484,80,25);root.gameObject.SetActive(false);return app;
 }
 static RectTransform Box(Transform p,string n,int x,int y,int w,int h,Color c){var g=new GameObject(n,typeof(RectTransform));g.layer=p.gameObject.layer;g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);if(c.a>0){var im=g.AddComponent<Image>();im.color=c;im.raycastTarget=false;}return r;}
 static Text TextAt(Transform p,Font f,string s,int x,int y,int w,int h,int size){var t=Box(p,"Text",x,y,w,h,Color.clear).gameObject.AddComponent<Text>();t.font=f;t.text=s;t.color=Ink;t.fontSize=size;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=22;t.resizeTextMaxSize=size;return t;}
 public void Open(){CurrentPage=Page.Home;Selection=0;message.text="";gameObject.SetActive(true);Refresh();}
 public void Move(int delta){int count=CurrentPage==Page.Home||CurrentPage==Page.Confirm?2:Choices.Length;if(count>0)Selection=(Selection+delta+count)%count;Refresh();}
 public bool Back(){if(CurrentPage==Page.Home)return false;CurrentPage=CurrentPage==Page.Confirm?(borrowing?Page.Borrow:Page.Repay):Page.Home;Selection=0;message.text="";Refresh();return true;}
 public void Confirm(){var loan=LoanAccount.Current;if(loan==null)return;
  if(CurrentPage==Page.Home){borrowing=Selection==0;CurrentPage=borrowing?Page.Borrow:Page.Repay;Selection=0;message.text="";}
  else if(CurrentPage==Page.Confirm){if(Selection==1){Back();return;}string result;bool ok=borrowing?loan.Borrow(amount,Day,out result):loan.Repay(amount,Day,out result);message.text=result;if(ok){CurrentPage=Page.Home;Selection=0;}}
  else {var choices=Choices;if(choices.Length==0)return;amount=choices[Mathf.Clamp(Selection,0,choices.Length-1)];borrowing=CurrentPage==Page.Borrow;CurrentPage=Page.Confirm;Selection=0;message.text="";}
  Refresh();
 }
 void Update(){var loan=LoanAccount.Current;int cash=MoneyManager.Instance?.currentMoney??0;if(cash!=balanceCache||(loan!=null&&loan.Principal!=principalCache))Refresh();}
 public void Refresh(){var loan=LoanAccount.Current;if(loan==null)return;balanceCache=MoneyManager.Instance.currentMoney;principalCache=loan.Principal;
  summary.text=$"내 돈  {balanceCache:N0}원\n빌린 원금  {loan.Principal:N0}원\n다음 날 이자  {loan.NextInterest:N0}원";
  string[] options;
  if(CurrentPage==Page.Home){heading.text="필요할 때만, 급전";details.text=$"남은 한도 {loan.Available:N0}원\n일일 이자 · 남은 원금의 20%\n원금은 직접 갚아야 해요.";options=new[]{"빌리기","갚기"};}
  else if(CurrentPage==Page.Confirm){heading.text=borrowing?"대출 확인":"상환 확인";int remaining=loan.Principal+(borrowing?amount:-amount);details.text=$"{amount:N0}원을 {(borrowing?"빌릴까요?":"갚을까요?")}\n거래 후 원금 {remaining:N0}원\n다음 날 이자 {(Mathf.Max(0,remaining)+4)/5:N0}원";options=new[]{"확정","취소"};}
  else {heading.text=CurrentPage==Page.Borrow?"얼마를 빌릴까요?":"얼마를 갚을까요?";details.text=CurrentPage==Page.Borrow?$"남은 한도 {loan.Available:N0}원\n다음 날부터 매일 이자가 나가요.\n잔액이 부족하면 파산할 수 있어요.":loan.Principal==0?"갚을 대출이 없어요.":"갚은 만큼 다음 이자가 줄어요.\n빌린 당일 전액 상환하면 이자 0원";options=Choices.Select(x=>$"{x:N0}원"+(CurrentPage==Page.Repay&&x==loan.Principal?" · 전액":"")).ToArray();}
  for(int i=0;i<3;i++){cards[i].gameObject.SetActive(i<options.Length);if(i>=options.Length)continue;labels[i].text=options[i];cards[i].color=i==Selection?Orange:Color.white;}
 }
}}
