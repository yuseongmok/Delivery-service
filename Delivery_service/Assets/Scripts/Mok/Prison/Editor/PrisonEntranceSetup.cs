#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonEntranceSetup
    {
        static PrisonEntranceSetup() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/PrisonEntrance")) return;
            File.Delete("Temp/PrisonEntrance");
            try { Build(); } catch (System.Exception e) { File.WriteAllText("Logs/Prison-entrance.txt", e.ToString()); }
        }
        static void Build()
        {
            var m = Object.FindFirstObjectByType<PrisonKeysMission>();
            if (!m || m.gameObject.scene.name != "PrisonChapter") throw new System.Exception("감옥 씬을 열어 주세요");
            EditorSceneManager.SaveScene(m.gameObject.scene, "Logs/Prison-before-entrance.unity", true);
            if (!m.officeDoor.GetComponent<PrisonOfficeEntrance>())
            {
                var panel = m.officeDoor;
                var bounds = panel.GetComponent<Renderer>().bounds;
                var root = new GameObject("간수장실 여닫이문").transform;
                root.SetParent(panel.parent, false);
                root.position = new Vector3(bounds.center.x, bounds.min.y, bounds.max.z);
                panel.SetParent(root, true);
                var entrance = root.gameObject.AddComponent<PrisonOfficeEntrance>();
                entrance.hinge = root; entrance.mission = m;
                var wait = new GameObject("민기 간수장실 대기 위치").transform;
                wait.SetParent(m.transform, false);
                wait.position = new Vector3(bounds.center.x - 3.3f, bounds.min.y, bounds.center.z + 1.2f);
                entrance.insideWait = wait;
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = "문 손잡이"; handle.transform.SetParent(panel, false);
                handle.transform.position = bounds.center + new Vector3(.12f,-.15f,-.7f);
                handle.transform.rotation = Quaternion.identity;
                handle.transform.localScale = new Vector3(.7f,.035f,.15f);
                Object.DestroyImmediate(handle.GetComponent<Collider>());
                handle.GetComponent<Renderer>().sharedMaterial = panel.GetComponent<Renderer>().sharedMaterial;
                m.officeDoor = root;
            }
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
            EditorSceneManager.SaveScene(m.gameObject.scene);
            File.WriteAllText("Logs/Prison-entrance.txt", "PASS saved hinged door and companion waiting point");
        }
    }
}
#endif
