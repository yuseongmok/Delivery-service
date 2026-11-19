#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static class AftermathFirstPersonSetup
 {
 public static void Apply()
 {
 try {
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
 EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-firstperson-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var f=Object.FindFirstObjectByType<AftermathFilm>();
 var view=f.GetComponent<AftermathFirstPerson>();if(!view)view=f.gameObject.AddComponent<AftermathFirstPerson>();f.firstPerson=view;
 view.forestPlayer=f.shots[0].motions.First(m=>m.actor&&m.actor.name=="플레이어").actor;
 view.shopPlayer=f.shots[13].motions.First(m=>m.actor).actor;view.mingi=f.shots[9].motions[0].actor;
 var pickup=f.shots[13].motions.First(m=>!m.actor);view.box=pickup.subject;
 var player=f.shots[13].motions.First(m=>m.actor);view.leftPalm=player.leftContact;view.rightPalm=player.rightContact;
 Transform Contact(string name,Vector3 local){var t=view.box.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(view.box,false);}t.localPosition=local;return t;}
 view.mingiLeft=Contact("민기 상자 밀기 왼손",new Vector3(.43f,.055f,.3f));view.mingiRight=Contact("민기 상자 밀기 오른손",new Vector3(-.43f,.055f,.3f));
 view.leftPalm.localPosition=new Vector3(-.63f,-.01f,-.42f);view.rightPalm.localPosition=new Vector3(.63f,-.01f,-.42f);
 view.boxRest=new Vector3(1,.97f,6.45f);view.boxOffered=new Vector3(1,.97f,6.26f);view.boxHeld=new Vector3(1,1.06f,6.5f);
 // First person for every shot containing the player, including the forest.
 foreach(var s in f.shots)s.fieldOfView=60;
 f.shots[0].lookAt.position=new Vector3(305,1,9);
 f.shots[1].lookAt.position=new Vector3(300,.9f,-.3f);
 f.shots[2].lookAt.position=new Vector3(300,1,-.3f);f.shots[3].lookAt.position=new Vector3(300,1.35f,-.12f);
 f.shots[7].lookAt.position=f.shots[8].lookAt.position=new Vector3(4,2.65f,10.95f);
 f.shots[8].name="수배 뉴스를 보며 굳어지는 시선";f.dialogue.lines[8].scene=f.shots[8].name;
 f.shots[9].lookAt.position=f.shots[8].lookAt.position;f.dialogue.lines[9].subtitle="플레이어: …아직도 우리를 찾고 있네.";
 f.shots[10].lookFrom=f.shots[8].lookAt;f.shots[10].lookAt.position=new Vector3(1,1.45f,7.2f);f.shots[10].subtitleDelay=.2f;
 f.dialogue.lines[10].subtitle="민기: …야, 듣고 있어? 피자 다 됐어. 얼른 갔다 와.";f.dialogue.lines[10].seconds=4.5f;
 f.dialogue.lines[11].subtitle="플레이어: …어. 알았어. 금방 다녀올게.";f.dialogue.lines[11].seconds=2.8f;
 f.shots[11].lookAt.position=new Vector3(1,1.45f,7.2f);f.shots[11].reactionTarget=view.mingi.transform;
 f.shots[12].lookAt.position=new Vector3(1,1.25f,7.2f);f.dialogue.lines[12].seconds=3.5f;
 f.shots[13].fieldOfView=74;f.shots[14].fieldOfView=68;f.shots[13].lookAt.position=new Vector3(1,.95f,6.2f);f.shots[13].name="상자를 밀어주고 양손으로 받기";f.dialogue.lines[13].scene=f.shots[13].name;f.dialogue.lines[13].seconds=4;
 f.shots[14].motions.First(m=>!m.actor).carryOffset=new Vector3(0,1.06f,.7f);f.dialogue.lines[14].seconds=6;
 foreach(var s in f.shots)s.set.SetActive(false);f.shots[0].set.SetActive(true);f.ApplyShot(0,0);
 EditorUtility.SetDirty(f.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Aftermath-firstperson.txt","Saved first-person presentation, delayed attention shift, two-stage slide/contact/lift and editable dialogue.");
 AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-firstperson.txt",e.ToString());EditorApplication.Exit(1);}
 }
 }
}
#endif
