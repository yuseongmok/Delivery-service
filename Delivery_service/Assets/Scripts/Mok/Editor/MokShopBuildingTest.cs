#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DeliveryService.Mok.ShopUI;
using DeliveryService.Yun.ShopUI;
public static class MokShopBuildingTest {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;static string log;static PlayerInteraction interaction;static Camera playerCamera;
static void Check(bool ok,string t){if(!ok)throw new Exception(t);log+="PASS: "+t+"\n";}
static void Invoke(object o,string n)=>o.GetType().GetMethod(n,F).Invoke(o,null);
static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
static void Aim(Vector3 from,Vector3 target){playerCamera.transform.position=from;playerCamera.transform.LookAt(target);Physics.SyncTransforms();Invoke(interaction,"TryInteract");}
static void Capture(string name,Vector3 pos,Vector3 target){var go=new GameObject("Building preview camera");var c=go.AddComponent<Camera>();c.transform.position=pos;c.transform.LookAt(target);c.fieldOfView=48;c.nearClipPlane=.05f;c.farClipPlane=400;
var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGB32);rt.Create();var old=RenderTexture.active;Texture2D tex=null;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-building-"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=old;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}}
public static string RunCore(){log="Building and light switch Play Mode verification\n";var root=GameObject.Find("PizzaStore/Mok Pizza Shop");Check(root!=null,"replacement building loaded from saved scene");var sw=root.GetComponentInChildren<ShopLightSwitch>();var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();interaction=UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();playerCamera=Get<Camera>(interaction,"playerCamera");var inv=interaction.GetComponent<ToppingInventory>();
Check(root.GetComponent<Renderer>().sharedMaterials.All(m=>m.shader.name=="Universal Render Pipeline/Lit"),"building uses scene-lit materials");
Physics.SyncTransforms();Check(!Physics.CapsuleCast(new Vector3(141.25f,.65f,64),new Vector3(141.25f,1.75f,64),.28f,Vector3.back,2.8f,~0,QueryTriggerInteraction.Ignore),"original right-hand entrance has clear player capsule passage");
var target=sw.GetComponent<Collider>().bounds.center;var from=target+Vector3.left*.8f;
Check(Physics.Raycast(from,(target-from).normalized,out var hit,1.5f,Get<LayerMask>(interaction,"interactLayer"))&&hit.collider.GetComponent<ShopLightSwitch>()==sw,"E interaction ray hits wall switch");
Aim(from,target);Check(!sw.LightsOn&&sw.lights.All(l=>!l.enabled),"existing player interaction switches all six lights off");Aim(from,target);Check(sw.LightsOn&&sw.lights.All(l=>l.enabled),"existing player interaction switches lights on again");
// Existing ingredient acquisition and workbench flow after shell/material replacement.
var dough=UnityEngine.Object.FindObjectsByType<Topping>(FindObjectsSortMode.None).Single(t=>Get<PizzaToppingData>(t,"toppingData").toppingType==ToppingType.Dough);
var d=dough.GetComponent<Collider>().bounds.center;Aim(d+Vector3.forward*1.2f+Vector3.up*.45f,d);Check(inv.HasTopping()&&inv.CurrentItem.toppingType==ToppingType.Dough,"dough tray is reachable and supplies paid dough");
var maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();var m=maker.GetComponent<Collider>().bounds.center;Aim(m+Vector3.forward*1.2f+Vector3.up*1.0f,m);Check(!inv.HasItem()&&Get<Pizza>(maker,"currentPizza")!=null,"dough can still be placed on original workbench");
foreach(var type in new[]{ToppingType.Sauce,ToppingType.Cheese}){var t=UnityEngine.Object.FindObjectsByType<Topping>(FindObjectsSortMode.None).Single(x=>Get<PizzaToppingData>(x,"toppingData").toppingType==type);var p=t.GetComponent<Collider>().bounds.center;Aim(p+Vector3.forward*1.2f+Vector3.up*.45f,p);Check(inv.HasTopping(),"ingredient reachable: "+type);maker.Interact(inv);Check(!inv.HasItem(),"workbench adds ingredient: "+type);}
var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.4f;Invoke(pc,"OpenPanel");Check(pc.pcUIPanel.activeSelf&&pc.pcUIPanel.GetComponent<ShopPcBusinessDashboard>()!=null,"original PC opens with orders and closing dashboard");Invoke(pc,"ClosePanel");
foreach(var renderer in interaction.GetComponentsInChildren<Renderer>()) renderer.enabled=false;
var previewPizza=Get<Pizza>(maker,"currentPizza");if(previewPizza!=null) previewPizza.gameObject.SetActive(false);
foreach(var time in new[]{6f,12f}){cycle.currentTime=time;Invoke(cycle,"UpdateLighting");sw.SetLights(true);Capture(time==6?"dawn":"day",new Vector3(145.4f,4.0f,74.5f),new Vector3(138,1.9f,60));}
cycle.currentTime=22;Invoke(cycle,"UpdateLighting");sw.SetLights(true);Capture("night-on",new Vector3(145.4f,4,74.5f),new Vector3(138,1.9f,60));Capture("interior-on",new Vector3(141.25f,1.95f,61.7f),new Vector3(137.6f,1.5f,57.6f));sw.SetLights(false);Capture("night-off",new Vector3(145.4f,4,74.5f),new Vector3(138,1.9f,60));Capture("interior-off",new Vector3(141.25f,1.95f,61.7f),new Vector3(137.6f,1.5f,57.6f));sw.SetLights(true);
Check(sw.label.text.Contains("끄기"),"switch label reflects current state");return log+"RESULT: PASS\n";}
}
#endif
