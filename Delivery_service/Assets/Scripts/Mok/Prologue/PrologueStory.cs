using UnityEngine;
namespace DeliveryService.Mok {
[System.Serializable]
public class PrologueLine {
    [TextArea(2, 5)] public string text;
    [Min(.1f)] public float seconds = 3;
    public PrologueLine(string value, float duration = 3) { text = value; seconds = duration; }
}
[CreateAssetMenu(menuName = "Delivery Service/프롤로그 대사", fileName = "BankPrologueStory")]
public sealed class PrologueStory : ScriptableObject {
    [Header("시작 컷신 — 문장과 표시 시간을 수정하세요")]
    public string title = "프롤로그  /  회수 작전";
    public PrologueLine vault = new PrologueLine("민기: 다른 조직에 빼앗긴 돈만 되찾는 거래. 금고는 내가 열게. 뒤를 봐 줘.");
    public PrologueLine alarm = new PrologueLine("민기: 경보야! 비어 있다더니! 돈은 내가 챙길게. 출구를 지켜!");
    public PrologueLine police = new PrologueLine("조직원: 금고에 침입자다! 얼굴 똑똑히 봤어. 출구 막아!", 3.5f);
    [Header("전투와 탈출")]
    [Min(1)] public float cashBundleSeconds = 8;
    [Header("근접전 리듬 — 초 단위, 마지막 압박은 현금 수거 72%부터")]
    [Min(1)] public float firstContactSeconds=10,breatherSeconds=6,supportSeconds=9;
    [TextArea] public string breather="민기: 잠깐 뜸해졌어! 장전해, 내가 서두를게!";
    [TextArea] public string reinforcement="조직원: 지원 왔다! 금고 쪽으로 들어가!";
    [TextArea] public string finalPressure="민기: 거의 다 담았어! 조금만 더 버텨!";
    [TextArea] public string collect = "민기: 놈들이 몰려온다! 가방은 내가 챙길게!";
    [TextArea] public string escape = "공범: 금고 복도를 지나 사무실, 대기 홀 순서로 나와!";
    public string[] routeNames = { "오른쪽 보안 통로", "왼쪽 사무실 통로", "오른쪽 대기 홀", "정문 앞 도주 차량" };
    [TextArea] public string[] routeLines = {
        "공범: 오른쪽 통로야! 기둥 뒤로 숨어서 움직여!",
        "공범: 사무실을 가로질러 왼쪽으로!",
        "공범: 마지막 홀이야. 오른쪽 정문으로 나와!",
        "공범: 차가 바로 앞이야. 뛰어!"
    };
    [Header("마지막 컷신")]
    [Header("도주 후 대화와 피자 가게 — 각 문장과 표시 시간을 수정")]
    public PrologueLine[] carConversation={
        new PrologueLine("나: 비어 있는 금고라더니, 무장한 놈들이 기다리고 있었잖아!",4.5f),
        new PrologueLine("민기: 빼앗긴 돈만 회수하면 된다고 했어. 우리도 속은 거야.",4.5f),
        new PrologueLine("나: 저쪽 조직이 우리 얼굴까지 봤어. 실장은 뭐래?",4f),
        new PrologueLine("민기: 전화가 안 돼. 약속 장소에도 없대.",3.5f),
        new PrologueLine("나: 우리가 미끼였던 건가?",3f),
        new PrologueLine("민기: 아직은 몰라. 연락 올 때까지 신분을 숨기고 조용히 기다리자.",5f),
        new PrologueLine("민기: 아버지가 하던 피자 가게가 있어. 거기로 가자.",4f)
    };
    public PrologueLine[] shopConversation={
        new PrologueLine("나: 여기서 피자 장사를 하자고?",3.2f),
        new PrologueLine("민기: 아버지가 접은 가게야. 열쇠랑 조리법은 남아 있어.",4.5f),
        new PrologueLine("민기: 저쪽은 금고를 턴 두 놈을 찾겠지. 우린 평범한 가게 주인과 배달원인 거야.",6f),
        new PrologueLine("나: 챙겨 온 돈은?",2.5f),
        new PrologueLine("민기: 따로 숨겨 둘게. 누구 돈인지부터 확인해야 해. 지금 쓰면 꼬리를 잡힐 수도 있어.",6f),
        new PrologueLine("나: 가방은 꽉 찼는데 우린 빈털터리네.",3.5f),
        new PrologueLine("민기: 내 비상금으로 재료부터 사자. 배달 오토바이도 여기 있고.",5f),
        new PrologueLine("나: 그냥 문 잠그고 기다리면 안 돼?",3f),
        new PrologueLine("민기: 숨어만 있으면 더 수상해. 장사로 생활비를 벌면서 실장 연락을 기다리는 거야.",5.5f),
        new PrologueLine("나: 네가 피자는 만들 줄 알고?",3f),
        new PrologueLine("민기: 어릴 때 지겹게 도왔어. 내일은 네 차례야.",3.8f),
        new PrologueLine("나: 좋아. 당분간은 평범한 배달원으로 지내 보자.",4f)
    };
    public PrologueLine boarding = new PrologueLine("공범: 타! 지금이야!", 2);
    public PrologueLine getaway = new PrologueLine("얼굴을 숨기고 실장의 연락을 기다리기 위해, 우리는 피자 가게 문을 다시 열었다.", 5);
    public PrologueLine ending = new PrologueLine("다음 날 아침\n\n이얏호 피자, 첫 영업일", 2.5f);
    [Header("직접 운전과 도시 진입")]
    [TextArea] public string driving="민기: 놈들이 차로 길을 막았어! 멈추지 말고 봉쇄를 밀어내!";
    [Min(1)] public float cityArrivalSeconds=5,cityPanSeconds=10,logoSeconds=3;
    [Header("실패")]
    [TextArea] public string failure = "탈출에 실패했습니다.";
}
}
