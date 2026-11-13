using UnityEngine;
namespace DeliveryService.Mok {
 [CreateAssetMenu(menuName="Delivery Service/감옥 치킨 미션 대사")]
 public sealed class PrisonChickenText:ScriptableObject {
  [Min(60)] public float kitchenSeconds=360;
  [TextArea] public string start="민기: 주방장이 낮잠 자는 동안 만들자. 나는 입구를 볼게. 바닥 조심해.";
  [TextArea] public string caught="주방장: 내 신성한 주방에서 당장 꺼져!";
  [TextArea] public string ambush="죄수: 잠깐. 그 치킨 냄새… 혼자 먹겠다는 건 아니지?";
  [TextArea] public string cooked="민기: 치킨 완성이네! 최형욱에게 가져가자.";
  [TextArea] public string fightCleared="민기: 잘 지켰어. 이제 최형욱에게 가져가자.";
  [TextArea] public string defeated="치킨을 지키지 못했다. 주방 입구에서 다시 준비하자.";
  [TextArea] public string[] delivery={
   "최형욱: 이 냄새는… 이게 얼마만이야!",
   "나: 넘겨주기 전에 우리 부탁부터 들어주셔야지.",
   "최형욱: 아, 그래. 뭘 알려 주면 되지?",
   "민기: 우린 탈옥할 생각이야. 간수복이 필요한데 어디 있는지 알아?",
   "최형욱: 예비 간수복은 간수장실에 있지. 문제는 앞을 지키는 간수야.",
   "최형욱: 의무실에서 수면제를 처방받은 놈을 봤어. 너희 요리 실력과 그 약이면 경계를 풀 수 있겠지.",
   "나: 음식에 수면제를 넣으라는 거군.",
   "민기: 그놈은 어디 있지?",
   "최형욱: 아까 작업실로 들어갔어. 순순히 내줄 녀석은 아닐 거다.",
   "나: 일단 만나 봐야겠네."
  };
  [Min(1)] public float lineSeconds=4;
 }
}
