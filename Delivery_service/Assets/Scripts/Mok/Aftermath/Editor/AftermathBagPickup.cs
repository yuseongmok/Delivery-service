#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathBagPickup
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-bag-pickup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>(); var h = film.hideout;
                h.hiddenBag = new Vector3(500.45f, .70f, 1.75f);
                h.bag.SetPositionAndRotation(h.hiddenBag, Quaternion.identity);
                h.leftGrip.position = h.hiddenBag + new Vector3(-.10f, .17f, .02f);
                h.rightGrip.position = h.hiddenBag + new Vector3(-.10f, .17f, -.22f);
                EditorSceneManager.SaveScene(scene); AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
