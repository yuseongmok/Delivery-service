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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class ShopPcModelCheck {
const string Key="Mok.PC.ModelCheck";
const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
[Serializable] public class SavedKey {public string key;public bool exists;public int value;}
[Serializable] public class Backup {public SavedKey[] values;public bool listExists;public string list;public string economyPath;public bool economyExists;public string economy;}
static string Log;
static double started;
static ShopPcModelCheck(){EditorApplication.playModeStateChanged+=Changed;
if(File.Exists("Temp/Mok-PC-run-request")&&!SessionState.GetBool(Key,false))EditorApplication.delayCall+=()=>{File.Delete("Temp/Mok-PC-run-request");Start();};
}
[MenuItem("Tools/Delivery Service/Mok/Verify Low Poly PC")]
public static void Start(){
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
// 설치된 모델을 수정하지 않고 검사한다.
EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
var list=PlayerPrefs.GetString("PendingToppingList","");
var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
var backup=new Backup {values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Select(k=>new SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
File.WriteAllText("Temp/Mok-PC-save-backup.json",JsonUtility.ToJson(backup,true));
SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
}
static void Changed(PlayModeStateChange state){
if(!SessionState.GetBool(Key,false))return;
if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Run;}
if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;
}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);Log+="PASS: "+message+"\n";}
static void Invoke(object target,string name)=>target.GetType().GetMethod(name,Flags).Invoke(target,null);
static void Capture(ShopPC pc,string filename){
var go=new GameObject("Temporary PC preview camera");var cam=go.AddComponent<Camera>();
cam.transform.position=pc.transform.TransformPoint(new Vector3(-.72f,.82f,1.22f));
cam.transform.LookAt(pc.transform.TransformPoint(new Vector3(0,.34f,0)),pc.transform.up);cam.fieldOfView=38;cam.nearClipPlane=.03f;cam.farClipPlane=300;
var rt=new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32);rt.Create();
var old=RenderTexture.active;Texture2D pixels=null;
try {RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;pixels=new Texture2D(1200,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1200,900),0,0);pixels.Apply();File.WriteAllBytes("Temp/"+filename,pixels.EncodeToPNG());}
finally{RenderTexture.active=old;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
}
static void Run(){
if(!EditorApplication.isPlaying)return;
if(Time.frameCount<5&&EditorApplication.timeSinceStartup-started<90)return;
EditorApplication.update-=Run;Log="PC model regression in saved JJinCity, actual Play Mode.\n";
try {
var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();var view=pc.pcUIPanel.GetComponent<ShopPcUiView>();
Check(view!=null&&pc.GetComponent<ShopPcUiHost>()!=null,"existing PC UI and runtime host remain connected");
Check(pc.GetComponentInChildren<OrderManager>(true)!=null,"original child OrderManager retained");
var cycle=UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsSortMode.None).Single(c=>c.pcUIPanel==pc.pcUIPanel);
cycle.currentTime=6;Invoke(cycle,"UpdateLighting");Capture(pc,"Mok-PC-dawn.png");
cycle.currentTime=12;Invoke(cycle,"UpdateLighting");Capture(pc,"Mok-PC-day.png");cycle.currentTime=6;Invoke(cycle,"UpdateLighting");
// 기존 레이 검사는 ShopPC 오브젝트 자체의 콜라이더를 요구한다.
var target=pc.transform.TransformPoint(new Vector3(0,.47f,0));var origin=target+pc.transform.forward*.9f;
Physics.SyncTransforms();Check(Physics.Raycast(origin,-pc.transform.forward,out var hit,pc.interactRange)&&hit.collider.gameObject==pc.gameObject,"original-distance raycast hits the exact ShopPC GameObject");
pc.playerCamera.position=origin;pc.playerCamera.rotation=Quaternion.LookRotation(-pc.transform.forward,pc.transform.up);
Invoke(pc,"OpenPanel");
Check(pc.pcUIPanel.activeSelf&&Cursor.visible&&Cursor.lockState==CursorLockMode.None&&!pc.cameraLookScript.enabled,"OpenPanel opens UI and releases cursor while suspending camera");
var buttons=view.GetComponentsInChildren<Button>(true);
for(int i=0;i<3;i++){buttons.Single(b=>b.name=="Tab_"+i).onClick.Invoke();Check(view.SelectedTab==i,"UI tab "+i+" switches correctly");}
Check(pc.pcUIPanel.GetComponentInChildren<DeliveryService.Mok.ShopUI.ShopPcOrderQueue>(true)!=null,"order queue remains connected (separate order regression tests actions)");
// 재고를 백업하고 임시 자금으로 UI 구매를 검사한다.
var money=MoneyManager.Instance;Check(money!=null,"existing economy manager available");
typeof(MoneyManager).GetField("saveFilePath",Flags).SetValue(money,Path.GetFullPath("Temp/Mok-PC-test-economy.json"));money.currentMoney=100000;
int index=0;var row=view.ingredients[index];var catalog=Resources.Load<ShopSupplyCatalog>("YunShopSupplyCatalog");
ToppingType type=row.artworkKind==3?ToppingType.Pepperoni:row.artworkKind==4?ToppingType.Chocolate:row.artworkKind==5?ToppingType.Pineapple:row.artworkKind==6?ToppingType.Jelly:ToppingType.Pepper;
var data=catalog.Find(type);PlayerPrefs.SetInt("Stock_"+data.toppingName,0);PlayerPrefs.SetInt("Pending_"+data.toppingName,0);row.quantity=1;view.RefreshQuantities();
var card=view.transform.Find("Background/Page_2/IngredientsScroll/Content/Ingredient_"+index+"/Card");
card.Find("Plus").GetComponent<Button>().onClick.Invoke();Check(row.quantity==2,"supply quantity plus works");card.Find("Minus").GetComponent<Button>().onClick.Invoke();Check(row.quantity==1,"supply quantity minus works");
int before=money.currentMoney;card.Find("Purchase").GetComponent<Button>().onClick.Invoke();
Check(ToppingStockManager.GetPendingStock(data.toppingName)==10&&money.currentMoney==before-data.cost*10,"supply purchase queues 10 ingredients and charges existing price");
buttons.Single(b=>b.name=="CloseWindow").onClick.Invoke();Check(!pc.pcUIPanel.activeSelf&&pc.cameraLookScript.enabled,"close button restores camera");
Invoke(pc,"OpenPanel");Invoke(pc,"ClosePanel");Check(!pc.pcUIPanel.activeSelf,"existing keyboard close method works");
Invoke(pc,"OpenPanel");pc.playerCamera.position=pc.transform.position+Vector3.up*(pc.interactRange+1);Invoke(pc,"Update");Check(!pc.pcUIPanel.activeSelf,"walking out of interaction range closes UI");pc.playerCamera.position=origin;
Invoke(pc,"OpenPanel");buttons.Single(b=>b.name=="OpenBusiness").onClick.Invoke();Check(cycle.isShopOpen&&pc.pcUIPanel.activeSelf,"business opening uses existing DayNightCycle and keeps order screen open");
Invoke(pc,"OpenPanel");buttons.Single(b=>b.name=="CloseBusiness").onClick.Invoke();Check(!cycle.isShopOpen&&cycle.summaryPanel.activeInHierarchy&&!pc.pcUIPanel.activeSelf,"business closing opens existing daily summary");
cycle.OnClick_CancelClose();Check(cycle.isShopOpen&&pc.pcUIPanel.activeSelf&&!cycle.summaryPanel.activeSelf,"cancel closing returns to PC");
Log+="NOTE: Order actions are verified by the separate PC order queue regression.\nRESULT: PASS\n";
}catch(Exception e){Log+="RESULT: FAIL\n"+e;Debug.LogException(e);}
finally{File.WriteAllText("Temp/Mok-PC-functional-check.txt",Log);Debug.Log(Log);EditorApplication.ExitPlaymode();}
}
static void Restore(){
try {
var backup=JsonUtility.FromJson<Backup>(File.ReadAllText("Temp/Mok-PC-save-backup.json"));
foreach(var v in backup.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
if(backup.listExists)PlayerPrefs.SetString("PendingToppingList",backup.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
if(backup.economyExists){if(!File.Exists(backup.economyPath)||File.ReadAllText(backup.economyPath)!=backup.economy)File.WriteAllText(backup.economyPath,backup.economy);}else if(File.Exists(backup.economyPath))File.Delete(backup.economyPath);
File.AppendAllText("Temp/Mok-PC-functional-check.txt","Saved economy, stock and pending-order values restored after Play mode.\n");
var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();Selection.activeGameObject=pc.gameObject;SceneView.lastActiveSceneView?.FrameSelected();
}finally{SessionState.SetBool(Key,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
if(File.Exists("Temp/Mok-PC-followup-orders")){File.Delete("Temp/Mok-PC-followup-orders");EditorApplication.delayCall+=ShopPcOrdersCheck.Start;}}
}
}}
#endif
