using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/감옥 야간 잠입 대사")]
    public sealed class PrisonNightText : ScriptableObject
    {
        [TextArea] public string waiting = "민기: 카메라는 껐어. 교대가 끝날 때까지 여기서 기다리자.";
        public string timeCard = "그날 저녁 · 호송 출발 직전";
        [TextArea] public string[] introduction = {
            "민기: 호송차가 곧 출발해. 이제 간수복을 입었어도 밖에서 돌아다니면 들킬 거야.",
            "나: 감시탑은 아직 움직이고 있어… 불빛이 지나간 다음 움직이자.",
            "민기: 상자 뒤로 숨어. 난 네 뒤를 따라갈게. 차량 옆에서 만나자."
        };
        [TextArea] public string caught = "간수: 운동장에 누구야! 거기 멈춰!";
        [TextArea] public string retry = "민기: 불빛이 돌아오는 걸 봐. 지나간 다음 움직이면 돼.";
        [TextArea] public string waitingForMingi = "나: 먼저 왔어. 민기, 이쪽이야!";
        [TextArea] public string complete = "민기: 좋아, 둘 다 왔어. 이제 이 차 밑으로 숨어서 나가는 거야.";
        [Min(.1f)] public float lineSeconds = 3.5f;
    }
}
