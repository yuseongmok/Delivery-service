using System;using UnityEngine;
namespace DeliveryService.Mok {
// 새 게임의 MoneyManager와 수명을 공유하며 같은 일차의 중복 청구를 막는다.
public sealed class DailyRent : MonoBehaviour {
 public int LastSettledDay {get;private set;}
 public static DailyRent Current {get{var m=MoneyManager.Instance;return m==null?null:m.GetComponent<DailyRent>()??m.gameObject.AddComponent<DailyRent>();}}
 public static int Cost(int day)=>(int)Math.Min(int.MaxValue,30000L+Math.Max(0L,(long)day-1)*3000L);
 public int Due(int day)=>day<=LastSettledDay?0:Cost(day);
 public int SettleDay(int day){
  if(day<1||day<=LastSettledDay)return 0;
  int cost=Cost(day);if(!GetComponent<MoneyManager>().ChargeRent(cost))return 0;
  LastSettledDay=day;return cost;
 }
 public static int InterestDue(int day){var loan=LoanAccount.Current;return loan==null||loan.LastSettledDay>=day?0:loan.NextInterest;}
 public static long ProjectedBalance(int day)=>(long)(MoneyManager.Instance?.currentMoney??0)-(Current?.Due(day)??Cost(day))-InterestDue(day);
 public static string Preview(int day)=>$"임대료 −{Current?.Due(day)??Cost(day):N0}원   대출 이자 −{InterestDue(day):N0}원\n정산 후 예상 잔액  {ProjectedBalance(day):N0}원";
}
}
