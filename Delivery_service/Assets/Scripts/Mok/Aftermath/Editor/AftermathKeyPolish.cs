#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathKeyPolish
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-key-gesture-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>(); var view = film.firstPerson;
                var gesture = film.GetComponent<AftermathShopKeyGesture>();
                if (!gesture) gesture = film.gameObject.AddComponent<AftermathShopKeyGesture>();
                gesture.mingi = view.mingi; gesture.viewer = view.shopPlayer.transform;
                gesture.key = view.shopKeyProp.transform;
                gesture.key.localScale = new Vector3(.021f, .012f, .12f);
                gesture.presented = new Vector3(.78f, 1.22f, 6.80f);
                gesture.lowered = new Vector3(.70f, .82f, 7.04f);
                gesture.key.SetPositionAndRotation(gesture.presented, Quaternion.Euler(70, 0, 0));
                gesture.grip = gesture.key.GetComponentsInChildren<Transform>(true).First(t => t.name == "민기 열쇠 쥐는 손");
                gesture.grip.position = gesture.presented + new Vector3(.01f, .10f, .025f);
                view.shopKeyGesture = gesture;
                EditorSceneManager.SaveScene(scene); AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
