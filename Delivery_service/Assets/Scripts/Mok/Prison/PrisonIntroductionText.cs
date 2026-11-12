using UnityEngine;

namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/감옥 도입 대사")]
    public sealed class PrisonIntroductionText : ScriptableObject
    {
        [Header("시간 경과 자막")]
        public string arrival = "수감 절차";
        public string elapsed = "수감 이틀째 · 같은 방";
        public string morning = "다음 날 아침 · 활동 시간";
        [Header("머그샷")]
        [TextArea] public string photograph = "간수: 정면 봐. 움직이지 말고.\n옆으로 돌아.";
        [Header("감방 대화 — 한 항목씩 순서대로 표시")]
        [TextArea] public string[] cell = {
            "나: 어떡하지… 이러다가 평생 감옥에서 썩을 거야.",
            "민기: 내게 좋은 생각이 있어.",
            "민기: 이곳에 대해 정보를 꽉 쥐고 있는 놈을 알아냈어.",
            "민기: 내일 아침 활동 시간에 운동장에서 만나 보자."
        };
        [Header("최형욱과 정보거래")]
        [TextArea] public string[] meeting = {
            "최형욱: 뭐야… 공무원 준비해야 하니까 저리 꺼져.",
            "민기: 이봐. 네가 이곳의 영감님이라고 들었어. 정보를 좀 사고 싶은데, 어때?",
            "최형욱: 거래는 나한테 없는 걸 가져와야 성립하는 거야. 내가 없는 게 있어 보이나?",
            "나: 뭐든 좋으니 원하는 게 있다면 말해 줘.",
            "최형욱: 흠… 눈빛을 보아하니 독기가 바짝 올랐군. 좋아, 내가 원하는 건…",
            "최형욱: 킨…",
            "민기: 킨?",
            "최형욱: 치킨이 먹고 싶다.",
            "민기: 치킨? 그 후라이드치킨 말하는 건가?",
            "최형욱: 그래. 이곳에 온 뒤로 먹은 튀김이라곤 망할 가지튀김뿐이야. 간만에 배에 기름칠 좀 하고 싶군.",
            "나: 맡겨 둬. 준비해서 다시 오지."
        };
        [Header("목표와 기록")]
        public string walkObjective = "정보거래 · 민기와 운동장에서 최형욱 만나기";
        public string talkObjective = "정보거래 · 최형욱과 대화하기";
        public string completeObjective = "정보거래 완료 · 최형욱은 갓 튀긴 치킨을 원한다";
        [TextArea] public string information = "최형욱은 감옥 사정에 밝은 정보통이다. 치킨을 가져오면 탈옥에 필요한 정보를 거래하기로 했다.";
        [Min(1)] public float lineSeconds = 4;
    }
}
