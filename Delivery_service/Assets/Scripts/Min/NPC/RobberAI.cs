using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using DeliveryService.Mok;

[RequireComponent(typeof(NavMeshAgent), typeof(NPCControl))]
public class RobberAI : MonoBehaviour
{
    public enum Phase { Approach, Windup, Miss, StolenPause, Escape, Down }
    [Header("강탈과 도주")]
    [SerializeField] float attackDistance = 1.5f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float escapeSpeed = 6f;
    [SerializeField] float windupSeconds = 1f;
    [Header("강탈 후 회수 제한 · 현실 초")]
    [SerializeField] float stolenPizzaEscapeSeconds = 45f;
    float stolenElapsed;
    int stolenCash;
    public int StolenCash => stolenCash;
    public bool HasStolenLoot => stolen.Count>0||stolenCash>0;
    public float RecoverySecondsRemaining => HasStolenLoot?Mathf.Max(0,stolenPizzaEscapeSeconds-stolenElapsed):0;
    public static BikePizzaStorage RiddenCargo => FindObjectsByType<BikePizzaStorage>(FindObjectsSortMode.None).FirstOrDefault(s=>s.bikeController!=null&&s.bikeController.isDriven);
    public static bool RiderCanAct(BikePizzaStorage s) => s!=null&&s.bikeController.IsControlAvailable&&Time.timeScale>0&&Cursor.lockState==CursorLockMode.Locked&&!PoliceControl.IsArrestInProgress;
    Vector3 VictimPosition => RiddenCargo!=null?RiddenCargo.transform.position:player.transform.position;
    Camera ViewCamera {get{var cargo=RiddenCargo;return cargo!=null&&cargo.bikeController.bikeCamera!=null?cargo.bikeController.bikeCamera.GetComponentInChildren<Camera>():player!=null?player.View:null;}}
    public Phase State { get; private set; }
    public bool IsTestSpawn { get; set; }
    public float StateAge => Time.time - stateStarted;
    public int StolenCount => stolen.Count;
    public bool CanBeKicked => npc != null && !npc.IsDead && !npc.IsKnockedDown;
    NavMeshAgent agent;
    NPCControl npc;
    PlayerCombat player;
    ToppingInventory inventory;
    DayNightCycle clock;
    readonly List<PizzaData> stolen = new List<PizzaData>();
    float stateStarted, born, nextPath;
    int day;
    Vector3 exit;
    bool hasExit;
    bool encounterEnded;
    void EndEncounter(){if(encounterEnded)return;encounterEnded=true;RobberyManager.NotifyEncounterEnded();}
    RobberTheftFeedback feedback;

