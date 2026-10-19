#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CityExpansionCheck {
 const string Key="Mok.CityExpansionCheck";static GameObject testWall;static MotorcycleController testBike;static double driveDeadline;static bool returning;static CarMov[] fleet;static Vector3[] starts;static Quaternion[] wheelStarts;static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static CityExpansionCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-city-play-request")){
 File.Delete("Temp/Mok-city-play-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-city-play-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){File.WriteAllText("Temp/Mok-bike-stage.txt","Loading JJinCity");stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 if(stage==1){File.WriteAllText("Temp/Mok-bike-stage.txt","Running navigation/traffic check");
 var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;cycle.enabled=false;typeof(DayNightCycle).GetMethod("UpdateLighting",Flags)?.Invoke(cycle,null);
 var allCars=UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None);Check(allCars.Length==50,"전체 차량 50대 유지");Check(allCars.Count(c=>c.transform.position.x>195)==16,"새 도시 차량 16대");Check(allCars.Where(c=>c.transform.position.x>195).All(c=>c.CurrentTravelSpeed>0),"새 도시 차량 주행 확인");foreach(var car in allCars)car.enabled=false;Check(GameObject.Find("East citizens").GetComponentsInChildren<NPCControl>().Length==18,"새 도시 시민 18명 생성");
 var root=GameObject.Find("Mok East City");Check(root!=null,"새 도시가 저장된 씬에서 로드됨");
 var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=UnityEngine.AI.NavMesh.GetSettingsByIndex(0).agentTypeID,areaMask=(1<<UnityEngine.AI.NavMesh.GetAreaFromName("Road"))|(1<<UnityEngine.AI.NavMesh.GetAreaFromName("SideWalk"))};
 foreach(var pos in new[]{new Vector3(190,0,-5),new Vector3(210,0,-5),new Vector3(440,0,100),new Vector3(440,0,-110)}){bool got=UnityEngine.AI.NavMesh.SamplePosition(pos,out var h,8,filter);log+="SAMPLE "+pos+" -> "+got+" "+h.position+"\n";}
 var nav=UnityEngine.Object.FindFirstObjectByType<NavigationManager>();var shop=UnityEngine.Object.FindObjectsByType<DeliveryService.Mok.Navigation.NavigationPlace>(FindObjectsSortMode.None).First(p=>p.kind==DeliveryService.Mok.Navigation.NavigationPlaceKind.Pizza);var old=nav.NavigationTarget;var origin=new GameObject("Route test");nav.SetNavigationTarget(origin.transform);
 foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Delivery entrance"))){origin.transform.position=t.position;nav.SelectPlace(shop);Check(nav.DisplayPath.Count>0,t.name+" → 기존 매장 경로");}nav.SetNavigationTarget(old);nav.StopGuidance();UnityEngine.Object.Destroy(origin);
 Physics.SyncTransforms();int total=0;foreach(float z in new[]{-110f,-5f,100f})for(float x=200;x<450;x+=2){Check(Physics.Raycast(new Vector3(x,1,z),Vector3.down,out var h,2,~(1<<20),QueryTriggerInteraction.Ignore)&&Mathf.Abs(h.point.y)<.15f,"도로 바닥 "+x+","+z);total++;}
 Check(!Physics.SphereCast(new Vector3(140,.8f,-5),.4f,Vector3.right,out var obstacle,295,~(1<<20),QueryTriggerInteraction.Ignore),"다리부터 새 도시 중앙 도로까지 오토바이 폭 통과 "+(obstacle.collider==null?"":obstacle.collider.name));
 stage=2;next=EditorApplication.timeSinceStartup+1;return;}
 if(stage==2){File.WriteAllText("Temp/Mok-bike-stage.txt","Starting physical drive");Capture(new Vector3(150,180,-215),new Vector3(295,0,-5),"overview");Capture(new Vector3(190,2,-6),new Vector3(290,6,-5),"bridge");Capture(new Vector3(292,2,-55),new Vector3(290,5,30),"street");testBike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();testBike.enabled=false;testBike.durabilitySystem.RepairFull();DeliveryService.Mok.BikeContactProbe.Report.Clear();testBike.sphereRB.gameObject.AddComponent<DeliveryService.Mok.BikeContactProbe>();foreach(var sensor in testBike.GetComponentsInChildren<BikeCollisionSensor>())if(sensor.gameObject!=testBike.sphereRB.gameObject)sensor.gameObject.AddComponent<DeliveryService.Mok.BikeContactProbe>();testBike.maxSpeed=30;testBike.isSnowing=false;testBike.transform.rotation=Quaternion.Euler(0,90,0);testBike.sphereRB.isKinematic=false;testBike.sphereRB.position=new Vector3(145,.6f,-5);testBike.sphereRB.linearVelocity=Vector3.zero;typeof(MotorcycleController).GetField("moveInput",Flags).SetValue(testBike,1f);driveDeadline=EditorApplication.timeSinceStartup+45;returning=false;stage=3;next=0;return;}
 if(stage==3){if(testBike.sphereRB.position.y<-.5f||testBike.sphereRB.position.y>2)throw new Exception("오토바이 바닥 이탈: "+testBike.sphereRB.position);if(EditorApplication.timeSinceStartup>driveDeadline)throw new Exception("Bike traversal timed out: "+testBike.sphereRB.position);typeof(MotorcycleController).GetMethod("Movement",Flags).Invoke(testBike,null);typeof(MotorcycleController).GetMethod("LateUpdate",Flags).Invoke(testBike,null);if(!returning&&testBike.sphereRB.position.x>432){log+="PASS: 실제 오토바이 물리로 다리 건너 새 도시 진입\n";returning=true;testBike.transform.rotation=Quaternion.Euler(0,270,0);}if(returning&&testBike.sphereRB.position.x<147){log+="PASS: 실제 오토바이 물리로 기존 도시 복귀\n";Check(testBike.durabilitySystem.currentDurability==testBike.durabilitySystem.maxDurability,"왕복 주행 후 내구도 100 유지");testWall=GameObject.CreatePrimitive(PrimitiveType.Cube);testWall.name="Impact regression wall";testWall.tag="Obstacle";testWall.transform.position=new Vector3(134,1.5f,-5);testWall.transform.localScale=new Vector3(1,3,8);Physics.SyncTransforms();stage=4;driveDeadline=EditorApplication.timeSinceStartup+6;}}if(stage==4){if(EditorApplication.timeSinceStartup>driveDeadline)throw new Exception("벽 충돌 검사 시간 초과");typeof(MotorcycleController).GetMethod("Movement",Flags).Invoke(testBike,null);typeof(MotorcycleController).GetMethod("LateUpdate",Flags).Invoke(testBike,null);if(testBike.durabilitySystem.currentDurability<100){Check(testBike.durabilitySystem.currentDurability==85,"실제 벽 충돌 피해 15 적용");typeof(MotorcycleController).GetField("moveInput",Flags).SetValue(testBike,0f);typeof(MotorcycleController).GetField("currentSpeed",Flags).SetValue(testBike,0f);testBike.sphereRB.linearVelocity=Vector3.zero;stage=5;next=EditorApplication.timeSinceStartup+.6;return;}}if(stage==5){Check(testBike.durabilitySystem.currentDurability==85,"벽 접촉 중 중복 피해 없음");UnityEngine.Object.Destroy(testWall);Finish();}}catch(Exception e){log+="FAIL: "+e;Capture(new Vector3(150,180,-215),new Vector3(295,0,-5),"overview");Finish();}}
 static void Capture(Vector3 position,Vector3 target,string suffix){var go=new GameObject("City inspection camera");var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=58;c.farClipPlane=1800;c.nearClipPlane=.05f;c.cullingMask=~((1<<20)|(1<<17));c.transform.position=position;c.transform.LookAt(target);var rt=new RenderTexture(1400,900,24);rt.Create();c.targetTexture=rt;try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Temp/Mok-city-"+suffix+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(t);}finally{rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}}
 static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){if(testBike!=null){log+="\nDURABILITY="+testBike.durabilitySystem.currentDurability+"\n"+DeliveryService.Mok.BikeContactProbe.Report; }File.WriteAllText("Temp/Mok-city-play-check.txt",log);EditorApplication.ExitPlaymode();}static void Changed(PlayModeStateChange s){File.AppendAllText("Temp/Mok-bike-states.txt",DateTime.Now+" "+s+" key="+SessionState.GetBool(Key,false)+"\n");
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-city-play-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif









