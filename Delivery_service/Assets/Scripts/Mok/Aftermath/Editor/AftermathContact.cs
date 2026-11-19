#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{public static partial class AftermathSetup{
public static void ContactPolish(){try{
var scene=EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-contact.unity",true);film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;
var forest=film.shots[0].set.transform;var bag=forest.GetComponentsInChildren<Transform>(true).First(t=>t.name.Contains("DuffleBag_Open"));
var grips=bag.GetComponentsInChildren<Transform>();var left=grips.First(t=>t.name=="돈가방 왼손");var right=grips.First(t=>t.name=="돈가방 오른손");var p=left.position;left.position=right.position;right.position=p;
var pickup=film.shots[10].motions.First(m=>!m.actor);pickup.to.position=new Vector3(0,1,3.87f);
var depart=film.shots[11].motions.First(m=>!m.actor);depart.from.position=new Vector3(0,1,2.57f);depart.to.position=new Vector3(0,1,-2.15f);
// Avoid passing the box through the torso during the initial lift.
var actor=film.shots[10].motions.First(m=>m.actor);actor.from.position=actor.to.position=new Vector3(0,0,3.1f);
film.shots[9].motions[0].to.position=new Vector3(0,0,3.1f);
var shop=film.shots[6].set.transform;var box=pickup.subject;
var rs=box.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
foreach(var t in box.GetComponentsInChildren<Transform>()){
if(t.name=="피자 상자 왼손 받침")t.position=new Vector3(b.center.x-b.extents.x*.75f,b.min.y-.025f,b.min.z+.055f);
if(t.name=="피자 상자 오른손 받침")t.position=new Vector3(b.center.x+b.extents.x*.75f,b.min.y-.025f,b.min.z+.055f);
}
// The camera can cut within a scene; fades remain for changes of time and place.
foreach(int i in new[]{1,3,5,6,7,8,9})film.shots[i].fadeOut=false;
foreach(int i in new[]{2,4,6,7,8,9,10})film.shots[i].fadeIn=false;
foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);EditorSceneManager.SaveScene(scene);Capture();File.WriteAllText("Logs/Aftermath-contact.txt","PASS saved");EditorApplication.Exit(0);
}catch(Exception e){File.WriteAllText("Logs/Aftermath-contact.txt",e.ToString());EditorApplication.Exit(1);}}
}}
#endif
