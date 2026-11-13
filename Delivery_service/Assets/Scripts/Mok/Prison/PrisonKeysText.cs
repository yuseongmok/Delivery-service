using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 소매치기 미션 대사")]
    public sealed class PrisonKeysText:ScriptableObject
    {
        [Min(.1f)] public float lineSeconds=4;
        [TextArea] public string start="민기: 의무실 뒤 창고야. 간수가 등을 돌렸을 때 열쇠를 빼 오자. 나는 망을 볼게.";
        [TextArea] public string stolen="민기: 좋아! 열쇠가 여러 개네. 창고 문에 맞는 걸 찾아봐. 간수가 돌아보면 멈추고 숨어.";
        [TextArea] public string caught="간수: 거기! 창고 앞에서 뭘 하는 거야?";
        [TextArea] public string supplies="민기: 도우, 소스, 치즈, 페페로니까지 챙겼어. 이제 주방으로 가자.";
        [TextArea] public string cook="민기: 평소처럼 페페로니 피자를 만들어. 구운 다음 내게 가져오면 돼.";
        [TextArea] public string wrongKey="맞지 않는 열쇠다. 다른 열쇠를 골라 보자.";
        [TextArea] public string baked="민기: 잘 구워졌네. 카트로 가져와.";
        [TextArea] public string officeOpen="민기: 허락받았어. 간수장실 안으로 가자.";
        [TextArea] public string[] load={"민기: 잘 구워졌네. 이제 준비해 둔 수면제를 넣자.","나: 효과가 확실해야 할 텐데…","민기: 카트에 실었어. 간수들에게 한 조각씩 나눠 주자."};
        [TextArea] public string served="간수: 직접 만든 거야? 냄새 좋은데. 한 조각 먹어 볼까.";
        [TextArea] public string[] officer={"간수: 잠깐 멈춰. 너희 뭐야?","민기: 피자를 만들었는데 간수장님도 드셔 보시면 좋을 것 같아서요.","간수: 수상한데… 이리 줘 봐. 검사를 해야겠어.","간수: 음, 맛있는데? 간수장 염윤태님도 좋아하시겠어. 가 봐.","나: 예. 피자만 드리고 바로 나오겠습니다."};
        [TextArea] public string[] warden={"염윤태: 너희 뭐야! 여기가 어디라고 들어와?","민기: 저희가 피자를 만들었는데 한번 드셔 보셨으면 해서 가져왔습니다. 허락도 받았습니다.","염윤태: 음, 그래? 내가 제일 좋아하는 건데. 가져와 봐!","염윤태: 이 피자는 대체 뭐냐… 너무 맛있잖아!"};
        [TextArea] public string end="민기: 계획대로야. 조용히 간수복을 찾자.";
    }
}
