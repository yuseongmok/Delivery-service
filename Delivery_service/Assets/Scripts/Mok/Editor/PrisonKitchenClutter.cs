#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonKitchenClutter
 {
  static PrisonKitchenClutter(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/PrisonKitchenClutter"))return;var mode=File.ReadAllText("Temp/PrisonKitchenClutter").Trim();File.Delete("Temp/PrisonKitchenClutter");try{if(EditorSceneManager.GetActiveScene().name!="PrisonChapter")throw new Exception("Open prison scene first");if(mode=="build")Build();else if(mode=="capture")Capture();else Inspect();}catch(Exception e){File.WriteAllText("Logs/KitchenClutter.txt",e.ToString());}}
  static void Build()
  {
   var scene=EditorSceneManager.GetActiveScene();
   EditorSceneManager.SaveScene(scene,"Logs/Prison-before-dense-kitchen-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();
   if(m.transform.Find("촘촘한 주방 바닥 장애물"))throw new Exception("Dense layout already exists");
   var hazards=UnityEngine.Object.FindObjectsByType<ChickenNoiseHazard>(FindObjectsSortMode.None);
   var oil=hazards.First(h=>h.oil&&h.name=="흘린 기름"&&h.transform.position.y<.05f);
   var utensil=hazards.First(h=>!h.oil&&h.name=="어지럽혀진 냄비와 도구"&&h.transform.position.y<.05f);
   var obstacles=new System.Collections.Generic.List<Bounds>();
   foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
   {
    if(!c.enabled)continue;var b=c.bounds;
    if(b.max.y<.2f||b.min.y>1.6f||b.max.x<21||b.min.x>41||b.max.z< -33.5f||b.min.z> -20.5f)continue;
    if(c.isTrigger&&!c.GetComponentInParent<ChickenNoiseHazard>())continue;
    b.Expand(new Vector3(.8f,0,.8f));obstacles.Add(b);
   }
   foreach(var h in UnityEngine.Object.FindObjectsByType<PrisonFloorHazard>(FindObjectsSortMode.None))obstacles.Add(new Bounds(h.transform.position,new Vector3(h.stepTriggerRadius*2+.8f,2,h.stepTriggerRadius*2+.8f)));
   const int nx=81,nz=53;const float step=.25f;
   Vector3 Point(int id)=>new Vector3(21+(id%nx)*step,.6f,-33.5f+(id/nx)*step);
   bool Blocked(Vector3 p)=>obstacles.Any(b=>p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z);
   int Nearest(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z+33.5f)/step),0,nz-1)*nx+Mathf.Clamp(Mathf.RoundToInt((p.x-21)/step),0,nx-1);
   var route=new System.Collections.Generic.List<Vector3>();
   bool FindRoute(Vector3 from,Vector3 to,bool collect)
   {
    int first=Nearest(from);var previous=Enumerable.Repeat(-2,nx*nz).ToArray();var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(first);previous[first]=-1;int end=-1;
    while(queue.Count>0){int id=queue.Dequeue();var p=Point(id);if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(to.x,to.z))<2.3f&&!Blocked(p)){end=id;break;}
     foreach(int next in new[]{id-1,id+1,id-nx,id+nx}){if(next<0||next>=nx*nz||Mathf.Abs(next%nx-id%nx)>1||previous[next]!=-2||Blocked(Point(next)))continue;previous[next]=id;queue.Enqueue(next);}}
    if(end<0)return false;if(collect)for(int id=end;id>=0;id=previous[id])route.Add(Point(id));return true;
   }
   var entry=new Vector3(30,.6f,-21.3f);
   foreach(var target in m.ingredients.Select(i=>i.transform).Concat(new[]{m.station.transform}))if(!FindRoute(entry,target.position,true))throw new Exception("Existing route blocked: "+target.name);
   var root=new GameObject("촘촘한 주방 바닥 장애물");root.transform.SetParent(m.transform,false);Undo.RegisterCreatedObjectUndo(root,"Add kitchen clutter");
   var random=new System.Random(417);int added=0;
   try{
    var places=new System.Collections.Generic.List<Vector3>();
    for(float z=-32.4f;z< -21.4f;z+=1.05f)for(float x=22;x<40.5f;x+=1.05f)places.Add(new Vector3(x+(float)(random.NextDouble()-.5)*.28f,.025f,z+(float)(random.NextDouble()-.5)*.28f));
    foreach(var p0 in places.OrderBy(p=>random.Next()))
    {
     if(added>=44)break;Vector3 p=p0;
     if(route.Any(v=>Vector2.Distance(new Vector2(v.x,v.z),new Vector2(p.x,p.z))<1.05f))continue;
     if(obstacles.Any(b=>p.x>b.min.x-.35f&&p.x<b.max.x+.35f&&p.z>b.min.z-.35f&&p.z<b.max.z+.35f))continue;
     if(Vector3.Distance(p,new Vector3(30,0,-21))<2.0f)continue;
     bool isOil=added%3!=1;var copy=UnityEngine.Object.Instantiate((isOil?oil:utensil).gameObject,root.transform);
     copy.name=(isOil?"기름 얼룩 ":"흩어진 주방 식기 ")+(added+1);copy.transform.SetPositionAndRotation(p,Quaternion.Euler(0,(float)random.NextDouble()*360,0));
     foreach(var col in copy.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
     var renderers=copy.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
     float width=isOil?1.05f:.8f;copy.transform.localScale*=width/Mathf.Max(bounds.size.x,bounds.size.z);
     bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);copy.transform.position+=Vector3.up*(.025f-bounds.min.y);
     bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
     // Match detection to the visible prop; each cluster has only one noise trigger.
     var trigger=copy.AddComponent<BoxCollider>();trigger.isTrigger=true;
     trigger.center=copy.transform.InverseTransformPoint(new Vector3(bounds.center.x,.18f,bounds.center.z));
     float scale=copy.transform.lossyScale.x;float angle=copy.transform.eulerAngles.y*Mathf.Deg2Rad;float footprint=width/(Mathf.Abs(Mathf.Cos(angle))+Mathf.Abs(Mathf.Sin(angle)));trigger.size=new Vector3(footprint,.36f,footprint)/scale;
     var h=copy.GetComponent<ChickenNoiseHazard>();h.mission=m;
     var block=new Bounds(new Vector3(bounds.center.x,.6f,bounds.center.z),new Vector3(width+.8f,1,width+.8f));obstacles.Add(block);added++;
    }
    foreach(var target in m.ingredients.Select(i=>i.transform).Concat(new[]{m.station.transform}))if(!FindRoute(entry,target.position,false))throw new Exception("New layout blocks "+target.name);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    File.WriteAllText("Logs/KitchenClutter.txt","PASS: backed up current edits and saved PrisonChapter only.\nAdded hazards: "+added+"; previous: "+hazards.Length+"\nPASS: 0.8m-wide player clearance routes from entrance to all five ingredients and cooking station.\n");
   }catch{UnityEngine.Object.DestroyImmediate(root);throw;}
  }
  static void Capture()
  {
   File.WriteAllText("Logs/KitchenFloorHeight.txt",string.Join("\n",Physics.RaycastAll(new Vector3(30,2,-23),Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name+" "+h.point)));
   File.AppendAllText("Logs/KitchenFloorHeight.txt","\n"+string.Join("\n",UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.bounds.min.x<30&&r.bounds.max.x>30&&r.bounds.min.z< -23&&r.bounds.max.z> -23&&r.bounds.center.y<1).Select(r=>r.name+" "+r.bounds)));
   var go=new GameObject("Kitchen clutter review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.fieldOfView=78;camera.nearClipPlane=.05f;camera.cullingMask=UnityEngine.Object.FindFirstObjectByType<PrisonMission>().player.View.cullingMask;
   var map=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name=="Mok Navigation Map");bool mapEnabled=map&&map.enabled;if(map)map.enabled=false;
   var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=texture;
   try{go.transform.position=new Vector3(30,2.25f,-21.2f);go.transform.LookAt(new Vector3(31,.1f,-28.5f));camera.Render();RenderTexture.active=texture;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("../Documents/Chicken_Cooking_Flow");File.WriteAllBytes("../Documents/Chicken_Cooking_Flow/kitchen-dense-floor.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
   finally{if(map)map.enabled=mapEnabled;RenderTexture.active=previous;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);}
  }
  static void Inspect(){
   var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();string s="Station "+m.station.transform.position+" Chef "+m.chef.position+"\n";
   foreach(var i in m.ingredients)s+="Ingredient "+i.name+" "+i.transform.position+"\n";
   foreach(var h in UnityEngine.Object.FindObjectsByType<ChickenNoiseHazard>(FindObjectsInactive.Include,FindObjectsSortMode.None))s+="Hazard "+h.name+" "+h.transform.position+" oil="+h.oil+" active="+h.gameObject.activeSelf+" scale="+h.transform.lossyScale+"\n";
   foreach(var h in UnityEngine.Object.FindObjectsByType<PrisonFloorHazard>(FindObjectsInactive.Include,FindObjectsSortMode.None))s+="Woong "+h.name+" "+h.transform.position+"\n";
   foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){var b=c.bounds;if(!c.isTrigger&&b.center.x>24&&b.center.x<39&&b.center.z> -36&&b.center.z< -20 && b.size.y>.3f)s+="Solid "+c.name+" "+b+"\n";}
   File.WriteAllText("Logs/KitchenClutter.txt",s);
  }
 }
}
#endif







