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
        bool shooting;
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
        public void ResetWatch() { Suspicion = 0; distractedUntil = 0; }
        public void Distract(Vector3 point) { distraction = point; distractedUntil = Time.time + distractionSeconds; Suspicion = 0; }
        void Update()
        {
            var mission = PrisonMission.Current;
            if (!mission || !agent || !agent.isOnNavMesh || mission.Failing || shooting) return;
            if (npc.IsKnockedDown || npc.IsDead) return;
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
                if (!agent.hasPath && !agent.pathPending && PrisonTraffic.PathBudget()) agent.SetDestination(route[waypoint].position);
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
            if (shooting || !PrisonMission.Current || PrisonMission.Current.Failing) return;
            shooting = true;
            if (agent && agent.isOnNavMesh) agent.isStopped = true;
            Face(player.transform.position);
            if (gun)
            {
                ArmedHandPose.Apply(animator, gun, player.View.transform.position, true, false);
                var muzzle = HandgunGeometry.Get(gun).Muzzle;
                GunshotSound.Play(gameObject, true); GunShotFeedback.Fire(gun, muzzle);
                PrologueTracer.Spawn(gun.TransformPoint(muzzle), player.View.transform.position, new Color(1,.65f,.2f));
            }
            PrisonMission.Current.Fail(true);
        }
        void LateUpdate()
        {
            if (gun && gun.gameObject.activeInHierarchy && animator && PrisonMission.Current && npc && !npc.IsDead && !npc.IsKnockedDown)
                ArmedHandPose.Apply(animator, gun, PrisonMission.Current.player.View.transform.position, shooting, false);
        }
    }
}




