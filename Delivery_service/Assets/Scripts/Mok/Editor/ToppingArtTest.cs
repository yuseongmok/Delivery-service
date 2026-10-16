#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
public static class ToppingArtTest {
const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
static object Get(object o,string field)=>o.GetType().GetField(field,Flags).GetValue(o);
static void Check(bool ok,string message,Action<string> log){if(!ok)throw new Exception(message);log("PASS: "+message);}
static void Capture(Vector3 target,string name,float distance=1){
var obj=new GameObject("Topping verification camera");var cam=obj.AddComponent<Camera>();cam.enabled=false;cam.nearClipPlane=.03f;cam.farClipPlane=80;cam.fieldOfView=48;
cam.cullingMask=~((1<<5)|(1<<17)|(1<<18)|(1<<20)|(1<<22));cam.transform.position=target+new Vector3(.55f,1.1f,-1.0f)*distance;cam.transform.LookAt(target);
var rt=new RenderTexture(1200,900,24);rt.Create();cam.targetTexture=rt;var previous=RenderTexture.active;
try{RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1200,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-topping-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);}}
public static IEnumerator RunCore(Action<string> log){
var regression=MokHeldItemTest.RunCore(log);while(regression.MoveNext())yield return regression.Current;
var inv=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();var maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();var oven=UnityEngine.Object.FindFirstObjectByType<Oven>();
maker.Interact(inv);var leftover=inv.RemovePizza();if(leftover!=null)UnityEngine.Object.Destroy(leftover);
var data=Resources.Load<PizzaIngredientLibrary>("MokPizzaArt/Ingredients").ingredients;
PizzaToppingData Find(ToppingType type)=>data.First(d=>d.toppingType==type);
foreach(var type in new[]{ToppingType.Pepperoni,ToppingType.Pepper,ToppingType.Pineapple,ToppingType.Chocolate,ToppingType.Jelly}){
inv.AddItem(Find(ToppingType.Dough));maker.Interact(inv);var pizza=(Pizza)Get(maker,"currentPizza");
foreach(var t in new[]{ToppingType.Sauce,ToppingType.Cheese,type}){inv.AddItem(Find(t));maker.Interact(inv);}
var model=pizza.transform.Find("Mok Pizza Stages");var raw=model.Find(type+"Raw");var baked=model.Find(type+"Baked");
Check(raw!=null&&raw.gameObject.activeSelf&&!baked.gameObject.activeSelf,"Raw topping visible: "+type,log);
float cheeseTop=model.Find("ShreddedCheese").GetComponent<MeshFilter>().sharedMesh.bounds.max.y;
Check(raw.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.min.y>cheeseTop,"Entire topping sits above shredded cheese: "+type,log);
Capture(pizza.transform.position,type+"-raw");var recipe=pizza.currentToppings.ToArray();
maker.Interact(inv);oven.Interact(inv);while(!pizza.IsBaked)yield return null;
Check(pizza.transform.Find("Mok Ingredient Tray")==null,"Supply tray never attached to actual pizza: "+type,log);
Check(!raw.gameObject.activeSelf&&baked.gameObject.activeSelf,"Oven switches topping to baked model: "+type,log);
Check(baked.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.min.y>model.Find("MeltedCheese").GetComponent<MeshFilter>().sharedMesh.bounds.max.y,"Baked topping above melted cheese: "+type,log);
Check(pizza.currentToppings.SequenceEqual(recipe),"Recipe preserved: "+type,log);
oven.Interact(inv);maker.Interact(inv);Capture(pizza.transform.position,type+"-baked");
maker.Interact(inv);var held=inv.GetComponent<CarriedItemVisual>().Display;Check(held.GetComponentsInChildren<Transform>().Any(t=>t.name==type+"Baked"),"Held pizza includes baked topping: "+type,log);
inv.RemovePizza();UnityEngine.Object.Destroy(pizza.gameObject);yield return null;
}
var trays=UnityEngine.Object.FindObjectsByType<Topping>(FindObjectsSortMode.None);
Check(trays.Length>=8,"Eight ingredient stations found",log);
foreach(var tray in trays){
var item=(PizzaToppingData)Get(tray,"toppingData");var visual=tray.GetComponent<ToppingTrayVisual>();
Check(visual!=null&&visual.Model!=null,"New tray visible: "+item.toppingType,log);
Check(tray.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Mok Ingredient Tray")==1,"Exactly one scene tray after Play initialization: "+item.toppingType,log);
Check(visual.Model.GetComponentsInChildren<Collider>().Length==0,"Display tray adds no blocking colliders: "+item.toppingType,log);
var collider=tray.GetComponentsInChildren<Collider>().First(c=>c.enabled&&!c.isTrigger);Physics.SyncTransforms();
Check(Physics.Raycast(collider.bounds.center+Vector3.up,Vector3.down,out var hit,2,1<<tray.gameObject.layer)&&hit.collider.GetComponentInParent<Topping>()==tray,"Original tray interaction ray preserved: "+item.toppingType,log);
bool basic=item.toppingType==ToppingType.Dough||item.toppingType==ToppingType.Sauce||item.toppingType==ToppingType.Cheese;
if(!basic)PlayerPrefs.SetInt("Stock_"+item.toppingName,2);
int money=MoneyManager.Instance.currentMoney;tray.Interact(inv);
Check(inv.CurrentItem==item,"Tray supplies correct ingredient: "+item.toppingType,log);
if(!basic)Check(ToppingStockManager.GetStock(item.toppingName)==1,"Tray consumes one stock: "+item.toppingType,log);
else Check(MoneyManager.Instance.currentMoney==money-Mathf.Max(0,item.cost),"Ingredient purchase cost unchanged: "+item.toppingType,log);
inv.RemoveItem();Capture(visual.Model.position+Vector3.up*.07f,"tray-"+item.toppingType,.6f);
log("TRAY "+item.toppingType+" position="+visual.Model.position+" scale="+visual.Model.lossyScale);
}
}
}}
#endif
