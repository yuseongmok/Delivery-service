#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class YamazakiOfficeUpgrade {
 const string H="Assets/Synty/PolygonHeist/Prefab/", Art="Assets/Scripts/Mok/Prologue/Art/YamazakiOffice";
 static Transform root;static Material wood,plaster,carpet,metal;static int count;
 static YamazakiOfficeUpgrade(){EditorApplication.update+=Request;}
 static void Request(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/YamazakiOffice-request"))return;var command=File.ReadAllText("Temp/YamazakiOffice-request").Trim();File.Delete("Temp/YamazakiOffice-request");try{if(command=="apply")Apply();else if(command=="refine")Refine();else if(command=="finish")Finish();else if(command=="clean")CleanSigns();else Preview();}catch(Exception e){File.WriteAllText("Logs/YamazakiOffice-result.txt",e.ToString());}}
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No renderers: "+g.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Prop(string asset,Vector3 bottom,Vector3 size,float yaw=0){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(asset.StartsWith("Assets/")?asset:H+asset+".prefab");if(!source)throw new Exception("Missing "+asset);
  var g=(GameObject)PrefabUtility.InstantiatePrefab(source,root);g.transform.rotation=Quaternion.Euler(0,yaw,0);var b=BoundsOf(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z)));b=BoundsOf(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);count++;return g;
 }
 static Material Material(string name,Color color,bool grain=false){
  var path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
  m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",grain?.23f:.08f);
  var tex=new Texture2D(128,128,TextureFormat.RGB24,false);var random=new System.Random(17);
  for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=(float)random.NextDouble()*.06f;float v=grain?.86f+.07f*Mathf.Sin(y*.43f+Mathf.Sin(x*.04f)*.8f)+n:.92f+n;tex.SetPixel(x,y,new Color(v,v,v));}
  tex.Apply();var tp=Art+"/"+name+".png";File.WriteAllBytes(tp,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(tp);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));m.SetTextureScale("_BaseMap",new Vector2(grain?2:4,grain?4:4));AssetDatabase.CreateAsset(m,path);return m;
 }
 static void Box(string name,Vector3 p,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
 static void Text(string message,Vector3 pos,float yaw,float size){var g=new GameObject("Office sign / "+message);g.transform.SetParent(root,false);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);var text=g.AddComponent<TextMesh>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.84f,.77f,.59f);text.text=message;g.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Prologue/Art/WorldText.mat");g.AddComponent<PrologueDepthText>();}
 static void Panel(Vector3 center,float length,float yaw){int n=Mathf.CeilToInt(length/3.5f);float width=length/n;var rot=Quaternion.Euler(0,yaw,0);for(int i=0;i<n;i++){var p=center+rot*Vector3.right*(-length*.5f+width*(i+.5f));Prop("Environment/SM_Env_Wall_Interior_Ground_01",p,yaw==0||yaw==180?new Vector3(width,4.65f,.16f):new Vector3(.16f,4.65f,width),yaw);}}
 static void Desk(Vector3 bottom,Vector3 size){Prop("Props/SM_Prop_Desk_02",bottom,size);var y=bottom.y+size.y+.02f;Prop("Props/SM_Prop_Computer_Screen_01",new Vector3(bottom.x-.5f,y,bottom.z+.15f),new Vector3(.65f,.48f,.23f));Prop("Props/SM_Prop_Computer_Keyboard_01",new Vector3(bottom.x-.5f,y,bottom.z-.34f),new Vector3(.56f,.045f,.20f));Prop("Props/SM_Prop_Folder_03",new Vector3(bottom.x+.75f,y,bottom.z),new Vector3(.46f,.10f,.36f));Prop("Props/SM_Prop_DeskLight_01",new Vector3(bottom.x-1,y,bottom.z+.2f),new Vector3(.3f,.45f,.3f));}
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/YamazakiOffice-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
  var world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
  if(world.Cast<Transform>().FirstOrDefault(t=>t.name=="Yamazaki office / saved"))throw new Exception("Already applied; preserve scene edits");
  var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
  root=new GameObject("Yamazaki office / saved").transform;SceneManager.MoveGameObjectToScene(root.gameObject,scene);root.SetParent(world,false);
  Directory.CreateDirectory(Art);AssetDatabase.Refresh();wood=Material("Smoked walnut",new Color(.22f,.15f,.105f),true);plaster=Material("Warm grey plaster",new Color(.42f,.43f,.39f));carpet=Material("Charcoal office carpet",new Color(.12f,.16f,.17f));metal=Material("Graphite steel",new Color(.09f,.115f,.12f));count=0;
  // 기존 엄폐물과 길의 충돌은 보존하며 은행 전용 외형만 교체한다.
  var furnishing=world.Find("Synty bank furnishing");
  foreach(var t in furnishing.GetComponentsInChildren<Transform>(true).ToArray())if(t.name.StartsWith("SM_Prop_TellerDesk")||t.name.StartsWith("SM_Prop_SafeDepositBoxes"))foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
  foreach(var t in world.GetComponentsInChildren<Transform>(true).ToArray()){
   if(t.IsChildOf(root))continue;
   if(t.name.Contains("SM_Prop_ATM")||t.name=="ATM overhead sign"||t.name.StartsWith("Bank information /"))foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
   var r0=t.GetComponent<MeshRenderer>();if(!r0)continue;
   if(new[]{"Floor inlay","Stone tile"}.Contains(t.name))r0.sharedMaterial=t.position.z>8?plaster:carpet;
   if(new[]{"Timber wall base","Divider wainscot"}.Contains(t.name))r0.sharedMaterial=wood;
   if(new[]{"West wall","East wall","Back wall","Outer wall","Security divider","Office divider","Lobby divider","North wing wall","Main facade","Right facade"}.Contains(t.name))r0.sharedMaterial=plaster;
   if(new[]{"Marble column","Wall pilaster"}.Contains(t.name))r0.sharedMaterial=metal;
   if(new[]{"Column trim","Bronze chair rail","Divider bronze rail","Wall panel joint"}.Contains(t.name))r0.sharedMaterial=wood;
   if(t.name=="Bank sign board")r0.sharedMaterial=metal;
  }
  foreach(var text in world.GetComponentsInChildren<TextMesh>(true)){
   if(text.text.Contains("UNION")||text.text.Contains("은행")||text.text.Contains("BANK"))text.text="야마자키 상사";
   if(text.text.Contains("대기 홀"))text.text=text.text.Replace("대기 홀","업무 구역");
   if(text.text.Contains("VAULT"))text.text="특별 보관실 / 출입 제한";
  }
  // 양면 패널은 기존 벽 두께 안쪽에만 배치하여 출입구 폭을 유지한다.
  Panel(new Vector3(-9.73f,.06f,4),23,90);Panel(new Vector3(9.73f,.06f,4),23,270);Panel(new Vector3(0,.06f,15.72f),19,0);
  Panel(new Vector3(-17.72f,.06f,-29),41,90);Panel(new Vector3(17.72f,.06f,-29),41,270);
  foreach(float z in new[]{-18f,-30f,-41f}){float x=z==-30?4:-4;Panel(new Vector3(x,.06f,z+.34f),27.5f,180);Panel(new Vector3(x,.06f,z-.34f),27.5f,0);}
  foreach(float x in new[]{-6f,6f})Desk(new Vector3(x,0,2),new Vector3(4.2f,1.41f,2.2f));
  foreach(float z in new[]{-13f,-25f,-36f,-46f})foreach(float x in new[]{-6f,5f})Desk(new Vector3(x,0,z),new Vector3(3.2f,1.41f,1.5f));
  // 통행선을 비켜 벽면 기록 보관함과 장부를 배치한다.
  foreach(float z in new[]{-12f,-24f,-35f})foreach(int side in new[]{-1,1}){
   Prop("Props/SM_Prop_Shelf_01",new Vector3(side*17.1f,0,z),new Vector3(.8f,2.5f,2.4f),side>0?270:90);
   for(int k=0;k<3;k++)Prop("Props/SM_Prop_Box_01",new Vector3(side*16.95f,.35f+k*.68f,z),new Vector3(.48f,.4f,1.5f),90);
  }
  foreach(float x in new[]{-7.5f,7.5f}){Prop("Props/SM_Prop_Shelf_01",new Vector3(x,0,15.25f),new Vector3(3,2.8f,.65f));for(int k=0;k<3;k++)Prop("Props/SM_Prop_Box_01",new Vector3(x,.25f+k*.75f,15.1f),new Vector3(2.6f,.5f,.45f));}
  Box("Executive carpet",new Vector3(0,.045f,1),new Vector3(5,.015f,10),wood);
  Box("Ledger board",new Vector3(5.8f,2.8f,7.53f),new Vector3(4.1f,2.2f,.09f),wood);
  Text("山崎\n야마자키파\n특별 회계실",new Vector3(5.8f,2.8f,7.46f),0,.12f);
  Box("Office directory",new Vector3(-10.8f,2.7f,-41.37f),new Vector3(4.2f,1.8f,.1f),metal);
  Text("야마자키 상사\n업무지원 · 회계 · 기록보관\n허가받은 직원만 출입",new Vector3(-10.8f,2.7f,-41.44f),0,.085f);
  Box("Accounts board",new Vector3(0,2.7f,-18.37f),new Vector3(4,1.8f,.1f),wood);
  Text("정산 장부 / 반출 금지\n01  항만   02  운송   03  현금",new Vector3(0,2.7f,-18.44f),0,.09f);
  foreach(var l in world.GetComponentsInChildren<Light>(true))if(l.type==LightType.Point&&l.transform.position.z>-50&&l.transform.position.z<16&&l.transform.position.y>3){l.color=new Color(.90f,.91f,.81f);l.intensity=Mathf.Min(l.intensity,3.2f);}
  foreach(float x in new[]{-6f,6f}){var g=new GameObject("Executive warm task light");g.transform.SetParent(root,false);g.transform.position=new Vector3(x,2.6f,2);var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.75f,.45f);l.intensity=1.4f;l.range=5;l.shadows=LightShadows.None;}
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Preview();File.WriteAllText("Logs/YamazakiOffice-result.txt","PASS saved office conversion: "+count+" Synty modules and props. Existing vault, cash, navigation and collision retained. Backup saved before editing.");
 }
 static void CleanSigns(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);var world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
  foreach(var t in world.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Teller station "))foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Preview();
 }
 static void Finish(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);var world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;root=world.Cast<Transform>().First(t=>t.name=="Yamazaki office / saved");
  if(root.Find("Office final dressing"))throw new Exception("Already finished");var marker=new GameObject("Office final dressing");marker.transform.SetParent(root,false);
  foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Bank prop / SM_Prop_ATM")).ToArray()){var b=BoundsOf(t.gameObject);Prop("Props/SM_Prop_Drawers_01",new Vector3(b.center.x,b.min.y,b.center.z),b.size,180);}
  foreach(float z in new[]{2f,-13f,-25f,-36f,-46f})foreach(float x in new[]{-6f,5f}){
   Prop("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_ShopInterior_Chair_01.prefab",new Vector3(x,0,z+1.35f),new Vector3(.65f,1.1f,.65f),180);
   Prop("Props/SM_Prop_Folder_05",new Vector3(x+.35f,1.44f,z-.28f),new Vector3(.4f,.06f,.3f),20);
  }
  foreach(float z in new[]{2f,-12f,-25f,-36f,-46f}){var g=new GameObject("Office soft overhead fill");g.transform.SetParent(root,false);g.transform.position=new Vector3(0,3.5f,z);var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.9f,.9f,.82f);l.intensity=2.5f;l.range=18;l.shadows=LightShadows.None;}
  // 금고 양옆은 목재 패널로 마감하고 문의 작동 영역은 비워 둔다.
  wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Smoked walnut.mat");foreach(float x in new[]{-6.1f,6.1f})Box("Executive vault wall walnut",new Vector3(x,2.35f,7.64f),new Vector3(7.5f,4.65f,.045f),wood);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Preview();File.WriteAllText("Logs/YamazakiOffice-result.txt","PASS office furnished and saved; original collisions, vault and mission references preserved; scene backup saved.");
 }
 static void Refine(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);var world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;root=world.Cast<Transform>().FirstOrDefault(t=>t.name=="Yamazaki office / saved");
  var furnishing=world.Find("Synty bank furnishing");foreach(var t in furnishing.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("SM_Prop_Computer")||t.name.StartsWith("SM_Prop_Folder")||t.name.StartsWith("SM_Prop_DeskLight"))foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
  foreach(var text in root.GetComponentsInChildren<TextMesh>())text.text=text.text.Replace("山崎\n","");
  carpet=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Charcoal office carpet.mat");
  Box("Continuous executive flooring",new Vector3(0,.028f,0),new Vector3(19.5f,.01f,15.5f),carpet);Box("Continuous office flooring",new Vector3(0,.028f,-29),new Vector3(35.4f,.01f,41.5f),carpet);
  root.Find("Executive carpet").GetComponent<Renderer>().sharedMaterial=Material("Burgundy rug",new Color(.18f,.075f,.065f));
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Preview();
 }
 static void Preview(){var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  var previousScene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);var hidden=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene!=scene&&r.enabled).ToArray();var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene!=scene&&r.enabled).ToArray();foreach(var r in hidden)r.enabled=false;foreach(var l in lights)l.enabled=false;
  try{Capture(scene,new Vector3(-2,1.75f,-4),new Vector3(3,1.8f,8),"Yamazaki-executive");Capture(scene,new Vector3(12,1.75f,-38),new Vector3(-6,1.5f,-34),"Yamazaki-office");Capture(scene,new Vector3(0,1.75f,11),new Vector3(6,1.6f,15),"Yamazaki-vault");}finally{foreach(var r in hidden)r.enabled=true;foreach(var l in lights)l.enabled=true;SceneManager.SetActiveScene(previousScene);}}
 static void Capture(Scene scene,Vector3 from,Vector3 at,string name){var g=new GameObject("Office preview camera");SceneManager.MoveGameObjectToScene(g,scene);var c=g.AddComponent<Camera>();c.nearClipPlane=.05f;c.fieldOfView=65;var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;try{c.transform.position=from;c.transform.LookAt(at);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);}}
}}
#endif
