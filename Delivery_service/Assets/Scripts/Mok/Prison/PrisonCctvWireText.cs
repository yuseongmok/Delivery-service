using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 CCTV 배선 대사")]
    public sealed class PrisonCctvWireText:ScriptableObject
    {
        public string title="CCTV 전원 차단";
        [TextArea] public string hint="민기: 배선도를 봐. 영상 전원만 끊어야 해. 다른 선은 경보 회로야.";
        public string controls="마우스로 전선 선택 · 좌클릭으로 절단    |    F 돌아가기";
        [TextArea] public string failed="경보 회로가 끊겼습니다. 배전판 앞부터 다시 시도합니다.";
        [TextArea] public string retry="민기: 배선도의 단자 번호를 확인해. 영상 전원에 연결된 선을 끊어.";
        [TextArea] public string success="민기: 됐어. 카메라 화면도 꺼졌어. 이제 나가자.";
    }
}
