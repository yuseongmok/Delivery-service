using System;using System.Collections.Generic;using UnityEngine;
namespace DeliveryService.Mok {
// 화면과 분리한 정산 상태: 핸드폰을 닫거나 다른 앱을 열어도 같은 판이 계속된다.
public sealed class LadderGame : MonoBehaviour {
 public const float RevealSeconds=3f;
 public const int Step=1000;
 public bool Revealing {get;private set;} public bool HasResult {get;private set;}
 public int Bet {get;private set;} public int Choice {get;private set;} public int WinningExit {get;private set;}
 public int Destination=>Trace(Choice,Bridges);
 public bool Won=>HasResult&&Destination==WinningExit;
 public int Prize=>HasResult&&Won?Bet*2:0;
 public int PendingCredit {get;private set;}
 public int[] Bridges {get;private set;}=Array.Empty<int>();
 public Vector2[] Path {get;private set;}=Array.Empty<Vector2>();
 float started;System.Random random=new System.Random();
 public float Progress=>Revealing?Mathf.Clamp01((Time.unscaledTime-started)/RevealSeconds):HasResult?1:0;
 public static LadderGame Current {get{var money=MoneyManager.Instance;return money==null?null:money.GetComponent<LadderGame>()??money.gameObject.AddComponent<LadderGame>();}}
 public static int MaxBet(int cash)=>Mathf.Max(0,Mathf.Min(cash,int.MaxValue/2)/Step*Step);
 public static int Trace(int start,int[] bridges){foreach(int pair in bridges){if(start==pair)start++;else if(start==pair+1)start--;}return start;}
 public static Vector2 Node(int column,float y)=>new Vector2(.15f+column*.35f,y);
 public bool StartRound(int bet,int choice,out string message){
  if(Revealing||PendingCredit>0){message="진행 중인 정산이 있어요.";return false;}
  var money=GetComponent<MoneyManager>();
  if(choice<0||choice>2||bet<Step||bet%Step!=0||bet>MaxBet(money.currentMoney)){message="보유 현금 안에서 1,000원 단위로 선택해 주세요.";return false;}
  // 결과 생성과 차감을 한 번만 수행하고 이후 입력으로 바꾸지 않는다.
  Revealing=true;HasResult=false;
  if(!money.TransferGamblingMoney(-bet)){Revealing=false;message="베팅할 현금이 부족해요.";return false;}
  Bet=bet;Choice=choice;WinningExit=random.Next(3);Bridges=new int[8];for(int i=0;i<Bridges.Length;i++)Bridges[i]=random.Next(2);
  var points=new List<Vector2>{Node(choice,0)};int column=choice;
  for(int row=0;row<Bridges.Length;row++){float y=(row+1f)/9f;points.Add(Node(column,y));int next=Trace(column,new[]{Bridges[row]});if(next!=column){column=next;points.Add(Node(column,y));}}
  points.Add(Node(column,1));Path=points.ToArray();started=Time.unscaledTime;message="";return true;
 }
 public void CompleteRound(){
  if(!Revealing)return;
  Revealing=false;HasResult=true;
  if(Destination==WinningExit)PendingCredit=Bet*2;
  CollectPrize();
 }
 void CollectPrize(){
  if(PendingCredit<=0)return;var money=GetComponent<MoneyManager>();
  // 잔액의 정수 한도를 넘는 상금은 버리지 않고 현금 여유가 생기면 지급한다.
  int credit=(int)Math.Min(PendingCredit,(long)int.MaxValue-money.currentMoney);
  if(credit<=0)return;PendingCredit-=credit;
  if(!money.TransferGamblingMoney(credit))PendingCredit+=credit;
 }
 void Update(){if(Revealing&&Progress>=1)CompleteRound();else CollectPrize();}
}
}
