#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathEnvelopePolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-envelope-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>(); var h=film.hideout;
                h.bag.SetPositionAndRotation(h.hiddenBag,Quaternion.identity);
                var renderers=h.bag.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds;
                foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                Vector3 correction=new Vector3(Mathf.Max(0,500.25f-bounds.min.x),0,Mathf.Max(0,1.59f-bounds.min.z));
                h.hiddenBag+=correction; h.bag.position=h.hiddenBag;
                File.WriteAllText("Logs/Aftermath-bag-containment.txt","Before="+bounds+" correction="+correction);
                var pose=film.GetComponent<AftermathEnvelopeHand>();
                if(!pose) pose=film.gameObject.AddComponent<AftermathEnvelopeHand>();
                pose.handoff=film.firstPerson.identityHandoff;
                pose.handoff.offered.z = -.25f;
                EditorSceneManager.SaveScene(scene); AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
