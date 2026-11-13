using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 강탈 미션 대사")]
    public sealed class PrisonMedicineText : ScriptableObject
    {
        [Min(.1f)] public float lineSeconds=4;
        [TextArea] public string clue="민기: 작업실로 가 보자. 최형욱이 말한 놈은 왼팔에 파란 완장을 하고 있대.";
        [TextArea] public string wrong="죄수: 수면제? 난 그런 거 없어. 파란 완장을 한 녀석에게 물어봐.";
        [TextArea] public string[] demand={"나: 의무실에서 받은 수면제를 가지고 있다며? 우리에게 좀 넘겨줘.","죄수: 너희가 뭔데? 여기 물건은 공짜가 아니야.","민기: 좋게 말할 때 끝내자.","죄수: 얘들아, 이 신입들한테 규칙 좀 알려 줘!"};
        [TextArea] public string won="민기: 저기 약 봉투가 떨어졌어. 챙기고 우리 방으로 돌아가자.";
        [TextArea] public string failed="제압당했다. 작업실 앞에서 다시 준비하자.";
        [TextArea] public string[] home={"나: 좋아. 수면제는 얻었고, 뭘 만들어야 간수들이 의심하지 않고 먹을까?","민기: 우리가 제일 잘하는 거 있잖아. 피자를 만들자.","나: 재료는 어디서 구하지?","민기: 의무실 뒤에 간수용 식료품 저장고가 있어. 열쇠를 관리하는 간수를 봤어.","나: 쉽지 않겠는걸…","민기: 우선 여기서 숨 좀 돌리자. 다음엔 그 열쇠야."};
        [TextArea] public string complete="강탈 완료 · 다음 계획: 식료품 저장고 열쇠 확보";
    }
}
