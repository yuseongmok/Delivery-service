using UnityEngine;

namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName = "Delivery Service/감옥 첫 임무 대사")]
    public sealed class PrisonMissionText : ScriptableObject
    {
        [TextArea(2, 5)] public string introduction = "간수: 배식 당번이다. 주방에서 세 사람 몫을 만들어 카트에 실어.\n나: 민기가 어디 있는지, 배식하면서 물어보자.";
        [TextArea(2, 5)] public string[] prisoners = {
            "나: 최근에 들어온 사람 못 봤어?\n재소자: 여긴 매일 새 얼굴이 와. 옆방에도 물어봐.",
            "나: 나랑 같이 잡혀 온 친구를 찾고 있어.\n재소자: 다친 사람? 저 끝방 친구가 의무실 청소를 했으니 알겠지.",
            "재소자: 다리에 총 맞은 새내기 말이지? 의무실에 누워 있어. 우린 일하는데 걔는 쉬기만 한다니까.\n나: 의무실… 간수 눈을 피해 가야겠어."
        };
        [TextArea(2, 5)] public string[] mingi = {
            "나: 민기, 괜찮아?\n민기: 목소리 낮춰. 다리는 좀 나아졌어. 여기도 안전하진 않아.",
            "민기: 간수장 염윤태가 출입 기록과 차량 열쇠를 관리해. 그의 방에 접근할 방법부터 찾자.",
            "나: 난 주방 일을 맡았어. 밥을 나르면서 알아볼게.\n민기: 좋아. 지금은 들키지 말고 방으로 돌아가. 다음 기회를 기다리자."
        };
        [TextArea] public string completed = "민기의 위치를 확인했다. 주방 일은 간수장에게 접근할 기회가 될지도 모른다.\n첫 임무 완료";
        [TextArea] public string caught = "간수: 허가 없이 의무실에 들어오면 안 된다!\n의무실 진입부터 다시 시도합니다.";
        [TextArea] public string attacked = "간수: 공격이다! 움직이지 마!\n임무를 처음부터 다시 시작합니다.";
        public string[] objectives = { "주방에서 밥 3인분 만들기", "완성된 식사를 배식카트에 싣기", "배식카트로 재소자에게 식사 전달", "간수의 시선을 피해 의무실로 들어가기", "민기와 조용히 대화하기", "들키지 않고 자기 방으로 돌아가기", "민기를 찾아서 · 완료" };
    }
}
