#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonPopulationCheck
    {
        const string Log = "Logs/Prison-population-check.txt";
        static int step;
        static double next;
        static Vector3[] starts;
        static bool running;
        static PrisonPopulation population;
        static int deadInmate, deadGuard, yieldsBefore;
        static NPCControl yieldingActor;

        static PrisonPopulationCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Prison.PopulationTest", false))
                {
                    SessionState.SetBool("Prison.PopulationTest", false);
                    running = true; step = 0; next = EditorApplication.timeSinceStartup + 5;
                }
                if (state == PlayModeStateChange.ExitingPlayMode) running = false;
            };
        }
        public static void Run()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save current scenes before testing");
            File.WriteAllText(Log, "Prison population Play Mode test\n");
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
            SessionState.SetBool("Prison.PopulationTest", true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool valid, string message)
        {
            File.AppendAllText(Log, (valid ? "PASS " : "FAIL ") + message + "\n");
            if (!valid) throw new Exception(message);
        }
        static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (step == 0)
                {
                    population = UnityEngine.Object.FindFirstObjectByType<PrisonPopulation>();
                    Check(population && population.slots.Length == 70, "70 managed roaming actors");
                    Check(population.slots.Count(s => s.guard) == 20, "20 guards");
                    Check(population.slots.Count(s => s.guard && s.holdPost) == 16, "16 fixed posts and 4 patrols");
                    Check(population.slots.Count(s => !s.guard) == 50, "50 inmates");
                    Check(population.slots.All(s => s.actor && s.actor.GetComponent<NavMeshAgent>().isOnNavMesh), "all actors on NavMesh");
                    starts = population.slots.Select(s => s.actor.transform.position).ToArray();
                    Check(!population.HiddenFromPlayer(PrisonMission.Current.player.transform.position), "no respawn beside player");
                    step++; next = EditorApplication.timeSinceStartup + 30; return;
                }
                if (step == 1)
                {
                    int inmates = 0, guards = 0;
                    for (int i = 0; i < starts.Length; i++)
                        if (Vector3.Distance(starts[i], population.slots[i].actor.transform.position) > 1)
                        { if (population.slots[i].guard) guards++; else inmates++; }
                    Check(inmates >= 45, "inmates moved: " + inmates + "/50");
                    Check(guards >= 3 && guards <= 6, "only patrol guards move: " + guards + "/20");
                    var mission=PrisonMission.Current;
                    var body=mission.player.GetComponent<CharacterController>();body.enabled=false;
                    mission.player.transform.SetPositionAndRotation(new Vector3(22,1,9),Quaternion.identity);body.enabled=true;
                    mission.cart.controller.minKnockdownSpeed=float.MaxValue;
                    mission.cart.controller.cartRigidbody.position=new Vector3(22,0,10);
                    mission.cart.controller.cartRigidbody.isKinematic=false;
                    yieldingActor=population.slots.First(s=>!s.guard).actor;
                    var agent=yieldingActor.GetComponent<NavMeshAgent>();agent.ResetPath();
                    NavMesh.SamplePosition(new Vector3(22,0,13),out var yieldPoint,1,NavMesh.AllAreas);agent.Warp(yieldPoint.position);
                    yieldsBefore=PrisonTraffic.CartYields;
                    step++;next=EditorApplication.timeSinceStartup+2;return;
                }
                if(step==2){
                    Check(PrisonTraffic.CartYields>yieldsBefore,"NPC chooses a path beside the active cart");
                    Check(Mathf.Abs(yieldingActor.transform.position.x-22)>1.4f,"NPC physically clears cart lane");
                    PrisonMission.Current.cart.controller.cartRigidbody.isKinematic=true;
                    var a = population.slots.First(s => !s.guard);
                    var b = population.slots.First(s => s.guard && s.holdPost);
                    deadInmate = a.actor.GetInstanceID(); deadGuard = b.actor.GetInstanceID();
                    a.actor.Die(Vector3.back, 2); b.actor.Die(Vector3.back, 2);
                    step++; next = EditorApplication.timeSinceStartup + 20; return;
                }
                Check(population.slots.Where(s=>s.guard).All(s=>s.actor.GetComponent<PrisonGuard>().holdPost==s.holdPost), "respawn preserves guard posts");
                Check(population.RespawnCount >= 2, "dead inmate and guard replenished");
                Check(population.slots.All(s => s.actor && !s.actor.IsDead), "population restored to 70");
                Check(population.slots.All(s => s.actor.GetInstanceID() != deadInmate && s.actor.GetInstanceID() != deadGuard), "new actors replace dead actors");
                Check(PrisonMission.Current.guards.All(g => g), "mission guard references restored");
                File.AppendAllText(Log, "RESULT: PASS\n");
                running = false; EditorApplication.ExitPlaymode();
            }
            catch (Exception e)
            {
                File.AppendAllText(Log, e + "\nRESULT: FAIL\n");
                running = false; EditorApplication.ExitPlaymode();
            }
        }
    }
}
#endif


