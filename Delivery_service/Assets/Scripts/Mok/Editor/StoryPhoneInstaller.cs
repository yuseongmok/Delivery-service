#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
public static class StoryPhoneInstaller {
 static Font font;
 static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static Text Label(Transform p,string name,float x,float y,float w,float h,int size){var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.08f,.14f,.19f);t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;}
 static void Backup(UnityEngine.SceneManagement.Scene scene){if(!EditorSceneManager.SaveScene(scene,"Logs/"+scene.name+"-before-story-phone-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");}
 static void Typography(Text t){if(!t)return;t.font=font;t.fontStyle=FontStyle.Normal;t.lineSpacing=1.15f;t.color=new Color(1,.97f,.90f);var shadow=t.GetComponent<Shadow>();if(!shadow)shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.95f);shadow.effectDistance=new Vector2(1.5f,-1.5f);EditorUtility.SetDirty(t);}
 static void Diamond(Text t){Typography(t);t.fontSize=20;t.alignment=TextAnchor.LowerCenter;t.rectTransform.sizeDelta=new Vector2(260,90);if(t.transform.Find("Orange target diamond") is Transform existing){if(!existing.GetComponent<CanvasRenderer>())existing.gameObject.AddComponent<CanvasRenderer>();return;}var r=Rect(t.transform,"Orange target diamond",0,0,32,32);r.anchorMin=r.anchorMax=new Vector2(.5f,.7f);r.pivot=new Vector2(.5f,.5f);var icon=r.gameObject.AddComponent<StoryTargetDiamond>();icon.color=new Color(1,.47f,.12f);icon.raycastTarget=false;var outline=r.gameObject.AddComponent<Shadow>();outline.effectColor=new Color(0,0,0,.85f);outline.effectDistance=new Vector2(2,-2);}
 public static void Install(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode");Directory.CreateDirectory("Logs");
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);if(!scene.isLoaded)throw new Exception("Open JJinCity first");Backup(scene);
  font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic-Regular.ttf");if(!font)throw new Exception("Missing story font");
  var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var pc=m.day.pcUIPanel.GetComponent<NightPreparationView>();
  var root=Rect(null,"Story Phone App",0,0,540,960);var app=root.gameObject.AddComponent<StoryPhoneApp>();app.plan=pc.plan;app.panel=root.gameObject;root.gameObject.AddComponent<Image>().color=new Color(.94f,.95f,.93f);
  var appFont=font;font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
  app.heading=Label(root,"Heading",28,36,484,90,36);app.body=Label(root,"Body",28,145,484,620,29);app.body.lineSpacing=1.2f;
  app.rows=new Text[4];app.cards=new Image[4];for(int i=0;i<4;i++){var r=Rect(root,"Entry "+i,24,165+i*140,492,120);app.cards[i]=r.gameObject.AddComponent<Image>();app.rows[i]=Label(r,"Title",20,18,452,92,29);}
  var bottom=Rect(root,"Action background",24,830,492,100);bottom.gameObject.AddComponent<Image>().color=new Color(.08f,.16f,.20f);app.action=Label(bottom,"Action",18,10,456,80,29);app.action.color=new Color(1,.75f,.44f);app.action.alignment=TextAnchor.MiddleCenter;
  root.gameObject.SetActive(false);var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Scripts/Mok/ShopUI/StoryPhoneApp.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
  var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();planner.storyAppsPrefab=prefab.GetComponent<StoryPhoneApp>();EditorUtility.SetDirty(planner);
  var inbox=m.GetComponent<StoryPhoneInbox>();if(!inbox)inbox=m.gameObject.AddComponent<StoryPhoneInbox>();inbox.cycle=m.day;inbox.mission=m;
  if(!inbox.toast){var canvas=new GameObject("Story text notifications",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(m.transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=76;var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);
   var r=Rect(canvas.transform,"Incoming message",0,0,420,100);r.anchorMin=r.anchorMax=new Vector2(1,.76f);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-28,0);r.gameObject.AddComponent<Image>().color=new Color(.045f,.09f,.13f,.95f);inbox.toast=r.gameObject;inbox.toastText=Label(r,"Preview",22,14,376,80,26);inbox.toastText.color=new Color(1,.78f,.5f);inbox.toast.SetActive(false);
  }
  font=appFont;Typography(m.objective);Typography(m.subtitle);Diamond(m.marker);
  var tutorial=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();Typography(tutorial.objective);Typography(tutorial.subtitle);Diamond(tutorial.marker);
  pc.Refresh();foreach(var b in pc.missionButtons)b.gameObject.SetActive(false);pc.startButton.gameObject.SetActive(false);
  var card=(RectTransform)pc.detailTitle.transform.parent;card.anchorMin=new Vector2(.045f,.18f);card.anchorMax=new Vector2(.955f,.66f);card.offsetMin=card.offsetMax=Vector2.zero;pc.detailTitle.text="미션은 휴대폰에서";pc.detailBody.text="민기의 문자와 밤 미션을 휴대폰에서 확인하세요.";pc.detailResult.text="가게 PC에서는 영업 정산과 다음 날 진행을 할 수 있습니다.";
  EditorUtility.SetDirty(inbox);EditorUtility.SetDirty(pc);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var bank=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);bool opened=!bank.isLoaded;if(opened)bank=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);Backup(bank);
  var prologue=bank.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();Typography(prologue.subtitle);Typography(prologue.objective);EditorSceneManager.MarkSceneDirty(bank);EditorSceneManager.SaveScene(bank);if(opened)EditorSceneManager.CloseScene(bank,true);
  AssetDatabase.SaveAssets();File.WriteAllText("Logs/StoryPhone-install.txt","PASS scene backups; saved phone app prefab; text notification UI; tutorial/mission diamonds; Nanum Gothic story typography in main and prologue");
 }
}
}
#endif
