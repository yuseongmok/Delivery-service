#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PlayerAvatarCheck {
 const string Key="Mok.PlayerAvatarCheck";static Vector3 footStart,launchPosition;static PlayerCombat player;static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static PlayerAvatarCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-avatar-request")){
 File.Delete("Temp/Mok-avatar-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-avatar-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
 if(player==null||player.avatar==null)throw new Exception("Player model missing");
 var avatar=player.avatar;
 if(stage==1){
 Check(avatar.animator.isHuman&&avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Length==2,"사람형 모델과 상체·하체 분리");
 Check(avatar.GetComponentsInChildren<Collider>().Length>5&&avatar.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"평상시 관절 충돌체 비활성");
 Check(player.kickDamage==30&&!player.kickVisual.gameObject.activeSelf,"데미지 30 유지 및 기존 가짜 다리 숨김");
 footStart=avatar.transform.InverseTransformPoint(avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
 Capture("standing");avatar.PoseKick(.22f);stage=2;next=EditorApplication.timeSinceStartup+.15;return;
 }
 if(stage==2){
 var foot=avatar.transform.InverseTransformPoint(avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
 log+="Foot rest="+footStart+" kick="+foot+"\n";
 Check(foot.z-footStart.z>.3f,"실제 오른발이 앞쪽으로 뻗음");
 log+="Camera="+player.View.transform.position+" foot="+avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot).position+" viewport="+player.View.WorldToViewportPoint(avatar.animator.GetBoneTransform(HumanBodyBones.RightFoot).position)+" FOV="+player.View.fieldOfView+" mask="+player.View.cullingMask+"\n";Capture("kick");CaptureFirst();avatar.StopKick();
 Cursor.lockState=CursorLockMode.Locked;
 Check(player.TryKick()&&!player.TryKick(),"실제 발차기 입력과 중복 공격 차단");
 stage=3;next=EditorApplication.timeSinceStartup+.12;return;
 }
 if(stage==3){
 var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
 var routine=(System.Collections.IEnumerator)typeof(MotorcycleController).GetMethod("EjectPlayerRoutine",Flags).Invoke(bike,new object[]{player.gameObject,2f,Vector3.forward});
 bike.StartCoroutine(routine);launchPosition=player.transform.position;stage=4;next=EditorApplication.timeSinceStartup+.5;return;
 }
 if(stage==4){
 Check(Vector3.Distance(avatar.ragdoll.hips.position,launchPosition)<15f,"관절 폭발 없이 정상 비행 거리");Check(avatar.IsPhysical,"팀원 충돌 루틴으로 전신 물리 전환");
 Check(!player.IsKicking,"물리 전환 시 발차기 중단");
 Check(avatar.ragdoll.IsActive&&!avatar.animator.enabled&&avatar.ragdoll.bodies.All(b=>!b.isKinematic),"사고 중 모든 관절 물리 활성 및 애니메이션 정지");Check(player.GetComponent<Rigidbody>()==null,"중복 캡슐 물리 없음");Capture("ejected");stage=5;next=EditorApplication.timeSinceStartup+3;return;
 }
 if(stage==5){
 Check(!avatar.IsPhysical&&player.GetComponent<CharacterController>().enabled&&player.GetComponent<PlayerMove>().enabled,"충돌 후 기존 이동 복구");
 Check(avatar.ragdoll.bodies.All(b=>b.isKinematic)&&avatar.ragdoll.colliders.All(c=>!c.enabled),"회복 후 관절 물리 비활성");Check(Vector3.Distance(player.transform.position,launchPosition)<40f,"안정적인 사고 회복 위치");var car=UnityEngine.Object.FindFirstObjectByType<CarMov>();typeof(CarMov).GetMethod("OnTriggerEnter",Flags).Invoke(car,new object[]{player.GetComponent<CharacterController>()});stage=6;next=EditorApplication.timeSinceStartup+.4;return;
 } if(stage==6){Check(avatar.ragdoll.IsActive,"차량 충돌 판정에서 관절 레그돌 실행");Check(!player.CanReceiveHit&&!player.CanAct,"사고 중 전투 중첩 방지");var hip=avatar.ragdoll.hips;log+="Ragdoll hip="+hip.position+" rightLeg="+avatar.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).localEulerAngles+"\n";avatar.ragdoll.Launch(Vector3.one*100);Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.name=="Player ragdoll camera")==1,"중복 사고 연출 차단");Capture("car-hit");stage=7;next=EditorApplication.timeSinceStartup+3;return;}if(stage==7){Check(!avatar.IsPhysical&&player.GetComponent<CharacterController>().enabled&&player.GetComponent<PlayerMove>().enabled,"보행자 사고 후 조작 복구");Cursor.lockState=CursorLockMode.Locked;Check(player.TryKick(),"사고 회복 후 발차기 가능");stage=8;next=EditorApplication.timeSinceStartup+.8;return;} Finish();
 }catch(Exception e){log+="FAIL: "+e;Finish();}
 }
 static void CaptureFirst(){
 var camera=player.View;var previous=camera.targetTexture;var rt=new RenderTexture(1280,720,24);rt.Create();
 try{camera.targetTexture=rt;UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});Save(rt,"first-person");}
 finally{camera.targetTexture=previous;rt.Release();UnityEngine.Object.Destroy(rt);}
 }
 static void Capture(string name){
 var go=new GameObject("Player inspection camera");var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=40;c.nearClipPlane=.05f;
 Vector3 center=player.avatar.ragdoll!=null&&player.avatar.ragdoll.IsActive?player.avatar.ragdoll.hips.position:player.transform.position;c.transform.position=center+new Vector3(1.8f,.6f,2.2f);c.transform.LookAt(center);
 var rt=new RenderTexture(1000,1000,24);rt.Create();c.targetTexture=rt;
 try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});Save(rt,name);}
 finally{rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}
 }
 static void Save(RenderTexture rt,string name){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Temp/Mok-avatar-"+name+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(t);}
 static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){File.WriteAllText("Temp/Mok-avatar-check.txt",log);EditorApplication.ExitPlaymode();}
 static void Changed(PlayModeStateChange s){
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-avatar-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif
