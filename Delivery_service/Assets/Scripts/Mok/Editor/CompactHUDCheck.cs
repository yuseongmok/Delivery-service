#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CompactHUDCheck {
 const string Key="Mok.HUDCheck";static int stage;static double next;static string log="";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static CompactHUDCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Mok-hud-request")){
 File.Delete("Temp/Mok-hud-request");
 // 기존 테스트의 저장 백업 형식을 사용하고 종료 시 복원한다.
 var list=PlayerPrefs.GetString("PendingToppingList","");
 var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
 string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
 var b=new StockCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new StockCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
 File.WriteAllText("Temp/Mok-hud-backup.json",JsonUtility.ToJson(b));SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
 if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false)||Time.frameCount<5||EditorApplication.timeSinceStartup<next)return;
 try{
 if(stage==0){stage=1;SceneManager.LoadScene("JJinCity");next=EditorApplication.timeSinceStartup+4;return;}
 var hud=UnityEngine.Object.FindFirstObjectByType<CompactHUD>();if(hud==null)throw new Exception("HUD missing");
 if(stage==1){
 Check(hud.GetComponentsInChildren<Text>(true).Any(t=>t.text=="주문 대기 중"),"작은 주문 대기 카드");
 var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();bike.isDriven=true;
 var storage=bike.GetComponentInChildren<BikePizzaStorage>(true);var list=(List<PizzaData>)typeof(BikePizzaStorage).GetField("storedPizzas",Flags).GetValue(storage);list.Add(new PizzaData(new List<string>(),true));storage.currentShake=0;
 stage=2;next=EditorApplication.timeSinceStartup+1;return;
 }
 if(stage==2){
 var row=hud.transform.Find("Vehicle/Gauge 2");Check(row!=null&&row.gameObject.activeInHierarchy,"피자 적재·탑승 시 흔들림 행 표시");
 var track=row.Find("Empty track").GetComponent<Image>();var fill=track.transform.Find("Fill").GetComponent<Image>();
 Check(track.enabled&&track.color.a>0&&fill.rectTransform.sizeDelta.x<.01f,"흔들림 0에도 빈 배경 유지");
 var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
 var durability=bike.durabilitySystem;
 Check(durability!=null&&!durability.durabilityGaugeUI.enabled,"기존 초록색 내구도 게이지 숨김");
 var durabilityFill=hud.transform.Find("Vehicle/Gauge 1/Empty track/Fill").GetComponent<RectTransform>();
 Check(Mathf.Abs(durabilityFill.sizeDelta.x-190*durability.currentDurability/durability.maxDurability)<.1f,"새 게이지가 실제 내구도 값 표시");
 var balance=hud.transform.Find("Balance").GetComponent<RectTransform>();
 Check(balance.anchorMax==Vector2.one&&balance.anchoredPosition.y==-94,"잔액을 시간 아래 오른쪽 위에 배치");
 Check(balance.GetComponentsInChildren<Text>().Any(t=>t.text=="$"),"잔액 달러 기호 표시");
 Check(hud.GetComponentsInChildren<Text>(true).All(t=>t.font==MokUIFont.Font),"HUD 전체 주아체");
 ScreenCapture.CaptureScreenshot("Temp/Mok-hud-loaded.png");stage=3;next=EditorApplication.timeSinceStartup+1;return;
 }
 if(stage==3){var b=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();b.isDriven=false;stage=4;next=EditorApplication.timeSinceStartup+1;return;}
 if(stage==4){Check(!hud.transform.Find("Vehicle").gameObject.activeSelf,"하차 시 주행 게이지 숨김");ScreenCapture.CaptureScreenshot("Temp/Mok-hud-walking.png");stage=5;next=EditorApplication.timeSinceStartup+1;return;}
 Finish();
 }catch(Exception e){log+="FAIL: "+e;Finish();}
 }
 static void Check(bool b,string s){if(!b)throw new Exception(s);log+="PASS: "+s+"\n";}
 static void Finish(){File.WriteAllText("Temp/Mok-hud-check.txt",log);EditorApplication.ExitPlaymode();}
 static void Changed(PlayModeStateChange s){
 if(!SessionState.GetBool(Key,false))return;
 if(s==PlayModeStateChange.EnteredPlayMode){stage=0;log="";next=0;SessionState.SetBool(Key+".bg",Application.runInBackground);Application.runInBackground=true;}
 if(s==PlayModeStateChange.EnteredEditMode){
 var b=JsonUtility.FromJson<StockCheck.Backup>(File.ReadAllText("Temp/Mok-hud-backup.json"));
 foreach(var k in b.values){if(k.exists)PlayerPrefs.SetInt(k.key,k.value);else PlayerPrefs.DeleteKey(k.key);}
 if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
 if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
 Application.runInBackground=SessionState.GetBool(Key+".bg",false);SessionState.SetBool(Key,false);
 }}
}}
#endif
