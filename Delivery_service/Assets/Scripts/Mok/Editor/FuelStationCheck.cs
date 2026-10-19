#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class FuelStationCheck {
 const string Key="Mok.FuelStationCheck";static CarMov[] fleet;static Vector3[] starts;static Quaternion[] wheelStarts;static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static FuelStationCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-fuel-play-request")){
 File.Delete("Temp/Mok-fuel-play-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-fuel-play-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 if(stage==1){
 var nozzle=UnityEngine.Object.FindFirstObjectByType<FuelNozzle>();var cap=UnityEngine.Object.FindFirstObjectByType<FuelCap>();var fuel=cap.bikeFuelSystem;var bike=fuel.GetComponent<MotorcycleController>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerInteract>();
 foreach(var car in UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None))car.enabled=false;
 bike.enabled=false;if(bike.sphereRB!=null)bike.sphereRB.isKinematic=true;
 var art=GameObject.Find("Mok Fuel Station").transform;Capture(art);
 Check(nozzle.hoseStartPoint!=null&&!nozzle.hoseStartPoint.IsChildOf(nozzle.transform)&&nozzle.hoseRender.sharedMaterial!=null,"호스 고정점과 정상 재질");
 player.GetComponent<PlayerMove>().enabled=false;foreach(var look in player.GetComponentsInChildren<CameraMove>())look.enabled=false;Cursor.lockState=CursorLockMode.Locked;
 var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=art.TransformPoint(new Vector3(0,1,-2));cc.enabled=true;
 var camera=player.playerCameraTransform;camera.position=nozzle.transform.position+art.TransformDirection(new Vector3(0,.2f,-1.3f));camera.LookAt(nozzle.transform.position);Physics.SyncTransforms();
 typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);Check(nozzle.currentState==FuelNozzle.NozzleState.InHand,"실제 조준 상호작용으로 노즐 집기");
 fuel.currentFuel=0;MoneyManager.Instance.currentMoney=20000;
 // 주유구를 정확히 겨누지 않고 차체 측면에 조준한다.
 camera.position=bike.transform.position+bike.transform.right*1.8f+Vector3.up*.7f;camera.LookAt(bike.transform.position+Vector3.up*.7f);Physics.SyncTransforms();
 Check(Physics.Raycast(camera.position,camera.forward,out var bodyHit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&bodyHit.collider.GetComponentInParent<MotorcycleController>()==bike,"차체 조준 판정");
 log+="Body hit="+bodyHit.collider.name+"\n";
 typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);Check(nozzle.currentState==FuelNozzle.NozzleState.Refueling&&bike.isRefueling,"차체를 통한 주유 연결");
  var tick=typeof(FuelNozzle).GetMethod("TickFuel",Flags);
 tick.Invoke(nozzle,new object[]{.2f});int partialMoney=MoneyManager.Instance.currentMoney;float partialFuel=fuel.currentFuel;
 typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);
 Check(nozzle.currentState==FuelNozzle.NozzleState.InHand&&nozzle.currentTargetCap==null&&!bike.isRefueling&&!bike.isDriven,"주유 중 차체 E로 노즐 회수하고 탑승하지 않음");
 tick.Invoke(nozzle,new object[]{1f});Check(MoneyManager.Instance.currentMoney==partialMoney&&fuel.currentFuel==partialFuel,"회수 후 주유와 결제 중단");
 typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);Check(nozzle.currentState==FuelNozzle.NozzleState.Refueling,"차체 E로 재연결");
 tick.Invoke(nozzle,new object[]{10f});Check(Mathf.Approximately(fuel.currentFuel,fuel.maxFuel)&&MoneyManager.Instance.currentMoney==16000,"빈 탱크 완충 정확히 4,000원");
  tick.Invoke(nozzle,new object[]{10f});Check(MoneyManager.Instance.currentMoney==16000,"완충 후 추가 결제 없음");
 typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);
 Check(nozzle.currentState==FuelNozzle.NozzleState.InHand&&nozzle.currentTargetCap==null&&!bike.isRefueling&&!bike.isDriven,"완충 후 차체 E로 노즐 회수");
 camera.position=art.TransformPoint(new Vector3(0,1.3f,-2));camera.LookAt(art.TransformPoint(new Vector3(0,1,0)));Physics.SyncTransforms();typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);
 Check(nozzle.currentState==FuelNozzle.NozzleState.OnPump&&!bike.isRefueling,"완충·분리 후 주유기 E로 반납");
 nozzle.ReturnPump();Check(!bike.isRefueling&&nozzle.currentState==FuelNozzle.NozzleState.OnPump,"노즐 반납 시 주유 상태 해제");
 nozzle.PickUpNozzle();fuel.currentFuel=95;nozzle.AttachToBike(cap);tick.Invoke(nozzle,new object[]{10f});Check(Mathf.Approximately(fuel.currentFuel,100)&&MoneyManager.Instance.currentMoney==15800,"남은 5퍼센트만 주유하고 200원 결제");
 nozzle.PickUpNozzle();fuel.currentFuel=0;MoneyManager.Instance.currentMoney=100;nozzle.AttachToBike(cap);tick.Invoke(nozzle,new object[]{10f});Check(Mathf.Abs(fuel.currentFuel-2.5f)<.001f&&MoneyManager.Instance.currentMoney==0&&!bike.isRefueling,"잔액 100원만큼 주유 후 중단");
 nozzle.ReturnPump();nozzle.PickUpNozzle();nozzle.AttachToBike(cap);Check(nozzle.currentState==FuelNozzle.NozzleState.InHand,"잔액 부족 시 연결 거부");
   nozzle.ReturnPump();camera.position=art.TransformPoint(new Vector3(0,1.3f,-2));camera.LookAt(art.TransformPoint(new Vector3(0,1,0)));Physics.SyncTransforms();typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);Check(nozzle.currentState==FuelNozzle.NozzleState.InHand,"주유기 본체에서도 노즐 집기");typeof(PlayerInteract).GetMethod("TryInteract",Flags).Invoke(player,null);Check(nozzle.currentState==FuelNozzle.NozzleState.OnPump,"주유기 상호작용으로 반납");MoneyManager.Instance.currentMoney=20000;fuel.currentFuel=0;nozzle.PickUpNozzle();nozzle.AttachToBike(cap);for(int k=0;k<400;k++)tick.Invoke(nozzle,new object[]{.02f});Check(fuel.currentFuel==100&&MoneyManager.Instance.currentMoney==16000,"여러 프레임에 걸친 완충도 정확히 4,000원");nozzle.ReturnPump();
 var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;
 stage=2;next=EditorApplication.timeSinceStartup+.1;return;
 }if(stage==2){Capture(GameObject.Find("Mok Fuel Station").transform,"day");Finish();return;} }catch(Exception e){log+="FAIL: "+e;Finish();}
 }
 static void Capture(Transform art,string suffix="dawn"){
 var go=new GameObject("Fuel station inspection camera");var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=48;c.nearClipPlane=.05f;c.transform.position=art.TransformPoint(new Vector3(7,4,-9));c.transform.LookAt(art.TransformPoint(new Vector3(0,1.7f,0)));
 var rt=new RenderTexture(1280,900,24);rt.Create();c.targetTexture=rt;
 try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Temp/Mok-fuel-station-"+suffix+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(t);}finally{rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}
 } static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){File.WriteAllText("Temp/Mok-fuel-play-check.txt",log);EditorApplication.ExitPlaymode();}
 static void Changed(PlayModeStateChange s){
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-fuel-play-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif
