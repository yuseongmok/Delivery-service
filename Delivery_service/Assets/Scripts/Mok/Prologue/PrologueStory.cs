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
    public PrologueLine vault = new PrologueLine("공범: 금고가 열렸어. 이번 일만 끝내면 손 씻는 거야.");
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
        new PrologueLine("나: 이 돈이면 당분간 아무것도 안 해도 되겠네.",3.8f),
        new PrologueLine("민기: 경찰만 찾는 돈이면 그랬겠지.",3f),
        new PrologueLine("나: …무슨 뜻이야?",2f),
        new PrologueLine("민기: 그 금고, 조직이 돈을 맡겨 둔 곳이야.",3.5f),
        new PrologueLine("나: 알고도 날 데려간 거야?",2.8f),
        new PrologueLine("민기: 지금 큰돈을 쓰면 우리가 살아 있다는 광고야.",3.8f),
        new PrologueLine("민기: 일단 숨자. 예전에 우리 집이 하던 가게가 있어.",4f)
    };
    public PrologueLine[] shopConversation={
        new PrologueLine("나: 피자 가게? 우리보고 장사하라고?",3.2f),
        new PrologueLine("민기: 문 닫기 직전이야. 열쇠도, 아버지 조리법도 남아 있어.",4.5f),
        new PrologueLine("민기: 챙긴 돈 일부로 밀린 임대료부터 막자. 배달 오토바이도 마련하고.",5f),
        new PrologueLine("나: 나머지 돈은?",2f),
        new PrologueLine("민기: 내가 숨겨 둘게. 놈들이 조용해질 때까지는 손대지 마.",4.2f),
        new PrologueLine("나: 돈가방을 두고 피자 팔아서 먹고살자고?",3.5f),
        new PrologueLine("민기: 가게가 벌어야 우리도 버텨. 밤에 움직일 준비도 공짜는 아니고.",5f),
        new PrologueLine("나: 그런데 왜 하필 배달이야?",2.7f),
        new PrologueLine("민기: 낯선 사람이 건물마다 드나들면 의심받지. 피자 상자를 들면 문을 열어 줘.",5.5f),
        new PrologueLine("나: 넌 처음부터 여기까지 생각한 거냐?",3f),
        new PrologueLine("민기: 반죽부터 배우자. 나머지는 밤에 얘기하고.",3.8f),
        new PrologueLine("나: …좋아. 적어도 이 가게에서 버는 돈은 내 돈이야.",4f)
    };
    public PrologueLine boarding = new PrologueLine("공범: 타! 지금이야!", 2);
    public PrologueLine getaway = new PrologueLine("그날 이후, 나는 작은 피자 가게를 차렸다.", 5);
    public PrologueLine ending = new PrologueLine("몇 주 후\n\n이얏호 피자, 첫 영업일", 2.5f);
    [Header("직접 운전과 도시 진입")]
    [TextArea] public string driving="공범: 앞에 저지선이야! 도로 끝까지 돌파해!";
    [Min(1)] public float cityArrivalSeconds=5,cityPanSeconds=10,logoSeconds=3;
    [Header("실패")]
    [TextArea] public string failure = "탈출에 실패했습니다.";
}
}
