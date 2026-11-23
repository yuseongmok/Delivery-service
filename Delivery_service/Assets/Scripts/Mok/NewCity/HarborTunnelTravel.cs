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

        [Header("목적지 플레이어 스폰 설정")]
        [SerializeField] string spawnObjectName = "HarborTunnelSpawn";
        [SerializeField] bool useFallbackCoordinates = false;
        [SerializeField] Vector3 fallbackPosition;
        [SerializeField] Vector3 fallbackEulerAngles;

        [Header("목적지 오토바이 스폰 설정")]
        [SerializeField] string bikeSpawnObjectName = "HarborTunnelBikeSpawn";
        [SerializeField] GameObject motorcyclePrefab;

        [SerializeField] bool useBikeFallbackCoordinates = false;
        [SerializeField] Vector3 bikeFallbackPosition;
        [SerializeField] Vector3 bikeFallbackEulerAngles;

        bool loading;

        void OnTriggerEnter(Collider other)
        {
            if (loading) return;

            var hitBike = other.GetComponentInParent<MotorcycleController>();
            bool isDrivenBike = hitBike != null && hitBike.isDriven;

            if (!other.GetComponentInParent<NewCitySurvey>() && !other.CompareTag("Player") && !isDrivenBike)
                return;

            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError("이동할 도시 씬이 빌드 목록에 없습니다.", this);
                return;
            }

            MotorcycleController activeBike = isDrivenBike ? hitBike : FindActiveOrNearbyBike();
            bool hasBike = activeBike != null;

            loading = true;
            StartCoroutine(Travel(activeBike, hasBike));
        }

        MotorcycleController FindActiveOrNearbyBike()
        {
            var bikes = FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
            for (int i = 0; i < bikes.Length; i++)
            {
                if (bikes[i] != null && bikes[i].isDriven)
                    return bikes[i];
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                for (int i = 0; i < bikes.Length; i++)
                {
                    if (bikes[i] != null && Vector3.Distance(player.transform.position, bikes[i].transform.position) <= 3.5f)
                        return bikes[i];
                }
            }

            return null;
        }

        IEnumerator Travel(MotorcycleController activeBike, bool hasBike)
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

            var runner = overlay.AddComponent<HarborTunnelFade>();
            runner.Begin(
                destinationScene,
                fadeSeconds,
                image,
                activeBike,
                hasBike,
                motorcyclePrefab,
                spawnObjectName,
                useFallbackCoordinates,
                fallbackPosition,
                Quaternion.Euler(fallbackEulerAngles),
                bikeSpawnObjectName,
                useBikeFallbackCoordinates,
                bikeFallbackPosition,
                Quaternion.Euler(bikeFallbackEulerAngles));

            yield break;
        }
    }

    public sealed class HarborTunnelFade : MonoBehaviour
    {
        public void Begin(
            string scene,
            float seconds,
            Image image,
            MotorcycleController activeBike,
            bool hasBike,
            GameObject motorcyclePrefab,
            string spawnObjectName,
            bool useFallback,
            Vector3 fallbackPos,
            Quaternion fallbackRot,
            string bikeSpawnObjectName,
            bool useBikeFallback,
            Vector3 bikeFallbackPos,
            Quaternion bikeFallbackRot)
        {
            StartCoroutine(Load(
                scene,
                Mathf.Max(.1f, seconds),
                image,
                activeBike,
                hasBike,
                motorcyclePrefab,
                spawnObjectName,
                useFallback,
                fallbackPos,
                fallbackRot,
                bikeSpawnObjectName,
                useBikeFallback,
                bikeFallbackPos,
                bikeFallbackRot));
        }

        IEnumerator Load(
            string scene,
            float seconds,
            Image image,
            MotorcycleController activeBike,
            bool hasBike,
            GameObject motorcyclePrefab,
            string spawnObjectName,
            bool useFallback,
            Vector3 fallbackPos,
            Quaternion fallbackRot,
            string bikeSpawnObjectName,
            bool useBikeFallback,
            Vector3 bikeFallbackPos,
            Quaternion bikeFallbackRot)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0, 0, 0, t / seconds);
                yield return null;
            }

            image.color = Color.black;
            Time.timeScale = 1;

            // 화면이 완전히 검게 변한 뒤 플레이어와 오토바이를 분리하고, 프리팹이 없으면 오토바이를 씬 전환 동안 유지
            MotorcycleController carriedBike = null;
            if (activeBike != null)
            {
                if (activeBike.isDriven)
                {
                    try { activeBike.ExitBike(); } catch { }
                }

                if (motorcyclePrefab == null)
                {
                    activeBike.transform.SetParent(null, true);
                    DontDestroyOnLoad(activeBike.gameObject);
                    carriedBike = activeBike;
                }
            }

            var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            yield return operation;

            // 새 씬의 Awake/Start 초기화가 끝난 직후 스폰 위치 적용
            yield return null;
            PlacePlayerAndBikeAtSpawn(
                hasBike,
                carriedBike,
                motorcyclePrefab,
                spawnObjectName,
                useFallback,
                fallbackPos,
                fallbackRot,
                bikeSpawnObjectName,
                useBikeFallback,
                bikeFallbackPos,
                bikeFallbackRot);

            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0, 0, 0, 1 - t / seconds);
                yield return null;
            }

            Destroy(gameObject);
        }

        void PlacePlayerAndBikeAtSpawn(
            bool hasBike,
            MotorcycleController carriedBike,
            GameObject motorcyclePrefab,
            string spawnObjectName,
            bool useFallback,
            Vector3 fallbackPos,
            Quaternion fallbackRot,
            string bikeSpawnObjectName,
            bool useBikeFallback,
            Vector3 bikeFallbackPos,
            Quaternion bikeFallbackRot)
        {
            Vector3 playerPos = Vector3.zero;
            Quaternion playerRot = Quaternion.identity;
            bool hasPlayerSpawn = false;

            GameObject spawnObj = !string.IsNullOrEmpty(spawnObjectName) ? GameObject.Find(spawnObjectName) : null;
            if (spawnObj != null)
            {
                playerPos = spawnObj.transform.position;
                playerRot = spawnObj.transform.rotation;
                hasPlayerSpawn = true;
            }
            else if (useFallback)
            {
                playerPos = fallbackPos;
                playerRot = fallbackRot;
                hasPlayerSpawn = true;
            }

            // 1. 플레이어 스폰 위치 배치
            if (hasPlayerSpawn)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    var move = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
                    if (move != null) player = move.gameObject;
                }

                if (player != null)
                {
                    Quaternion flatRot = Quaternion.Euler(0f, playerRot.eulerAngles.y, 0f);

                    if (player.TryGetComponent<CharacterController>(out var cc))
                    {
                        cc.enabled = false;
                        player.transform.SetPositionAndRotation(playerPos, flatRot);
                        Physics.SyncTransforms();
                        cc.enabled = true;
                    }
                    else
                    {
                        player.transform.SetPositionAndRotation(playerPos, flatRot);
                        Physics.SyncTransforms();
                    }

                    var camMove = player.GetComponentInChildren<CameraMove>(true);
                    if (camMove != null)
                    {
                        camMove.SyncViewFromTransform();
                    }
                }
            }

            // 2. 오토바이를 타고 들어온 경우 지정된 위치에 오토바이 스폰
            if (!hasBike)
            {
                if (carriedBike != null) Destroy(carriedBike.gameObject);
                return;
            }

            Vector3 targetBikePos;
            Quaternion targetBikeRot;

            GameObject bikeSpawnObj = !string.IsNullOrEmpty(bikeSpawnObjectName) ? GameObject.Find(bikeSpawnObjectName) : null;
            if (bikeSpawnObj != null)
            {
                targetBikePos = bikeSpawnObj.transform.position;
                targetBikeRot = bikeSpawnObj.transform.rotation;
            }
            else if (useBikeFallback)
            {
                targetBikePos = bikeFallbackPos;
                targetBikeRot = bikeFallbackRot;
            }
            else if (hasPlayerSpawn)
            {
                // 오토바이 스폰 오브젝트를 아직 배치하지 않았다면 플레이어 스폰 위치 바로 옆에 배치
                targetBikePos = playerPos + playerRot * new Vector3(1.8f, 0f, 0f);
                targetBikeRot = playerRot;
            }
            else
            {
                if (carriedBike != null) Destroy(carriedBike.gameObject);
                return;
            }

            MotorcycleController targetBike = null;

            if (motorcyclePrefab != null)
            {
                if (carriedBike != null) Destroy(carriedBike.gameObject);
                GameObject spawned = Instantiate(motorcyclePrefab, targetBikePos, targetBikeRot);
                targetBike = spawned.GetComponentInChildren<MotorcycleController>(true);
            }
            else
            {
                // 목적지 씬(JJinCity)에 이미 배치된 오토바이가 있으면 그 오토바이를 이동, 없으면 타고 온 오토바이를 이동
                var sceneBikes = FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneBikes.Length; i++)
                {
                    if (sceneBikes[i] != null && sceneBikes[i] != carriedBike)
                    {
                        targetBike = sceneBikes[i];
                        break;
                    }
                }

                if (targetBike != null)
                {
                    if (carriedBike != null) Destroy(carriedBike.gameObject);
                }
                else if (carriedBike != null)
                {
                    SceneManager.MoveGameObjectToScene(carriedBike.gameObject, SceneManager.GetActiveScene());
                    targetBike = carriedBike;
                }
            }

            if (targetBike != null)
            {
                targetBike.gameObject.SetActive(true);
                targetBike.isDriven = false;
                if (targetBike.bikeCamera != null)
                {
                    targetBike.bikeCamera.SetActive(false);
                }

                if (targetBike.TryGetComponent<Rigidbody>(out var rb))
                {
                    bool wasKinematic = rb.isKinematic;
                    rb.isKinematic = true;
                    rb.position = targetBikePos;
                    rb.rotation = targetBikeRot;
                    rb.isKinematic = wasKinematic;
                    if (!rb.isKinematic)
                    {
                        rb.Sleep();
                    }
                }

                targetBike.transform.SetPositionAndRotation(targetBikePos, targetBikeRot);
                Physics.SyncTransforms();
            }
        }
    }
}