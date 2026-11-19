#if UNITY_EDITOR
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathReviewFinal
    {
        public static void Record()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
            EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-camera-pan.unity",true);
            var f=Object.FindFirstObjectByType<AftermathFilm>();
            f.shots[11].lookFrom=f.shots[10].lookAt;
            EditorSceneManager.SaveScene(scene);
            AftermathReview.Record();
        }
    }
}
#endif
