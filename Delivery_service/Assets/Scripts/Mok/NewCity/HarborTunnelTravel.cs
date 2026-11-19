using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok.NewCity
{
    public sealed class HarborTunnelTravel : MonoBehaviour
    {
        [SerializeField] string destinationScene = "JJinCity";
        [SerializeField] float fadeSeconds = .7f;

        [Header("목적지 스폰 설정")]
        [SerializeField] string spawnObjectName = "HarborTunnelSpawn";

        [Tooltip("오브젝트를 찾지 못했을 때 아래 좌표로 강제 스폰할지 여부")]
        [SerializeField] bool useFallbackCoordinates = false;
        [SerializeField] Vector3 fallbackPosition;
        [SerializeField] Vector3 fallbackEulerAngles;

        bool loading;

        void OnTriggerEnter(Collider other)
        {
            if (loading) return;
            if (!other.GetComponentInParent<NewCitySurvey>() && !other.CompareTag("Player")) return;
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError("이동할 도시 씬이 빌드 목록에 없습니다.", this);
                return;
            }

            loading = true;
            StartCoroutine(Travel());
        }

        IEnumerator Travel()
        {
            var overlay = new GameObject("도시 이동 암전", typeof(Canvas));
            DontDestroyOnLoad(overlay);

            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            var panel = new GameObject("암전", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            // 전환 코루틴은 씬 로드 뒤에도 살아 있는 객체가 수행한다.
            var runner = overlay.AddComponent<HarborTunnelFade>();
            runner.Begin(
                destinationScene,
                fadeSeconds,
                image,
                spawnObjectName,
                useFallbackCoordinates,
                fallbackPosition,
                Quaternion.Euler(fallbackEulerAngles));

            yield break;
        }
    }

    public sealed class HarborTunnelFade : MonoBehaviour
    {
        public void Begin(
            string scene,
            float seconds,
            Image image,
            string spawnObjectName,
            bool useFallback,
            Vector3 fallbackPos,
            Quaternion fallbackRot)
        {
            StartCoroutine(Load(
                scene,
                Mathf.Max(.1f, seconds),
                image,
                spawnObjectName,
                useFallback,
                fallbackPos,
                fallbackRot));
        }

        IEnumerator Load(
            string scene,
            float seconds,
            Image image,
            string spawnObjectName,
            bool useFallback,
            Vector3 fallbackPos,
            Quaternion fallbackRot)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0, 0, 0, t / seconds);
                yield return null;
            }

            image.color = Color.black;
            Time.timeScale = 1;

            var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            yield return operation;

            // 새 씬의 Awake/Start 초기화가 끝난 직후에 위치 이동
            yield return null;
            PlacePlayerAtSpawn(spawnObjectName, useFallback, fallbackPos, fallbackRot);

            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0, 0, 0, 1 - t / seconds);
                yield return null;
            }

            Destroy(gameObject);
        }

        void PlacePlayerAtSpawn(string spawnObjectName, bool useFallback, Vector3 fallbackPos, Quaternion fallbackRot)
        {
            Vector3 targetPos;
            Quaternion targetRot;

            GameObject spawnObj = !string.IsNullOrEmpty(spawnObjectName) ? GameObject.Find(spawnObjectName) : null;
            if (spawnObj != null)
            {
                targetPos = spawnObj.transform.position;
                targetRot = spawnObj.transform.rotation;
            }
            else if (useFallback)
            {
                targetPos = fallbackPos;
                targetRot = fallbackRot;
            }
            else
            {
                Debug.LogWarning($"스폰 지점 '{spawnObjectName}'을(를) 찾지 못해 기본 위치에서 시작합니다.");
                return;
            }

            // 오토바이에 탑승한 채로 씬이 시작된 경우 오토바이 전체를 스폰 위치로 이동
            var bikes = FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
            for (int i = 0; i < bikes.Length; i++)
            {
                if (bikes[i] != null && bikes[i].isDriven)
                {
                    if (bikes[i].TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.position = targetPos;
                        rb.rotation = targetRot;
                        rb.Sleep();
                    }
                    bikes[i].transform.SetPositionAndRotation(targetPos, targetRot);
                    Physics.SyncTransforms();
                    return;
                }
            }

            // 도보 상태인 경우 플레이어 이동
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var move = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
                if (move != null) player = move.gameObject;
            }

            if (player != null)
            {
                Quaternion flatRot = Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f);

                if (player.TryGetComponent<CharacterController>(out var cc))
                {
                    cc.enabled = false;
                    player.transform.SetPositionAndRotation(targetPos, flatRot);
                    Physics.SyncTransforms();
                    cc.enabled = true;
                }
                else
                {
                    player.transform.SetPositionAndRotation(targetPos, flatRot);
                    Physics.SyncTransforms();
                }

                var camMove = player.GetComponentInChildren<CameraMove>(true);
                if (camMove != null)
                {
                    camMove.SyncViewFromTransform();
                }
            }
        }
    }
}