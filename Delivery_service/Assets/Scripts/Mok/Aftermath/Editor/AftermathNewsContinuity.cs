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
    public static partial class AftermathSetup
    {
        public static void FixNewsSeat()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                EditorSceneManager.SaveScene(scene, "Logs/Aftermath-before-news-seat-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                film = Object.FindFirstObjectByType<AftermathFilm>(); root = film.transform;
                var player = film.firstPerson.shopPlayer;
                var seat = film.shots[8].motions.First(m => m.actor == player);
                Vector3 position = seat.from.position; Quaternion rotation = seat.from.rotation;
                for (int i = 7; i <= 10; i++)
                {
                    var motions = film.shots[i].motions == null ? new System.Collections.Generic.List<AftermathFilm.Motion>() : film.shots[i].motions.ToList();
                    var motion = motions.FirstOrDefault(m => m.actor == player);
                    if (motion == null) { motion = Move(player.transform, position, position, rotation.eulerAngles.y, player); motions.Add(motion); }
                    motion.from.SetPositionAndRotation(position, rotation); motion.to.SetPositionAndRotation(position, rotation);
                    motion.seatedFrom = motion.seatedTo = seat.seatedFrom;
                    motion.reach = 0; motion.leftContact = motion.rightContact = null; motion.bend = null;
                    film.shots[i].motions = motions.ToArray();
                }
                var stand = film.shots[11].motions.First(m => m.actor == player);
                stand.from.SetPositionAndRotation(position, rotation);
                film.ApplyShot(7, 1);
                Vector3 endPosition = film.film.transform.position; Quaternion endRotation = film.film.transform.rotation; float endFov = film.film.fieldOfView;
                film.ApplyShot(8, 0);
                float gap = Vector3.Distance(endPosition, film.film.transform.position);
                float angle = Quaternion.Angle(endRotation, film.film.transform.rotation);
                float fovGap = Mathf.Abs(endFov - film.film.fieldOfView);
                if (gap > .001f || angle > .01f || fovGap > .001f) throw new Exception("News transition discontinuity: " + gap + ", " + angle + ", " + fovGap);
                File.WriteAllText("Logs/Aftermath-news-seat-check.txt", "PASS news boundary: position=" + gap + " rotation=" + angle + " FOV=" + fovGap + ". Explicit seated actor pose in shots 7–10; standing starts from same pose.");
                foreach (var shot in film.shots) shot.set.SetActive(false);
                film.shots[0].set.SetActive(true); film.ApplyShot(0, 0);
                EditorSceneManager.SaveScene(scene); AftermathReview.Record();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
