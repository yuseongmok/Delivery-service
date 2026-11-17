#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class StoveHeroRender {
 const string Art="../Documents/STOVE_Demo_Hero_v4/";
 const string Props="Assets/Synty/PolygonHeist/Prefab/Props/";
 static StoveHeroRender(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-stove-hero-request"))return;File.Delete("Temp/Mok-stove-hero-request");try{Create();File.WriteAllText("Temp/Mok-stove-hero-result.txt","PASS");}catch(Exception e){File.WriteAllText("Temp/Mok-stove-hero-result.txt",e.ToString());Debug.LogException(e);}}
 static GameObject Prop(Transform root,string path,Vector3 position,float yaw=0,float size=1){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception(path);var g=UnityEngine.Object.Instantiate(asset,root);g.transform.localPosition=position;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*size;if(path.Contains("/Environment/")||path.Contains("/Props/")){var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.22f,.31f,.42f));tint.SetColor("_Color",new Color(.22f,.31f,.42f));foreach(var r in g.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(tint);}return g;}
 static void Aim(Transform bone,Transform child,Vector3 target){if(bone&&child)bone.rotation=Quaternion.FromToRotation(child.position-bone.position,target-bone.position)*bone.rotation;}
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Actor(Transform root,string name,Vector3 position,float yaw,bool point){
  var g=Prop(root,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_"+name+".prefab",position,yaw);
  var a=g.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);a.enabled=false;
  foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
  var forward=g.transform.forward;var side=g.transform.right;
  var grip=position+Vector3.up*(point?1.38f:1.18f)+forward*(point?.48f:.29f);
  float bottom=0;
  if(!point){
   var box=Prop(root,"Assets/Scripts/Mok/Resources/MokPizzaArt/Box.prefab",Vector3.zero,yaw);
   var b=BoundsOf(box);box.transform.localScale*=.67f/Mathf.Max(b.size.x,b.size.z);
   b=BoundsOf(box);box.transform.position+=grip+Vector3.up*.08f-b.center;
   b=BoundsOf(box);grip=b.center;bottom=b.min.y;
  }else{
   var gun=Prop(root,"Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab",Vector3.zero,yaw+180);
   var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Prologue/Art/M1911_URP.mat");
   if(mat)foreach(var r in gun.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
   var b=BoundsOf(gun);gun.transform.localScale*=.34f/Mathf.Max(b.size.x,b.size.y,b.size.z);
   var mag=gun.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("magazin"));
   var anchor=mag?mag.transform.TransformPoint(mag.sharedMesh.bounds.center):BoundsOf(gun).center;
   gun.transform.position+=grip-anchor;
  }
  foreach(bool right in new[]{true,false}){
   var u=a.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
   var l=a.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
   var h=a.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
   Vector3 target=point?grip-forward*.075f+side*(right?.028f:-.060f)+Vector3.up*(right?.015f:.005f)
    :new Vector3(grip.x,bottom-.005f,grip.z)+forward*.05f+side*(right?.25f:-.25f);
   var origin=u.position;float al=Vector3.Distance(origin,l.position),bl=Vector3.Distance(l.position,h.position);
   var dir=(target-origin).normalized;float d=Mathf.Min(Vector3.Distance(origin,target),al+bl-.005f);
   float along=(al*al-bl*bl+d*d)/(2*d);
   var bend=Vector3.ProjectOnPlane(Vector3.down+side*(right?.25f:-.25f),dir).normalized;
   Aim(u,l,origin+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,al*al-along*along)));
   Aim(l,h,origin+dir*d);
   h.rotation=point?Quaternion.LookRotation(right?Vector3.down:Vector3.up,-side)
      :Quaternion.LookRotation(side,right?Vector3.down:Vector3.up);
   foreach(var t in h.GetComponentsInChildren<Transform>()){
    if(t==h)continue;
    if(t.name.StartsWith("Finger_")||t.name.StartsWith("IndexFinger_")){
     if(point){bool index=t.name.StartsWith("Index");float z=t.name.Contains("_01")?(index?25:60):t.name.Contains("_02")?75:t.name.Contains("_03")?50:0;t.localRotation=Quaternion.Euler(0,0,z);}
     else t.localRotation=Quaternion.identity;
    }
    if(point&&t.name.StartsWith("Thumb_"))t.localRotation=Quaternion.Euler(0,t.name.Contains("_01")?15:0,t.name.Contains("_01")?30:15);
   }
  }
 }
 static void Render(string name,bool story){
  var preview=new PreviewRenderUtility();var root=new GameObject("Card staging");preview.AddSingleGO(root);
  try{
   var wall=Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Wall_Interior_01.prefab",Vector3.zero);var bounds=wall.GetComponentInChildren<Renderer>().bounds;wall.transform.localScale=new Vector3(14/bounds.size.x,4/bounds.size.y,1);bounds=wall.GetComponentInChildren<Renderer>().bounds;wall.transform.position+=new Vector3(0,1.7f,2.2f)-bounds.center; var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.08f,.18f,.28f));tint.SetColor("_Color",new Color(.08f,.18f,.28f));foreach(var wr in wall.GetComponentsInChildren<Renderer>())wr.SetPropertyBlock(tint); var floor=Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Floor_01.prefab",Vector3.zero,0,4);var floorBounds=floor.GetComponentInChildren<Renderer>().bounds;floor.transform.position+=new Vector3(0,-.06f,0)-floorBounds.center;
   Prop(root.transform,Props+"SM_Prop_SafeDepositBoxes_01.prefab",new Vector3(.8f,0,1.4f),180,.6f);
   Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Pillar_01.prefab",new Vector3(-1.5f,0,1.7f),0,.7f);
   Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Door_03.prefab",new Vector3(-.7f,0,2f),180,.8f);
   var desk=Prop(root.transform,Props+"SM_Prop_Desk_01.prefab",new Vector3(-1.3f,0,.6f),180,.65f);
   // 기존 캐릭터와 사무실 소품을 카드용 장면으로 촬영한다.
   if(story){
        var cover=Prop(root.transform,Props+"SM_Prop_Desk_01.prefab",Vector3.zero,180,1);
    var cr=cover.GetComponentsInChildren<Renderer>();var cb=cr[0].bounds;foreach(var r in cr)cb.Encapsulate(r.bounds);
    cover.transform.localScale=new Vector3(2.1f/cb.size.x,.5f/cb.size.y,.65f/cb.size.z);
    cb=cr[0].bounds;foreach(var r in cr)cb.Encapsulate(r.bounds);cover.transform.position+=new Vector3(-.25f,.25f,-.7f)-cb.center;
    Actor(root.transform,"Hoodie",new Vector3(-.26f,.02f,-.08f),175,false);
        var shield=Prop(root.transform,Props+"SM_Prop_SafeDepositBoxes_Half_01.prefab",Vector3.zero,180);
    var sr=shield.GetComponentsInChildren<Renderer>();var sb=sr[0].bounds;foreach(var r in sr)sb.Encapsulate(r.bounds);
    shield.transform.localScale=new Vector3(1.1f/sb.size.x,.8f/sb.size.y,.55f/sb.size.z);
    sb=sr[0].bounds;foreach(var r in sr)sb.Encapsulate(r.bounds);shield.transform.position+=new Vector3(1.4f,.4f,.4f)-sb.center;
    Actor(root.transform,"Jacket",new Vector3(.22f,.10f,.46f),175,true);
   }else{
    Prop(root.transform,Props+"SM_Prop_Computer_Screen_01.prefab",new Vector3(0,.85f,0),180,1.35f);
    Prop(root.transform,Props+"SM_Prop_Computer_Keyboard_01.prefab",new Vector3(0,.86f,-.55f),180,1.2f);
    Prop(root.transform,Props+"SM_Prop_Computer_Mouse_01.prefab",new Vector3(.6f,.86f,-.55f),180,1.2f);
    Prop(root.transform,Props+"SM_Prop_DeskLight_01.prefab",new Vector3(-.8f,.85f,.1f),150,1);
   }
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.055f,.075f);preview.camera.fieldOfView=27;
   bool wide=name.Contains("wide");
   preview.camera.transform.position=wide?new Vector3(-.4f,1.7f,-3.1f):story?new Vector3(.05f,1.7f,-3.1f):new Vector3(2,2.3f,-3.2f);
   preview.camera.transform.LookAt(wide?new Vector3(-.4f,1.48f,.22f):story?new Vector3(.03f,1.53f,.22f):new Vector3(0,1.1f,-.1f));
   preview.lights[0].intensity=1.1f;preview.lights[0].color=new Color(1,.80f,.60f);preview.lights[0].transform.rotation=Quaternion.Euler(28,-40,0);
   preview.lights[1].intensity=1.5f;preview.lights[1].color=new Color(.4f,.65f,1);preview.lights[1].transform.rotation=Quaternion.Euler(12,145,0);
   preview.ambientColor=new Color(.04f,.07f,.12f);
   preview.BeginStaticPreview(new Rect(0,0,wide?1920:1600,wide?1080:1600));preview.Render(true);var tex=preview.EndStaticPreview();File.WriteAllBytes(Art+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }finally{preview.Cleanup();}
 }
 public static void Create(){Directory.CreateDirectory(Art);Render("Hero-square-master",true);Render("Hero-wide-master",true);}
}
}
#endif
