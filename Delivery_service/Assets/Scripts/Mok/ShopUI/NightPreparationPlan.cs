using System;
using UnityEngine;
namespace DeliveryService.Mok {
[CreateAssetMenu(menuName="Delivery Service/Night Preparation Plan")]
public sealed class NightPreparationPlan : ScriptableObject {
 [Serializable] public class Mission {
  public string id,title;public bool optional;
  [TextArea(2,5)] public string description;
  [TextArea(1,3)] public string result;
 }
 public string chapterTitle="첫 번째 사건 · 마지막 배달";
 [TextArea(2,4)] public string premise="조직의 의뢰로 은행을 털었지만 보수도 연락도 끊겼습니다. 낮에는 피자 가게로 위장하며 생활비와 준비 비용을 벌고, 밤마다 한 가지씩 단서를 쫓아 박진웅의 속내를 알아냅니다.";
 public Mission[] missions;
}
}
