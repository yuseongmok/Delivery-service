#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PerformanceRegressionCheck {
const string Key="Mok.PerformanceRegression.Check";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
[Serializable]public class SavedKey{public string key;public bool exists;public int value;}
[Serializable]public class Backup{public SavedKey[] values;public bool listExists;public string list;public string economyPath;public bool economyExists;public string economy;}
static System.Collections.IEnumerator routine; static int lastFrame=-1; static int stage;static double began;static string log;static ShopPC pc;static OrderManager manager;static DayNightCycle cycle;static ShopPcUiView view;static ShopPcOrderQueue queue;
static PerformanceRegressionCheck(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool(Key,false)||File.Exists("Temp/Mok-performance-regression-build"))return;if(File.Exists("Temp/Mok-performance-regression-test-request")){File.Delete("Temp/Mok-performance-regression-test-request");Start();}}
[MenuItem("Tools/Delivery Service/Mok/Verify Performance Regression")]
public static void Start(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
var list=PlayerPrefs.GetString("PendingToppingList","");
var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
var backup=new Backup {values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
File.WriteAllText("Temp/Mok-performance-regression-save-backup.json",JsonUtility.ToJson(backup,true));

foreach(var n in names){PlayerPrefs.SetInt("Stock_"+n,9);PlayerPrefs.SetInt("Pending_"+n,3);}PlayerPrefs.SetString("PendingToppingList",string.Join(",",names));PlayerPrefs.Save();
SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){SessionState.SetBool(Key+".background",Application.runInBackground);Application.runInBackground=true;stage=0;began=EditorApplication.timeSinceStartup;log="Order queue Play Mode regression.\n";EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;}
static void Check(bool ok,string text){if(!ok)throw new Exception(text);log+="PASS: "+text+"\n";}
static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
static void Invoke(object o,string f)=>o.GetType().GetMethod(f,Flags).Invoke(o,null);
static void Open(){pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;Invoke(pc,"OpenPanel");view.SelectTab(0);Canvas.ForceUpdateCanvases();}
static void Tick(){if(!EditorApplication.isPlaying||Time.frameCount<5||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;try{
if(EditorApplication.timeSinceStartup-began>300)throw new Exception("Pizza test timed out");
if(stage==0){UnityEngine.SceneManagement.SceneManager.LoadScene("JJinCity");stage=1;began=EditorApplication.timeSinceStartup;return;}if(EditorApplication.timeSinceStartup-began<4)return;
if(routine==null){Set(MoneyManager.Instance,"saveFilePath",Path.GetFullPath("Temp/Mok-performance-regression-test-economy.json"));log="East traffic Play Mode regression.\n";routine=PerformanceRegressionTest.RunCore(message=>log+=message+"\n");}
if(routine.MoveNext())return;
}catch(Exception e){log+="FAIL: "+e;Debug.LogException(e);}Finish();}
static void Finish(){EditorApplication.update-=Tick;File.WriteAllText("Temp/Mok-performance-regression-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();}
static void Restore(){try{
var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Temp/Mok-performance-regression-save-backup.json"));
foreach(var v in backup.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
if(backup.listExists)PlayerPrefs.SetString("PendingToppingList",backup.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
if(backup.economyExists){if(!File.Exists(backup.economyPath)||File.ReadAllText(backup.economyPath)!=backup.economy)File.WriteAllText(backup.economyPath,backup.economy);}else if(File.Exists(backup.economyPath))File.Delete(backup.economyPath);
File.AppendAllText("Temp/Mok-performance-regression-check.txt","Saved economy, stock and pending-order values restored after Play mode.\n");

}finally{Application.runInBackground=SessionState.GetBool(Key+".background",false);SessionState.SetBool(Key,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
}}
#endif



