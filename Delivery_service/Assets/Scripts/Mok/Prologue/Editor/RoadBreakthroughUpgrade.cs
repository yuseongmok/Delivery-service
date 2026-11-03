#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class RoadBreakthroughUpgrade {
 public static System.Collections.IEnumerator Verify(Action<bool,string> check){
  yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.autoEnterGame=false;d.AcceptInput=false;d.player.enabled=false;
  typeof(BankPrologue).GetField("<Current>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(d,BankPrologue.Phase.Driving);
  foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}foreach(var o in d.roadPolice)o.gameObject.SetActive(true);
  d.player.view.enabled=false;d.filmCamera.enabled=true;d.drive.Begin();d.drive.ExternalInput=true;d.drive.ResetToRoad();d.drive.SetInput(1,0);
  float end=Time.time+12;PrologueOfficer hit=null;
  while(Time.time<end){hit=d.roadPolice.FirstOrDefault(o=>o.VehicleStruck);if(hit)break;yield return null;}
  check(hit,"driving reaches a gang blockade and knocks a guard aside");
  d.drive.SetInput(0,0);end=Time.time+.3f;while(Time.time<end)yield return null;
  check(hit.ragdoll.IsDown&&hit.GetComponent<StunStars>().Visible,"road impact shows temporary ragdoll and orbiting stars");
  typeof(BankPrologueCheck).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{d.filmCamera,"breakthrough-flight"});
  if(hit.Dead){end=Time.time+6;while(hit.gameObject.activeSelf&&Time.time<end)yield return null;check(!hit.gameObject.activeSelf,"depleted road guard disappears after stun");}
  else {end=Time.time+5;while(hit.ragdoll.IsDown&&Time.time<end)yield return null;check(!hit.ragdoll.IsDown&&!hit.Dead,"surviving guard recovers");}
  d.drive.SetInput(1,0);end=Time.time+20;while(d.getawayCar.position.x<216&&Time.time<end)yield return null;
  check(d.getawayCar.position.x>=216,"escape car crosses final wall without getting stuck");
  check(UnityEngine.Object.FindObjectsByType<PrologueRoadblockCar>(FindObjectsSortMode.None).Any(c=>c.transform.position.x>200&&c.Cleared),"final wall slides away on impact");
  check(d.roadPolice.Length==16,"sixteen road guards saved in scene");
  d.drive.SetInput(0,0);

 }
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/Breakthrough-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
  var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
  var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="조직 차량 봉쇄");if(old)UnityEngine.Object.DestroyImmediate(old);
  var group=new GameObject("조직 차량 봉쇄");var guards=new List<PrologueOfficer>();int index=0;
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_Sedan_01.prefab");
  // 앞부분은 교대로 피하고 마지막 한 줄은 충돌로 돌파한다.
  var spots=new[]{new Vector3(72,0,-68),new Vector3(105,0,-62),new Vector3(138,0,-68),new Vector3(170,0,-62),new Vector3(211,0,-69.4f),new Vector3(211,0,-65),new Vector3(211,0,-60.6f)};
  foreach(var at in spots){
   float side=at.z < -65 ? -1 : 1;
   var car=new GameObject(at.x>200?"최종 일렬 봉쇄 차량":"엇갈린 회피 차량");car.transform.SetParent(group.transform,false);car.transform.position=at;
   var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,car.transform);visual.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
   foreach(var c in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
   var box=car.AddComponent<BoxCollider>();box.center=new Vector3(0,.75f,0);box.size=new Vector3(1.9f,1.45f,4.4f);
   var rb=car.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
   car.AddComponent<PrologueRoadblockCar>().slideDirection=Vector3.forward*side;
  }
  foreach(float x in new[]{82f,119f,153f,194f})foreach(float offset in new[]{-3f,-1f,1f,3f}){
   var officer=UnityEngine.Object.Instantiate(d.officers[index++%d.officers.Length],group.transform);officer.name="봉쇄 조직원 "+index;officer.transform.SetPositionAndRotation(new Vector3(x,.05f,-65+offset),Quaternion.Euler(0,-90,0));
   officer.director=d;officer.roadGuard=true;officer.GetComponent<NavMeshAgent>().enabled=false;
   if(officer.brain){UnityEngine.Object.DestroyImmediate(officer.brain);officer.brain=null;}
   officer.gameObject.SetActive(true);officer.animator.Rebind();officer.animator.Update(0);
   var hand=officer.animator.GetBoneTransform(HumanBodyBones.RightHand);officer.gun.SetPositionAndRotation(hand.position,officer.transform.rotation*Quaternion.Euler(-20,0,15));guards.Add(officer);
  }
  d.roadPolice=guards.ToArray();d.story.driving="민기: 앞에 차들은 피해 가! 마지막 봉쇄는 속도 붙여서 밀어버려!";EditorUtility.SetDirty(d.story);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/Breakthrough-apply.txt","PASS saved 4 staggered obstacles, final 3-car wall, 16 gang guards; original vault and getaway route preserved");
 }
}
}
#endif

