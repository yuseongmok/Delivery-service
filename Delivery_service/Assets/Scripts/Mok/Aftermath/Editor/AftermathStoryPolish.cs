#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static class AftermathStoryPolish
 {
 static Transform root;
 static Transform Mark(string name,Vector3 p,float yaw=0){var t=new GameObject(name).transform;t.SetParent(root);t.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return t;}
 static AftermathFilm.Motion Move(Transform t,Vector3 a,Vector3 b,float yaw=0,AftermathActor actor=null){return new AftermathFilm.Motion{subject=t,from=Mark("동작 시작",a,yaw),to=Mark("동작 끝",b,yaw),actor=actor};}
 static AftermathFilm.Shot Shot(string name,GameObject set,Vector3 a,Vector3 b,Vector3 look,params AftermathFilm.Motion[] motions){return new AftermathFilm.Shot{name=name,set=set,cameraFrom=Mark(name+" 시작",a),cameraTo=Mark(name+" 끝",b),lookAt=Mark(name+" 시선",look),motions=motions,fieldOfView=45};}
 public static void Apply(){try{
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
 EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-story-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var f=Object.FindFirstObjectByType<AftermathFilm>();root=f.transform;
 if(f.shots.Any(s=>s.nightCard))throw new Exception("Already applied; preserve current scene.");
 var old=f.shots;var shop=old[6].set;var ts=shop.GetComponentsInChildren<Transform>(true);
 Transform Find(string n)=>ts.First(t=>t && t.name==n);
 Object.DestroyImmediate(Find("TV 받침대").gameObject);
 var tv=Find("TV 프레임");tv.position=new Vector3(4,2.65f,10.95f);
 Find("TV 뉴스 화면").position=new Vector3(4,2.65f,10.859f);
 var mount=GameObject.CreatePrimitive(PrimitiveType.Cube);mount.name="TV 천장 고정 브래킷";mount.transform.SetParent(shop.transform);mount.transform.position=new Vector3(4,3.65f,11);mount.transform.localScale=new Vector3(.08f,1.1f,.08f);mount.GetComponent<Renderer>().sharedMaterial=tv.GetComponent<Renderer>().sharedMaterial;
 foreach(var light in shop.GetComponentsInChildren<Light>(true))if(light.color.b>light.color.r*1.3f)light.transform.position=new Vector3(4,2.6f,10.5f);
 old[6].cameraFrom.position=new Vector3(4,2.65f,8.6f);old[6].cameraTo.position=new Vector3(4,2.65f,8.4f);old[6].lookAt.position=tv.position;
 old[7].cameraFrom.position=new Vector3(-1.4f,1.7f,1.4f);old[7].cameraTo.position=new Vector3(-1.1f,1.7f,1.7f);old[7].lookAt.position=new Vector3(2,1.8f,8);old[7].fieldOfView=54;
 var player=old[10].motions.First(m=>m.actor).actor;var mingi=old[8].motions.First(m=>m.actor).actor;
 old[7].motions[0].from.rotation=old[7].motions[0].to.rotation=Quaternion.Euler(0,27,0);
 var boxMotion=old[10].motions.First(m=>!m.actor);var box=boxMotion.subject;var left=old[10].motions.First(m=>m.actor).leftContact;var right=old[10].motions.First(m=>m.actor).rightContact;
 var lid=box.GetComponentsInChildren<Transform>(true).First(t=>t.name=="LidHinge");
 var table=Find("배달 준비 테이블");table.position=new Vector3(1,.47f,11.15f);table.localScale=new Vector3(1.3f,.94f,.8f);table.name="포장 픽업 작업대";
 box.position=new Vector3(1,.96f,11.15f);box.rotation=Quaternion.identity;
 var grip=Mark("뚜껑을 닫는 민기 손",Vector3.zero);grip.SetParent(lid,false);grip.localPosition=new Vector3(.22f,0,-.65f);
 var rest=Mark("상자를 지탱하는 민기 손",new Vector3(.68f,1,11.45f));
 var pack=Shot("상자를 닫는 민기",shop,new Vector3(-.6f,1.9f,10),new Vector3(-.4f,1.8f,10.1f),new Vector3(1,1.1f,11.6f),Move(mingi.transform,new Vector3(1,0,12),new Vector3(1,0,12),180,mingi));
 pack.closingLid=lid;pack.motions[0].leftContact=grip;pack.motions[0].rightContact=rest;pack.motions[0].reach=1;
 old[8].motions[0].reach=0;old[8].reactionActor=mingi;old[8].reactionTarget=player.transform;
 old[9].motions[0].from.rotation=Quaternion.Euler(0,27,0);old[9].subtitleDelay=.8f;
 var approach=Shot("카운터로 다가가다",shop,new Vector3(-2,1.8f,7),new Vector3(-2,1.8f,8),new Vector3(1,1,10),Move(player.transform,new Vector3(0,0,3.1f),new Vector3(1,0,10.5f),0,player));
 var receive=old[10];receive.name="민기에게 피자 상자를 받다";receive.cameraFrom.position=new Vector3(-1,1.65f,10);receive.cameraTo.position=new Vector3(-.8f,1.6f,10);receive.lookAt.position=new Vector3(1,1.05f,11.2f);
 boxMotion.from.position=new Vector3(1,.96f,11.15f);boxMotion.to.position=new Vector3(1,1,11.03f);boxMotion.moveDelay=.4f;
 var pm=receive.motions.First(m=>m.actor);pm.from.position=pm.to.position=new Vector3(1,0,10.5f);pm.reachBlend=.35f;
 var retreat=old[11].motions.First(m=>m.actor);retreat.from.position=new Vector3(1,0,10.5f);retreat.to.position=new Vector3(0,0,-1.5f);retreat.bend.position=new Vector3(3,0,6);retreat.moveDelay=.1f;
 old[11].cameraFrom.position=new Vector3(-2.3f,1.8f,8);old[11].cameraTo.position=new Vector3(-2,1.7f,5);old[11].lookAt.position=new Vector3(1,1,5);old[11].lookFrom=null;old[11].chapterCard=false;
 var night=Shot("그날 밤",old[0].set,new Vector3(300,7,-9),new Vector3(300,7,-9),new Vector3(300,1,0));night.nightCard=true;night.night=true;night.fadeIn=true;night.fadeOut=true;
 old[1].night=old[2].night=true;old[1].fadeIn=true;old[0].fadeOut=true;
 var newShots=new List<AftermathFilm.Shot>();var lines=new List<AftermathDialogue.Line>();
 void Add(AftermathFilm.Shot s,string text,float seconds){newShots.Add(s);lines.Add(new AftermathDialogue.Line{scene=s.name,subtitle=text,seconds=seconds});}
 for(int i=0;i<old.Length;i++){
 if(i==1)Add(night,"",2.8f);
 if(i==8)Add(pack,"",2.8f);
 if(i==10)Add(approach,"",5);
 newShots.Add(old[i]);lines.Add(f.dialogue.lines[i]);
 }
 f.shots=newShots.ToArray();f.dialogue.lines=lines.ToArray();f.dialogue.lines.Last().seconds=9;
 foreach(var s in f.shots)s.set.SetActive(false);f.shots[0].set.SetActive(true);f.ApplyShot(0,0);
 EditorUtility.SetDirty(f.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Aftermath-story-polish.txt","Saved 15 shots: night transition, overhead mounted TV, hinged box closing, call/reaction, counter pickup, departure.");
 AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-story-polish.txt",e.ToString());EditorApplication.Exit(1);}}
 }
}
#endif

