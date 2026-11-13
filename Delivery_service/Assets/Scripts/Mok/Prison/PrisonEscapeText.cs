using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 탈옥 대사")]
    public sealed class PrisonEscapeText : ScriptableObject
    {
        [TextArea] public string ready = "민기: 준비는 끝났어. 간수복으로 갈아입고 호송차량으로 가자.";
        [TextArea] public string changed = "민기: 제법 간수처럼 보이는데? 당황하지 말고 나란히 걸어가자.";
        [TextArea] public string boarding = "민기: 내가 옆에 탈게. 정문에서는 평소처럼 말해.";
        [TextArea] public string checkpoint = "간수: 정지. 호송 일정 확인하겠습니다. 오늘의 암구호는?";
        [TextArea] public string wrong = "간수: 암구호가 틀렸습니다. 두 사람 모두 내려!";
        [TextArea] public string retry = "검문 직전부터 다시 시작합니다.";
        [TextArea] public string accepted = "간수: 확인됐습니다. 통과하세요.";
        [TextArea] public string escaped = "민기: 해냈어… 이제 여기서 멀리 떠나자.";
        [TextArea] public string checking = "간수: 잠시만요. 명단을 확인하겠습니다.";
        [TextArea] public string[] relief = { "민기: …문 닫혔어? 우리 진짜 나온 거지?", "나: 응. 이제 숨 쉬어도 돼.", "민기: 네가 그 암구호를 기억 못 했으면 끝장이었어.", "나: 아직 멀리 가야 해. 이번엔 같이 끝까지 가자.", "민기: 그래. 같이." };
        public string[] decoys = { "붉은 등대", "검은 파도" };
        [Min(.5f)] public float lineSeconds = 3;
    }
}
