#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using DeliveryService.Mok;
using DeliveryService.Yun.Settings;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class LobbyBuilder {
 const string Path="Assets/Scenes/Lobby.unity";static Font font;
 static LobbyBuilder(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!File.Exists("Temp/Mok-lobby-build-request"))return;File.Delete("Temp/Mok-lobby-build-request");try{Build();File.WriteAllText("Temp/Mok-lobby-build.txt","PASS: saved Lobby scene, menu, walkers and build entry; gameplay return path connected.");}catch(Exception e){File.WriteAllText("Temp/Mok-lobby-build.txt",e.ToString());Debug.LogException(e);}}
 [MenuItem("Tools/Delivery Service/Mok/Create Lobby Scene")]
 public static void Build(){
  if(File.Exists(Path))throw new Exception("Lobby already exists. Preserve scene edits.");
  var source=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");if(!source.isLoaded)source=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
  Directory.CreateDirectory("Temp/MokLobbyBackup");EditorSceneManager.SaveScene(source,"Temp/MokLobbyBackup/JJinCity-before.unity",true);File.Copy("ProjectSettings/EditorBuildSettings.asset","Temp/MokLobbyBackup/EditorBuildSettings.asset",true);
  // 기존 설정의 공개된 로비 연결 필드만 씬에 저장한다.
  foreach(var panel in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameSettingsPanel>(true))){var so=new SerializedObject(panel);so.FindProperty("lobbyScenePath").stringValue=Path;so.ApplyModifiedPropertiesWithoutUndo();}
  EditorSceneManager.SaveScene(source);EditorSceneManager.SaveScene(source,Path,true);
  var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var keep=new[]{"SUN","MOON","Global Volume","Demo","Road_Cars","PizzaStore","Bike 1","Yun Streetlights (16h)"};
  foreach(var root in scene.GetRootGameObjects()){
   if(!keep.Contains(root.name)){UnityEngine.Object.DestroyImmediate(root);continue;}
   if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   Strip(root);
  }
  var shop=scene.GetRootGameObjects().First(g=>g.name=="PizzaStore");
  foreach(var text in shop.GetComponentsInChildren<TextMesh>(true))if(text.name!="ShopName"&&text.name!="ShopSubtitle")text.gameObject.SetActive(false);
  foreach(var l in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true))){if(l.type==LightType.Directional){l.enabled=l.name!="MOON";l.intensity=.7f;l.color=new Color(.67f,.79f,1);l.transform.rotation=Quaternion.Euler(22,220,0);}else if(l.transform.IsChildOf(shop.transform)){l.enabled=true;l.gameObject.SetActive(true);}}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.28f,.39f,.50f);RenderSettings.ambientEquatorColor=new Color(.22f,.29f,.34f);RenderSettings.ambientGroundColor=new Color(.12f,.16f,.19f);
  RenderSettings.fog=true;RenderSettings.fogColor=new Color(.24f,.34f,.43f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0025f;
  var bike=scene.GetRootGameObjects().First(g=>g.name=="Bike 1");bike.name="Lobby Parked Delivery Bike";bike.transform.position=new Vector3(137.2f,.66f,67.5f);bike.transform.rotation=Quaternion.Euler(0,120,-5);
  foreach(var l in bike.GetComponentsInChildren<Light>(true))l.enabled=false;
  var camera=new GameObject("Lobby Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(149,3,80);camera.transform.LookAt(new Vector3(142.5f,2,61));camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=450;camera.backgroundColor=new Color(.18f,.29f,.39f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
  var controller=new GameObject("Lobby Director").AddComponent<LobbyController>();controller.lobbyCamera=camera;controller.warmLights=shop.GetComponentsInChildren<Light>(true);
  controller.audioSettings=controller.gameObject.AddComponent<GameAudioSettings>();
  for(int i=0;i<3;i++){
   string asset="Assets/Scripts/Min/Prefab/NPC/"+(i==1?"Female_Adult_NPC_2":"Male_Adult_NPC_"+(i+1))+".prefab";
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(!prefab)continue;var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);Strip(g);g.name="Lobby Pedestrian "+(i+1);
   var walker=g.AddComponent<LobbyWalker>();walker.from=new Vector3(i==1?159:113,.23f,69+i*.45f);walker.to=new Vector3(i==1?113:159,.23f,69+i*.45f);walker.speed=.9f+i*.13f;walker.delay=i*10;g.transform.position=walker.from;g.transform.rotation=Quaternion.LookRotation(walker.to-walker.from);
  }
  BuildUI(controller);
  var builds=EditorBuildSettings.scenes.Where(s=>s.path!=Path).ToList();builds.Insert(0,new EditorBuildSettingsScene(Path,true));if(!builds.Any(s=>s.path==source.path))builds.Add(new EditorBuildSettingsScene(source.path,true));foreach(var b in builds)if(b.path==source.path)b.enabled=true;EditorBuildSettings.scenes=builds.ToArray();
  EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(source,true);AssetDatabase.SaveAssets();Selection.activeGameObject=camera.gameObject;
 }
 static void Strip(GameObject root){
  foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))if(canvas)UnityEngine.Object.DestroyImmediate(canvas.gameObject);
  foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(b&&!(b is Volume)&&b.GetType().Name!="UniversalAdditionalLightData")UnityEngine.Object.DestroyImmediate(b);
  foreach(var c in root.GetComponentsInChildren<Component>(true))if(c&&(c is Camera||c is AudioListener||c is AudioSource||c is UnityEngine.AI.NavMeshAgent||c is Joint||c is Rigidbody||c is Collider||c is LineRenderer)){
    if(c is Rigidbody rb){rb.isKinematic=true;rb.detectCollisions=false;}else if(c is Joint)UnityEngine.Object.DestroyImmediate(c);else if(c is Collider col)col.enabled=false;else UnityEngine.Object.DestroyImmediate(c);
  }
  foreach(var a in root.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;}
 }
 static RectTransform R(Transform p,string name,float x0,float y0,float x1,float y1){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
 static Text T(Transform p,string value,int size,float x0,float y0,float x1,float y1){var t=R(p,value,x0,y0,x1,y1).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=new Color(.94f,.91f,.82f);t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;return t;}
 static Image I(Transform p,string name,Color color,float x0,float y0,float x1,float y1){var img=R(p,name,x0,y0,x1,y1).gameObject.AddComponent<Image>();img.color=color;return img;}
 static Button B(Transform p,string value,float x0,float y0,float x1,float y1,bool main=false){var img=I(p,value,main?new Color(.68f,.34f,.20f):new Color(.12f,.22f,.25f,.85f),x0,y0,x1,y1);var b=img.gameObject.AddComponent<Button>();var c=b.colors;c.highlightedColor=new Color(1.25f,1.22f,1.12f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.7f,.8f,.85f);b.colors=c;T(img.transform,value,29,.08f,0,.95f,1);return b;}
 static Slider S(Transform p,string label,float y,out Text percent){T(p,label,24,.1f,y+.055f,.55f,y+.12f);percent=T(p,"100%",22,.76f,y+.055f,.92f,y+.12f);var r=R(p,label+" Slider",.1f,y,.9f,y+.045f);var s=r.gameObject.AddComponent<Slider>();I(r,"Track",new Color(.25f,.34f,.36f),0,.3f,1,.7f);var fill=I(r,"Fill",new Color(.78f,.48f,.26f),0,.3f,1,.7f);var area=R(r,"Handle Area",0,0,1,1);var handle=I(area,"Handle",new Color(.95f,.9f,.76f),0,0,0,1);handle.rectTransform.sizeDelta=new Vector2(18,0);s.fillRect=fill.rectTransform;s.handleRect=handle.rectTransform;s.targetGraphic=handle;s.value=1;return s;}
 static void BuildUI(LobbyController c){
  font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");var canvas=new GameObject("Lobby UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
  var menu=R(canvas.transform,"Main Menu",0,0,1,1);c.menu=menu.gameObject.AddComponent<CanvasGroup>();var shade=R(menu,"Quiet Menu Backdrop",0,0,.62f,1).gameObject.AddComponent<LobbyShade>();shade.raycastTarget=false;
  T(menu,"DELIVERY",68,.065f,.73f,.44f,.85f);T(menu,"SERVICE",68,.065f,.64f,.44f,.76f);T(menu,"오늘도, 무사히 배달하세요.",23,.069f,.585f,.39f,.645f);I(menu,"Accent",new Color(.85f,.48f,.25f),.07f,.562f,.12f,.566f).raycastTarget=false;
  c.startButton=B(menu,"게임 시작",.07f,.40f,.295f,.478f,true);c.settingsButton=B(menu,"설정",.07f,.30f,.295f,.378f);c.quitButton=B(menu,"종료",.07f,.20f,.295f,.278f);
  T(menu,"이얏호 피자  /  영업 준비 중",20,.07f,.065f,.45f,.105f);c.status=T(menu,"",22,.07f,.12f,.55f,.18f);
  c.settingsPanel=I(canvas.transform,"Settings Overlay",new Color(0,0,0,.62f),0,0,1,1).gameObject;var panel=I(c.settingsPanel.transform,"Settings Window",new Color(.065f,.12f,.15f),.30f,.18f,.70f,.82f);T(panel.transform,"설정",40,.1f,.82f,.9f,.94f);
  c.bgm=S(panel.transform,"배경음",.65f,out c.bgmValue);c.sfx=S(panel.transform,"효과음",.46f,out c.sfxValue);
  T(panel.transform,"화면 모드",24,.1f,.30f,.48f,.38f);c.screenButton=B(panel.transform,"",.5f,.28f,.9f,.38f);c.screenValue=T(c.screenButton.transform,"",23,.08f,0,.95f,1);
  c.backButton=B(panel.transform,"돌아가기",.1f,.08f,.9f,.20f);
  var fade=I(canvas.transform,"Scene Fade",Color.black,0,0,1,1);c.fade=fade.gameObject.AddComponent<CanvasGroup>();c.fade.alpha=0;c.fade.blocksRaycasts=false;
  new GameObject("Lobby EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));c.settingsPanel.SetActive(false);
 }
}
}
#endif
