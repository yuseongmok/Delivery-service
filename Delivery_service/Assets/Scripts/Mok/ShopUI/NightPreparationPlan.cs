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
 [TextArea(2,4)] public string premise="배달원으로 행사장에 들어가 박진웅의 접촉 상대를 확인해야 합니다. 낮에는 작전 자금을 마련하고 밤에는 잠입과 탈출을 준비합니다.";
 public Mission[] missions;
}
}
