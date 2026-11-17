using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/감옥 소매치기 미션 대사")]
    public sealed class PrisonKeysText:ScriptableObject
    {
        [TextArea] public string theftSnagLeft="고리가 걸렸어 · 왼쪽으로 살살 움직여 풀기";
        [TextArea] public string theftSnagRight="고리가 걸렸어 · 오른쪽으로 살살 움직여 풀기";
        [TextArea] public string theftFreeze="간수가 뒤척인다 · 마우스를 움직이지 마세요";
        [TextArea] public string theftCatch="열쇠가 떨어진다! · 우클릭으로 반대 손으로 받치기";
        [TextArea] public string theftReceive="조용히 열쇠를 받쳤다";
        [TextArea] public string theftDropped="민기: 소리가 났어! 열쇠는 챙겼으니 빨리 숨어!";
        [TextArea] public string theftGrab="열쇠고리를 클릭해서 잡으세요 · 천천히 위로 당기기";
        [TextArea] public string theftPull="천천히 위로 빼내기 · 손을 놓으면 소음이 가라앉습니다";
        [TextArea] public string theftWarning="간수: 무슨 소리지? · F로 손을 빼고 물러나세요!";
        [TextArea] public string lockChoose="열쇠를 클릭해 자물쇠에 넣기 · 휠로 선택";
        [TextArea] public string lockInsert="열쇠를 조심스럽게 넣고 있습니다";
        [TextArea] public string lockTurn="열쇠 머리를 잡고 오른쪽으로 드래그해서 돌리기 · 우클릭으로 빼기";
        [TextArea] public string lockWarning="민기: 간수가 이쪽을 본다! 손을 빼고 숨어!";
        [TextArea] public string lockPickHint="휠로 열쇠 고르기 → 열쇠를 클릭해서 잡기";
        [TextArea] public string lockPush="클릭을 누른 채 위로 밀어 꽂으세요 · 걸리면 다른 열쇠!";
        [TextArea] public string lockDoesNotFit="끝까지 들어가지 않는다 · 우클릭으로 빼고 다른 열쇠를 고르세요";
        [TextArea] public string lockFeel="열쇠 머리를 잡고 좌우로 천천히 · 딸깍 소리가 나면 멈추세요";
        [TextArea] public string lockHold="맞물렸다! 클릭을 누른 채 잠깐 가만히 유지하세요";
        [TextArea] public string lockFinalTurn="잠금이 풀렸다 · 오른쪽으로 끝까지 돌리세요";
        [TextArea] public string lockWithdraw="열쇠를 빼는 중";
        [TextArea] public string lockRemembered="맞는 열쇠를 기억하고 있습니다 · 클릭해서 다시 꽂으세요";
        [Min(.1f)] public float lineSeconds=4;
        [TextArea] public string keyDistraction="민기: 열쇠 챙겼지? 내가 말 걸고 있을 테니 창고 문을 열어.";
        [TextArea] public string keyConversation="민기: 간수님, 들어오던 골목에서 이상한 소리가 났는데요.";
        [TextArea] public string keyGuardReply="간수: 무슨 소리? 어디서 들었는데?";
        [TextArea] public string alleyLead="민기: 저희가 들어왔던 쪽입니다. 제가 안내하겠습니다.";
        [TextArea] public string alleyGather="간수가 골목 입구로 이동했다. 재료를 챙겨 밖으로 나가자.";
        [TextArea] public string diversionReady="민기: 열렸어? 내가 간수를 데려갈게. 잠깐 기다려.";
        [TextArea] public string diversionAsk="민기: 간수님, 배식카트 바퀴가 걸렸는데 잠깐 봐주시겠습니까?";
        [TextArea] public string diversionReply="간수: 또 뭐야… 앞장서.";
        [TextArea] public string diversionGather="민기: 카트는 저쪽입니다. 먼저 가시면 곧 따라가겠습니다.";
        [TextArea] public string meetAgain="재료를 모두 챙겼다. 골목 입구에서 민기와 합류하자.";
        [TextArea] public string rejoined="민기: 다 챙겼지? 간수가 돌아오기 전에 주방으로 가자.";
        [TextArea] public string start="민기: 의무실 뒤 창고야. 간수가 등을 돌렸을 때 열쇠를 빼 오자. 나는 망을 볼게.";
        [TextArea] public string stolen="민기: 좋아! 창고 문에 맞는 열쇠를 찾아봐. 천천히 움직이고, 짤랑 소리가 나면 멈춰.";
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
