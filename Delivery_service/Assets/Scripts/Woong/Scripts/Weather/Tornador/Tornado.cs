using UnityEngine;

public class Tornado : MonoBehaviour
{
    [HideInInspector]
    public TornadoManager manager;

    [Header("끌어당김 설정")]
    [Tooltip("자력이 미치는 최대 범위")]
    public float pullRadius = 30f;
    [Tooltip("텔레포트가 발동되는 중심부 범위")]
    public float coreRadius = 3f;
    [Tooltip("끌어당기는 최대 힘 ")]
    public float maxPullForce = 25f;

    private MotorcycleController bike;
    private GameObject player;

    private void Start()
    {
        bike = FindObjectOfType<MotorcycleController>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (manager != null && manager.isTeleporting) return;

        // 플레이어가 오토바이에 타고 있는지 확인
        if (bike != null && bike.isDriven)
        {
            PullTarget(bike.gameObject, true);
        }
        else if (player != null)
        {
            PullTarget(player, false);
        }
    }

    private void PullTarget(GameObject target, bool isBike)
    {
        Vector3 directionToTornado = transform.position - target.transform.position;
        // Y축 차이는 무시하고 평면으로만 계산
        directionToTornado.y = 0;
        float distance = directionToTornado.magnitude;

        // 범위 내에 들어오면 자력 발동
        if (distance < pullRadius)
        {
            // 가까울수록 끌어당기는 힘이 강해짐
            float forceMultiplier = 1f - (distance / pullRadius);
            float currentPullForce = maxPullForce * forceMultiplier;

            if (isBike)
            {
                // 오토바이는 물리 엔진으로 끌어당김
                if (bike.sphereRB != null)
                {
                    bike.sphereRB.AddForce(directionToTornado.normalized * currentPullForce, ForceMode.Acceleration);
                }
            }
            else
            {
                // 걸어다니는 플레이어는 CharacterController로 끌어당김
                CharacterController cc = target.GetComponent<CharacterController>();
                if (cc != null && cc.enabled)
                {
                    cc.Move(directionToTornado.normalized * currentPullForce * Time.deltaTime);
                }
            }

            // 중심부에 닿았을 때 텔레포트 시퀀스 시작
            if (distance <= coreRadius)
            {
                manager.StartTeleportSequence(bike, player);
            }
        }
    }
}