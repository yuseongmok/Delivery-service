#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{
public static partial class AftermathSetup{
public static void FinalContinuity(){try{
var scene=EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-continuity.unity",true);film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;
var forest=film.shots[0].set.transform;int index=0;
foreach(var t in forest.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="SM_Gen_Env_Mountain_02")){
var rs=t.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);t.localScale=Vector3.Scale(t.localScale,new Vector3(24/b.size.x,13/b.size.y,8/b.size.z));b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);t.position+=new Vector3(268+index++*11,0,24)-new Vector3(b.center.x,b.min.y,b.center.z);
}
foreach(var a in forest.GetComponentsInChildren<AftermathActor>(true))foreach(var r in a.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
var bag=forest.GetComponentsInChildren<Transform>(true).First(t=>t.name.Contains("DuffleBag_Open"));var rr=bag.GetComponentsInChildren<Renderer>();Bounds bb=rr[0].bounds;foreach(var r in rr)bb.Encapsulate(r.bounds);bag.position+=Vector3.up*(.45f-bb.min.y);bb=rr[0].bounds;foreach(var r in rr)bb.Encapsulate(r.bounds);
foreach(var t in bag.GetComponentsInChildren<Transform>()){if(t.name=="돈가방 왼손")t.position=new Vector3(bb.center.x-.22f,bb.max.y-.02f,bb.center.z+.1f);if(t.name=="돈가방 오른손")t.position=new Vector3(bb.center.x+.22f,bb.max.y-.02f,bb.center.z+.1f);}
var bm=film.shots[2].motions.First(m=>m.subject==bag);bm.from.position=bag.position;bm.to.position=bag.position+Vector3.up*.35f;
// Keep the mugshot TV entirely inside its physical bezel.
var tv=film.newsHeadline.GetComponentInParent<Canvas>(true).transform;tv.localScale=Vector3.one*.0018f;tv.position=new Vector3(0,1.35f,5.909f);
film.shots[6].lookAt.position=new Vector3(0,1.35f,6);
film.shots[6].cameraFrom.position=new Vector3(0,1.35f,4.15f);film.shots[6].cameraTo.position=new Vector3(.1f,1.35f,3.95f);
foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);EditorSceneManager.SaveScene(scene);Capture();File.WriteAllText("Logs/Aftermath-continuity.txt","PASS saved");EditorApplication.Exit(0);
}catch(Exception e){File.WriteAllText("Logs/Aftermath-continuity.txt",e.ToString());EditorApplication.Exit(1);}}
}}
#endif
