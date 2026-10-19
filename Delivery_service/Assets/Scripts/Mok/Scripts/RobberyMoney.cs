using UnityEngine;
namespace DeliveryService.Mok {
public static class RobberyMoney {
 static float protectedUntil;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){protectedUntil=0;}
 // 여러 강도가 한 프레임에 연속 인출하지 못하도록 현금 강탈 보호시간을 공유한다.
 public static bool CanTake => MoneyManager.Instance!=null&&MoneyManager.Instance.currentMoney>0&&Time.unscaledTime>=protectedUntil;
 public static int TryTake(){
  if(!CanTake)return 0;
  int balance=MoneyManager.Instance.currentMoney;
  int amount=Mathf.Clamp((int)System.Math.Floor((double)balance*Random.Range(.1f,.2f)),1,balance);
  if(!MoneyManager.Instance.SpendMoney(amount,ExpenseType.Loss))return 0;
  protectedUntil=Time.unscaledTime+10f;return amount;
 }
}
}
