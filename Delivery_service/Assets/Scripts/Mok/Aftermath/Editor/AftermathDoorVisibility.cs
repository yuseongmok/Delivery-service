#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathDoorVisibility
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-door-visibility-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>();
                var h = film.hideout; h.cabinetDoor.localRotation = Quaternion.identity;
                h.doorGrip.position = new Vector3(500.4f, 1.38f, 1.4f);
                h.cabinetDoor.GetComponentsInChildren<Transform>().First(t => t.name == "문 손잡이").position = new Vector3(500.4f, 1.38f, 1.42f);
                var outer = h.cabinetDoor.GetComponentsInChildren<Transform>().First(t => t.name == "문 손잡이");
                var inner = h.cabinetDoor.Find("안쪽 당김 손잡이");
                if (!inner) { inner = Object.Instantiate(outer, h.cabinetDoor); inner.name = "안쪽 당김 손잡이"; }
                inner.position = new Vector3(500.4f, 1.38f, 1.58f);
                Vector3 viewingPosition = new Vector3(501.3f, 0, -.8f);
                for (int i = 1; i <= 3; i++)
                {
                    var motion = film.shots[i].motions.First(m => m.actor == film.firstPerson.hideoutPlayer);
                    if (i > 1) motion.from.position = viewingPosition;
                    motion.to.position = viewingPosition;
                }
                EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
