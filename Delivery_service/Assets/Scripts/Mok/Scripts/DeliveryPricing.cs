using UnityEngine;
namespace DeliveryService.Mok {
// 수락 후 30분부터 60분까지 선형 감액한다. 지연 감액만으로는 70%를 넘지 않는다.
public static class DeliveryPricing {
 public const float PromisedMinutes=30f,MaximumPenaltyMinutes=60f;
 public static float LateMultiplier(float elapsedMinutes)=>1f-.7f*Mathf.Clamp01((elapsedMinutes-PromisedMinutes)/(MaximumPenaltyMinutes-PromisedMinutes));
 public static int ApplyDelay(int price,float elapsedMinutes)=>Mathf.Max(0,Mathf.RoundToInt(price*LateMultiplier(elapsedMinutes)));
}
}
