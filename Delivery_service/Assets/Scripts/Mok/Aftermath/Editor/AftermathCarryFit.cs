#if UNITY_EDITOR
using System.IO;using System.Linq;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathCarryFit
    {
        public static void Record()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-carry-fit.unity",true);
            var f=Object.FindFirstObjectByType<AftermathFilm>();var pickup=f.shots[10].motions.First(m=>!m.actor);
            pickup.subject.localScale*=.7f;
            pickup.to.position=new Vector3(0,1,3.63f);
            var carry=f.shots[11].motions.First(m=>!m.actor);carry.carryOffset=new Vector3(0,1,.53f);
            EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Aftermath-carry-fit.txt","Box resized to realistic hand span and moved within arm reach; torso clearance retained.");
            AftermathReview.Record();
        }
    }
}
#endif
