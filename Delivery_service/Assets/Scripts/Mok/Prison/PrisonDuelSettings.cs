using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 김정민 결투")]
    public sealed class PrisonDuelSettings : ScriptableObject
    {
        public string bossName="김정민";
        [TextArea] public string[] challenge={"나: 김정민이지? 수면제를 가지고 있다던데.","김정민: 갖고 싶으면 운동장으로 와. 다른 놈들은 빠지고, 너랑 나 둘이 붙자.","민기: 무작정 덤비지 마. 어깨가 움직이는 쪽을 보고 반대로 피해."};
        [TextArea] public string[] arena={"죄수: 비켜! 김정민이 신입이랑 붙는다!","김정민: 날 이기면 약은 네 거다. 어디 한번 피해 봐!"};
        [TextArea] public string won="김정민: 젠장… 졌다. 약은 가져가. 약속은 지킨다.";
        [TextArea] public string lost="김정민에게 제압당했습니다.";
        [TextArea] public string tutorial="어깨를 보고 반대로 회피! Q 왼쪽 · E 오른쪽\n공격 직전에 눌러 회피한 뒤, 빈틈에 좌클릭으로 반격";
        [TextArea] public string perfect="아슬아슬한 회피!";
        [TextArea] public string counter="빈틈이다!  좌클릭 · 반격";
        [TextArea] public string early="너무 빨랐다! 공격 직전에 눌러야 해.";
        [TextArea] public string wrong="공격 반대쪽으로 피해야 해!";
        [TextArea] public string hit="피격! 다음 공격을 살펴보세요.";
        [TextArea] public string guarded="아직 빈틈이 없다! 회피 후 반격하세요.";
        [Min(.1f)] public float lineSeconds=3;
        [Min(1)] public int bossHealth=120,playerHealth=100,enemyDamage=25,counterDamage=20,perfectDamage=30;
        [Min(.3f)] public float teachingTell=1.15f,normalTell=.9f,fastTell=.72f;
        [Range(.1f,.4f)] public float dodgeWindow=.27f;
        [Range(.04f,.2f)] public float perfectWindow=.12f;
        [Min(.3f)] public float counterWindow=.85f;
        [Range(.02f,.3f)] public float slowScale=.045f;
        [Range(.2f,1.2f)] public float slowSeconds=.8f;
    }
}

