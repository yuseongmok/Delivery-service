#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DeliveryService.Mok.Editor
{
    // 완료 상태를 넘겨 연결을 검사한다. 이동 배치와 전투 주입은 개별 로그에 명시한다.
    [InitializeOnLoad]
    public static class PrisonChapterCheck
    {
        public static bool Active { get => SessionState.GetBool("PrisonContinuousCheck", false); private set => SessionState.SetBool("PrisonContinuousCheck", value); }
        static int chapter;
        static readonly string[] Names = { "Introduction", "Chicken", "Medicine", "Keys", "Office", "Escape" };
        static PrisonChapterCheck() { EditorApplication.update += Poll; UnityEngine.Application.logMessageReceived += (message, trace, type) => { if(Active && (type==UnityEngine.LogType.Exception || type==UnityEngine.LogType.Error || type==UnityEngine.LogType.Assert)) { SessionState.SetBool("PrisonCheckRuntimeError",true); File.AppendAllText("Logs/Prison-chapter-check.txt","RUNTIME ERROR "+message+"\n"+trace+"\n"); } }; }
        static void Poll()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/PrisonCartCheck"))
            {
                File.Delete("Temp/PrisonCartCheck");SessionState.SetBool("PrisonCartCheck",true);PrisonKeysCheck.Run();return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/PrisonChapterCheck")) return;
            File.Delete("Temp/PrisonChapterCheck");
            if (EditorSceneManager.GetActiveScene().name != "PrisonChapter") return;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Logs/Prison-before-chapter-review.unity", true);
            Active = true; chapter = 0; SessionState.SetBool("PrisonCheckRuntimeError",false);
            File.WriteAllText("Logs/Prison-chapter-check.txt", "Same play session; no mission completion seeding. Intro collision traversal; later travel/combat assisted.\n");
            PrisonIntroductionCheck.Run();
        }
        public static void Finish(bool passed)
        {
            passed &= !SessionState.GetBool("PrisonCheckRuntimeError",false);
            File.AppendAllText("Logs/Prison-chapter-check.txt", (passed ? "PASS " : "FAIL ") + Names[chapter] + "\n");
            if (!passed || ++chapter == Names.Length) { Active = false; EditorApplication.ExitPlaymode(); return; }
            EditorApplication.delayCall += () =>
            {
                switch (chapter)
                {
                    case 1: PrisonChickenCheck.ContinueChapter(); break;
                    case 2: PrisonMedicineCheck.ContinueChapter(); break;
                    case 3: SessionState.SetBool("PrisonCartCheck",true); PrisonKeysCheck.ContinueChapter(); break;
                    case 4: PrisonOfficeCheck.ContinueChapter(); break;
                    case 5: PrisonEscapeCheck.ContinueChapter(); break;
                }
            };
        }
    }
}
#endif


