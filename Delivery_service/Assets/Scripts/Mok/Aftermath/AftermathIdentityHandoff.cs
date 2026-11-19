using UnityEngine;

namespace DeliveryService.Mok
{
    // 신분 거래 장면 전용. 봉투와 두 사람의 손을 같은 시간축으로 움직인다.
    public sealed class AftermathIdentityHandoff : MonoBehaviour
    {
        public AftermathActor player, dealer;
        public Transform envelope, playerGrip, dealerGrip;
        public Vector3 rest, offered, held;

        static float Blend(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));

        public void Present(AftermathFilm film, int index, float progress)
        {
            if (index != 4 && index != 5) return;
            float t = index == 5 ? 1 : progress;
            // Push, settle, grasp, then collect. No prop motion during the change of hands.
            float lift = Blend(.68f, .95f, t);
            envelope.position = Vector3.Lerp(Vector3.Lerp(rest, offered, Blend(.08f, .30f, t)), held, lift);
            envelope.rotation = Quaternion.Euler(Mathf.Lerp(0, -12, lift), 0, 0);
            player.rightContact = playerGrip;
            player.leftContact = null;
            player.reach = Blend(.45f, .62f, t);
            dealer.rightContact = dealerGrip;
            dealer.reach = 1 - Blend(.23f, .32f, t);
            dealer.forwardLean = 30 * (1 - Blend(.25f, .43f, t));
            if (index == 4)
            {
                film.film.fieldOfView = 56;
                film.film.transform.LookAt(Vector3.Lerp(new Vector3(400.05f, .86f, -.08f), envelope.position, Blend(.35f, .8f, t)));
            }
        }
    }
}
