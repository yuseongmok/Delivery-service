using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    // 도시의 목표 표시를 사용하되 프롤로그 진행 단계만 이곳에서 선택한다.
    [DefaultExecutionOrder(200)]
    public sealed class PrologueObjectiveMarker : MonoBehaviour
    {
        public BankPrologue director;
        public Text marker;

        void LateUpdate()
        {
            if (!marker || !director) return;
            Transform target = null;
            string title = "";
            Camera camera = director.player.view;
            Vector3 observer = director.player.transform.position;
            if (!director.Paused && !director.player.KnockedDown)
            {
                switch (director.Current)
                {
                    case BankPrologue.Phase.Robbery:
                        if (director.accomplice) target = director.accomplice.transform;
                        title = "민기를 엄호하세요";
                        break;
                    case BankPrologue.Phase.Escape:
                        int step = director.RouteStep;
                        target = step < director.escapeRoute.Length ? director.escapeRoute[step] : director.exitPoint;
                        title = step < director.story.routeNames.Length ? director.story.routeNames[step] : "도주 차량";
                        // 차량에 도착하면 건너뛴 경유지로 되돌아가도록 안내하지 않는다.
                        if (Vector3.Distance(observer, director.exitPoint.position) < 5)
                        { target = director.exitPoint; title = "도주 차량"; }
                        break;
                    case BankPrologue.Phase.Driving:
                        if (director.drive) target = director.drive.finish;
                        camera = director.filmCamera;
                        observer = director.getawayCar.position;
                        title = "도시 진입로";
                        break;
                }
            }
            marker.gameObject.SetActive(target && camera && camera.enabled);
            if (marker.gameObject.activeSelf)
                StoryTargetDiamond.Place(marker, camera, target.position + Vector3.up * 1.2f, observer, title);
        }
    }
}
