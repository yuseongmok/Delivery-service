using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 최형욱의 현재 배치를 기준으로 세 사람의 대화 구도를 잡는다.
    public sealed class PrisonMeetingPresentation : MonoBehaviour
    {
        public PrisonIntroduction introduction;
        public Transform playerActor;
        Camera camera;
        Vector3 mingiPosition;
        Quaternion mingiRotation;
        bool active;
        Renderer[] gameplayRenderers;
        bool[] rendererEnabled;

        public void Begin(Camera film)
        {
            camera = film;
            var host = introduction.informant;
            var companion = introduction.mingi;
            mingiPosition = companion.transform.position;
            mingiRotation = companion.transform.rotation;
            companion.Following = false;
            Vector3 playerSpot = host.position + host.forward * 1.8f - host.right * .7f;
            Vector3 mingiSpot = host.position + host.forward * 1.65f + host.right * .85f;
            if (NavMesh.SamplePosition(mingiSpot, out var hit, .8f, NavMesh.AllAreas)) mingiSpot = hit.position;
            if (companion.agent.isOnNavMesh) { companion.agent.ResetPath(); companion.agent.Warp(mingiSpot); }
            companion.transform.rotation = Face(companion.transform.position, host.position);
            playerActor.SetPositionAndRotation(playerSpot, Face(playerSpot, host.position));
            playerActor.gameObject.SetActive(true);
            gameplayRenderers = introduction.mission.player.GetComponentsInChildren<Renderer>(true);
            rendererEnabled = new bool[gameplayRenderers.Length];
            for(int i=0;i<gameplayRenderers.Length;i++)
            {
                rendererEnabled[i]=gameplayRenderers[i].enabled;
                gameplayRenderers[i].enabled=false;
            }
            active = true;
            // 먼저 세 사람을 한 화면에 보여 주어 대화 상대와 위치를 설명한다.
            Shot(host.position + host.right * 3.1f + host.forward * 2.5f + Vector3.up * 1.65f,
                host.position + host.forward * .8f + Vector3.up * 1.15f, 55);
        }

        public void ShowLine(string line)
        {
            if (!active) return;
            var host = introduction.informant;
            bool mingi = line.TrimStart().StartsWith("민기:");
            bool player = line.TrimStart().StartsWith("나:") || line.TrimStart().StartsWith("플레이어:");
            if (mingi || player)
            {
                var speaker = mingi ? introduction.mingi.transform : playerActor;
                // 최형욱 어깨 너머로 발화자를 보여 준다.
                Shot(host.position + host.right * (mingi ? -.65f : .65f) + host.forward * .35f + Vector3.up * 1.6f,
                    speaker.position + Vector3.up * 1.3f, 52);
            }
            else
            {
                // 플레이어 옆에서 최형욱을 바라보는 역구도.
                Shot(playerActor.position - host.right * .4f + Vector3.up * 1.6f,
                    host.position + Vector3.up * 1.4f, 48);
            }
        }

        void Shot(Vector3 position, Vector3 target, float fov)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
            camera.fieldOfView = fov;
        }
        static Quaternion Face(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from; direction.y = 0;
            return Quaternion.LookRotation(direction, Vector3.up);
        }
        public void End()
        {
            if (!active) return;
            active = false;
            playerActor.gameObject.SetActive(false);
            for(int i=0;i<gameplayRenderers.Length;i++)
                if(gameplayRenderers[i])gameplayRenderers[i].enabled=rendererEnabled[i];
            var companion = introduction.mingi;
            if (companion.agent.enabled && companion.agent.isOnNavMesh) companion.agent.Warp(mingiPosition);
            companion.transform.rotation = mingiRotation;
        }
        void OnDisable() { End(); }
    }
}
