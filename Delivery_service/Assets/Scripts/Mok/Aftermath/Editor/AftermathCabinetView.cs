#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static class AftermathCabinetView
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-cabinetview-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>();
                var player = film.firstPerson.hideoutPlayer;
                Vector3 close = new Vector3(500.7f, 0, -.9f);
                for (int i = 1; i <= 3; i++)
                {
                    var motion = film.shots[i].motions.First(m => m.actor == player);
                    motion.from.position = i == 1 ? new Vector3(502.4f, 0, -1.8f) : close;
                    motion.to.position = close;
                    motion.moveDelay = i == 1 ? .45f : 0;
                }
                EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
