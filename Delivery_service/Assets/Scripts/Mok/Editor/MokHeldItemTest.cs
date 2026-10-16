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

namespace DeliveryService.Mok.Editor
{
    public static class MokHeldItemTest
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool value,string message,Action<string> log){if(!value)throw new Exception(message);log("PASS: "+message);}
        static Pizza Current(PizzaMaker maker)=>(Pizza)typeof(PizzaMaker).GetField("currentPizza",Flags).GetValue(maker);
        static void RayInteract(PlayerInteraction interaction, Vector3 point)
        {
            var camera=interaction.InteractionCamera;
            camera.transform.position=point+Vector3.up*1.1f;
            camera.transform.LookAt(point);
            Physics.SyncTransforms();
            typeof(PlayerInteraction).GetMethod("TryInteract",Flags).Invoke(interaction,null);
        }
        static void Capture(Camera camera,string name)
        {
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var target=new RenderTexture(1200,900,24);target.Create();
            try{
                camera.targetTexture=target;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
                File.WriteAllBytes("Temp/Mok-held-"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
            }finally{camera.targetTexture=previousTarget;RenderTexture.active=previousActive;target.Release();UnityEngine.Object.Destroy(target);}
        }
        static void Visible(CarriedItemVisual visual,Action<string> log,string name)
        {
            Check(visual.Display!=null,"Held display exists: "+name,log);
            var renderers=visual.Display.GetComponentsInChildren<MeshRenderer>();
            Check(renderers.Length>0&&renderers.All(r=>r.enabled),"Visible meshes: "+name,log);
            Check(visual.Display.GetComponentsInChildren<Collider>().Length==0&&visual.Display.GetComponentsInChildren<MonoBehaviour>().Length==0,"Preview has no gameplay scripts or colliders: "+name,log);
            Check(renderers.Any(r=>{var p=visual.ViewCamera.WorldToViewportPoint(r.bounds.center);return p.z>visual.ViewCamera.nearClipPlane&&p.x>.02f&&p.x<.98f&&p.y>.02f&&p.y<.65f;}),"Held model is inside lower camera view: "+name,log);
            Check(visual.OverlayCamera!=null&&visual.ViewCamera.GetUniversalAdditionalCameraData().cameraStack.Contains(visual.OverlayCamera)&&visual.OverlayCamera.GetUniversalAdditionalCameraData().clearDepth,"Held items render over nearby geometry",log);
        }
        public static IEnumerator RunCore(Action<string> log)
        {
            var inventory=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();
            var visual=inventory.GetComponent<CarriedItemVisual>();
            var interaction=inventory.GetComponent<PlayerInteraction>();var camera=interaction.InteractionCamera;
            var maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();
            var data=Resources.Load<PizzaIngredientLibrary>("MokPizzaArt/Ingredients").ingredients;
            PizzaToppingData Find(ToppingType type)=>data.First(d=>d.toppingType==type);
            var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.timeMultiplier=0;cycle.currentTime=12;
            typeof(DayNightCycle).GetMethod("UpdateLighting",Flags).Invoke(cycle,null);
            Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
            foreach(var ingredient in data)
            {
                inventory.AddItem(ingredient);Visible(visual,log,ingredient.toppingName);
                if(ingredient.toppingType==ToppingType.Cheese||ingredient.toppingType==ToppingType.Sauce)Capture(camera,ingredient.toppingType.ToString());
                inventory.RemoveItem();Check(visual.Display==null,"Removing ingredient clears preview",log);
            }
            inventory.AddItem(Find(ToppingType.Dough));maker.Interact(inventory);var pizza=Current(maker);
            foreach(var type in new[]{ToppingType.Sauce,ToppingType.Cheese,ToppingType.Pepperoni})
            {
                inventory.AddItem(Find(type));RayInteract(interaction,pizza.transform.position);
                Check(!inventory.HasItem()&&pizza.currentToppings.Contains(Find(type).toppingName),"Ray on pizza still adds topping: "+type,log);
            }
            var expected=pizza.currentToppings.ToArray();var scale=pizza.transform.localScale;
            foreach(var offset in new[]{Vector3.zero,new Vector3(.3f,0,0),new Vector3(-.3f,0,0),new Vector3(0,0,.3f),new Vector3(0,0,-.3f),new Vector3(.24f,0,.24f),new Vector3(-.24f,0,.24f),new Vector3(.24f,0,-.24f),new Vector3(-.24f,0,-.24f)})
            {
                RayInteract(interaction,pizza.transform.position+offset);
                Check(inventory.HeldPizza==pizza.gameObject&&Current(maker)==null,"Pizza picked up by ray at "+offset,log);
                Visible(visual,log,"pizza");
                Check(pizza.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),"Original held pizza hidden; preview prevents duplicate rendering",log);
                maker.Interact(inventory);
                Check(Current(maker)==pizza&&pizza.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Workbench return restores real pizza visibility",log);
                Check(pizza.transform.localScale==scale&&pizza.currentToppings.SequenceEqual(expected),"Display preserves actual scale and recipe",log);
            }
            RayInteract(interaction,pizza.transform.position);
            camera.transform.position=cameraPosition;camera.transform.rotation=cameraRotation;
            Capture(camera,"pizza");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position=camera.transform.position+camera.transform.forward*.4f;blocker.transform.rotation=camera.transform.rotation;blocker.transform.localScale=new Vector3(2,2,.02f);
            blocker.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("MokPizzaArt/Kraft");
            Capture(camera,"near-wall");blocker.SetActive(false);UnityEngine.Object.Destroy(blocker);
            pizza.SetBaked(true);
            var package=pizza.ToData();inventory.RemovePizza();UnityEngine.Object.Destroy(pizza.gameObject);
            Check(visual.Display==null,"Removing pizza clears carried preview immediately",log);
            inventory.AddPackagedDataStack(new List<PizzaData>{package,new PizzaData(package),new PizzaData(package)});
            Visible(visual,log,"three boxes");Capture(camera,"boxes");
            var bike=UnityEngine.Object.FindFirstObjectByType<BikePizzaStorage>();
            bike.Interact(inventory);while(bike.GetComponent<BikeCargoVisual>()!=null&&bike.GetComponent<BikeCargoVisual>().IsBusy)yield return null;Check(!inventory.HasItem()&&visual.Display==null,"Storing packages on bike clears hand display",log);
            bike.Interact(inventory);while(bike.GetComponent<BikeCargoVisual>()!=null&&bike.GetComponent<BikeCargoVisual>().IsBusy)yield return null;Check(inventory.GetHeldPackagedPizzas().Count==3,"Taking packages from bike restores the stack",log);Visible(visual,log,"returned boxes");
            inventory.ClearPackagedPizzas();Check(visual.Display==null,"Clearing packages clears display",log);
            var regression=MokPizzaArtTest.RunCore(log);while(regression.MoveNext())yield return regression.Current;
        }
    }
}
#endif
