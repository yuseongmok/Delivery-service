using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/호송차량 탈출 대사")]
    public sealed class PrisonTransportText : ScriptableObject
    {
        [TextArea] public string ready = "민기: 바퀴 사이로 들어가. 아래쪽 철제 지지대를 잡으면 돼. [F] 차량 밑으로 숨기";
        [TextArea] public string board = "민기: 내가 먼저 들어갈게. 옆으로 붙어. 출발하면 절대 손 놓지 마.";
        [TextArea] public string departure = "민기: 움직인다! [SPACE]를 반복해서 눌러 꽉 잡아!";
        [TextArea] public string warning = "민기: 손에 힘 줘! 조금만 더 버텨!";
        [TextArea] public string fall = "나: 윽… 손이 미끄러졌어!";
        [TextArea] public string retry = "차량 출발 전부터 다시 시작합니다. SPACE를 길게 누르지 말고 반복해서 누르세요.";
        [TextArea] public string clearGate = "민기: 정문 지났어. 아직 놓지 마!";
        [TextArea] public string[] escaped = {
            "민기: 이제 됐어. 내려!",
            "나: 하아… 진짜 밖이야. 우리 나왔어…!",
            "민기: 그러게. 네 손 풀리는 줄 알고 심장이 멎는 줄 알았다.",
            "나: 너 혼자 보내겠냐. 같이 나가기로 했잖아.",
            "민기: 고맙다. 조금만 숨 돌리고, 멀리 가자."
        };
        public string complete = "탈옥 성공 · 민기와 함께 교도소를 벗어났다";
        [Min(.1f)] public float lineSeconds = 3.5f;
    }
}
