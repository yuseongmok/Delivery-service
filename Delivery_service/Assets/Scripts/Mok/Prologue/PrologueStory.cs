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
    public string title = "프롤로그  /  마지막 한탕";
    public PrologueLine vault = new PrologueLine("민기: 실장 부탁이야. 금고는 내가 열게. 뒤를 봐 줘.");
    public PrologueLine alarm = new PrologueLine("공범: 경보야! 돈 챙겨. 차는 정문 앞에 세워 뒀어!");
    public PrologueLine police = new PrologueLine("경찰: 건물을 포위했다! 무기를 버리고 나와라!", 3.5f);
    [Header("전투와 탈출")]
    [Min(1)] public float cashBundleSeconds = 8;
    [Header("총격전 리듬 — 초 단위, 마지막 압박은 현금 수거 72%부터")]
    [Min(1)] public float firstContactSeconds=10,breatherSeconds=6,supportSeconds=9;
    [TextArea] public string breather="민기: 잠깐 뜸해졌어! 장전해, 내가 서두를게!";
    [TextArea] public string reinforcement="경찰: 지원 병력 도착! 안쪽으로 진입한다!";
    [TextArea] public string finalPressure="민기: 거의 다 담았어! 조금만 더 버텨!";
    [TextArea] public string collect = "공범: 경찰이 몰려온다! 돈 세 묶음을 챙겨!";
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
        new PrologueLine("나: 이제 실장 만나서 우리 몫 받으면 끝이지?",3.8f),
        new PrologueLine("민기: 그래. 조직이 시킨 일은 끝냈으니까.",3f),
        new PrologueLine("나: 실장, 전화 받아?",2f),
        new PrologueLine("민기: …안 받아. 약속 장소에도 없대.",3.5f),
        new PrologueLine("나: 경찰 때문에 잠적한 건가?",2.8f),
        new PrologueLine("민기: 모르겠어. 연락 올 때까지 조용히 있자.",3.8f),
        new PrologueLine("민기: 우리 아버지가 하던 가게가 있어. 거기로 가자.",4f)
    };
    public PrologueLine[] shopConversation={
        new PrologueLine("나: 피자 가게에 숨자고?",3.2f),
        new PrologueLine("민기: 아버지가 접은 가게야. 열쇠랑 조리법은 남아 있어.",4.5f),
        new PrologueLine("민기: 내 비상금으로 재료부터 사자. 배달 오토바이도 여기 있고.",5f),
        new PrologueLine("나: 은행 돈은?",2f),
        new PrologueLine("민기: 숨겨 둘게. 조직에 넘길 돈이야. 우리 몫은 따로 받기로 했잖아.",4.2f),
        new PrologueLine("나: 가방은 꽉 찼는데 우린 빈털터리네.",3.5f),
        new PrologueLine("민기: 실장 연락을 기다리려면 먹고살 돈은 벌어야지.",5f),
        new PrologueLine("나: 그냥 문 잠그고 있으면?",2.7f),
        new PrologueLine("민기: 닫힌 가게에 둘이 숨어 있으면 더 눈에 띄어. 평범하게 장사하는 거야.",5.5f),
        new PrologueLine("나: 네가 피자는 만들 줄 알고?",3f),
        new PrologueLine("민기: 어릴 때 지겹게 도왔어. 내일은 네 차례야.",3.8f),
        new PrologueLine("나: …좋아. 적어도 여기서 번 돈은 우리가 쓰자.",4f)
    };
    public PrologueLine boarding = new PrologueLine("공범: 타! 지금이야!", 2);
    public PrologueLine getaway = new PrologueLine("실장의 연락을 기다리며, 우리는 피자 가게 문을 다시 열었다.", 5);
    public PrologueLine ending = new PrologueLine("다음 날 아침\n\n이얏호 피자, 첫 영업일", 2.5f);
    [Header("직접 운전과 도시 진입")]
    [TextArea] public string driving="공범: 앞에 저지선이야! 도로 끝까지 돌파해!";
    [Min(1)] public float cityArrivalSeconds=5,cityPanSeconds=10,logoSeconds=3;
    [Header("실패")]
    [TextArea] public string failure = "탈출에 실패했습니다.";
}
}
