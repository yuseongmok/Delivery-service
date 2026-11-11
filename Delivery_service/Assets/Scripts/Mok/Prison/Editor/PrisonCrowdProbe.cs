#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonCrowdProbe
    {
        static PrisonCrowdProbe()
        {
            EditorApplication.update += () =>
            {
                const string request = "Temp/Prison-crowd-probe";
                if (!EditorApplication.isPlaying || !File.Exists(request)) return;
                var label = File.ReadAllText(request).Trim(); File.Delete(request);
                var probe = new GameObject("Editor crowd sample").AddComponent<PrisonCrowdSample>(); probe.label = label;
            };
        }
    }
    public sealed class PrisonCrowdSample : MonoBehaviour
    {
        public string label;
        readonly List<float> frames = new List<float>();
        readonly List<double> cpu = new List<double>();
        ProfilerRecorder recorder;
        float start, next;
        int peakCluster;
        PrisonPopulation population;
        void Start()
        {
            Application.runInBackground = true;
            population = FindFirstObjectByType<PrisonPopulation>(); start = Time.realtimeSinceStartup;
            recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        }
        void Update()
        {
            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed < 5) return;
            frames.Add(Time.unscaledDeltaTime * 1000);
            if (recorder.Valid) cpu.Add(recorder.LastValue / 1000000.0);
            if (Time.realtimeSinceStartup > next)
            {
                next = Time.realtimeSinceStartup + 1;
                foreach (var a in population.slots)
                {
                    if (!a.actor) continue;
                    int near = 0;
                    foreach (var b in population.slots) if (b.actor && (a.actor.transform.position - b.actor.transform.position).sqrMagnitude < 9) near++;
                    peakCluster = Mathf.Max(peakCluster, near);
                }
            }
            if (elapsed < 35) return;
            frames.Sort(); cpu.Sort();
            File.WriteAllText("Logs/Prison-crowd-" + label + ".txt", $"Editor stationary camera; 30s sample, 5s warmup\nactors={population.slots.Count(s=>s.actor&&!s.actor.IsDead)}\nframe mean={frames.Average():F2}ms p95={frames[(int)(frames.Count*.95f)]:F2}ms\nmain-thread mean={(cpu.Count>0?cpu.Average():0):F2}ms\npeak actors within 3m={peakCluster}\n");
            Destroy(gameObject);
        }
        void OnDestroy() { recorder.Dispose(); }
    }
}
#endif
