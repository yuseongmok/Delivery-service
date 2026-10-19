#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LobbyExhibitionBuilder {
 static LobbyExhibitionBuilder(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-exhibition-request"))return;File.Delete("Temp/Mok-exhibition-request");try{Apply();}catch(Exception e){File.WriteAllText("Temp/Lobby-exhibition-result.txt",e.ToString());Debug.LogException(e);}}
 const string Art="Assets/Scripts/Mok/Art/Lobby/Exhibition/";
 static RectTransform Place(Transform p,string name,float x,float y,float w,float h){var t=p.Find(name);var r=t?(RectTransform)t:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
 static void LightAt(Transform root,string name,Vector3 point,float power,float range){var g=new GameObject(name);g.transform.SetParent(root);g.transform.position=point;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.61f,.28f);l.intensity=power;l.range=range;l.shadows=LightShadows.None;}
 static void Strip(GameObject g){foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(b)UnityEngine.Object.DestroyImmediate(b);foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(b)UnityEngine.Object.DestroyImmediate(b);foreach(var c in g.GetComponentsInChildren<Component>(true)){if(!c)continue;if(c is Joint)UnityEngine.Object.DestroyImmediate(c);else if(c is Rigidbody rb){rb.isKinematic=true;rb.detectCollisions=false;}else if(c is Collider col)col.enabled=false;else if(c is UnityEngine.AI.NavMeshAgent||c is AudioSource)UnityEngine.Object.DestroyImmediate(c);} }
 static void Actor(Scene scene,string path,string name,float delay){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception(path);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);Strip(g);g.name=name;var p=g.AddComponent<LobbyPasser>();p.from=new Vector3(129,.23f,110);p.to=new Vector3(129,.23f,25);p.speed=4.6f;p.period=65;p.delay=delay;p.running=true;g.transform.position=p.from;foreach(var a in g.GetComponentsInChildren<Animator>()){a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;} }
 [MenuItem("Tools/Delivery Service/Mok/Apply Exhibition Lobby")]
 public static void Apply(){
 for(int i=0;i<SceneManager.sceneCount;i++){var open=SceneManager.GetSceneAt(i);if(open.isLoaded)EditorSceneManager.SaveScene(open,"Temp/Exhibition-backup-"+i+".unity",true);}
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);EditorSceneManager.SaveScene(scene,"Temp/Lobby-before-exhibition.unity",true);
 var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyController>(true)).Single();
 var old=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Exhibition Decor");if(old)UnityEngine.Object.DestroyImmediate(old);var decor=new GameObject("Exhibition Decor");
 var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"DeliveryLogo.png");ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=2048;ti.SaveAndReimport();
 foreach(var t in c.menu.GetComponentsInChildren<Text>(true))if(t.text=="DELIVERY"||t.text=="SERVICE")t.gameObject.SetActive(false);
 var symbol=c.menu.transform.Find("Delivery Symbol");if(symbol)symbol.gameObject.SetActive(false);
 var logo=Place(c.menu.transform,"Integrated Logo",.043f,.59f,.31f,.29f);var oldImage=logo.GetComponent<Image>();if(oldImage)UnityEngine.Object.DestroyImmediate(oldImage);var image=logo.GetComponent<RawImage>()??logo.gameObject.AddComponent<RawImage>();image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"DeliveryLogo.png");image.raycastTarget=false;
 var sub=Place(c.menu.transform,"Genre Subtitle",.065f,.565f,.32f,.05f);sub.GetComponent<Text>().fontSize=24;
 Place(c.menu.transform,"Accent",.065f,.535f,.048f,.003f);
 var canvas=c.menu.transform.parent;Place(canvas,"Cinema Top",0,.945f,1,.055f);Place(canvas,"Cinema Bottom",0,0,1,.055f);
 int index=0;foreach(var b in new[]{c.startButton,c.settingsButton,c.quitButton}){var r=b.GetComponent<RectTransform>();r.anchorMin=new Vector2(.065f,.36f-index*.095f);r.anchorMax=new Vector2(.315f,.44f-index*.095f);r.offsetMin=r.offsetMax=Vector2.zero;b.GetComponent<Image>().color=index==0?new Color(.73f,.29f,.12f,1):new Color(.12f,.09f,.08f,.68f);var colors=b.colors;colors.highlightedColor=new Color(1.2f,1.12f,1);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.75f,.7f,.65f);b.colors=colors;
 var outline=b.GetComponent<UnityEngine.UI.Outline>()??b.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.89f,.55f,.3f,index==0?.8f:.35f);outline.effectDistance=new Vector2(1,-1);
 var f=b.GetComponent<LobbyMenuFeedback>()??b.gameObject.AddComponent<LobbyMenuFeedback>();f.director=c;index++;}
 var fx=c.GetComponent<LobbyExhibition>()??c.gameObject.AddComponent<LobbyExhibition>();fx.director=c;
 c.lobbyCamera.transform.position=new Vector3(147,2.3f,74);c.lobbyCamera.transform.LookAt(new Vector3(141,1.85f,61));c.lobbyCamera.fieldOfView=51;
 var bike=scene.GetRootGameObjects().First(x=>x.name=="Lobby Parked Delivery Bike");bike.transform.position=new Vector3(139f,.66f,67.8f);var bikeBottom=bike.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);bike.transform.position+=Vector3.up*(.25f-bikeBottom);
 foreach(var l in bike.GetComponentsInChildren<Light>(true)){l.enabled=true;l.gameObject.SetActive(true);l.intensity=2;l.range=12;l.color=new Color(1,.84f,.57f);}
 var shop=scene.GetRootGameObjects().First(x=>x.name=="PizzaStore");var bounds=new Bounds(shop.transform.position,Vector3.zero);bool first=true;foreach(var r in shop.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
 File.WriteAllText("Temp/Lobby-exhibition-bounds.txt",bounds.ToString()+"\n"+string.Join("\n",shop.GetComponentsInChildren<Light>(true).Select(l=>l.name+" "+l.transform.position)));
 foreach(var l in shop.GetComponentsInChildren<Light>(true)){l.enabled=true;l.gameObject.SetActive(true);l.color=new Color(1,.65f,.32f);l.intensity=Mathf.Max(l.intensity,l.name=="InteriorLight"?8f:2.2f);l.range=Mathf.Max(l.range,6);}
 LightAt(decor.transform,"Warm shop interior",bounds.center+Vector3.down*.7f,9f,10);
 LightAt(decor.transform,"Warm entrance spill",new Vector3(bounds.center.x,2.2f,bounds.max.z+.2f),4f,9);
 foreach(var l in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional)){l.intensity=l.name=="MOON"?0:.32f;l.color=new Color(.60f,.71f,1);}
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.22f,.28f,.40f);RenderSettings.ambientEquatorColor=new Color(.14f,.17f,.23f);RenderSettings.ambientGroundColor=new Color(.07f,.08f,.10f);RenderSettings.fogColor=new Color(.19f,.24f,.32f);RenderSettings.fogDensity=.003f;
 c.warmLights=shop.GetComponentsInChildren<Light>(true).Concat(decor.GetComponentsInChildren<Light>()).ToArray();
 foreach(var g in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Lobby Chase ")).ToArray())UnityEngine.Object.DestroyImmediate(g);
 Actor(scene,"Assets/Scripts/Min/Prefab/NPC/Robbery.prefab","Lobby Chase Robber",30);Actor(scene,"Assets/Scripts/Min/Prefab/NPC/Police.prefab","Lobby Chase Police",31.2f);
 var sourceCar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_Sedan_01.prefab");if(sourceCar){var car=UnityEngine.Object.Instantiate(sourceCar,decor.transform);car.name="Lobby passing car";Strip(car);var p=car.AddComponent<LobbyPasser>();p.from=new Vector3(125,.3f,115);p.to=new Vector3(125,.3f,15);p.speed=5;p.delay=5;p.period=45;car.transform.position=p.from;}
 var head=new GameObject("Parked bike headlight");head.transform.SetParent(decor.transform);head.transform.position=bike.transform.position+bike.transform.forward*.85f+Vector3.up*.95f;head.transform.rotation=Quaternion.LookRotation(bike.transform.forward-Vector3.up*.15f);var beam=head.AddComponent<Light>();beam.type=LightType.Spot;beam.color=new Color(1,.82f,.5f);beam.intensity=5;beam.range=13;beam.spotAngle=62;
for(int i=0;i<2;i++){var box=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/Box.prefab"),decor.transform);box.name="Display pizza box "+i;box.transform.position=new Vector3(140.1f,1.38f+i*.13f,62.1f);box.transform.localScale=Vector3.one*.8f;Strip(box);}
var pizza=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/PizzaStages.prefab"),decor.transform);pizza.name="Display baked pizza";pizza.transform.position=new Vector3(138.9f,1.40f,62.1f);pizza.transform.localScale=Vector3.one*.8f;Strip(pizza);foreach(Transform t in pizza.transform)t.gameObject.SetActive(t.name!="RawCrust"&&t.name!="ShreddedCheese");
LightAt(decor.transform,"Oven glow",new Vector3(137.6f,1.3f,58.3f),2.2f,3);c.warmLights=shop.GetComponentsInChildren<Light>(true).Concat(decor.GetComponentsInChildren<Light>().Where(l=>l.type==LightType.Point)).ToArray();
EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Capture(c,"Temp/Lobby-exhibition.png");File.WriteAllText("Temp/Lobby-exhibition-result.txt","PASS: scene saved; integrated logo, warm light, menu feedback, camera drift, isolated chase and traffic.\n");
 }
 public static void Capture(LobbyController c,string path){var canvas=c.menu.GetComponentInParent<Canvas>();var cam=c.lobbyCamera;var rt=new RenderTexture(1600,900,24);var previous=RenderTexture.active;var mode=canvas.renderMode;var wc=canvas.worldCamera;var distance=canvas.planeDistance;try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{cam.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=wc;canvas.planeDistance=distance;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}
}
#endif







