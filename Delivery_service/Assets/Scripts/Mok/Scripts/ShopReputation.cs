using System;
using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
// 평판은 현재 플레이의 일차 사이에는 유지하고 새 게임 씬에서는 초기화한다.
public sealed class ShopReputation : MonoBehaviour {
 public sealed class Review {public string address,pizza,text;public int stars,day;public float minutes;public int payment;}
 static ShopReputation instance;
 public static ShopReputation Instance {get{if(instance==null){instance=FindFirstObjectByType<ShopReputation>();if(instance==null)instance=new GameObject("Shop Reputation").AddComponent<ShopReputation>();}return instance;}}
 public static event Action Changed;
 public readonly List<Review> Reviews=new();
 public float Rating {get;private set;}=3f;
 public string LastNotice {get;private set;}="첫 리뷰를 기다리고 있어요";
 public int Revision {get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){instance=null;Changed=null;}
 void Awake(){if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;}
 void OnDestroy(){if(instance==this)instance=null;}
 public static float PriceMultiplier(float rating){rating=Mathf.Clamp(rating,1,5);float[] levels={.15f,.5f,1f,1.35f,1.8f};int i=Mathf.Min(3,Mathf.FloorToInt(rating)-1);return Mathf.Lerp(levels[i],levels[i+1],rating-(i+1));}
 public float Multiplier=>PriceMultiplier(Rating);
 public static int DeliveryStars(float minutes,bool correct,bool baked,bool ruined){if(!correct||!baked||ruined)return 1;return minutes<=15?5:minutes<=30?4:minutes<=40?3:minutes<=50?2:1;}
 public void Penalize(bool arrest)=>ApplyPenalty(arrest?.2f:.1f,arrest?"경찰 체포":"소매치기");
 public void Declined()=>ApplyPenalty(.05f,"주문 거절");
 void ApplyPenalty(float amount,string reason){Rating=Mathf.Clamp(Rating-amount,1,5);LastNotice=reason+$" · 평점 -{amount:0.00}";Notify();DeliveryRequestNotice.Show(LastNotice);}
 static readonly string[][] Comments={
  new[]{"너무 오래 기다렸어요. 다음엔 다른 가게를 찾겠습니다.","배달이 너무 늦어서 식사 시간을 놓쳤어요.","기다리는 동안 기대가 다 사라졌어요."},
  new[]{"약속보다 많이 늦었어요. 배달에 신경 써 주세요.","피자는 맞지만 기다린 시간이 아쉬워요.","다음에는 조금 더 일찍 받았으면 좋겠어요."},
  new[]{"조금 늦었지만 주문한 피자는 잘 받았어요.","맛은 괜찮았는데 배달이 살짝 아쉽네요.","무난했어요. 다음에는 시간도 지켜 주세요."},
  new[]{"주문한 피자가 제시간에 도착했어요. 잘 먹겠습니다!","약속을 지켜 주셔서 좋았어요. 또 주문할게요.","원하던 피자를 잘 받았어요. 만족합니다!"},
  new[]{"이렇게 빨리 오다니! 다음에도 여기서 주문할게요.","빠른 배달에 주문도 정확해요. 별 다섯 개!","기다릴 틈도 없이 도착했어요. 정말 만족해요!"}};
 public Review Record(string address,string pizza,float minutes,bool correct,bool baked,bool ruined,int payment,int day){
  int stars=DeliveryStars(minutes,correct,baked,ruined);string text;
  if(!correct)text=new[]{"제가 주문한 피자가 아니에요. 정말 실망했습니다.","다른 피자가 왔어요. 주문 확인부터 해 주세요.","기다렸는데 주문과 다른 피자라니요. 다시 주문하기 어렵겠어요."}[UnityEngine.Random.Range(0,3)];
  else if(!baked)text="피자가 제대로 구워지지 않았어요. 먹을 수가 없네요.";
  else if(ruined)text="피자가 엉망으로 도착했어요. 배달 상태가 너무 나빠요.";
  else text=Comments[stars-1][UnityEngine.Random.Range(0,3)];
  if(!correct&&minutes>30)text+=" 배달도 늦었어요.";
  var review=new Review{address=address,pizza=pizza,minutes=minutes,stars=stars,text=text,payment=payment,day=day};Reviews.Insert(0,review);if(Reviews.Count>100)Reviews.RemoveAt(100);
  float next=Mathf.Lerp(Rating,stars,.25f);if(!correct||!baked||ruined)next=Mathf.Min(next,Rating-.8f);Rating=Mathf.Clamp(next,1,5);
  LastNotice=$"{address} · {stars}점 리뷰 도착";Notify();DeliveryRequestNotice.Show(LastNotice);return review;
 }
 void Notify(){Revision++;Changed?.Invoke();}
}
}
