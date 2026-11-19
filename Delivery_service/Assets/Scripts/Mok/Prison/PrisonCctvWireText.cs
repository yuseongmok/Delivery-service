using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 CCTV 배선 대사")]
    public sealed class PrisonCctvWireText:ScriptableObject
    {
        public string title="CCTV 배선 추적";
        [TextArea] public string hint="민기: CCTV 단자에서 연결된 선을 따라가. 다른 선은 경보 회로야.";
        public string controls="선 클릭 · 선택   |   자르기 버튼 · 절단   |   F 돌아가기";
        [TextArea] public string failed="경보 회로가 끊겼습니다. 배전판 앞부터 다시 시도합니다.";
        [TextArea] public string retry="민기: CCTV 단자부터 연결을 따라가. 아래에서 맞는 선을 골라.";
        [TextArea] public string success="민기: 됐어. 카메라 화면도 꺼졌어. 이제 나가자.";
    }
}
