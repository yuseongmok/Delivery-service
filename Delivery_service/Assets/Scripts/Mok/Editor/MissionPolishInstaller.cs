#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class MissionPolishInstaller {
 const string Dir="Assets/Scripts/Mok/Art/StoryCharacters/";
 // 어두운 골목에서도 인물별 색과 1인칭 손 모양이 읽히도록 최소 명도를 보장한다.
 public static void Refine(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-costume-refine-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Dir.TrimEnd('/')})){var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",mat.color*.22f);EditorUtility.SetDirty(mat);}
  var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var a=m.broker.GetComponentInChildren<Animator>();var chest=a.GetBoneTransform(HumanBodyBones.Chest)??a.GetBoneTransform(HumanBodyBones.Spine);
  // 원본 모델의 머리 위 모자 부분을 피부색과 분리한다.
  foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Dir.TrimEnd('/')})){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));if(mesh.subMeshCount!=4)continue;var skinTriangles=new List<int>();var darkTriangles=mesh.GetTriangles(3).ToList();var tri=mesh.GetTriangles(2);var vertices=mesh.vertices;for(int i=0;i<tri.Length;i+=3){var list=(vertices[tri[i]].y+vertices[tri[i+1]].y+vertices[tri[i+2]].y)/3>1.62f?darkTriangles:skinTriangles;list.AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}mesh.SetTriangles(skinTriangles,2);mesh.SetTriangles(darkTriangles,3);EditorUtility.SetDirty(mesh);}
  if(!m.broker.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="Broker fitted coat")){
   var jacket=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Broker jacket.mat");Prop("Broker fitted coat",chest,chest.position+m.broker.rotation*new Vector3(0,-.12f,0),new Vector3(.42f,.34f,.27f),m.broker.rotation,jacket,PrimitiveType.Capsule);
  }
  foreach(var root in new[]{m.player.avatar.leftFist,m.player.avatar.rightFist}){
   if(!root)continue;foreach(var child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   var skin=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Fist skin.mat");var cloth=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Fist sleeve.mat");
   void Part(string name,Vector3 pos,Vector3 size,Material mat){var o=Prop(name,root,root.position,size,root.rotation,mat,PrimitiveType.Sphere);o.transform.localPosition=pos;o.layer=2;o.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
   Part("Closed palm",Vector3.zero,new(.105f,.075f,.11f),skin);
   for(int i=0;i<4;i++)Part("Curled finger "+i,new(-.036f+i*.024f,.012f,.039f),new(.026f,.059f,.05f),skin);
   float sign=root==m.player.avatar.leftFist?1:-1;Part("Folded thumb",new(sign*.047f,-.015f,.006f),new(.035f,.038f,.075f),skin);
   Part("Wrist",new(0,-.005f,-.07f),new(.063f,.06f,.09f),skin);Part("Jacket cuff",new(0,-.012f,-.16f),new(.083f,.08f,.16f),cloth);
  }
  foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(text.text.Contains("임대료")&&text.text.Contains("매일"))text.text="낮에는 주문을 골라 배달하고, 밤에는 휴대폰에서 다음 사건을 준비하세요.";
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Mission-art-refine.txt","PASS saved refined costume and fists");
 }
 public static void Inspect(){var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var list=new List<string>();foreach(var r in m.broker.GetComponentsInChildren<Renderer>(true)){list.Add(r.name+" "+r.GetType().Name+" pos="+r.transform.position+" scale="+r.transform.lossyScale+" mat="+string.Join(",",r.sharedMaterials.Select(x=>x?x.name:"null")));if(r is SkinnedMeshRenderer s)list.Add("weights="+s.sharedMesh.boneWeights.Length+" bones="+s.bones.Length);}list.Add("root="+m.broker.position+" rot="+m.broker.eulerAngles);File.WriteAllLines("Logs/Mission-art-inspect.txt",list);}
 static Material Mat(string name,Color color){var path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(m);return m;}
 static GameObject Prop(string name,Transform bone,Vector3 world,Vector3 scale,Quaternion rotation,Material material,PrimitiveType type=PrimitiveType.Cube){var o=GameObject.CreatePrimitive(type);o.name=name;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.transform.SetPositionAndRotation(world,rotation);o.transform.localScale=scale;o.transform.SetParent(bone,true);o.GetComponent<Renderer>().sharedMaterial=material;return o;}
 static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
 static RuntimeAnimatorController Locomotion(){string path=Dir+"StoryWalk.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(c)return c;c=AnimatorController.CreateAnimatorControllerAtPath(path);c.AddParameter("MoveSpeed",AnimatorControllerParameterType.Float);var sm=c.layers[0].stateMachine;var idle=sm.AddState("Idle");idle.motion=Clip("Male_Idle");var walk=sm.AddState("Walk");walk.motion=Clip("Male_Walking");walk.speed=1.4f;sm.defaultState=idle;var a=idle.AddTransition(walk);a.hasExitTime=false;a.duration=.15f;a.AddCondition(AnimatorConditionMode.Greater,.1f,"MoveSpeed");var b=walk.AddTransition(idle);b.hasExitTime=false;b.duration=.15f;b.AddCondition(AnimatorConditionMode.Less,.1f,"MoveSpeed");return c;}
 static void Style(Transform root,string role){
  if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Story costume "+role))return;
  var animator=root.GetComponentInChildren<Animator>(true);if(!animator||!animator.isHuman)throw new Exception("Humanoid missing "+root.name);
  bool mingi=role=="Mingi",broker=role=="Broker";
  var jacket=Mat(role+" jacket",mingi?new Color(.12f,.42f,.36f):broker?new Color(.40f,.13f,.17f):new Color(.055f,.07f,.095f));
  var trousers=Mat(role+" trousers",new Color(.075f,.105f,.14f));var skin=Mat(role+" skin",new Color(.65f,.46f,.32f));var dark=Mat("Leather",new Color(.035f,.04f,.045f));var accent=Mat(role+" accent",mingi?new Color(.96f,.48f,.14f):broker?new Color(.88f,.80f,.62f):new Color(.7f,.07f,.08f));
  var head=animator.GetBoneTransform(HumanBodyBones.Head);var handL=animator.GetBoneTransform(HumanBodyBones.LeftHand);var handR=animator.GetBoneTransform(HumanBodyBones.RightHand);
  var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};var legs=new[]{animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)};
  foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
   if(!renderer.sharedMesh||renderer.sharedMesh.boneWeights.Length==0)continue;
   string path=Dir+role+"-"+renderer.name.Replace("/","_")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(!mesh){mesh=UnityEngine.Object.Instantiate(renderer.sharedMesh);var weights=mesh.boneWeights;var groups=new List<int>[4];for(int i=0;i<4;i++)groups[i]=new();
    int Slot(int vertex){var w=weights[vertex];int index=w.boneIndex0;float best=w.weight0;if(w.weight1>best){best=w.weight1;index=w.boneIndex1;}if(w.weight2>best){best=w.weight2;index=w.boneIndex2;}if(w.weight3>best)index=w.boneIndex3;var bone=renderer.bones[index];if(bone.IsChildOf(head)||bone.IsChildOf(handL)||bone.IsChildOf(handR))return 2;if(feet.Any(f=>bone.IsChildOf(f)))return 3;if(legs.Any(l=>bone.IsChildOf(l)))return 1;return 0;}
    var tri=mesh.triangles;for(int i=0;i<tri.Length;i+=3){int slot=Slot(tri[i]);groups[slot].AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}mesh.subMeshCount=4;for(int i=0;i<4;i++)mesh.SetTriangles(groups[i],i);AssetDatabase.CreateAsset(mesh,path);
   }renderer.sharedMesh=mesh;renderer.sharedMaterials=new[]{jacket,trousers,skin,dark};
  }
  var tag=new GameObject("Story costume "+role);tag.transform.SetParent(root,false);
  var chest=animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);var neck=animator.GetBoneTransform(HumanBodyBones.Neck)??head;
  float h=Vector3.Distance(head.position,(feet[0].position+feet[1].position)*.5f);float u=h/1.5f;
  GameObject Item(string n,Transform bone,Vector3 off,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)=>Prop(n,bone,bone.position+root.rotation*(off*u),size*u,root.rotation,mat,type);
  if(mingi){Item("Mingi orange work cap",head,new(0,.12f,0),new(.29f,.10f,.28f),accent,PrimitiveType.Sphere);Item("Mingi cap brim",head,new(0,.09f,.16f),new(.28f,.025f,.15f),accent);Item("Mingi cream chest band",chest,new(0,.02f,.14f),new(.31f,.055f,.025f),Mat("Cream",new Color(.92f,.85f,.65f)));Item("Mingi shoulder pouch",chest,new(.22f,-.18f,0),new(.15f,.23f,.10f),dark);}
  else if(broker){Item("Broker shirt",chest,new(0,.07f,.13f),new(.13f,.26f,.03f),accent);Item("Broker tie",chest,new(0,.04f,.153f),new(.045f,.23f,.025f),dark);Item("Broker glasses left",head,new(-.07f,.035f,.12f),new(.10f,.045f,.035f),dark);Item("Broker glasses right",head,new(.07f,.035f,.12f),new(.10f,.045f,.035f),dark);Item("Broker glasses bridge",head,new(0,.035f,.13f),new(.055f,.012f,.025f),dark);Item("Broker grey hair",head,new(0,.12f,-.02f),new(.28f,.08f,.24f),Mat("Silver hair",new Color(.54f,.52f,.47f)),PrimitiveType.Sphere);}
  else {Item("Gang face mask",head,new(0,-.03f,.12f),new(.20f,.11f,.045f),dark);Item("Gang red armband",animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),new(-.07f,-.08f,0),new(.14f,.10f,.16f),accent);Item("Gang red collar",neck,new(0,-.05f,0),new(.23f,.09f,.20f),accent);}
 }
 static void Fists(PlayerCombat p){var avatar=p.avatar;var skin=Mat("Fist skin",new Color(.68f,.46f,.31f));var cloth=Mat("Fist sleeve",new Color(.86f,.34f,.12f));var camera=p.GetComponent<PlayerInteraction>().InteractionCamera.transform;
  for(int side=0;side<2;side++){var existing=side==0?avatar.leftFist:avatar.rightFist;if(existing)continue;var root=new GameObject(side==0?"Left punch hand":"Right punch hand").transform;root.SetParent(camera,false);
   var fist=Prop("Closed fist",root,root.position,new Vector3(.115f,.10f,.15f),camera.rotation,skin);fist.transform.localPosition=Vector3.zero;
   var sleeve=Prop("Jacket cuff",root,root.position,new Vector3(.105f,.09f,.13f),camera.rotation,cloth);sleeve.transform.localPosition=new Vector3(0,-.015f,-.12f);
   for(int i=0;i<4;i++){var finger=Prop("Knuckle "+i,root,root.position,new Vector3(.027f,.035f,.055f),camera.rotation,skin,PrimitiveType.Sphere);finger.transform.localPosition=new Vector3(-.041f+i*.027f,.035f,.065f);}
   foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=2;root.gameObject.SetActive(false);if(side==0)avatar.leftFist=root;else avatar.rightFist=root;
  }p.kickCooldown=.65f;EditorUtility.SetDirty(avatar);EditorUtility.SetDirty(p);
 }
 public static void Install(){
  Directory.CreateDirectory(Dir);var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=StoryTutorialBuilder.Main)throw new Exception("Open main scene");if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-mission-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var t=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();Style(t.actor,"Mingi");Style(m.broker,"Broker");Style(m.enemyTemplate.transform,"Gang");
  foreach(var actor in new[]{m.broker,t.actor}){var animator=actor.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=Locomotion();animator.enabled=true;animator.speed=1;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;EditorUtility.SetDirty(animator);}
  Fists(m.player);
  var lesson=m.GetComponent<FirstWantedLesson>();if(!lesson){lesson=m.gameObject.AddComponent<FirstWantedLesson>();var source=m.subtitle;var label=UnityEngine.Object.Instantiate(source,m.missionUI.transform.parent);var canvas=new GameObject("First wanted lesson",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(m.transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=77;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);label.transform.SetParent(canvas.transform,false);label.rectTransform.anchorMin=new Vector2(.18f,.25f);label.rectTransform.anchorMax=new Vector2(.82f,.37f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;label.text=lesson.message;lesson.subtitle=label;lesson.panel=canvas;canvas.SetActive(false);EditorUtility.SetDirty(lesson);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var bank=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);if(!EditorSceneManager.SaveScene(bank,"Logs/Bank-before-character-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Bank backup failed");var d=bank.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();Style(d.accomplice.transform,"Mingi");Style(d.shopArrival.actor,"Mingi");EditorSceneManager.MarkSceneDirty(bank);EditorSceneManager.SaveScene(bank);EditorSceneManager.CloseScene(bank,true);AssetDatabase.SaveAssets();File.WriteAllText("Logs/MissionPolish-install.txt","PASS saved Mingi, broker, gang costumes; dedicated walk controller; two punch hands; first wanted lesson; scene backups");
 }
}
}
#endif
