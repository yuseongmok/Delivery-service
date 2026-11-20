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
        [Header("수감 직후와 시간 경과")]
        public string secondPhotograph = "수감 절차 · 다음 수감자";
        public string firstNight = "수감 첫날 · 같은 방";
        public string daysLater = "며칠 후 · 감방";
        [TextArea] public string[] arrivalCell = {
            "민기: 그래도 같은 방이라 다행이야. 어떻게든 나갈 방법을 찾아보자.",
            "나: 그래… 일단 이곳이 어떻게 돌아가는지부터 알아보자."
        };
        [Header("첫날 작업과 취침")]
        [TextArea] public string workOrder="간수: 작업시간이다! 작업실로 이동해!";
        public string evening="그날 저녁 · 작업 종료";
        [TextArea] public string workEnd="민기: 오늘은 이만 들어가자. 몸이 녹초네.";
        [TextArea] public string bedtime="나: 내일도 또 이걸 해야 하는 건가… 일단 좀 자자.";
        [Header("함께 방으로 돌아온 뒤의 대화")]
        [TextArea] public string[] returnCell = {
            "민기: 오늘 고생했어. 손은 괜찮아?",
            "나: 아직도 얼얼해. 내일도 똑같겠지?",
            "민기: 일단 쉬자. 나갈 방법은… 내일도 같이 찾아보자."
        };
        [Min(1)] public float sleepFadeSeconds=3.5f;
        [Header("머그샷")]
        [TextArea] public string photograph = "간수: 정면 봐. 움직이지 말고.\n옆으로 돌아.";
        [Header("감방 대화 — 한 항목씩 순서대로 표시")]
        [TextArea] public string[] cell = {
            "나: 어떡하지… 이러다가 평생 감옥에서 썩을 거야.",
            "민기: 내게 좋은 생각이 있어.",
            "민기: 이곳에 대해 정보를 꽉 쥐고 있는 놈을 알아냈어.",
            "민기: 내일 아침 활동 시간에 운동장에서 만나 보자."
        };
        [Header("운동장으로 가는 길")]
        [TextArea] public string[] yardWalk={
            "민기: 며칠 지켜봤는데, 간수들도 저 사람한테는 먼저 말을 걸더라.",
            "나: 여기 사정을 꽤 잘 아나 보네.",
            "민기: 그래. 괜히 영감님이라고 부르는 게 아니겠지."
        };
        [Header("최형욱 첫 등장")]
        [TextArea] public string[] informantEntrance={
            "죄수: 영감님 덕분에 작업반 옮겼습니다. 정말 감사합니다.",
            "최형욱: 됐어. 이제 조용히 지내.",
            "죄수: 예. 필요한 거 있으시면 말씀하십시오."
        };
        [Header("치킨을 구하러 가는 이유")]
        [TextArea] public string[] kitchenPlan={
            "나: 탈옥할 정보를 얻으려면 치킨부터 구해 와야 한다니…",
            "민기: 아까 배식 때 닭 들어오는 거 봤어. 주방장이 쉬는 틈을 노리자.",
            "민기: 나는 입구를 볼게. 너는 재료를 찾아서 만들어 줘."
        };
        [Header("최형욱과 정보거래")]
        [TextArea] public string[] meeting = {
            "최형욱: 이번엔 또 뭐야? 공무원 준비해야 하니까 꺼져",
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





