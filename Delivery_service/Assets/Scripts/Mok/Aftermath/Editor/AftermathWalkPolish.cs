#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathWalkPolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-walk-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>();
                var dealer=film.firstPerson.identityHandoff.dealer;
                var position=dealer.transform.position;position.z=.88f;dealer.transform.position=position;
                foreach(var shot in film.shots)
                    foreach(var motion in shot.motions)
                        if(motion.actor==dealer){motion.from.position=position;motion.to.position=position;}
                var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic-Regular.ttf");
                if(!font)throw new Exception("Mission subtitle font missing");
                foreach(var label in film.subtitle.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true))
                {
                    label.font=font;label.fontStyle=FontStyle.Normal;label.lineSpacing=1.15f;
                }
                film.subtitle.fontSize=34;
                film.subtitle.color=new Color(1,.97f,.90f);
                var shadow=film.subtitle.GetComponent<Shadow>();if(!shadow)shadow=film.subtitle.gameObject.AddComponent<Shadow>();
                shadow.effectColor=new Color(0,0,0,.95f);shadow.effectDistance=new Vector2(1.5f,-1.5f);
                EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Aftermath-walk-check.txt","Dealer root z="+position.z+"; desk rear edge z=0.65; subtitle="+font.name);
                AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
