#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class DistrictCheck {
 const string Key="Mok.DistrictCheck";static GameObject testWall;static MotorcycleController testBike;static double driveDeadline;static bool returning;static CarMov[] fleet;static Vector3[] starts;static Quaternion[] wheelStarts;static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static DistrictCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-district-play-request")){
 File.Delete("Temp/Mok-district-play-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-district-play-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 if(stage==1){
 var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;cycle.enabled=false;typeof(DayNightCycle).GetMethod("UpdateLighting",Flags)?.Invoke(cycle,null);
 foreach(var car in UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None))car.enabled=false;
 var west=GameObject.Find("Mok West Quarter");var east=GameObject.Find("Mok East City");Check(west!=null&&east!=null,"두 구역 씬 저장 확인");
 var nav=UnityEngine.Object.FindFirstObjectByType<NavigationManager>();var shop=UnityEngine.Object.FindObjectsByType<DeliveryService.Mok.Navigation.NavigationPlace>(FindObjectsSortMode.None).First(p=>p.kind==DeliveryService.Mok.Navigation.NavigationPlaceKind.Pizza);var old=nav.NavigationTarget;var origin=new GameObject("Route test");nav.SetNavigationTarget(origin.transform);int failures=0;
 foreach(var t in west.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("West entrance"))){origin.transform.position=t.position;nav.SelectPlace(shop);if(nav.DisplayPath.Count==0){log+="ROUTE FAIL "+t.name+" "+t.position+"\n";var hit=new UnityEngine.AI.NavMeshHit();UnityEngine.AI.NavMesh.SamplePosition(t.position,out hit,30,UnityEngine.AI.NavMesh.AllAreas);var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(hit.position,new Vector3(-180,0,-5),UnityEngine.AI.NavMesh.AllAreas,path);log+="LOCAL "+hit.position+" "+path.status+" corners="+path.corners.Length+"\n";failures++;}}
 foreach(var t in east.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Delivery entrance"))){origin.transform.position=t.position;nav.SelectPlace(shop);if(nav.DisplayPath.Count==0){log+="ROUTE FAIL "+t.name+"\n";var hit=new UnityEngine.AI.NavMeshHit();UnityEngine.AI.NavMesh.SamplePosition(t.position,out hit,30,UnityEngine.AI.NavMesh.AllAreas);var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(hit.position,new Vector3(-180,0,-5),UnityEngine.AI.NavMesh.AllAreas,path);log+="LOCAL "+hit.position+" "+path.status+" corners="+path.corners.Length+"\n";failures++;}}nav.SetNavigationTarget(old);nav.StopGuidance();UnityEngine.Object.Destroy(origin);log+="Navigation failures="+failures+"\n";
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerMove>();player.enabled=false;foreach(var look in player.GetComponentsInChildren<CameraMove>())look.enabled=false;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-190,1,-5);cc.enabled=true;Physics.SyncTransforms();cc.Move(new Vector3(-10,-.1f,0));Check(player.transform.position.x< -198,"플레이어 도보로 입구 통과");cc.Move(new Vector3(10,-.1f,0));Check(player.transform.position.x> -192,"도보로 입구 복귀");cc.enabled=false;player.transform.position=new Vector3(-180,1,2);cc.enabled=true;
 testBike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();testBike.enabled=false;testBike.durabilitySystem.RepairFull();testBike.maxSpeed=10;testBike.isSnowing=false;testBike.transform.rotation=Quaternion.Euler(0,270,0);testBike.sphereRB.isKinematic=false;testBike.sphereRB.position=new Vector3(-181,.6f,-5);testBike.sphereRB.linearVelocity=Vector3.zero;typeof(MotorcycleController).GetField("moveInput",Flags).SetValue(testBike,1f);driveDeadline=EditorApplication.timeSinceStartup+4;stage=2;next=0;return;
 }
 if(stage==2){typeof(MotorcycleController).GetMethod("Movement",Flags).Invoke(testBike,null);typeof(MotorcycleController).GetMethod("LateUpdate",Flags).Invoke(testBike,null);if(EditorApplication.timeSinceStartup<driveDeadline)return;Check(testBike.sphereRB.position.x> -194.7f&&testBike.sphereRB.position.x< -193,"오토바이는 입구에서 정지 "+testBike.sphereRB.position);Check(testBike.durabilitySystem.currentDurability==100,"입구 정지 시 내구도 손실 없음");testBike.sphereRB.isKinematic=true;stage=3;next=EditorApplication.timeSinceStartup+.1;return;}
 if(stage==3){CaptureAll();Finish();}
 }catch(Exception e){log+="FAIL: "+e;CaptureAll();Finish();}}
 static void CaptureAll(){Capture(new Vector3(292,2,-55),new Vector3(290,4,-5),"east-street");Capture(new Vector3(-175,2,-5),new Vector3(-220,2,-5),"west-entry");Capture(new Vector3(-237,1.8f,-5),new Vector3(-307,2,-5),"west-market");Capture(new Vector3(-199,2,-37),new Vector3(-285,3,-35),"west-alley");Capture(new Vector3(-150,105,-155),new Vector3(-275,0,-5),"west-overview");var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=20;cycle.isShopOpen=true;typeof(DayNightCycle).GetMethod("UpdateLighting",Flags)?.Invoke(cycle,null);foreach(var light in UnityEngine.Object.FindObjectsByType<DeliveryService.Yun.Streetlights.StreetlightTimeController>(FindObjectsSortMode.None))light.Refresh();Capture(new Vector3(292,2,-55),new Vector3(290,4,-5),"east-night");Capture(new Vector3(-237,1.8f,-5),new Vector3(-307,2,-5),"west-night");}
 static void Capture(Vector3 position,Vector3 target,string suffix){var go=new GameObject("District inspection camera");var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=58;c.farClipPlane=1800;c.nearClipPlane=.05f;c.cullingMask=~((1<<20)|(1<<17));c.transform.position=position;c.transform.LookAt(target);var rt=new RenderTexture(1400,900,24);rt.Create();c.targetTexture=rt;try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Temp/Mok-district-"+suffix+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(t);}finally{rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}}
 static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){File.WriteAllText("Temp/Mok-district-play-check.txt",log);EditorApplication.ExitPlaymode();}static void Changed(PlayModeStateChange s){File.AppendAllText("Temp/Mok-district-states.txt",DateTime.Now+" "+s+" key="+SessionState.GetBool(Key,false)+"\n");
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-district-play-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif












