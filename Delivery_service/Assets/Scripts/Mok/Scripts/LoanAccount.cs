using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
// 잔액 관리자와 같은 수명으로 유지하며 새 게임의 잔액 관리자에서는 빚도 0원이다.
public sealed class LoanAccount : MonoBehaviour {
 public const int Limit=50000, DailyRatePercent=20;
 public int Principal {get;private set;}
 public int Available=>Limit-Principal;
 public int NextInterest=>(Principal+4)/5;
 public int LastSettledDay {get;private set;}=-1;
 public sealed class Totals {public int Borrowed,Repaid,Interest;}
 readonly Dictionary<int,Totals> days=new();
 public static LoanAccount Current {get{var money=MoneyManager.Instance;if(money==null)return null;return money.GetComponent<LoanAccount>()??money.gameObject.AddComponent<LoanAccount>();}}
 public Totals ForDay(int day){if(!days.TryGetValue(day,out var value)){value=new Totals();days.Add(day,value);}return value;}
 public bool Borrow(int amount,int day,out string message){
  if(amount!=10000&&amount!=30000&&amount!=50000){message="대출 금액을 다시 선택해 주세요.";return false;}
  if(day<=LastSettledDay){message="오늘 정산이 끝났습니다.";return false;}
  if(amount>Available){message="남은 대출 한도를 초과했어요.";return false;}
  if(!GetComponent<MoneyManager>().TransferLoanMoney(amount,ExpenseType.LoanBorrow)){message="대출을 처리할 수 없습니다.";return false;}
  Principal+=amount;ForDay(day).Borrowed+=amount;message=$"{amount:N0}원 입금 완료";return true;
 }
 public bool Repay(int amount,int day,out string message){
  if(day<=LastSettledDay){message="오늘 정산이 끝났습니다.";return false;}
  if(amount<=0||amount>Principal){message="상환할 금액을 다시 선택해 주세요.";return false;}
  if(!GetComponent<MoneyManager>().TransferLoanMoney(-amount,ExpenseType.LoanRepayment)){message="갚을 현금이 부족해요.";return false;}
  Principal-=amount;ForDay(day).Repaid+=amount;message=$"{amount:N0}원 상환 완료";return true;
 }
 public int SettleDay(int endingDay){
  if(endingDay<=LastSettledDay)return 0;
  int interest=NextInterest;
  if(interest>0&&!GetComponent<MoneyManager>().TransferLoanMoney(-interest,ExpenseType.LoanInterest))return 0;
  LastSettledDay=endingDay;ForDay(endingDay).Interest+=interest;
  return interest;
 }
}
}
