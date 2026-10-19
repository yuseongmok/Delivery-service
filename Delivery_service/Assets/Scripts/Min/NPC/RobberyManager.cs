using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

public class RobberyManager : MonoBehaviour
{
    [SerializeField] DayNightCycle dayNightCycle;
    [SerializeField] GameObject robberPrefab;
    [SerializeField] Transform playerTarget;
    [Header("토핑별 출몰 설정 · 현실 초")]
    [SerializeField] float lowInterval=60f, mediumInterval=45f, highInterval=30f;
    [SerializeField] float lowChance=15f, mediumChance=25f, highChance=35f;
    [SerializeField] float encounterCooldown=90f;
    [Header("빈민촌 출몰 설정 · 현실 초")]
    [SerializeField] float districtInterval=12f, districtChance=85f, districtCooldown=20f;
    [SerializeField] int districtWaveMin=2, districtWaveMax=3, districtMaxActive=3;
    RobberyDistrict district;
    float districtProtectedUntil;
    public bool InHighRiskDistrict => district!=null&&player!=null&&district.Contains(RobberAI.RiddenCargo!=null?RobberAI.RiddenCargo.transform.position:player.transform.position);
    public int ActiveLimit => InHighRiskDistrict?Mathf.Max(1,districtMaxActive):1;
    float encounterProgress;
    BoxCollider shopFloor;
    public static bool HasCompletedDelivery {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession(){HasCompletedDelivery=false;}
    public static void NotifyDeliveryCompleted(){HasCompletedDelivery=true;}
    int currentDay=-1;
    float protectedUntil;
    PlayerCombat player;
    RobberAI active;
    bool CanOperate => RobberAI.RiddenCargo!=null?RobberAI.RiderCanAct(RobberAI.RiddenCargo):player!=null&&player.CanAct;
    public int CarriedToppingCount {
        get {
            var cargo=RobberAI.RiddenCargo;
            if(cargo!=null)return cargo.MaxExtraToppingCount;
            var pizzas=player!=null?player.GetComponent<ToppingInventory>().GetHeldPackagedPizzas():null;
            return pizzas==null?0:pizzas.Select(CountExtraToppings).DefaultIfEmpty(0).Max();
        }
    }
    public static int CountExtraToppings(PizzaData pizza) => pizza?.toppings==null?0:pizza.toppings
        .Where(t=>!string.IsNullOrWhiteSpace(t)).Select(t=>t.Trim().ToLowerInvariant())
        .Where(t=>t!="도우"&&t!="소스"&&t!="치즈"&&t!="dough"&&t!="sauce"&&t!="cheese").Distinct().Count();
    public float CheckInterval => InHighRiskDistrict?districtInterval:CarriedToppingCount>=4?highInterval:CarriedToppingCount>=2?mediumInterval:lowInterval;
    public float SpawnChance => InHighRiskDistrict?districtChance:CarriedToppingCount>=4?highChance:CarriedToppingCount>=2?mediumChance:lowChance;
    bool HasTargetPizza => (RobberAI.RiddenCargo!=null?RobberAI.RiddenCargo.StoredCount>0:player!=null&&player.GetComponent<ToppingInventory>().HasPackagedPizzas())||RobberyMoney.CanTake;
    void Start(){
        district=FindFirstObjectByType<RobberyDistrict>();
        dayNightCycle=dayNightCycle!=null?dayNightCycle:FindFirstObjectByType<DayNightCycle>();
        player=FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
        shopFloor=GameObject.Find("PizzaStore")?.transform.Find("Mok Pizza Shop/Foundation")?.GetComponent<BoxCollider>();
    }
    public bool IsInsideShop {
        get {
            if(shopFloor==null||player==null)return false;
            var cargo=RobberAI.RiddenCargo;
            var position=cargo!=null?cargo.transform.position:player.transform.position;
            var local=shopFloor.transform.InverseTransformPoint(position)-shopFloor.center;
            return Mathf.Abs(local.x)<=shopFloor.size.x*.5f&&Mathf.Abs(local.z)<=shopFloor.size.z*.5f&&Mathf.Abs(local.y)<5;
        }
    }
    public bool CanSpawnNow => dayNightCycle!=null && dayNightCycle.isShopOpen
        && Time.timeScale>0 && FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Length<ActiveLimit
        && player!=null && CanOperate && Time.unscaledTime>=(InHighRiskDistrict?districtProtectedUntil:protectedUntil) && HasTargetPizza && !IsInsideShop
        && !FindObjectsByType<PoliceControl>(FindObjectsSortMode.None).Any(p=>p.RemainingChaseSeconds>0);
    public static void NotifyEncounterEnded(){
        var manager=FindFirstObjectByType<RobberyManager>();
        if(manager==null)return;
        // 한 무리의 다른 강도가 아직 접근 중이면 보호시간을 먼저 시작하지 않는다.
        if(FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Any(r=>r.isActiveAndEnabled&&(r.State==RobberAI.Phase.Approach||r.State==RobberAI.Phase.Windup||r.State==RobberAI.Phase.StolenPause)))return;
        if(manager.InHighRiskDistrict)manager.districtProtectedUntil=Time.unscaledTime+manager.districtCooldown;
        else manager.protectedUntil=Time.unscaledTime+manager.encounterCooldown;
        manager.encounterProgress=0;
    }
    void Update(){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(Input.GetKeyDown(KeyCode.F8)){TrySpawnForTesting();return;}
#endif
        if(dayNightCycle!=null&&currentDay!=dayNightCycle.currentDay){currentDay=dayNightCycle.currentDay;encounterProgress=0;}
        if(player!=null&&player.IsDown){protectedUntil=Mathf.Max(protectedUntil,Time.unscaledTime+30);districtProtectedUntil=Mathf.Max(districtProtectedUntil,Time.unscaledTime+30);}
        if(!CanSpawnNow)return;
        // 들기·승하차로 진행률을 초기화하지 않고, 안전한 장소에서는 잠시 멈춘다.
        encounterProgress+=Time.unscaledDeltaTime/Mathf.Max(1,CheckInterval);
        if(encounterProgress<1)return;
        encounterProgress=0;
        if(Random.Range(0f,100f)<SpawnChance)TrySpawn();
    }
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트 소환은 등장 간격·확률·보호시간을 바꾸지 않는다.
    public bool TrySpawnForTesting(){
        if(Time.timeScale<=0){Debug.Log("[강도 테스트] 일시정지를 해제해 주세요.");return false;}
        if(player==null)player=FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
        if(player==null||!CanOperate){Debug.Log("[강도 테스트] 조작 가능한 상태에서 창을 닫고 F8을 눌러주세요.");return false;}
        if(FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Length>=ActiveLimit){Debug.Log("[강도 테스트] 이미 강도가 있습니다.");return false;}
        bool result=TrySpawn(true);
        Debug.Log(result?"[강도 테스트] 소환 완료. 손 또는 배달통의 포장 피자를 노립니다.":"[강도 테스트] 소환 가능한 도로가 없습니다. 바깥 도로에서 다시 눌러주세요.");
        return result;
    }
#endif
    bool TrySpawn(bool testSpawn=false){
        int available=ActiveLimit-FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Length;
        int count=InHighRiskDistrict?Random.Range(Mathf.Max(1,districtWaveMin),Mathf.Max(districtWaveMin,districtWaveMax)+1):1;
        bool spawned=false;
        for(int n=0;n<Mathf.Min(count,available);n++)spawned=TrySpawnOne(testSpawn)||spawned;
        if(spawned&&InHighRiskDistrict)districtProtectedUntil=Time.unscaledTime+districtCooldown;
        return spawned;
    }
    bool TrySpawnOne(bool testSpawn){
        if(robberPrefab==null||player==null)return false;
        var ridden=RobberAI.RiddenCargo;
        playerTarget=ridden!=null?ridden.bikeController.transform:player.transform;
        var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
        if(!NavMesh.SamplePosition(playerTarget.position,out var target,3f,filter))return false;
        for(int i=0;i<24;i++){
            Vector2 circle=Random.insideUnitCircle.normalized*Random.Range(12f,18f);
            if(!NavMesh.SamplePosition(target.position+new Vector3(circle.x,0,circle.y),out var point,4f,filter))continue;
            if(Vector3.Distance(point.position,target.position)<10f||Mathf.Abs(point.position.y-target.position.y)>2)continue;
            if(InHighRiskDistrict&&!district.Contains(point.position))continue;
            if(FindObjectsByType<RobberAI>(FindObjectsSortMode.None).Any(r=>(r.transform.position-point.position).sqrMagnitude<6.25f))continue;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(point.position,target.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            // 화면에 보이는 위치에서는 갑자기 생성하지 않는다.
            var camera=ridden!=null&&ridden.bikeController.bikeCamera!=null?ridden.bikeController.bikeCamera.GetComponentInChildren<Camera>():player.View;
            if(camera==null)return false;
            var screen=camera.WorldToViewportPoint(point.position+Vector3.up);
            if(!testSpawn&&screen.z>0&&screen.x>-.1f&&screen.x<1.1f&&screen.y>-.1f&&screen.y<1.1f)continue;
            active=Instantiate(robberPrefab,point.position,Quaternion.identity).GetComponent<RobberAI>();
            active.IsTestSpawn=testSpawn;
            active.GetComponent<NavMeshAgent>().Warp(point.position);active.SetTarget(playerTarget);
            if(InHighRiskDistrict)active.gameObject.AddComponent<SlumRobberCombat>();
            return true;
        }
        return false;
    }
}



