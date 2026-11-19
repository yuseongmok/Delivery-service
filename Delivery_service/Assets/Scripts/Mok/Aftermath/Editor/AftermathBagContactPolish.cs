#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathBagContactPolish
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-bag-contact-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>(); var h = film.hideout;
                h.cabinetDoor.localRotation = Quaternion.identity;
                var children = h.cabinetDoor.Cast<Transform>().ToArray();
                var positions = children.Select(t => t.position).ToArray();
                h.cabinetDoor.position = new Vector3(501.7f, .25f, 1.5f);
                for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
                h.openAngle = -105;
                h.hiddenBag = new Vector3(500.25f, .70f, 1.60f);
                h.bag.SetPositionAndRotation(h.hiddenBag, Quaternion.identity);
                h.leftGrip.position = h.hiddenBag + new Vector3(-.10f, .21f, .02f);
                h.rightGrip.position = h.hiddenBag + new Vector3(-.10f, .35f, -.22f);
                var pose = film.GetComponent<AftermathBagHands>();
                if (!pose) pose = film.gameObject.AddComponent<AftermathBagHands>();
                pose.hideout = h;
                EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
