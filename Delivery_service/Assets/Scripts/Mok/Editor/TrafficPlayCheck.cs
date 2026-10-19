#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class TrafficPlayCheck {
 const string Key="Mok.TrafficPlayCheck";static CarMov[] fleet;static Vector3[] starts;static Quaternion[] wheelStarts;static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static TrafficPlayCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-traffic-play-request")){
 File.Delete("Temp/Mok-traffic-play-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-traffic-play-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 if(stage==1){
 fleet=UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None);Check(fleet.Length==50,"50대 씬 연결");
 Check(fleet.Select(c=>(float)typeof(CarMov).GetField("speed",Flags).GetValue(c)).Distinct().Count()==50,"차량별 서로 다른 속도");
 Check(fleet.All(c=>c.GetComponent<TrafficWheelVisual>().wheels.Length==4&&c.GetComponent<TrafficSpacing>()!=null&&c.GetComponent<BoxCollider>().isTrigger&&c.GetComponent<Rigidbody>().isKinematic),"바퀴·차간 거리·사고 판정 연결");
 var a=new GameObject("Spacing test rear");var b=new GameObject("Spacing test front");
 try{a.transform.position=new Vector3(1000,10,1000);b.transform.position=a.transform.position+Vector3.forward*5.5f;var ca=a.AddComponent<CarMov>();b.AddComponent<CarMov>();a.AddComponent<TrafficSpacing>();b.AddComponent<TrafficSpacing>();Check(TrafficSpacing.IsBlocked(ca),"앞차 근접 시 대기");b.transform.position=a.transform.position+Vector3.forward*12;Check(!TrafficSpacing.IsBlocked(ca),"앞차 멀어지면 재출발");b.transform.position=a.transform.position+Vector3.forward*5+Vector3.right*5;Check(!TrafficSpacing.IsBlocked(ca),"옆 차선 때문에 정지하지 않음");}finally{UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);}
 Check(fleet.All(c=>{float v=(float)typeof(CarMov).GetField("speed",Flags).GetValue(c);return v>=7.5f&&v<=8.5f;}),"속도 범위 7.5~8.5");
 var origin=new GameObject("Route test junction");var straight=new GameObject("Route test busy");var turn=new GameObject("Route test empty");var rear=new GameObject("Route test self");var front=new GameObject("Route test lead");
 try{
 origin.transform.position=new Vector3(1000,10,1000);straight.transform.position=origin.transform.position+Vector3.forward*30;turn.transform.position=origin.transform.position+Vector3.right*30;
 var j=origin.AddComponent<WayPoint>();var busy=straight.AddComponent<WayPoint>();var empty=turn.AddComponent<WayPoint>();j.nextWaypoints.Add(busy);j.nextWaypoints.Add(empty);
 rear.transform.position=origin.transform.position;front.transform.position=origin.transform.position+Vector3.forward*9;
 var self=rear.AddComponent<CarMov>();var lead=front.AddComponent<CarMov>();rear.AddComponent<TrafficSpacing>();front.AddComponent<TrafficSpacing>();
 Check(Enumerable.Range(0,50).All(_=>TrafficSpacing.ChooseNext(self,j)==empty),"혼잡한 직진보다 빈 연결 도로 우선");
 j.nextWaypoints.Remove(empty);Check(TrafficSpacing.ChooseNext(self,j)==busy,"외길은 기존 연결 유지");
 var speedProp=typeof(CarMov).GetProperty("CurrentTravelSpeed");speedProp.SetValue(self,8f);
 float slow=TrafficSpacing.TravelSpeed(self,8f,.1f);Check(slow>0&&slow<8,"앞차 접근 시 점진적 감속");
 front.transform.position=origin.transform.position+Vector3.forward*6;Check(TrafficSpacing.TravelSpeed(self,8f,.1f)==0,"최소 차간 거리에서 안전 정지");
 front.transform.position=origin.transform.position+Vector3.forward*30;speedProp.SetValue(self,0f);float accelerate=TrafficSpacing.TravelSpeed(self,8f,.1f);Check(accelerate>0&&accelerate<1,"길이 열리면 점진적 재출발");
 }finally{UnityEngine.Object.Destroy(origin);UnityEngine.Object.Destroy(straight);UnityEngine.Object.Destroy(turn);UnityEngine.Object.Destroy(rear);UnityEngine.Object.Destroy(front);}
 starts=fleet.Select(c=>c.transform.position).ToArray();wheelStarts=fleet.Select(c=>c.GetComponent<TrafficWheelVisual>().wheels[0].localRotation).ToArray();stage=2;next=EditorApplication.timeSinceStartup+60;return;
 }
 if(stage==2){int moving=0;for(int i=0;i<fleet.Length;i++){float distance=Vector3.Distance(starts[i],fleet[i].transform.position);log+=fleet[i].name+" distance="+distance.ToString("F1")+" wheel="+Quaternion.Angle(wheelStarts[i],fleet[i].GetComponent<TrafficWheelVisual>().wheels[0].localRotation).ToString("F1")+"\n";if(distance>1)moving++;}Check(moving==50,"50대 모두 주행 확인");Finish();return;} }catch(Exception e){log+="FAIL: "+e;Finish();}
 }
 static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){File.WriteAllText("Temp/Mok-traffic-play-check.txt",log);EditorApplication.ExitPlaymode();}
 static void Changed(PlayModeStateChange s){
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-traffic-play-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif
