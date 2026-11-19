using UnityEngine;

namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/탈옥 이후 시네마틱 대사")]
    public sealed class AftermathDialogue : ScriptableObject
    {
        [System.Serializable]
        public class Line
        {
            public string scene;
            [TextArea(2, 4)] public string subtitle;
            [Min(1)] public float seconds = 5;
            public AudioClip voice;
        }

        public Line[] lines;
        public Line[] escapeBridge;
        public string nightCard = "그날 밤 · 숨겨둔 돈을 찾아서";
        public string shopKeyCard = "며칠 뒤 · 새로 구한 가게";
        public string timeCard = "몇 주 뒤 · 다른 도시";
        public string newsHeadline = "교도소 탈옥수 2명… 행방은 여전히 오리무중";
        public string newsFollowupHeadline = "신분 위장 가능성… 수사 계속";
        public string newsTicker = "경찰, 탈옥한 수감자 두 명 공개 수배 · 시민 제보 요청";
        public string chapterTitle = "챕터 3\n세탁";
    }
}
