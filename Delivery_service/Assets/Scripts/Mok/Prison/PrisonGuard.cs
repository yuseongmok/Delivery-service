using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    public sealed class PrisonGuard : MonoBehaviour
    {
        public Transform[] route;
        public Transform gun;
        public bool infirmaryWatch;
        public bool holdPost;
        [HideInInspector] public bool receivingMeal;
        public Vector3 postPosition;
        public float postYaw;
        public Bounds restrictedArea = new Bounds(new Vector3(-30, 2, 4), new Vector3(26, 5, 22));
        public float sightRange = 10, sightAngle = 65, noticeSeconds = 1.5f;
        [Min(1)] public float distractionSeconds = 20;
        public float Suspicion { get; private set; }
        NavMeshAgent agent;
        NPCControl npc;
        Animator animator;
        int waypoint;
        float waitUntil, distractedUntil;
        Vector3 distraction;
        bool shooting;float nextShot;
        public bool IsAggressive=>shooting;
        public int ShotsFired {get;private set;}
        float nextThink, lastThink, yieldUntil, nextYield;
        NavMeshPath yieldPath;
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        void Start()
        {
            yieldPath = new NavMeshPath();
            agent = GetComponent<NavMeshAgent>(); npc = GetComponent<NPCControl>();
            animator = GetComponentInChildren<Animator>(); npc.ExternalControl = true;
            agent.speed = Random.Range(1.65f, 1.95f); agent.stoppingDistance = .25f;
            nextThink = Time.time + Random.Range(0f, .2f); lastThink = Time.time;
        }
        public void ResetWatch() { shooting=false;Suspicion=0;distractedUntil=0;waitUntil=0;yieldUntil=0;nextShot=Time.time+1;if(npc)npc.ExternalControl=true;if(agent&&agent.enabled&&agent.isOnNavMesh){agent.ResetPath();agent.isStopped=false;} }
        public void Distract(Vector3 point) { distraction = point; distractedUntil = Time.time + distractionSeconds; Suspicion = 0; }
        void Update()
        {
            var mission = PrisonMission.Current;
            if (!mission || !agent || !agent.isOnNavMesh || mission.Failing) return;
            if (npc.IsKnockedDown || npc.IsDead) return;
            if(Time.timeScale<=0)return;
            if(shooting){PursueAndShoot(mission.player);return;}
            if (animator) animator.SetFloat(MoveSpeed, agent.velocity.magnitude, .15f, Time.deltaTime);
            // 배식 대상은 카트를 피해서 상호작용 범위 밖으로 이동하지 않는다.
            if(receivingMeal){if(agent.hasPath)agent.ResetPath();agent.isStopped=true;Face(mission.player.transform.position);return;}
            if (holdPost && Time.time >= distractedUntil && (transform.position-postPosition).sqrMagnitude < .5f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0,postYaw+Mathf.Sin(Time.time*.35f+GetInstanceID()%20)*40,0),35*Time.deltaTime);
            if (Time.time < nextThink) return;
            float elapsed = Mathf.Min(.4f, Time.time-lastThink); lastThink=Time.time; nextThink=Time.time+.2f;
            if(mission.InConversation){agent.isStopped=true;Suspicion=0;return;}
            agent.isStopped=false;
            if (Time.time>=nextYield && PrisonTraffic.YieldToCart(agent,yieldPath)) {nextYield=Time.time+1.5f;yieldUntil=Time.time+3;}
            if (Time.time<yieldUntil) return;
            if (Time.time < distractedUntil)
            {
                if ((!agent.hasPath || (agent.destination-distraction).sqrMagnitude>.1f) && PrisonTraffic.PathBudget()) agent.SetDestination(distraction);
                if (agent.remainingDistance < 1) Face(distraction);
            }
            else if (holdPost)
            {
                if ((transform.position-postPosition).sqrMagnitude>.3f)
                { if ((!agent.hasPath || (agent.destination-postPosition).sqrMagnitude>.1f) && PrisonTraffic.PathBudget())agent.SetDestination(postPosition); }
                else { if(agent.hasPath)agent.ResetPath();agent.isStopped=true; }
            }
            else if (route.Length > 0 && Time.time >= waitUntil)
            {
                // 일반 NPC 초기화나 카트 회피가 남긴 경로를 지정 순찰 경로로 되돌린다.
                if (!agent.pathPending && (!agent.hasPath || (agent.destination-route[waypoint].position).sqrMagnitude>.25f) && PrisonTraffic.PathBudget()) agent.SetDestination(route[waypoint].position);
                if (agent.hasPath && !agent.pathPending && agent.remainingDistance < .5f)
                { agent.ResetPath(); waypoint = (waypoint + 1) % route.Length; waitUntil = Time.time + Random.Range(1f, 3f); }
            }
            bool trespassing = infirmaryWatch && (mission.State == PrisonMission.Phase.Infirmary || mission.State == PrisonMission.Phase.Talk || mission.State == PrisonMission.Phase.Return)
                && restrictedArea.Contains(mission.player.transform.position);
            Suspicion = Mathf.Clamp01(Suspicion + elapsed * (trespassing && Time.time >= distractedUntil && CanSee(mission.player) ? 1 / noticeSeconds : -.8f));
            if (Suspicion >= 1) mission.Fail(false);
        }
        public bool CanSee(PlayerCombat player)
        {
            Vector3 eye = transform.position + Vector3.up * 1.55f;
            Vector3 delta = player.View.transform.position - eye;
            if (delta.magnitude > sightRange || Vector3.Angle(transform.forward, delta) > sightAngle * .5f) return false;
            if (Physics.Raycast(eye, delta.normalized, out var hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return hit.transform.IsChildOf(player.transform);
            return true;
        }
        void Face(Vector3 point)
        {
            var direction = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 220 * Time.deltaTime);
        }
        public void Attacked(PlayerCombat player)
        {
            if(shooting||!PrisonMission.Current||PrisonMission.Current.Failing||PrisonMission.Current.recovery&&PrisonMission.Current.recovery.Recovering)return;
            shooting=true;nextShot=Time.time+.35f;Suspicion=1;
            if(npc)npc.ExternalControl=true;
            Face(player.transform.position);
        }
        void PursueAndShoot(PlayerCombat player)
        {
            var recovery=PrisonMission.Current.recovery;
            if(player.Health<=0||recovery&&recovery.Recovering){if(agent.hasPath)agent.ResetPath();return;}
            Vector3 eye=transform.position+Vector3.up*1.5f,target=player.View.transform.position;
            bool visible=true;
            foreach(var hit in Physics.RaycastAll(eye,(target-eye).normalized,Vector3.Distance(eye,target),~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(player.transform)){visible=false;break;}
            float distance=Vector3.Distance(transform.position,player.transform.position);
            if((!visible||distance>10)&&Time.time>=nextThink){nextThink=Time.time+.3f;agent.isStopped=false;if(NavMesh.SamplePosition(player.transform.position,out var p,1.5f,NavMesh.AllAreas))agent.SetDestination(p.position);}
            else if(visible&&distance<=10){if(agent.hasPath)agent.ResetPath();agent.isStopped=true;}
            Face(player.transform.position);if(animator)animator.SetFloat(MoveSpeed,agent.velocity.magnitude,.15f,Time.deltaTime);
            if(!visible||distance>25||Time.time<nextShot||!gun)return;
            nextShot=Time.time+.7f;ShotsFired++;
            ArmedHandPose.Apply(animator,gun,target,true,false);var muzzle=HandgunGeometry.Get(gun).Muzzle;
            GunshotSound.Play(gameObject,true);GunShotFeedback.Fire(gun,muzzle);
            PrologueTracer.Spawn(gun.TransformPoint(muzzle),target,new Color(1,.65f,.2f));
            player.ReceiveBullet((player.transform.position-transform.position).normalized);
        }
        void LateUpdate()
        {
            if (gun && gun.gameObject.activeInHierarchy && animator && PrisonMission.Current && npc && !npc.IsDead && !npc.IsKnockedDown)
                ArmedHandPose.Apply(animator, gun, PrisonMission.Current.player.View.transform.position, shooting, false);
        }
    }
}
