using UnityEngine;

namespace DeliveryService.Mok
{
    // This component owns the first-person presentation, not mission or player controls.
    public sealed class AftermathFirstPerson : MonoBehaviour
    {
        public AftermathActor forestPlayer, shopPlayer, mingi;
        public AftermathActor hideoutPlayer, exchangePlayer;
        public Transform box, leftPalm, rightPalm, mingiLeft, mingiRight;
        public Vector3 boxRest, boxOffered, boxHeld;
        public int newsShot = 8, callShot = 10, receiveShot = 13, walkShot = 14;
        public float eyeHeight = 1.55f;
        public AftermathIdentityHandoff identityHandoff;
        public GameObject shopKeyProp;
        public AftermathShopKeyGesture shopKeyGesture;

        public void Present(AftermathFilm film, int index, float progress)
        {
            var shot = film.shots[index];
            if (shot.shopKeyCard && shopKeyGesture) shopKeyGesture.Present(film, progress);
            if (shopKeyProp)
            {
                if (shopKeyProp.activeSelf && !shot.shopKeyCard && mingi.rightContact && mingi.rightContact.IsChildOf(shopKeyProp.transform))
                { mingi.reach = 0; mingi.rightContact = null; }
                shopKeyProp.SetActive(shot.shopKeyCard);
            }
            var actor = index >= 1 && index <= 3 && hideoutPlayer ? hideoutPlayer : index <= 3 ? forestPlayer : index <= 5 ? exchangePlayer : index >= 7 ? shopPlayer : null;
            if (shot.shopKeyCard) actor = shopPlayer;
            if (actor)
            {
                // Keep the camera in front of the head, without inheriting animation roll.
                film.film.transform.position = actor.transform.position + Vector3.up * eyeHeight + actor.transform.forward * .14f;
                Vector3 target = shot.lookAt.position;
                if (shot.shopKeyCard)
                {
                    Vector3 face = mingi.transform.position + Vector3.up * 1.48f;
                    float glance = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.30f, .40f, progress)) *
                        (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.56f, .74f, progress)));
                    target = Vector3.Lerp(face, face + Vector3.down * .065f, glance);
                }
                if (film.hideout && index >= 1 && index <= 3)
                {
                    var hideout = film.hideout;
                    // Frame the fixed cabinet opening, not the moving door handle.
                    Vector3 handleView = hideout.hiddenBag + new Vector3(-.25f, .43f, -.35f);
                    Vector3 bagView = hideout.bag.position + Vector3.up * .18f;
                    if (index == 1)
                    {
                        float approach = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1, progress));
                        target = Vector3.Lerp(target, handleView, approach);
                        film.film.fieldOfView = Mathf.Lerp(shot.fieldOfView, 55, approach);
                    }
                    if (index == 2)
                    {
                        target = Vector3.Lerp(handleView, bagView, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .78f, progress)));
                        film.film.fieldOfView = 55;
                    }
                    if (index == 3)
                    {
                        float response = Mathf.SmoothStep(0, 1, progress / .42f);
                        target = Vector3.Lerp(bagView, target, response);
                        film.film.fieldOfView = Mathf.Lerp(55, shot.fieldOfView, response);
                    }
                }
                if (index == newsShot - 1)
                {
                    film.newsHeadline.text = film.dialogue.newsHeadline;
                    film.film.fieldOfView = Mathf.Lerp(52, 48, Mathf.SmoothStep(0, 1, progress));
                }
                if (index == newsShot)
                {
                    film.newsHeadline.text = film.dialogue.newsFollowupHeadline;
                    target = Vector3.Lerp(film.shots[index - 1].lookAt.position, target,
                        Mathf.SmoothStep(0, 1, progress / .4f));
                }
                if (index == newsShot + 1 && shot.lookFrom)
                    target = Vector3.Lerp(shot.lookFrom.position, target, Mathf.SmoothStep(0, 1, progress / .30f));
                if (index == callShot) film.film.fieldOfView = Mathf.Lerp(50, 60, Mathf.SmoothStep(0, 1, progress / .62f));
                if (index == callShot && shot.lookFrom)
                    target = Vector3.Lerp(shot.lookFrom.position, target, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, .62f, progress)));
                if (index == newsShot)
                {
                    // The gaze holds on the broadcast; breathing settles before the call.
                    float tension = Mathf.Sin(progress * Mathf.PI);
                    film.film.fieldOfView = Mathf.Lerp(48, 50, progress) - tension;
                    film.film.transform.position += Vector3.up * (Mathf.Sin(progress * Mathf.PI * 4) * .007f * tension);
                }
                if (index == walkShot)
                {
                    // Continue the pickup gaze before turning with the body; walking starts afterwards.
                    Transform start = actor.transform;
                    foreach (var motion in shot.motions)
                        if (motion.actor == actor) { start = motion.from; break; }
                    var startEye = start.position + Vector3.up * eyeHeight + start.forward * .14f;
                    var initialLook = Quaternion.LookRotation(boxHeld + Vector3.up * .03f - startEye);
                    var forwardLook = Quaternion.LookRotation(actor.transform.forward * 5 + Vector3.down * 1.2f);
                    film.film.transform.rotation = Quaternion.Slerp(initialLook, forwardLook, Mathf.SmoothStep(0, 1, progress / .28f));
                    target = film.film.transform.position + film.film.transform.forward * 5;
                }
                film.film.transform.LookAt(target);
            }
            if (identityHandoff && !shot.shopKeyCard) identityHandoff.Present(film, index, progress);
            if (index == walkShot) ApplyWalkMotion(film, shot, progress);
            if (index != receiveShot) return;

            float slide = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.1f, .43f, progress));
            float lift = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.62f, .92f, progress));
            box.position = Vector3.Lerp(Vector3.Lerp(boxRest, boxOffered, slide), boxHeld, lift);
            box.rotation = Quaternion.identity;
            film.film.fieldOfView = Mathf.Lerp(60, 68, Mathf.SmoothStep(0, 1, progress / .2f));
            Vector3 handoffGaze = Vector3.Lerp(mingi.transform.position + Vector3.up * 1.4f, box.position + Vector3.up * .03f, Mathf.SmoothStep(0, 1, progress / .22f));
            film.film.transform.LookAt(handoffGaze);
            shopPlayer.leftContact = leftPalm; shopPlayer.rightContact = rightPalm;
            shopPlayer.reach = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.23f, .55f, progress));
            mingi.leftContact = mingiLeft; mingi.rightContact = mingiRight;
            mingi.reach = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.53f, .69f, progress));
        }
        void ApplyWalkMotion(AftermathFilm film, AftermathFilm.Shot shot, float progress)
        {
            foreach (var motion in shot.motions)
            {
                if (motion.actor != shopPlayer) continue;
                float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(motion.moveDelay, 1, progress));
                float weight = Mathf.SmoothStep(0, 1, t / .06f) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.92f, 1, t)));
                Vector3 previous = motion.from.position;
                float distance = 0;
                for (int i = 1; i <= 16; i++)
                {
                    float sample = t * i / 16f;
                    Vector3 current = motion.bend ? Vector3.Lerp(Vector3.Lerp(motion.from.position, motion.bend.position, sample), Vector3.Lerp(motion.bend.position, motion.to.position, sample), sample) : Vector3.Lerp(motion.from.position, motion.to.position, sample);
                    distance += Vector3.Distance(previous, current); previous = current;
                }
                float phase = distance * (Mathf.PI * 2 / 1.25f);
                var camera = film.film.transform;
                camera.position += (Vector3.up * (Mathf.Sin(phase * 2) * .009f) + camera.right * (Mathf.Sin(phase) * .005f)) * weight;
                box.position += (Vector3.up * (Mathf.Sin(phase * 2 + .35f) * .012f) + shopPlayer.transform.right * (Mathf.Sin(phase + .2f) * .009f)) * weight;
                box.rotation *= Quaternion.Euler(Mathf.Sin(phase * 2) * .3f * weight, 0, Mathf.Sin(phase) * .35f * weight);
                break;
            }
        }    }
}
