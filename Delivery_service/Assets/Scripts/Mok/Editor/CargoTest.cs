#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
public static class CargoTest {
static void Check(bool ok,string text,Action<string> log){if(!ok)throw new Exception(text);log("PASS: "+text);}
static void Capture(BikePizzaStorage storage,string name){var o=new GameObject("Cargo verification camera");var cam=o.AddComponent<Camera>();cam.enabled=false;cam.nearClipPlane=.03f;cam.farClipPlane=80;cam.fieldOfView=48;cam.cullingMask=~((1<<5)|(1<<17)|(1<<18)|(1<<20)|(1<<22));var target=storage.transform.position;
cam.transform.position=target+new Vector3(1.25f,1.0f,-1.35f);cam.transform.LookAt(target+new Vector3(0,.12f,.12f));var rt=new RenderTexture(1200,900,24);rt.Create();cam.targetTexture=rt;var prev=RenderTexture.active;
try{RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1200,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-cargo-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}finally{RenderTexture.active=prev;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(o);}}
static PizzaData Data()=>new PizzaData(new List<string>{"도우","소스","치즈"},true);
public static IEnumerator RunCore(Action<string> log){
var bike=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();var cargo=bike.GetComponent<BikeCargoVisual>();var inv=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.timeMultiplier=0;cycle.currentTime=12;typeof(DayNightCycle).GetMethod("UpdateLighting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cycle,null);
Check(cargo!=null&&cargo.Lid!=null,"Cargo scene model and lid references survive reload",log);
// Move the test player near the boot to make the transfer path representative.
var camera=inv.GetComponent<PlayerInteraction>().InteractionCamera;var pos=camera.transform.position;var rot=camera.transform.rotation;
camera.transform.position=bike.transform.position+new Vector3(.15f,.55f,-1.2f);camera.transform.LookAt(bike.transform.position+Vector3.up*.25f);
Physics.SyncTransforms();Check(Physics.Raycast(bike.transform.position+new Vector3(0,.25f,-1),Vector3.forward,out var hit,2,1<<bike.gameObject.layer)&&hit.collider.GetComponentInParent<BikePizzaStorage>()==bike,"Cargo interaction collider reachable",log);
var first=Data();inv.AddPackagedDataStack(new List<PizzaData>{first,Data()});bike.Interact(inv);bike.Interact(inv);Check(cargo.IsBusy&&bike.StoredCount==0,"Rapid input ignored; transfer waits for animation",log);
float time=0;bool captured=false;while(cargo.IsBusy){time+=Time.deltaTime;if(!captured&&time>.75f){Capture(bike,"loading");captured=true;}yield return null;}
Check(bike.StoredCount==2&&!inv.HasItem(),"Two packages stored exactly once",log);Capture(bike,"closed");
inv.AddPackagedDataStack(new List<PizzaData>{Data()});bike.Interact(inv);while(cargo.IsBusy)yield return null;Check(bike.StoredCount==3,"Additional load preserves existing packages",log);
bike.Interact(inv);time=0;captured=false;while(cargo.IsBusy){time+=Time.deltaTime;if(!captured&&time>.75f){Capture(bike,"unloading");captured=true;}yield return null;}
Check(bike.StoredCount==0&&inv.GetHeldPackagedPizzas().Count==3&&ReferenceEquals(inv.GetHeldPackagedPizzas()[0],first),"Retrieval preserves every package and its data",log);inv.ClearPackagedPizzas();
inv.AddPackagedDataStack(new List<PizzaData>{Data()});bike.Interact(inv);cargo.enabled=false;Check(inv.HasPackagedPizzas()&&bike.StoredCount==0,"Interrupted loading does not lose packages",log);cargo.enabled=true;inv.ClearPackagedPizzas();
inv.AddPackagedDataStack(new List<PizzaData>{Data()});bike.Interact(inv);while(cargo.IsBusy)yield return null;bike.Interact(inv);Check(bike.TryStealPizzas(),"Theft still removes cargo",log);while(cargo.IsBusy)yield return null;Check(!inv.HasItem()&&bike.StoredCount==0,"Theft during retrieval cannot duplicate pizzas",log);
camera.transform.position=pos;camera.transform.rotation=rot;
var regression=ToppingArtTest.RunCore(log);while(regression.MoveNext())yield return regression.Current;
}
}}
#endif
