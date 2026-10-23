#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok.Editor {
public static class NightPreparationInstaller {
 static Font font;static Sprite rounded;static Color navy=new Color32(25,43,66,255),muted=new Color32(86,108,124,255),teal=new Color32(21,143,164,255);
 static RectTransform Rect(string name,Transform parent,float x,float y,float xx,float yy){var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);t.anchorMin=new Vector2(x,y);t.anchorMax=new Vector2(xx,yy);t.offsetMin=t.offsetMax=Vector2.zero;return t;}
 static RectTransform Panel(string name,Transform parent,Color color,float x,float y,float xx,float yy){var t=Rect(name,parent,x,y,xx,yy);var image=t.gameObject.AddComponent<Image>();image.color=color;image.sprite=rounded;image.type=Image.Type.Sliced;return t;}
 static Text Label(string name,Transform parent,string value,int size,Color color,float x,float y,float xx,float yy){var t=Rect(name,parent,x,y,xx,yy).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
 public static void Install(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
  Directory.CreateDirectory("Logs");if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-night-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var roots=scene.GetRootGameObjects();var day=roots.SelectMany(g=>g.GetComponentsInChildren<DayNightCycle>(true)).Single();var view=day.pcUIPanel.GetComponent<ShopPcUiView>();
  if(view.GetComponent<NightPreparationView>())throw new Exception("Night view already installed; preserve scene edits");
  day.storyDayNightEnabled=true;EditorUtility.SetDirty(day);
  font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");rounded=view.GetComponentsInChildren<Image>(true).First(i=>i.sprite).sprite;
  string asset="Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset";var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>(asset);
  if(!plan){plan=ScriptableObject.CreateInstance<NightPreparationPlan>();plan.missions=new[]{
   new NightPreparationPlan.Mission{id="broker",title="연락이 끊긴 중개인",description="은행 의뢰를 연결했던 실장을 찾아야 합니다. 그를 쫓는 조직원에게서 벗어나 박진웅의 행선지를 알아내세요.",result="박진웅과 비공개 행사 정보 확보"},
   new NightPreparationPlan.Mission{id="special_order",title="특별 주문",description="행사 관계자의 배달 의뢰를 처리해 정식 납품 기회를 얻으세요. 피자 배달원 신분으로 행사장에 들어갈 준비입니다.",result="정식 납품 기회와 위장 신분 확보"},
   new NightPreparationPlan.Mission{id="backdoor",title="도시의 뒷문",description="민기와 행사장 주변을 확인하세요. 관찰을 마친 뒤 돌아올 수 있도록 잠입 지점과 탈출 경로를 준비합니다.",result="잠입 지점과 기본 탈출 경로 확인"},
   new NightPreparationPlan.Mission{id="mechanic",title="빚진 정비사",optional=true,description="정비사의 부탁을 해결해 도움을 구하세요. 필수 준비는 아니지만 위급할 때 탈출을 도울 차량을 확보할 수 있습니다.",result="피날레 탈출 차량 지원 확보"}};AssetDatabase.CreateAsset(plan,asset);}
  var ui=view.gameObject.AddComponent<NightPreparationView>();ui.cycle=day;ui.view=view;ui.plan=plan;ui.orders=roots.SelectMany(g=>g.GetComponentsInChildren<OrderManager>(true)).Single();
  var bg=view.transform.Find("Background");ui.tabLabel=bg.Find("Sidebar/Tab_0").GetComponentInChildren<Text>(true);
  var panel=Panel("NightPreparation",bg.Find("Page_0"),Color.white,0,0,1,1);ui.panel=panel.gameObject;
  ui.chapterLabel=Label("Chapter",panel,plan.chapterTitle,39,navy,.045f,.88f,.955f,.98f);
  ui.balanceLabel=Label("Balance",panel,"",22,muted,.045f,.82f,.955f,.88f);
  ui.introLabel=Label("Premise",panel,plan.premise,23,navy,.045f,.69f,.955f,.81f);
  ui.missionButtons=new Button[4];ui.missionLabels=new Text[4];
  for(int i=0;i<4;i++){float top=.66f-i*.125f;var row=Panel("Mission_"+i,panel,new Color32(237,242,246,255),.045f,top-.105f,.44f,top);ui.missionButtons[i]=row.gameObject.AddComponent<Button>();ui.missionLabels[i]=Label("Label",row,plan.missions[i].title,24,navy,.05f,.08f,.95f,.92f);}
  var card=Panel("MissionDetail",panel,new Color32(245,247,249,255),.465f,.18f,.955f,.66f);
  ui.detailTitle=Label("Title",card,"",30,navy,.055f,.78f,.945f,.96f);
  ui.detailBody=Label("Description",card,"",24,navy,.055f,.39f,.945f,.77f);
  ui.detailResult=Label("Result",card,"",22,muted,.055f,.04f,.945f,.38f);
  ui.footer=Label("Footer",panel,"",20,muted,.045f,.015f,.69f,.145f);
  var rest=Panel("NextDay",panel,teal,.71f,.03f,.955f,.125f);ui.restButton=rest.gameObject.AddComponent<Button>();ui.restLabel=Label("Label",rest,"다음 날로",26,Color.white,.04f,.05f,.96f,.95f);ui.restLabel.alignment=TextAnchor.MiddleCenter;
  panel.gameObject.SetActive(false);EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Night-install.txt","PASS Saved JJinCity night UI, plan asset and enabled 18:00 transition");
 }
}
}
#endif