    void Awake()
    {
        feedback=GetComponent<RobberTheftFeedback>();if(feedback==null)feedback=gameObject.AddComponent<RobberTheftFeedback>();
        agent = GetComponent<NavMeshAgent>(); npc = GetComponent<NPCControl>();
        npc.ExternalControl = true;
        agent.speed = moveSpeed; agent.acceleration = 20; agent.angularSpeed = 540;
        agent.stoppingDistance = .9f; agent.autoBraking = true;
        var animator = GetComponent<Animator>(); if(animator != null) animator.applyRootMotion = false;
    }
    void Start()
    {
        SetTarget(null); clock = FindFirstObjectByType<DayNightCycle>();
        day = clock != null ? clock.currentDay : 0; born = Time.time;
        Change(Phase.Approach);
    }
    // 기존 승하차 코드의 호출 경로를 유지한다.
    public void SetTarget(Transform target)
    {
        player = FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
        inventory = player != null ? player.GetComponent<ToppingInventory>() : null;
    }
    void Change(Phase phase)
    {
        State = phase; stateStarted = Time.time;
        if(phase==Phase.Down||phase==Phase.Escape)EndEncounter();
        if(agent.enabled && agent.isOnNavMesh) agent.isStopped = phase != Phase.Approach && phase != Phase.Escape;
    }
    bool Available => player != null && player.CanReceiveHit && inventory != null && (inventory.HasPackagedPizzas()||RobberyMoney.CanTake);
    public bool CanReachVictim()
    {
        if(!Available) return false;
        Vector3 delta = player.transform.position - transform.position;
        if(Mathf.Abs(delta.y) > 1.6f || new Vector2(delta.x,delta.z).magnitude > attackDistance) return false;
        Vector3 start = transform.position + Vector3.up * 1.1f;
        Vector3 end = player.transform.position + Vector3.up * 1.1f;
        return !Physics.RaycastAll(start,(end-start).normalized,(end-start).magnitude,~0,QueryTriggerInteraction.Ignore)
            .Any(h=>!h.transform.IsChildOf(transform) && !h.transform.IsChildOf(player.transform));
    }
    void Update()
    {
        if(Time.timeScale <= 0) return;
        if(clock != null && (clock.currentDay != day || (!IsTestSpawn && !clock.isShopOpen))) { DropLoot(); Destroy(gameObject); return; }
        if(npc.IsDead) { DropLoot(); return; }
        // 시야나 경로 도착 여부와 무관하게 강탈 시점부터 회수 기회를 준다.
        if(HasStolenLoot){
            stolenElapsed+=Time.unscaledDeltaTime;
            if(stolenElapsed>=stolenPizzaEscapeSeconds){stolen.Clear();stolenCash=0;Destroy(gameObject);return;}
        }
        if(State == Phase.Down || !agent.enabled || !agent.isOnNavMesh) return;
        if(player == null) { SetTarget(null); return; }
        var ridden=RiddenCargo;
        // 주유·수리·메뉴로 조작할 수 없는 동안 준비 시간을 진행하지 않는다.
        bool inputBlocked=Cursor.lockState!=CursorLockMode.Locked || (ridden!=null&&!RiderCanAct(ridden));
        if(inputBlocked && (State==Phase.Approach||State==Phase.Windup)){
            agent.isStopped=true;born+=Time.deltaTime;stateStarted+=Time.deltaTime;return;
        }
        // 시민 초기화의 보행 속도 대신 강도 상태별 속도를 유지한다.
        agent.speed = State == Phase.Escape ? escapeSpeed : moveSpeed;
        switch(State)
        {
            case Phase.Approach:
                bool hasTarget=ridden!=null?(ridden.StoredCount>0||RobberyMoney.CanTake):Available;
                if(!hasTarget || Vector3.Distance(transform.position,VictimPosition)>25 || Time.time-born>30) { BeginEscape(); break; }
                agent.isStopped=false;
                Navigate(ridden!=null?CargoApproach(ridden):player.transform.position);
                if(ridden!=null){
                    if(CanReachCargo(ridden))TakeCargo(ridden);
                }else if(CanReachVictim()) { Change(Phase.Windup); }
                break;
            case Phase.Windup:
                FacePlayer();
                if(StateAge>=windupSeconds) {
                    if(CanReachVictim()) TakePackage();
                    else { Change(Phase.Miss); }
                }
                break;
            case Phase.Miss:
                if(StateAge>.9f) BeginEscape();
                break;
            case Phase.StolenPause:
                if(StateAge>=1f) BeginEscape();
                break;
            case Phase.Escape:
                if(!hasExit || (!agent.pathPending && agent.remainingDistance<1.2f)) {
                    if(!HasStolenLoot && !VisibleToPlayer() && StateAge>1f) { stolen.Clear(); Destroy(gameObject); return; }
                    FindExit();
                }
                if(hasExit) Navigate(exit);
                // 화면 앞에서 갑자기 사라지지 않도록 시야 밖에서만 퇴장한다.
                if(!HasStolenLoot && StateAge>45f && !VisibleToPlayer()) { stolen.Clear(); Destroy(gameObject); }
                break;
        }
    }
    Vector3 CargoApproach(BikePizzaStorage storage) => storage.transform.position-storage.bikeController.transform.forward*.75f;
    public bool CanReachCargo(BikePizzaStorage storage){
        if(storage==null||(storage.StoredCount==0&&!RobberyMoney.CanTake)||storage.bikeController==null)return false;
        var visual=storage.GetComponent<BikeCargoVisual>();
        if(visual==null||!visual.isActiveAndEnabled||(visual.IsBusy&&!visual.IsRobberyBy(this)))return false;
        var delta=storage.transform.position-transform.position;
        if(new Vector2(delta.x,delta.z).magnitude>1.7f||Mathf.Abs(delta.y)>2.2f)return false;
        var from=transform.position+Vector3.up*1.1f;var to=storage.transform.position+Vector3.up*.15f;
        return !Physics.RaycastAll(from,(to-from).normalized,(to-from).magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)&&h.collider!=storage.bikeController.sphereCollider&&!h.transform.IsChildOf(storage.bikeController.transform)&&!h.transform.IsChildOf(player.transform));
    }
    // 접근한 프레임에 한 상자를 옮긴다. 뚜껑 동작과 속도 대기는 없다.
    void TakeCargo(BikePizzaStorage storage){
        if(storage.StoredCount==0){TakeCash();return;}
        var visual=storage.GetComponent<BikeCargoVisual>();
        if(!visual.BeginRobbery(this))return;
        PizzaData pizza;
        try { pizza=storage.TakeOneForRobber(this); }
        finally { visual.EndRobbery(this); }
        if(pizza==null)return;
        stolen.Add(pizza);
        stolenElapsed=0;
        feedback.NotifyTheft();
        BeginEscape();
       
    }
    void OnDisable(){EndEncounter();}
    void FacePlayer(){FacePoint(player.transform.position);}
    void FacePoint(Vector3 point)
    {
        var d=point-transform.position; d.y=0;
        if(d.sqrMagnitude>.01f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),360*Time.deltaTime);
    }
    void TakePackage()
    {
        if(!inventory.HasPackagedPizzas()){TakeCash();return;}
        // 피자 데이터를 그대로 옮겨 레시피와 조리 상태를 보존한다. 배달통은 건드리지 않는다.
        stolen.AddRange(inventory.ClearPackagedPizzas());
        stolenElapsed=0;
        feedback.NotifyTheft();
        Change(Phase.StolenPause);
    }
    void TakeCash(){
        if(HasStolenLoot)return;
        stolenCash=RobberyMoney.TryTake();
        if(stolenCash<=0){BeginEscape();return;}
        stolenElapsed=0;feedback.NotifyTheft();BeginEscape();
    }
    void BeginEscape()
    {
        Change(Phase.Escape); agent.speed=escapeSpeed; agent.stoppingDistance=.25f;
        FindExit();
    }
    bool SetPath(Vector3 destination)
    {
        if(!NavMesh.SamplePosition(destination,out var hit,3f,agent.areaMask)) return false;
        if(agent.hasPath && !agent.pathPending && (agent.destination-hit.position).sqrMagnitude<.0625f) return true;
        var path = new NavMeshPath();
        var filter = new NavMeshQueryFilter {agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
        if(!NavMesh.CalculatePath(agent.nextPosition,hit.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete || path.corners.Length<2) return false;
        agent.isStopped=false; return agent.SetPath(path);
    }
    void Navigate(Vector3 destination)
    {
        if(Time.time<nextPath) return; nextPath=Time.time+.35f;
        SetPath(destination);
    }
    void FindExit()
    {
        hasExit=false;
        Vector3 away=player!=null?transform.position-VictimPosition:transform.forward; away.y=0;
        if(away.sqrMagnitude<.1f) away=transform.forward;
        foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
        {
            Vector3 wanted=transform.position+Quaternion.Euler(0,angle,0)*away.normalized*22f;
            if(NavMesh.SamplePosition(wanted,out var hit,8f,agent.areaMask) && Vector3.Distance(transform.position,hit.position)>8f && SetPath(hit.position)) {exit=hit.position;hasExit=true;break;}
        }
    }
    bool VisibleToPlayer()
    {
        if(player==null || ViewCamera==null) return false;
        var camera=ViewCamera; Vector3 point=transform.position+Vector3.up;
        Vector3 v=camera.WorldToViewportPoint(point);
        if(v.z<=0 || v.x<0 || v.x>1 || v.y<0 || v.y>1) return false;
        var start=camera.transform.position;
        return !Physics.RaycastAll(start,(point-start).normalized,Vector3.Distance(start,point),~0,QueryTriggerInteraction.Ignore)
            .Any(h=>!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(player.transform));
    }
    // 차량 충돌은 기존 시민 레그돌을 사용하고 훔친 피자는 회수 가능하게 남긴다.
    public bool ReceiveBikeImpact(Vector3 direction,float speed){
        if(npc==null||npc.IsDead||npc.IsKnockedDown||speed<2f)return false;
        DropLoot();Change(Phase.Down);
        npc.Die(direction,Mathf.Clamp(speed*3f,15f,60f));return true;
    }
    public bool ReceiveKick(Vector3 direction)
    {
        var fighter=GetComponent<SlumRobberCombat>();if(fighter!=null)return fighter.ReceiveKick(direction);
        if(!CanBeKicked) return false;
        DropLoot(); Change(Phase.Down);
        npc.KnockDown(direction,5f); StartCoroutine(Recover()); return true;
    }
    IEnumerator Recover()
    {
        yield return new WaitForSeconds(2.5f);
        while(!npc.IsDead && !npc.RecoverFromKnockDown()) yield return new WaitForSeconds(.5f);
        if(!npc.IsDead) BeginEscape();
    }
    public void DropLoot()
    {
        if(!HasStolenLoot) return;
        Vector3 position=transform.position+transform.forward*.9f;
        if(NavMesh.SamplePosition(position,out var hit,3f,NavMesh.AllAreas)) position=hit.position;
        if(stolen.Count>0){RobberLoot.Create(position,stolen);stolen.Clear();}
        if(stolenCash>0){RobberLoot.CreateCash(position,stolenCash);stolenCash=0;}
    }
}


