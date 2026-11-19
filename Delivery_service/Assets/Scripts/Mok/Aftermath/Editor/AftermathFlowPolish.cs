#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static class AftermathFlowPolish
    {
        public static void Apply()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-flow-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                var film = Object.FindFirstObjectByType<AftermathFilm>();
                var handoff = film.firstPerson.identityHandoff;
                var receiverPosition = handoff.player.transform.position;
                receiverPosition.z = -.88f;
                handoff.player.transform.position = receiverPosition;
                handoff.offered = new Vector3(399.95f, .85f, -.15f);
                handoff.dealerGrip.localPosition = new Vector3(.17f, .025f, .20f);
                handoff.playerGrip.localPosition = new Vector3(.26f, .015f, -.20f);
                var key = film.shots[4].set.GetComponentsInChildren<Transform>(true).First(t => t.name == "새 매장 열쇠");
                key.position = new Vector3(399.45f, .85f, .4f);
                film.shots[5].lookAt.position = key.position;
                var walk = film.shots[14].motions.First(m => m.actor == film.firstPerson.shopPlayer);
                var receive = film.shots[13].motions.First(m => m.actor == film.firstPerson.shopPlayer);
                walk.from.SetPositionAndRotation(receive.to.position, receive.to.rotation);
                walk.moveDelay = .30f;
                var carry = film.shots[14].motions.First(m => m.carriedBy);
                carry.carryOffset = Quaternion.Inverse(walk.from.rotation) * (film.firstPerson.boxHeld - walk.from.position);
                film.dialogue.lines[14].seconds = 8;
                EditorUtility.SetDirty(film.dialogue);
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
                AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
