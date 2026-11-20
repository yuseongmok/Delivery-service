#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonInformantPolish
    {
        static PrisonInformantPolish() { EditorApplication.update += Run; }
        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists("Temp/PrisonInformantPolish")) return;
            File.Delete("Temp/PrisonInformantPolish");
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity"); bool added = !scene.isLoaded;
            if (added) scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity", OpenSceneMode.Additive);
            try
            {
                if (!EditorSceneManager.SaveScene(scene, "Logs/Prison-before-informant-polish-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true)) throw new System.Exception("Backup failed");
                var intro = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrisonIntroduction>(true)).Single();
                var entrance = intro.informantEntrance; var host = intro.informant;
                var start = host.position + host.forward * 1.65f + host.right * .8f;
                if (!NavMesh.SamplePosition(start, out var standing, 1, NavMesh.AllAreas)) throw new System.Exception("No standing point");
                if (Physics.OverlapCapsule(standing.position + Vector3.up * .5f, standing.position + Vector3.up * 1.6f, .25f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.gameObject.scene == scene && !c.transform.IsChildOf(entrance.petitioner))) throw new System.Exception("Petitioner overlaps furniture");
                entrance.petitioner.position = standing.position;
                entrance.petitioner.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(host.position - standing.position, Vector3.up));
                entrance.animator.applyRootMotion = false;
                var path = new NavMeshPath(); bool found = false;
                foreach (var offset in new[] { new Vector3(-6, 0, 2), new Vector3(-7, 0, 4), new Vector3(7, 0, 3) })
                {
                    var desired = host.position + host.right * offset.x + host.forward * offset.z;
                    if (!NavMesh.SamplePosition(desired, out var target, 1, NavMesh.AllAreas) || !NavMesh.CalculatePath(standing.position, target.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                    if (Physics.OverlapCapsule(target.position + Vector3.up * .5f, target.position + Vector3.up * 1.6f, .3f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.gameObject.scene == scene)) continue;
                    if (!entrance.exitPoint) { entrance.exitPoint = new GameObject("감사 인사를 마친 죄수 퇴장 위치").transform; entrance.exitPoint.SetParent(entrance.transform); }
                    entrance.exitPoint.position = target.position; found = true; break;
                }
                if (!found) throw new System.Exception("No clear connected exit route");
                var dialogue = intro.dialogue;
                File.Copy(AssetDatabase.GetAssetPath(dialogue), "Logs/Prison-dialogue-before-informant-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".asset", true);
                dialogue.informantEntrance = new[] { "죄수: 영감님 덕분에 작업반 옮겼습니다. 정말 감사합니다.", "최형욱: 됐어. 이제 조용히 지내.", "죄수: 예. 필요한 거 있으시면 말씀하십시오." };
                dialogue.meeting[0] = "최형욱: 이번엔 또 뭐야? 공무원 준비해야 하니까 꺼져";
                entrance.entranceLineSeconds = 3.2f; entrance.bowDegrees = 30;
                EditorUtility.SetDirty(dialogue); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/Prison-informant-polish.txt", "Saved petitioner=" + standing.position + " exit=" + entrance.exitPoint.position + " path corners=" + path.corners.Length);
            }
            catch (System.Exception e) { File.WriteAllText("Logs/Prison-informant-polish.txt", e.ToString()); }
            finally { if (added) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
#endif
