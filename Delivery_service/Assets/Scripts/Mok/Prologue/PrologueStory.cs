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
