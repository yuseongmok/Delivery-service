using System.Collections;
using UnityEngine;

namespace DeliveryService.Mok
{
    // 주방장의 잠든 자세와 기상 연출을 미션 판정에서 분리한다.
    public sealed class PrisonChefReaction : MonoBehaviour
    {
        public Transform actor, head, standingPoint, wideShot;
        [Min(.2f)] public float riseSeconds = .7f;
        [Min(.1f)] public float closeupSeconds = .35f;
        [Min(.1f)] public float reactionSeconds = .55f;
        public Transform playerView;
        Vector3 sleepingPosition;
        Quaternion sleepingRotation;
        bool awake;

        void Awake()
        {
            sleepingPosition = actor.position;
            sleepingRotation = actor.rotation;
        }
        void LateUpdate()
        {
            if (!awake && Time.timeScale > 0)
                actor.position = sleepingPosition + Vector3.up * (Mathf.Sin(Time.time * 1.4f) * .006f);
        }
        public void Sleep()
        {
            awake = false;
            actor.SetPositionAndRotation(sleepingPosition, sleepingRotation);
        }
        public IEnumerator Wake(Camera camera)
        {
            awake = true;
            camera.fieldOfView = 55;
            var view = playerView ? playerView : wideShot;
            camera.transform.SetPositionAndRotation(view.position, view.rotation);
            Quaternion viewRotation = camera.transform.rotation;
            var from = actor.position;
            var rotation = actor.rotation;
            for (float t = 0; t < riseSeconds; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / riseSeconds);
                actor.position = Vector3.Lerp(from, standingPoint.position, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * .17f;
                actor.rotation = Quaternion.Slerp(rotation, standingPoint.rotation, p);
                var look = Quaternion.LookRotation(head.position - camera.transform.position, Vector3.up);
                camera.transform.rotation = Quaternion.Slerp(viewRotation, look, p);
                yield return null;
            }
            actor.SetPositionAndRotation(standingPoint.position, standingPoint.rotation);
            var facing = camera.transform.position - actor.position;
            facing.y = 0;
            if (facing.sqrMagnitude > .01f) actor.rotation = Quaternion.LookRotation(facing);
            PrisonAudio.Clink(actor.position, .2f);
            camera.transform.LookAt(head.position);
            yield return new WaitForSeconds(reactionSeconds);
            Vector3 start = camera.transform.position;
            // 현재 머리 위치를 따라가므로 모델을 바꿔도 얼굴을 놓치지 않는다.
            for (float t = 0; t < closeupSeconds; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / closeupSeconds);
                camera.transform.position = Vector3.Lerp(start, head.position + actor.forward * .82f + Vector3.up * .06f, p);
                camera.transform.LookAt(head.position + Vector3.up * .04f);
                camera.fieldOfView = Mathf.Lerp(55, 42, p);
                yield return null;
            }
            camera.transform.position = head.position + actor.forward * .82f + Vector3.up * .06f;
            camera.transform.LookAt(head.position + Vector3.up * .04f);
            camera.fieldOfView = 42;
        }
    }
}
