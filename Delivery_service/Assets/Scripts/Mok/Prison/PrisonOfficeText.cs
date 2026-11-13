using UnityEngine;
namespace DeliveryService.Mok {
[CreateAssetMenu(menuName="Delivery Service/감옥 간수장실 조사 대사")]
public sealed class PrisonOfficeText:ScriptableObject {
 [TextArea] public string start="민기: 잠들었어. 소리 내지 말고 간수복이랑 차량 열쇠를 찾아보자.";
 [TextArea] public string opened="나: 안에 뭔가 있어. 챙기자.";
 [TextArea] public string uniforms="나: 예비 간수복 두 벌이야. 우리 둘 다 입을 수 있겠어.";
 [TextArea] public string keys="민기: 호송차량 열쇠다. 잃어버리지 않게 챙겨.";
 public string password="푸른 까마귀";
 [TextArea] public string passwordLine="민기: 오늘의 암구호네. 정문에서 물어볼 수도 있으니 기억해 둬.";
 [TextArea] public string complete="민기: 간수복에 차량 열쇠, 암구호까지 확보했어. 이제 탈출 준비가 됐어.";
 [Min(1)] public float subtitleSeconds=6;
}}
