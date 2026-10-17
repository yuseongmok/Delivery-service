#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DeliveryService.Yun.ShopUI;

namespace DeliveryService.Mok.Editor
{
    public static class MokPizzaArtTest
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Get(object o,string field)=>o.GetType().GetField(field,Flags).GetValue(o);
        static void Check(bool value,string message,Action<string> log) {if(!value)throw new Exception(message);log("PASS: "+message);}
        static void Capture(Transform target,string name,float distance=1)
        {
            var obj=new GameObject("Pizza art verification camera"); var camera=obj.AddComponent<Camera>();
            camera.enabled=false; camera.nearClipPlane=.03f;camera.farClipPlane=80;camera.fieldOfView=48;
            camera.cullingMask=~((1<<5)|(1<<17)|(1<<18)|(1<<20));
            camera.transform.position=target.position+new Vector3(.75f,.95f,-1.05f)*distance;
            camera.transform.LookAt(target.position+Vector3.up*.02f);
            var rt=new RenderTexture(1200,900,24);rt.Create();camera.targetTexture=rt;
            var old=RenderTexture.active;
            try {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-pizza-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
            } finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);}
        }
        public static IEnumerator RunCore(Action<string> log)
        {
            var ingredients=Resources.Load<PizzaIngredientLibrary>("MokPizzaArt/Ingredients").ingredients;
            PizzaToppingData Find(ToppingType type)=>ingredients.First(d=>d.toppingType==type);
            var inv=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();
            var maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();
            var oven=UnityEngine.Object.FindFirstObjectByType<Oven>();
            var packing=UnityEngine.Object.FindFirstObjectByType<PizzaPacking>();
            var pickup=(PizzaPickUpZone)Get(packing,"pickupZone");
            var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.timeMultiplier=0;cycle.currentTime=12;
            cycle.GetType().GetMethod("UpdateLighting",Flags).Invoke(cycle,null);
            Check(MoneyManager.Instance.currentMoney==20000,"Fresh session starts at 20,000",log);
            foreach(var data in ingredients) if(data!=null) Check(ToppingStockManager.GetStock(data.toppingName)==0&&ToppingStockManager.GetPendingStock(data.toppingName)==0,"Fresh inventory: "+data.toppingName,log);
            NewGameStockResetCheck.Run();log("PASS: Stock reset and next-day carryover regression");
            inv.AddItem(Find(ToppingType.Dough)); maker.Interact(inv);
            var pizza=(Pizza)Get(maker,"currentPizza");var visual=pizza.GetComponent<PizzaStageVisual>();
            Check(visual.Ready,"New dough visual attached to actual dough prefab",log);
            Check(pizza.GetComponent<BoxCollider>().size.x>.9f,"Interaction collider matches visible dough",log);
            inv.AddItem(Find(ToppingType.Sauce));maker.Interact(inv);
            Check(visual.transform.Find("Mok Pizza Stages/Sauce").gameObject.activeSelf,"Sauce visible",log);Capture(pizza.transform,"sauce");
            inv.AddItem(Find(ToppingType.Cheese));maker.Interact(inv);
            Check(visual.transform.Find("Mok Pizza Stages/ShreddedCheese").gameObject.activeSelf,"Unbaked cheese uses individual shreds",log);Capture(pizza.transform,"raw-cheese");
            var recipe=new PizzaOrder {toppings=new List<string>()};
            var expected=new List<string>(pizza.currentToppings);
            maker.Interact(inv);Check(inv.HeldPizza==pizza.gameObject,"Workbench pickup retains pizza",log);
            Check(!pizza.IsPlaced&&pizza.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Held pizza does not block interaction rays",log);
            Check(Mathf.Approximately((float)Get(oven,"bakingTime"),5),"Existing 5-second baking duration retained",log);
            oven.Interact(inv);
            while(!pizza.IsBaked)yield return null;
            Check(!visual.transform.Find("Mok Pizza Stages/ShreddedCheese").gameObject.activeSelf&&visual.transform.Find("Mok Pizza Stages/MeltedCheese").gameObject.activeSelf&&visual.transform.Find("Mok Pizza Stages/BakedCrust").gameObject.activeSelf,"Oven completion switches crust and melted cheese",log);
            oven.Interact(inv);Check(inv.HeldPizza==pizza.gameObject,"Oven pickup retains baked pizza",log);
            // 같은 조명과 카메라에서 구운 상태를 확인하도록 제작대로 돌아간다.
            maker.Interact(inv);Capture(pizza.transform,"baked-cheese");maker.Interact(inv);
            var station=packing.GetComponent<PizzaBoxStation>();
            packing.Interact(inv);packing.Interact(inv);
            while(station.IsBusy)yield return null;
            Check(station.IsOpen&&inv.HeldPizza==pizza.gameObject,"First interaction opens box without consuming pizza; rapid input ignored",log);
            packing.Interact(inv);packing.Interact(inv);
            while(station.IsBusy)yield return null;
            Check(!inv.HasPizza()&&station.ContainedPizza==pizza,"Second interaction places actual pizza inside",log);
            Check(pizza.currentToppings.SequenceEqual(expected)&&pizza.IsBaked,"Insertion preserves recipe and baked state",log);
            Check(pizza.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Contained pizza cannot intercept box interactions",log);
            Physics.SyncTransforms();
            Check(Physics.Raycast(packing.transform.position+Vector3.up,Vector3.down,out var hit,2,1<<packing.gameObject.layer)&&hit.collider.GetComponentInParent<PizzaPacking>()==packing,"Open box remains reachable by interaction ray",log);
            Capture(packing.transform,"open-box",.8f);
            packing.Interact(inv);packing.Interact(inv);
            while(station.IsBusy)yield return null;
            yield return null;
            Check(!station.IsOpen&&station.ContainedPizza==null,"Third interaction closes box and completes packaging",log);
            var packages=(List<PizzaData>)Get(pickup,"packagedDataList");
            Check(packages.Count==1&&packages[0].isBaked&&packages[0].toppings.SequenceEqual(expected),"Exactly one package with unchanged pizza data",log);
            Check(packages[0].MatchesOrder(recipe),"Packed cheese pizza still matches delivery recipe",log);
            Capture(pickup.transform,"closed-box",.8f);
            pickup.Interact(inv);
            Check(inv.GetHeldPackagedPizzas().Count==1,"Existing pickup transfers packaged pizza to inventory",log);
            Check(((List<GameObject>)Get(pickup,"visualBoxes")).Count==0,"Pickup removes only completed box visuals",log);
            inv.ClearPackagedPizzas();
            // 빈 상자 조작과 두 번째 포장 후에도 정상 작동하는지 확인한다.
            packing.Interact(inv);while(station.IsBusy)yield return null;
            packing.Interact(inv);while(station.IsBusy)yield return null;
            Check(!station.IsOpen&&station.ContainedPizza==null,"Empty box can reopen and close for reuse",log);
            var dataPepperoni=Find(ToppingType.Pepperoni);
            inv.AddItem(Find(ToppingType.Dough));maker.Interact(inv);pizza=(Pizza)Get(maker,"currentPizza");
            inv.AddItem(dataPepperoni);maker.Interact(inv);
            Check(pizza.currentToppings.Contains(dataPepperoni.toppingName)&&pizza.transform.childCount>1,"Other topping prefabs still spawn and retain recipe data",log);
            log("PASS: All assets use non-emissive URP Lit materials");
            foreach(var r in pizza.transform.Find("Mok Pizza Stages").GetComponentsInChildren<Renderer>(true))
                foreach(var m in r.sharedMaterials)Check(m.shader.name=="Universal Render Pipeline/Lit"&&!m.IsKeywordEnabled("_EMISSION"),"Lit material: "+m.name, _=>{});
        }
    }
}
#endif

