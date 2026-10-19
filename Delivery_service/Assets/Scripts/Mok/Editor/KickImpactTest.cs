#if UNITY_EDITOR
using System;using System.Collections;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering;
namespace DeliveryService.Mok.Editor {
public static class KickImpactTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static IEnumerator RunCore(Action<string> log){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();p.GetComponent<PlayerMove>().enabled=false;p.enabled=false;
  foreach(var look in UnityEngine.Object.FindObjectsByType<CameraMove>(FindObjectsSortMode.None))look.enabled=false;
  var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
  p.GetComponent<CharacterController>().enabled=false;p.transform.SetPositionAndRotation(new Vector3(-176,0,-5),Quaternion.identity);p.View.transform.rotation=Quaternion.identity;
  Physics.SyncTransforms();p.View.transform.rotation=Quaternion.LookRotation(Vector3.up);Check(!p.ResolveKick()&&p.GetComponent<KickImpactFeedback>()==null,"헛발차기에는 효과 없음");
  foreach(string name in new[]{"Male_Adult_NPC_1","Robbery"}){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/"+name+".prefab");
   NavMesh.SamplePosition(p.transform.position+Vector3.forward*1.3f,out var spot,3,NavMesh.AllAreas);
   var enemy=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity);enemy.GetComponent<NavMeshAgent>().Warp(spot.position);
   if(name=="Male_Adult_NPC_1"){var combat=enemy.GetComponent<CitizenCombat>()??enemy.AddComponent<CitizenCombat>();combat.enabled=false;enemy.GetComponent<NPCControl>().ExternalControl=true;}
   else {enemy.GetComponent<RobberAI>().IsTestSpawn=true;enemy.GetComponent<RobberAI>().enabled=false;}
   yield return null;enemy.GetComponent<NavMeshAgent>().isStopped=true;p.View.transform.LookAt(enemy.transform.position+Vector3.up*1.2f);Physics.SyncTransforms();
   int before=p.GetComponent<KickImpactFeedback>()?.HitCount??0;
   Check(p.ResolveKick(),name+" 실제 발차기 명중");var effect=p.GetComponent<KickImpactFeedback>();Check(effect!=null&&effect.HitCount==before+1&&effect.IsShaking,name+" 명중당 효과 한 번");
   var source=(AudioSource)typeof(KickImpactFeedback).GetField("source",F).GetValue(effect);Check(source.isPlaying,name+" 타격 효과음 재생");
   var pos=p.View.transform.localPosition;var rot=p.View.transform.localRotation;
   typeof(KickImpactFeedback).GetMethod("BeforeCamera",F).Invoke(effect,new object[]{default(ScriptableRenderContext),p.View});
   Check(Vector3.Distance(pos,p.View.transform.localPosition)>0&&Vector3.Distance(pos,p.View.transform.localPosition)<.02f&&Quaternion.Angle(rot,p.View.transform.localRotation)<1,name+" 약한 카메라 충격");
   typeof(KickImpactFeedback).GetMethod("AfterCamera",F).Invoke(effect,new object[]{default(ScriptableRenderContext),p.View});
   Check(Vector3.Distance(pos,p.View.transform.localPosition)<.00001f&&Quaternion.Angle(rot,p.View.transform.localRotation)<.001f,name+" 렌더 후 원래 시점 복원");
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=p.View.transform.position+p.View.transform.forward*.5f;wall.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();Check(!p.ResolveKick()&&effect.HitCount==before+1,name+" 벽 차단 시 추가 효과 없음");UnityEngine.Object.Destroy(wall);
   float until=Time.time+.3f;while(Time.time<until)yield return null;Check(!effect.IsShaking,name+" 흔들림 자동 종료");UnityEngine.Object.Destroy(enemy);yield return null;
  }
 }
}
}
#endif
