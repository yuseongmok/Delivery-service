using System.Collections;
using UnityEngine;

namespace DeliveryService.Mok
{
    // 조사 중에는 한 물건만 꺼내며 확보 판정은 동작이 끝난 뒤 적용한다.
    public sealed class PrisonOfficePickup : MonoBehaviour
    {
        public PrisonOfficeSearch mission;
        public bool Busy { get; private set; }
        public IEnumerator Take(PrisonOfficePoint point)
        {
            if (Busy) yield break;
            Busy = true;
            var player = mission.previous.previous.prison.player;
            var move = player.GetComponent<PlayerMove>();
            bool controlled = move.isControllable;
            move.isControllable = false;
            var item = point.contents.transform;
            Vector3 start = item.position;
            Quaternion rotation = item.rotation;
            var view = player.View.transform;
            var look = player.GetComponentInChildren<CameraMove>();
            bool lookControl = look.isControllable;
            bool combatEnabled = player.enabled;
            look.isControllable = false; player.enabled = false;
            Quaternion originalView = view.rotation;
            Quaternion focus = Quaternion.LookRotation(point.contents.transform.position - view.position);
            for (float t = 0; t < .35f; t += Time.deltaTime)
            { view.rotation = Quaternion.Slerp(originalView, focus, Mathf.SmoothStep(0, 1, t / .35f)); yield return null; }
            view.rotation = focus;
            // 서랍에서 위로 들어 올린 다음 눈앞에서 확인하고 몸쪽으로 챙긴다.
            for (float t = 0; t < 1.8f; t += Time.deltaTime)
            {
                Vector3 inspect = view.TransformPoint(new Vector3(.14f, -.30f, .65f));
                float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .85f));
                Vector3 target = t < 1.25f ? inspect : view.TransformPoint(new Vector3(.20f, -.85f, .25f));
                item.position = t < .85f ? Vector3.Lerp(start, inspect, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * .15f : Vector3.Lerp(inspect, target, Mathf.SmoothStep(0, 1, (t - 1.25f) / .55f));
                item.rotation = Quaternion.Slerp(rotation, view.rotation, progress * .65f);
                yield return null;
            }
            point.contents.SetActive(false);
            PrisonAudio.Clink(player.transform.position, .08f);
            mission.Collect(point.kind);
            for (float t = 0; t < .25f; t += Time.deltaTime)
            { view.rotation = Quaternion.Slerp(focus, originalView, Mathf.SmoothStep(0, 1, t / .25f)); yield return null; }
            view.rotation = originalView;
            look.isControllable = lookControl; player.enabled = combatEnabled;
            move.isControllable = controlled;
            Busy = false;
        }
    }
}
