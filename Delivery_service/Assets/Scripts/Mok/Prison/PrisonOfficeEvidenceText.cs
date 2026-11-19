using UnityEngine;

namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/감옥 비리 증거와 협박 대사")]
    public sealed class PrisonOfficeEvidenceText : ScriptableObject
    {
        [TextArea] public string lockedSupplies = "민기: 잠겨 있어. 책상 주변에서 열쇠나 단서부터 찾아보자.";
        public string[] titles = { "간수장의 비밀 장부", "오늘 밤 특별 호송", "박진웅의 메시지" };
        [TextArea(3, 8)] public string[] documents = {
            "가족 면회 편의금 — 개인 계좌 입금\n의무실 약품 외부 판매 — 정산 완료\n치료 요청 — 미납자는 보류\n결재: 염윤태",
            "특별 호송 명단\n대상: 민기와 그놈\n출발: 오늘 밤\n목적지: 공란\n인계자: 박진웅 측 / 간수장 직접 승인",
            "발신: 박진웅\n약속한 돈은 보냈다.\n오늘 밤 두 놈 넘겨.\n인계 후에는 내가 처리하지."
        };
        [TextArea] public string[] ledger = {
            "나: 치료비를 받고도 약을 안 줬다고…? 약은 밖에 팔아넘기고?",
            "민기: 죄수들 목숨으로 돈을 벌고 있었네. 이 장부, 챙겨."
        };
        [TextArea] public string[] transfer = {
            "나: 민기, 여기 우리를 넘긴다는 내용이 있어. 오늘 밤 이송이래.",
            "민기: 어디로?",
            "나: 목적지가 비어 있어. 인계자가… 박진웅 측이야."
        };
        [TextArea] public string[] message = {
            "민기: 경찰만 한패인 줄 알았는데… 여기까지 연결돼 있었어.",
            "나: 오늘 밤 저 차에 실려 가면 끝이겠네.",
            "민기: 그럼 우리가 먼저 타고 나가자. 이 증거면 저 인간 입도 열 수 있어."
        };
        [TextArea] public string[] confrontation = {
            "민기: 일어나, 간수장. 우리 얘기 좀 하지.",
            "염윤태: 너희…! 문밖에 간수들이 있어. 소리 한 번이면 끝이야!",
            "나: 불러. 약을 빼돌리고 죄수 가족에게 돈 뜯은 장부부터 보여주게.",
            "민기: 박진웅에게 우리를 넘기기로 한 메시지도 챙겼어.",
            "염윤태: …원하는 게 뭐야?",
            "나: 간수복 두 벌, 호송차량 열쇠. 그리고 출발 승인.",
            "염윤태: 간수복은 옆 보관함, 차량 열쇠는 서랍에 있어. 둘 다 잠금은 풀어주지.",
            "민기: 정문에 연락해. 예정된 호송을 앞당긴다고. 평소처럼 말해야지."
        };
        [TextArea] public string[] call = {
            "염윤태: 나다. 오늘 밤 예정된 호송을 지금 출발시킨다.",
            "염윤태: 서류와 암구호 확인하고 보내. 내 승인이다.",
            "민기: 우리가 나갈 때까지 여기 있어. 허튼짓하면 장부는 네 부하들 손에 들어가.",
            "나: 서류도 챙겼어. 갈아입고 나가자."
        };
        [TextArea] public string evidenceReady = "민기: 증거는 충분해. 염윤태를 깨우자.";
        [TextArea] public string approved = "민기: 잠금은 풀렸어. 간수복과 차량 열쇠를 챙기고 암구호도 확인하자.";
        [Min(1)] public float lineSeconds = 3.5f;
    }
}
