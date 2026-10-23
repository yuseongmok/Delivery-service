using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoliceManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject policePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTransform;

    [Header("사고 발생 시 출동 경찰 수")]
    [SerializeField] private int defaultSpawnCount = 3;

    [Header("경찰서 및 UI 설정 (씬 오브젝트 연결)")]
    [SerializeField] private Transform policeStationSpawn;
    [SerializeField] private Image fadeScreen;

    // 현재 추격 중인 경찰 객체 목록
    private List<PoliceControl> activePoliceList = new List<PoliceControl>();

    // 포위 오프셋 테이블
    private readonly (float angle, float dist)[] flankPattern = new (float, float)[]
    {
        (0f, 0f),      // 바로 뒤 추격
        (180f, 6f),    // 도주로 앞 지점 차단
        (-60f, 4f),    // 좌측 압박
        (60f, 4f),     // 우측 압박
        (-120f, 5f),   // 좌측 후방
        (120f, 5f)     // 우측 후방
    };

    // 사고 합의 거부 버튼 클릭 시 호출
    public void SpawnPoliceOnAccidentRefused()
    {
        activePoliceList.RemoveAll(p => p == null);

        if (activePoliceList.Count > 0)
        {
            foreach (PoliceControl police in activePoliceList)
            {
                if (police != null)
                {
                    police.ResetChaseTimers();
                }
            }
        }
        else
        {
            SpawnPoliceGroup(defaultSpawnCount);
        }
    }

    // 지정된 수만큼 경찰 스폰
    public void SpawnPoliceGroup(int count)
    {
        if (DeliveryService.Mok.StoryTutorial.Active) return;
        if (spawnPoints == null || spawnPoints.Length == 0 || policePrefab == null) return;

        activePoliceList.RemoveAll(p => p == null);

        for (int i = 0; i < count; i++)
        {
            Transform selectedSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject policeObj = Instantiate(policePrefab, selectedSpawn.position, selectedSpawn.rotation);
            PoliceControl police = policeObj.GetComponent<PoliceControl>();

            if (police != null)
            {
                var pattern = flankPattern[i % flankPattern.Length];
                police.Initialize(playerTransform, pattern.angle, pattern.dist, policeStationSpawn, fadeScreen);
                activePoliceList.Add(police);
            }
        }
    }
    // 공격마다 증원하지 않고 현재 무장 지원 경찰 한 명을 유지한다.
    public PoliceControl SpawnArmedSupport(Transform target)
    {
        if (DeliveryService.Mok.StoryTutorial.Active) return null;
        foreach(var existing in DeliveryService.Mok.SceneActors.Police){var fighter=existing.GetComponent<DeliveryService.Mok.PoliceCombat>();if(fighter!=null&&fighter.Armed&&!fighter.IsDead){existing.ResetChaseTimers();return existing;}}
        if(policePrefab==null||target==null)return null;
        for(int i=0;i<12;i++){
            Vector3 offset=Quaternion.Euler(0,i*30,0)*Vector3.forward*12;
            if(!UnityEngine.AI.NavMesh.SamplePosition(target.position+offset,out var point,4,UnityEngine.AI.NavMesh.AllAreas))continue;
            if(Vector3.Distance(point.position,target.position)<7)continue;
            var go=Instantiate(policePrefab,point.position,Quaternion.LookRotation(target.position-point.position));
            var cop=go.GetComponent<PoliceControl>();cop.Initialize(target,0,0,policeStationSpawn,fadeScreen);
            cop.GetComponent<DeliveryService.Mok.PoliceCombat>().Arm();activePoliceList.Add(cop);return cop;
        }
        return null;
    }

    int pendingNormal,pendingArmed;float refillAt,refillDeadline;Transform reinforcementTarget;
    public int PendingReplacements=>pendingNormal+pendingArmed;
    public void CancelReplacements(){pendingNormal=pendingArmed=0;reinforcementTarget=null;refillDeadline=0;}
    public void ReportDeath(PoliceControl fallen, bool armed, Transform target){
        if(fallen==null||PoliceControl.IsArrestInProgress)return;
        if(PendingReplacements==0)refillDeadline=0;
        if(armed)pendingArmed=1;else pendingNormal=Mathf.Min(3,pendingNormal+1);
        reinforcementTarget=target;refillAt=Time.time+2;
        refillDeadline=Mathf.Max(refillDeadline,Time.time+fallen.RemainingChaseSeconds);
    }
    public void ReportEscape(PoliceControl leaving){
        foreach(var cop in DeliveryService.Mok.SceneActors.Police){if(cop==leaving)continue;var fight=cop.GetComponent<DeliveryService.Mok.PoliceCombat>();if(fight==null||!fight.IsDead)return;}
        CancelReplacements();
    }
    void Update(){
        if(PendingReplacements==0)return;
        if(PoliceControl.IsArrestInProgress||Time.time>=refillDeadline){CancelReplacements();return;}
        if(Time.time<refillAt)return;
        int normal=0,armed=0;
        foreach(var cop in DeliveryService.Mok.SceneActors.Police){var fight=cop.GetComponent<DeliveryService.Mok.PoliceCombat>();if(fight!=null&&fight.IsDead)continue;if(fight!=null&&fight.Armed)armed++;else normal++;}
        int amount=Mathf.Min(pendingNormal,Mathf.Max(0,3-normal));
        if(amount>0){var before=new HashSet<PoliceControl>(activePoliceList);SpawnPoliceGroup(amount);foreach(var added in activePoliceList)if(added!=null&&!before.Contains(added))added.LimitChaseTime(refillDeadline-Time.time);}
        pendingNormal=0;
        if(pendingArmed>0&&armed<1){var added=SpawnArmedSupport(reinforcementTarget);if(added!=null){added.LimitChaseTime(refillDeadline-Time.time);pendingArmed=0;}else refillAt=Time.time+2;}
        else pendingArmed=0;
    }

}