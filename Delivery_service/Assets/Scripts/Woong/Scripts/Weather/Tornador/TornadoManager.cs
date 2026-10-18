using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TornadoManager : MonoBehaviour
{
    [Header("토네이도 설정")]
    public WeatherManager weatherManager;
    public GameObject tornadoPrefab;
    [Tooltip("토네이도가 생성될 위치 ")]
    public Transform[] spawnPoints;

    [Tooltip("한 번에 유지할 토네이도 개수")]
    public int activeCount = 4;
    [Tooltip("토네이도 위치 갱신 주기")]
    public float refreshInterval = 30f;
    [Tooltip("하늘에서 떨어질 높이")]
    public float dropHeightOffset = 50f;

    private List<GameObject> activeTornadoes = new List<GameObject>();
    private float timer = 0f;
    public bool isTeleporting { get; private set; } = false;

    private void Update()
    {
        if (weatherManager == null) return;

        // 태풍이 불 때만 생성 사이클 작동
        if (weatherManager.isTyphoon)
        {
            if (activeTornadoes.Count == 0)
            {
                SpawnTornadoes();
            }

            timer += Time.deltaTime;
            if (timer >= refreshInterval)
            {
                RefreshTornadoes();
                timer = 0f;
            }
        }
        else
        {
            // 태풍이 끝나면 싹 다 지우기
            if (activeTornadoes.Count > 0)
            {
                ClearTornadoes();
                timer = 0f;
            }
        }
    }

    private void SpawnTornadoes()
    {
        if (spawnPoints.Length == 0 || tornadoPrefab == null) return;

        // 위치 배열을 섞어서 무작위로 선택
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < availablePoints.Count; i++)
        {
            Transform temp = availablePoints[i];
            int randomIndex = Random.Range(i, availablePoints.Count);
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        int spawnLimit = Mathf.Min(activeCount, availablePoints.Count);

        for (int i = 0; i < spawnLimit; i++)
        {
            GameObject tornado = Instantiate(tornadoPrefab, availablePoints[i].position, Quaternion.identity);
            Tornado tornadoScript = tornado.GetComponent<Tornado>();
            if (tornadoScript != null) tornadoScript.manager = this;

            activeTornadoes.Add(tornado);
        }
    }

    private void RefreshTornadoes()
    {
        ClearTornadoes();
        SpawnTornadoes();
    }

    private void ClearTornadoes()
    {
        foreach (var t in activeTornadoes)
        {
            if (t != null) Destroy(t);
        }
        activeTornadoes.Clear();
    }

    public void StartTeleportSequence(MotorcycleController bike, GameObject player)
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(bike, player));
    }

    private IEnumerator TeleportRoutine(MotorcycleController bike, GameObject player)
    {
        isTeleporting = true;

        bool isRiding = (bike != null && bike.isDriven);
        GameObject activeTarget = isRiding ? bike.gameObject : player;

        if (isRiding && bike.sphereRB != null) bike.SetControllable(false);
        if (!isRiding) player.GetComponent<CharacterController>().enabled = false;

        float floatTimer = 0f;
        while (floatTimer < 1.5f)
        {
            floatTimer += Time.deltaTime;

            if (isRiding)
            {
                bike.sphereRB.linearVelocity = Vector3.up * 15f;
                bike.transform.Rotate(0, 720f * Time.deltaTime, 0);
            }
            else
            {
                player.transform.position += Vector3.up * 15f * Time.deltaTime;
                player.transform.Rotate(0, 720f * Time.deltaTime, 0);
            }
            yield return null;
        }

        UnityEngine.UI.Image fadeUI = weatherManager.tornadoFadeUI;
        if (fadeUI != null)
        {
            fadeUI.gameObject.SetActive(true);
            float fadeTimer = 0f;
            while (fadeTimer < 1f)
            {
                fadeTimer += Time.deltaTime;
                Color c = fadeUI.color;
                c.a = Mathf.Lerp(0f, 1f, fadeTimer / 1f);
                fadeUI.color = c;
                yield return null;
            }
        }

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 dropPosition = randomSpawnPoint.position + (Vector3.up * dropHeightOffset);

        if (isRiding)
        {
            // 오토바이 탄 채로 순간이동
            bike.sphereRB.position = dropPosition;
            bike.transform.position = dropPosition;
            bike.sphereRB.linearVelocity = Vector3.zero;
        }
        else
        {
            // 몸만 순간이동
            player.transform.position = dropPosition;
        }

        yield return new WaitForSeconds(0.5f);

        // 페이드 인 추락 시작
        if (fadeUI != null)
        {
            float fadeTimer = 0f;
            while (fadeTimer < 1f)
            {
                fadeTimer += Time.deltaTime;
                Color c = fadeUI.color;
                c.a = Mathf.Lerp(1f, 0f, fadeTimer / 1f);
                fadeUI.color = c;
                yield return null;
            }
            fadeUI.gameObject.SetActive(false);
        }

      
        if (isRiding) bike.SetControllable(true);
        if (!isRiding) player.GetComponent<CharacterController>().enabled = true;

        isTeleporting = false;
    }
}