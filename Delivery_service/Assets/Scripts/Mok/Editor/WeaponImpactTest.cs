#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering;
namespace DeliveryService.Mok.Editor {
public static class WeaponImpactTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static GameObject Enemy(string name,Vector3 at){var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/"+name+".prefab"),at,Quaternion.identity);go.GetComponent<NavMeshAgent>().Warp(at);go.GetComponent<NPCControl>().ExternalControl=true;
  if(name=="Robbery"){go.GetComponent<RobberAI>().IsTestSpawn=true;go.GetComponent<RobberAI>().enabled=false;}else{var c=go.GetComponent<CitizenCombat>()??go.AddComponent<CitizenCombat>();c.enabled=false;}return go;}
 public static IEnumerator RunCore(Action<string> log){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var weapon=p.GetComponent<PlayerWeapon>();p.GetComponent<PlayerMove>().enabled=false;p.enabled=false;weapon.enabled=false;
  foreach(var look in UnityEngine.Object.FindObjectsByType<CameraMove>(FindObjectsSortMode.None))look.enabled=false;
  UnityEngine.Object.FindFirstObjectByType<RobberyManager>().enabled=false;p.GetComponent<CharacterController>().enabled=false;p.transform.SetPositionAndRotation(new Vector3(-176,0,-5),Quaternion.identity);
  var usedSounds=new HashSet<AudioClip>();
  foreach(var kind in new[]{WeaponKind.Bat,WeaponKind.Tuna,WeaponKind.Brick}){
   weapon.Equip(kind);yield return null;p.View.transform.rotation=Quaternion.LookRotation(Vector3.up);int before=p.GetComponent<KickImpactFeedback>()?.HitCount??0;
   Check(weapon.Resolve()==0&&(p.GetComponent<KickImpactFeedback>()?.HitCount??0)==before,kind+" 헛스윙 효과 없음");
   foreach(string name in new[]{"Male_Adult_NPC_1","Robbery"}){
    NavMesh.SamplePosition(p.transform.position+Vector3.forward*1.3f,out var spot,3,NavMesh.AllAreas);var enemy=Enemy(name,spot.position);yield return null;enemy.GetComponent<NavMeshAgent>().isStopped=true;
    p.View.transform.LookAt(enemy.transform.position+Vector3.up*1.2f);Physics.SyncTransforms();before=p.GetComponent<KickImpactFeedback>()?.HitCount??0;
    Check(weapon.Resolve()==1,kind+" "+name+" 실제 명중");var effect=p.GetComponent<KickImpactFeedback>();Check(effect.HitCount==before+1&&effect.LastWeapon==kind&&effect.IsShaking,kind+" 무기별 효과 한 번");
    var source=(AudioSource)typeof(KickImpactFeedback).GetField("source",F).GetValue(effect);Check(source.isPlaying,kind+" 타격음 재생");
    var sounds=(Dictionary<WeaponKind,AudioClip>)typeof(KickImpactFeedback).GetField("weaponClips",F).GetValue(effect);usedSounds.Add(sounds[kind]);
    var pos=p.View.transform.localPosition;var rot=p.View.transform.localRotation;typeof(KickImpactFeedback).GetMethod("BeforeCamera",F).Invoke(effect,new object[]{default(ScriptableRenderContext),p.View});Check(Vector3.Distance(pos,p.View.transform.localPosition)>0&&Quaternion.Angle(rot,p.View.transform.localRotation)<1,kind+" 약한 카메라 충격");typeof(KickImpactFeedback).GetMethod("AfterCamera",F).Invoke(effect,new object[]{default(ScriptableRenderContext),p.View});Check(Vector3.Distance(pos,p.View.transform.localPosition)<.00001f&&Quaternion.Angle(rot,p.View.transform.localRotation)<.001f,kind+" 시점 원복");
    UnityEngine.Object.Destroy(enemy);yield return null;
   }
  }
  Check(usedSounds.Count==3,"세 무기 서로 다른 오디오 클립");
  weapon.Equip(WeaponKind.Tuna);yield return null;var enemies=new List<GameObject>();
  foreach(float x in new[]{-.65f,.65f}){NavMesh.SamplePosition(p.transform.position+new Vector3(x,0,2),out var spot,2,NavMesh.AllAreas);enemies.Add(Enemy("Male_Adult_NPC_1",spot.position));}yield return null;
  foreach(var e in enemies)e.GetComponent<NavMeshAgent>().isStopped=true;
  p.View.transform.LookAt(p.transform.position+new Vector3(0,1.2f,2));Physics.SyncTransforms();var feedback=p.GetComponent<KickImpactFeedback>();int previous=feedback.HitCount;
  Check(weapon.Resolve()==2&&feedback.HitCount==previous+1,"참치 2명 동시 타격에도 효과 중첩 없음");foreach(var e in enemies)UnityEngine.Object.Destroy(e);yield return null;
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=p.View.transform.position+p.View.transform.forward;wall.transform.localScale=new Vector3(4,3,.4f);Physics.SyncTransforms();Check(weapon.Resolve()==0&&feedback.HitCount==previous+1,"벽 타격 효과 없음");UnityEngine.Object.Destroy(wall);
  KickImpactFeedback.Play(p);Check(feedback.LastWeapon==WeaponKind.None,"기존 발차기 효과 유지");float until=Time.time+.3f;while(Time.time<until)yield return null;Check(!feedback.IsShaking,"흔들림 자동 종료");
 }
}
}
#endif
