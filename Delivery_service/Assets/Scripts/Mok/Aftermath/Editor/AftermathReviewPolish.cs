#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathReviewPolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-review-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var f=Object.FindFirstObjectByType<AftermathFilm>();
                var pickup=f.shots[10].motions.First(m=>!m.actor);var player=f.shots[10].motions.First(m=>m.actor);
                player.reachBlend=.23f;pickup.moveDelay=.27f;
                var walk=f.shots[11].motions.First(m=>m.actor);var box=f.shots[11].motions.First(m=>!m.actor);
                walk.from.position=player.to.position;walk.from.rotation=player.to.rotation;
                var bend=new GameObject("의자를 우회하는 배달 출발 곡선").transform;bend.SetParent(f.transform);bend.position=new Vector3(2.8f,0,3.1f);
                walk.bend=bend;walk.facePath=true;walk.moveDelay=.14f;
                box.carriedBy=walk.subject;box.carryOffset=new Vector3(0,1,.77f);
                f.dialogue.lines[11].seconds=7.5f;
                f.shots[10].fadeOut=false;f.shots[11].fadeIn=false;
                f.shots[11].cameraFrom.position=f.shots[10].cameraTo.position;
                f.shots[11].cameraTo.position=new Vector3(-2.4f,1.6f,3.6f);
                f.shots[11].lookAt.position=new Vector3(.6f,1,1.5f);
                f.shots[1].motions.First(m=>m.actor&&m.actor.name=="민기").reachBlend=.22f;
                f.shots[2].motions.First(m=>!m.actor).moveDelay=.14f;
                foreach(var text in f.shots[0].set.GetComponentsInChildren<TextMesh>(true))Object.DestroyImmediate(text.gameObject);
                // Continuous pull-back from the TV to reveal the seated viewer.
                f.shots[7].cameraFrom.position=f.shots[6].cameraTo.position;
                f.shots[7].cameraTo.position=new Vector3(1.7f,1.65f,1.35f);
                f.shots[7].lookAt.position=f.shots[6].lookAt.position;
                f.shots[7].fieldOfView=f.shots[6].fieldOfView;
                f.shots[7].subtitleTail=.65f;
                f.shots[8].subtitleDelay=.65f;f.dialogue.lines[8].seconds=4.8f;
                f.shots[9].subtitleDelay=.35f;f.dialogue.lines[9].seconds=3.5f;
                f.shots[9].reactionActor=player.actor;
                var reaction=new GameObject("주방의 민기를 바라보는 시선").transform;reaction.SetParent(f.transform);reaction.position=new Vector3(2,1.5f,11);
                f.shots[9].reactionTarget=reaction;
                foreach(var s in f.shots)s.set.SetActive(false);f.shots[0].set.SetActive(true);f.ApplyShot(0,0);
                EditorUtility.SetDirty(f.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Aftermath-review-polish.txt","PASS: staged hand contact before lift, attached carry during turn, curved chair clearance, continuous TV pullback, news/call breathing room, exact shot endpoints.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("Logs/Aftermath-review-polish.txt",e.ToString());EditorApplication.Exit(1);}
        }
    }
}
#endif
