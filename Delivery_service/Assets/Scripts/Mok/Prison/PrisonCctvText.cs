using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 CCTV 잠입 대사")]
    public sealed class PrisonCctvText:ScriptableObject
    {
        public string departureTime="19:30";
        [TextArea] public string[] interrogation={
            "민기: 비리 장부와 박진웅의 메시지는 우리가 가지고 있어. 지금부터 묻는 말에 대답해.",
            "염윤태: …원하는 게 뭐야?",
            "나: 간수복은 어디 있지? 밖으로 나가는 호송차량은 언제 출발해?",
            "염윤태: 옆 보관함에 예비 간수복이 있다. 잠금은 풀어주마. 정기 호송은 저녁 {departure} 출발이야.",
            "민기: 그 차가 박진웅한테 가는 건가?",
            "염윤태: 아니! 그건 정기 이송 차량이야. 너희 둘은 그 차가 나간 뒤 별도로 넘기기로 했어.",
            "나: 그 전에 빠져나가야겠네. 감시 카메라는 어디서 관리하지?",
            "염윤태: 아래층 보안실 안 CCTV실. 담당자 외에는 간수라도 못 들어가. 거기서 녹화와 전송을 함께 끌 수 있어.",
            "민기: 간수복으로 접근해서 카메라부터 끄자. 저녁엔 호송차 밑에 숨어서 나가는 거야."
        };
        [TextArea] public string[] restraint={
            "나: 우릴 넘기려던 인간을 그냥 두고 갈 순 없어.",
            "민기: 다시 의자에 묶자. 아직 약기운도 남아 있으니 조금 더 자게 둬.",
            "염윤태: 잠깐, 이봐…!"
        };
        [TextArea] public string secured="민기: 단단히 묶였어. 잠든 틈에 움직이자.";
        [TextArea] public string changed="민기: 간수복은 입었어. 하지만 CCTV실은 출입 금지야. 담당 간수가 등을 돌릴 때 들어가.";
        [TextArea] public string opened="민기: 내가 뒤따라갈게. 안쪽 제어반에서 녹화와 전송을 모두 끊어.";
        [TextArea] public string caught="간수: 거기! 허가 없이 CCTV실에 들어온 게 누구야!";
        [TextArea] public string retry="보안실 앞부터 다시 시도합니다. 간수의 시선과 벽 뒤 사각지대를 이용하세요.";
        [TextArea] public string disabled="민기: 화면이 꺼졌어. 녹화도 멈췄고. 들키기 전에 복도로 나가자.";
        [TextArea] public string complete="민기: 준비는 끝났어. 호송차 출발은 {departure}. 이제 저녁이 될 때까지 몸을 낮추자.";
        public string[] channels={"수감동 녹화 차단","운동장 녹화 차단","정문 영상 전송 차단"};
        [Min(1)] public float lineSeconds=3.5f;
        public string Format(string text)=>text.Replace("{departure}",departureTime);
    }
}
