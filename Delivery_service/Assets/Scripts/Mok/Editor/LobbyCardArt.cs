#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LobbyCardArt {
 const string Art="Assets/Scripts/Mok/Art/Lobby/";
 const string Props="Assets/Synty/PolygonHeist/Prefab/Props/";
 static LobbyCardArt(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-card-art-request"))return;File.Delete("Temp/Mok-card-art-request");try{Create();File.WriteAllText("Temp/Mok-card-art-result.txt","PASS");}catch(Exception e){File.WriteAllText("Temp/Mok-card-art-result.txt",e.ToString());Debug.LogException(e);}}
 static GameObject Prop(Transform root,string path,Vector3 position,float yaw=0,float size=1){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception(path);var g=UnityEngine.Object.Instantiate(asset,root);g.transform.localPosition=position;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*size;return g;}
 static void Aim(Transform bone,Transform child,Vector3 target){if(bone&&child)bone.rotation=Quaternion.FromToRotation(child.position-bone.position,target-bone.position)*bone.rotation;}
 static void Actor(Transform root,string name,Vector3 position,float yaw,bool point){
  var g=Prop(root,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_"+name+".prefab",Vector3.zero,0);
  var a=g.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);a.enabled=false;
  foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
  g.transform.localPosition=position;g.transform.localRotation=Quaternion.Euler(0,yaw,0);
  if(!point){var spine=a.GetBoneTransform(HumanBodyBones.Spine);if(spine)spine.rotation=Quaternion.AngleAxis(12,g.transform.right)*spine.rotation;}
  var forward=g.transform.forward;
  var grip=position+Vector3.up*1.38f+forward*.52f;
  foreach(bool right in new[]{true,false}){
   var u=a.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
   var l=a.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
   var h=a.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
   var target=grip+(right?Vector3.zero:-g.transform.right*.065f-Vector3.up*.025f+forward*.025f);
   var origin=u.position;float al=Vector3.Distance(origin,l.position),bl=Vector3.Distance(l.position,h.position);
   var dir=(target-origin).normalized;float d=Mathf.Min(Vector3.Distance(origin,target),al+bl-.005f);
   float along=(al*al-bl*bl+d*d)/(2*d);
   var bend=Vector3.ProjectOnPlane(Vector3.down+g.transform.right*(right?.3f:-.3f),dir).normalized;
   Aim(u,l,origin+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,al*al-along*along)));
   Aim(l,h,origin+dir*d);
      h.rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(0,-90,0);
   // 이 모델은 Finger_01 형태의 개별 관절을 사용한다.
   foreach(var finger in h.GetComponentsInChildren<Transform>()){
    if(finger==h)continue;
    if(finger.name.StartsWith("Finger_")||finger.name.StartsWith("IndexFinger_")){
     bool first=finger.name.Contains("_01");bool tip=finger.name.Contains("_04");
     finger.localRotation=Quaternion.Euler(0,0,tip?0:first?-48:-65);
    }else if(finger.name.StartsWith("Thumb_"))finger.localRotation=Quaternion.Euler(0,30,-25);
   }
  }
  var gun=Prop(root,"Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab",grip,yaw+180);
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Prologue/Art/M1911_URP.mat");
    if(mat)foreach(var r in gun.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
  var renderers=gun.GetComponentsInChildren<Renderer>();var gb=renderers[0].bounds;foreach(var r in renderers)gb.Encapsulate(r.bounds);
  gun.transform.localScale*=.38f/Mathf.Max(gb.size.x,gb.size.y,gb.size.z);
  gb=renderers[0].bounds;foreach(var r in renderers)gb.Encapsulate(r.bounds);
    // 총 전체 중심 대신 실제 탄창 중심을 손바닥 안쪽에 맞춘다.
  var magazine=gun.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("magazin"));
  var palm=a.GetBoneTransform(HumanBodyBones.RightHand).position+forward*.075f-Vector3.up*.025f;
  if(magazine)gun.transform.position+=palm-magazine.transform.TransformPoint(magazine.sharedMesh.bounds.center);
  else gun.transform.position+=palm+forward*.10f+Vector3.up*.04f-gb.center;
 }
 static void Gear(){
  // 선명한 톱니바퀴 실루엣을 충분한 해상도로 그린다.
  var t=new Texture2D(1024,768,TextureFormat.RGB24,false);
  for(int y=0;y<768;y++)for(int x=0;x<1024;x++){
   float dx=x-512,dy=y-384,r=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
   float tooth=Mathf.Cos(angle*10);float edge=tooth>.35f?213:180;
   float alpha=Mathf.Clamp01(edge-r)*Mathf.Clamp01(r-85);
   Color bg=Color.Lerp(new Color(.045f,.075f,.105f),new Color(.10f,.16f,.20f),Mathf.Clamp01(1-r/650));
   Color fg=Color.Lerp(new Color(.52f,.72f,.79f),new Color(.88f,.95f,.96f),y/768f);
   t.SetPixel(x,y,Color.Lerp(bg,fg,alpha));
  }
  t.Apply();File.WriteAllBytes(Art+"ModeSettings.png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 } static void Render(string name,bool story){
  var preview=new PreviewRenderUtility();var root=new GameObject("Card staging");preview.AddSingleGO(root);
  try{
   var wall=Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Wall_Interior_01.prefab",Vector3.zero);var bounds=wall.GetComponentInChildren<Renderer>().bounds;wall.transform.localScale=new Vector3(14/bounds.size.x,4/bounds.size.y,1);bounds=wall.GetComponentInChildren<Renderer>().bounds;wall.transform.position+=new Vector3(0,1.7f,2.2f)-bounds.center; var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.20f,.29f,.36f));foreach(var wr in wall.GetComponentsInChildren<Renderer>())wr.SetPropertyBlock(tint); var floor=Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Floor_01.prefab",Vector3.zero,0,4);var floorBounds=floor.GetComponentInChildren<Renderer>().bounds;floor.transform.position+=new Vector3(0,-.06f,0)-floorBounds.center;
   Prop(root.transform,Props+"SM_Prop_SafeDepositBoxes_01.prefab",new Vector3(.8f,0,1.4f),180,.6f);
   Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Pillar_01.prefab",new Vector3(-1.5f,0,1.7f),0,.7f);
   Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Door_03.prefab",new Vector3(-.7f,0,2f),180,.8f);
   var desk=Prop(root.transform,Props+"SM_Prop_Desk_01.prefab",new Vector3(-1.3f,0,.6f),180,.65f);
   // 기존 캐릭터와 사무실 소품을 카드용 장면으로 촬영한다.
   if(story){
        var cover=Prop(root.transform,Props+"SM_Prop_Desk_01.prefab",Vector3.zero,180,1);
    var cr=cover.GetComponentsInChildren<Renderer>();var cb=cr[0].bounds;foreach(var r in cr)cb.Encapsulate(r.bounds);
    cover.transform.localScale=new Vector3(2.1f/cb.size.x,.8f/cb.size.y,.65f/cb.size.z);
    cb=cr[0].bounds;foreach(var r in cr)cb.Encapsulate(r.bounds);cover.transform.position+=new Vector3(-.25f,.4f,-.55f)-cb.center;
    Actor(root.transform,"Hoodie",new Vector3(-.55f,-.23f,-.1f),115,false);
        var shield=Prop(root.transform,Props+"SM_Prop_SafeDepositBoxes_Half_01.prefab",Vector3.zero,180);
    var sr=shield.GetComponentsInChildren<Renderer>();var sb=sr[0].bounds;foreach(var r in sr)sb.Encapsulate(r.bounds);
    shield.transform.localScale=new Vector3(1.1f/sb.size.x,1.08f/sb.size.y,.55f/sb.size.z);
    sb=sr[0].bounds;foreach(var r in sr)sb.Encapsulate(r.bounds);shield.transform.position+=new Vector3(.95f,.54f,.1f)-sb.center;
    Actor(root.transform,"Jacket",new Vector3(.45f,0,.65f),100,true);
    Prop(root.transform,"Assets/Synty/PolygonHeist/Prefab/Items/SM_Item_DuffleBag_Open_Full_01.prefab",new Vector3(-1.3f,.85f,.6f),30,.8f);
    Prop(root.transform,Props+"SM_Prop_Money_Stack_01.prefab",new Vector3(-.9f,.85f,.6f),0,.55f);
   }else{
    Prop(root.transform,Props+"SM_Prop_Computer_Screen_01.prefab",new Vector3(0,.85f,0),180,1.35f);
    Prop(root.transform,Props+"SM_Prop_Computer_Keyboard_01.prefab",new Vector3(0,.86f,-.55f),180,1.2f);
    Prop(root.transform,Props+"SM_Prop_Computer_Mouse_01.prefab",new Vector3(.6f,.86f,-.55f),180,1.2f);
    Prop(root.transform,Props+"SM_Prop_DeskLight_01.prefab",new Vector3(-.8f,.85f,.1f),150,1);
   }
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.055f,.075f);preview.camera.fieldOfView=38;
   preview.camera.transform.position=story?new Vector3(1.6f,1.65f,-2.45f):new Vector3(2,2.3f,-3.2f);
   preview.camera.transform.LookAt(story?new Vector3(.1f,1.22f,.25f):new Vector3(0,1.1f,-.1f));
   preview.lights[0].intensity=1.1f;preview.lights[0].color=new Color(1,.60f,.30f);preview.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);
   preview.lights[1].intensity=1.8f;preview.lights[1].color=new Color(.4f,.65f,1);preview.lights[1].transform.rotation=Quaternion.Euler(20,150,0);
   preview.ambientColor=new Color(.08f,.10f,.15f);
   preview.BeginStaticPreview(new Rect(0,0,1024,768));preview.Render(true);var tex=preview.EndStaticPreview();File.WriteAllBytes(Art+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }finally{preview.Cleanup();}
 }
 static void Shop(){
  var c=UnityEngine.Object.FindFirstObjectByType<LobbyController>();var camera=c.lobbyCamera;var canvas=c.menu.GetComponentInParent<Canvas>();
  var rt=new RenderTexture(1024,768,24);var old=camera.targetTexture;var active=RenderTexture.active;bool enabled=canvas.enabled;float fov=camera.fieldOfView;
  try{canvas.enabled=false;camera.fieldOfView=44;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var t=new Texture2D(1024,768,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1024,768),0,0);t.Apply();File.WriteAllBytes(Art+"ModeShop.png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
  finally{canvas.enabled=enabled;camera.fieldOfView=fov;camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 public static void Create(){Render("ModeStory",true);Gear();Shop();AssetDatabase.Refresh();foreach(var name in new[]{"ModeStory","ModeShop","ModeSettings"}){var i=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");i.textureType=TextureImporterType.Default;i.mipmapEnabled=false;i.maxTextureSize=1024;i.textureCompression=TextureImporterCompression.CompressedHQ;i.SaveAndReimport();}LobbyModeBuilder.Apply();}
}
}
#endif















