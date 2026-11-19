#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static partial class AftermathSetup
 {
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject SmallProp(string path,Transform parent,Vector3 bottom,float size,Vector3 angles)
 {
 var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);g.name=Path.GetFileNameWithoutExtension(path);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(angles));
 foreach(var c in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
 foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
 var b=BoundsOf(g);g.transform.localScale*=size/Mathf.Max(b.size.x,b.size.y,b.size.z);b=BoundsOf(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);return g;
 }
 static float[] ShelfSurfaces(Transform shelf)
 {
 var areas=new Dictionary<float,float>();
 foreach(var mf in shelf.GetComponentsInChildren<MeshFilter>()){
 var mesh=mf.sharedMesh;if(!mesh)continue;var v=mesh.vertices;var tri=mesh.triangles;
 for(int i=0;i<tri.Length;i+=3){var a=mf.transform.TransformPoint(v[tri[i]]);var b=mf.transform.TransformPoint(v[tri[i+1]]);var c=mf.transform.TransformPoint(v[tri[i+2]]);var cross=Vector3.Cross(b-a,c-a);if(cross.normalized.y<.95f)continue;float y=Mathf.Round((a.y+b.y+c.y)/3*40)/40;float area=cross.magnitude*.5f;if(!areas.ContainsKey(y))areas[y]=0;areas[y]+=area;}}
 var result=new List<float>();foreach(var p in areas.Where(p=>p.Value>.1f&&p.Key>.15f).OrderBy(p=>p.Key))if(result.Count==0||p.Key-result.Last()>.18f)result.Add(p.Key);return result.ToArray();
 }
 public static void DressHideout(){try{
 var scene=EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-dressing-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;var set=film.shots[1].set.transform;
 if(set.Find("민기의 생활과 작업 흔적"))throw new Exception("Already dressed; retain authored changes.");
 var details=At("민기의 생활과 작업 흔적",set,Vector3.zero);var o=new Vector3(500,0,0);
 const string city="Assets/Synty/PolygonCity/Prefabs/Props/";const string generic="Assets/Synty/PolygonGeneric/Prefabs/Props/";const string heist="Assets/Synty/PolygonHeist/Prefab/";
 var desk=Prop(heist+"Props/SM_Prop_Table_01.prefab",details,o+new Vector3(-2.5f,0,.8f),.79f,90);var db=BoundsOf(desk);float top=db.max.y;
 Prop(heist+"Props/SM_Prop_DeskLight_01.prefab",details,new Vector3(497.2f,top,1.1f),.42f,150);
 Prop(generic+"SM_Gen_Prop_Mug_01.prefab",details,new Vector3(497.6f,top,.6f),.13f,20);
 SmallProp(generic+"SM_Gen_Prop_Papers_03.prefab",details,new Vector3(497.6f,top,.95f),.37f,new Vector3(0,16,0));
 SmallProp(heist+"Items/SM_Item_Wrench_01.prefab",details,new Vector3(497.55f,top,.4f),.29f,new Vector3(90,15,0));
 SmallProp(heist+"Items/SM_Item_ScrewDriver_01.prefab",details,new Vector3(497.85f,top,.55f),.24f,new Vector3(90,70,0));
 SmallProp(heist+"Items/SM_Item_Hammer_01.prefab",details,new Vector3(497.75f,top,1.2f),.3f,new Vector3(90,120,0));
 Prop(generic+"SM_Gen_Prop_Chair_01.prefab",details,o+new Vector3(-2.6f,0,-.1f),.85f,-25);
 var shelf=set.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_Gen_Prop_Shelf_03");var sb=BoundsOf(shelf.gameObject);var surfaces=ShelfSurfaces(shelf);if(surfaces.Length<2)throw new Exception("Cannot identify shelf surfaces");
 var ys=surfaces;float x=sb.center.x,z=sb.center.z;float spacing=Mathf.Min(.48f,sb.size.x*.23f);
 SmallProp(heist+"Items/SM_Item_DuffleBag_01.prefab",details,new Vector3(x,ys[0]+.005f,z),Mathf.Min(.65f,sb.size.x*.7f),Vector3.zero);
 SmallProp(city+"SM_Prop_CardboardBox_02.prefab",details,new Vector3(x-spacing,ys[1]+.005f,z),.33f,new Vector3(0,9,0));
 Prop(generic+"SM_Gen_Prop_Bottle_03.prefab",details,new Vector3(x+spacing,ys[1]+.005f,z),.23f,0);
 if(ys.Length>2){Prop(generic+"SM_Gen_Prop_Mug_01.prefab",details,new Vector3(x-spacing,ys[2]+.005f,z),.13f,45);SmallProp(city+"SM_Prop_Newspaper_01.prefab",details,new Vector3(x+.1f,ys[2]+.005f,z),.32f,new Vector3(0,-8,0));}
 if(ys.Length>3)SmallProp(city+"SM_Prop_CardboardBox_04.prefab",details,new Vector3(x,ys[3]+.005f,z),.3f,Vector3.zero);
 // Repair the backing behind the modular wall, without touching the room's action area.
 var plaster=AssetDatabase.LoadAssetAtPath<Material>(Folder+"비밀 창고 회벽.mat");Box("벽 뒤 틈 차단",details,o+new Vector3(0,1.6f,4.03f),new Vector3(8.2f,3.3f,.12f),plaster);
 var wood=AssetDatabase.LoadAssetAtPath<Material>(Folder+"오래된 나무.mat");var paper=AssetDatabase.LoadAssetAtPath<Material>(Folder+"서류.mat");
 Box("개인 메모판",details,o+new Vector3(-1.9f,1.9f,3.5f),new Vector3(.85f,.62f,.035f),wood);
 for(int i=0;i<3;i++){var note=Box("작업 메모",details,o+new Vector3(-2.14f+i*.24f,1.92f+(i%2)*.07f,3.47f),new Vector3(.18f,.29f,.008f),paper);note.localRotation=Quaternion.Euler(0,0,-8+i*7);}
 // Warm practical work light, cooler entry light, and a soft key on Mingi's hands.
 foreach(var light in set.GetComponentsInChildren<Light>(true)){light.intensity=light.color.b>light.color.r? .45f:.38f;light.range=4;}
 LightAt(details,new Vector3(497.25f,1.32f,1.05f),new Color(1,.62f,.28f),2.4f,3.4f);
 var key=At("보관함과 손을 비추는 작업등",details,o+new Vector3(1.3f,2.7f,-.7f)).gameObject.AddComponent<Light>();key.type=LightType.Spot;key.color=new Color(1,.83f,.61f);key.intensity=5;key.range=7;key.spotAngle=76;key.shadows=LightShadows.Soft;key.transform.LookAt(o+new Vector3(.25f,.9f,1.5f));
 set.gameObject.AddComponent<AftermathHideoutLighting>();
 foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Aftermath-hideout-dressing.txt","Saved personal bag, bottles/mug, papers, tools, stool and work lights. Shelf surface heights: "+string.Join(",",surfaces)+". Camera and motion data unchanged.");AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-hideout-dressing.txt",e.ToString());EditorApplication.Exit(1);}}
 }
}
#endif
