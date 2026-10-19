using UnityEngine;
namespace DeliveryService.Mok {
// 주문별 권장 시간을 넘긴 뒤 30분에 걸쳐 최대 70%까지 감액한다.
public static class DeliveryPricing {
 public const float PromisedMinutes=30f,MaximumPenaltyMinutes=60f;
 public static float LateMultiplier(float elapsedMinutes,float promisedMinutes=PromisedMinutes)=>1f-.7f*Mathf.Clamp01((elapsedMinutes-promisedMinutes)/30f);
 public static int ApplyDelay(int price,float elapsedMinutes,float promisedMinutes=PromisedMinutes)=>Mathf.Max(0,Mathf.RoundToInt(price*LateMultiplier(elapsedMinutes,promisedMinutes)));
}
}
