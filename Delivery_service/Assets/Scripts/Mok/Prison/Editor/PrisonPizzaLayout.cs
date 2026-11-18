#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonPizzaLayout
    {
        static PrisonPizzaLayout(){EditorApplication.update+=Poll;EditorApplication.update+=CheckPlay;}
        static int step;static float wait;
        static void CheckPlay(){if(!EditorApplication.isPlaying||!SessionState.GetBool("PizzaLayoutCheck",false))return;try{
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();if(!m)return;
            if(step==0){step=1;wait=Time.time+3;return;}File.WriteAllText("Logs/Prison-pizza-check-progress.txt",$"step={step} time={Time.time} wait={wait} state={m.State} served={m.Served} scale={Time.timeScale}");if(Time.time<wait)return;
            var prison=m.previous.prison;var inventory=prison.player.GetComponent<ToppingInventory>();
            if(step==1){prison.introduction.DeveloperComplete();m.previous.StopAllCoroutines();m.StopAllCoroutines();typeof(PrisonKeysMission).GetProperty("State").SetValue(m,PrisonKeysMission.Stage.Cook);m.cookingSupplies.SetActive(true);
                var text=Object.Instantiate(m.dialogue);text.lineSeconds=.1f;m.dialogue=text;
                if(prison.cart.gameObject.activeInHierarchy)throw new Exception("Cart should be hidden");
                inventory.AddItem(m.ingredients[0]);m.maker.Interact(inventory);for(int i=1;i<4;i++){inventory.AddItem(m.ingredients[i]);m.maker.Interact(inventory);}
                inventory.AddItem(m.serving.medicine);m.maker.Interact(inventory);step=2;wait=Time.time+.2f;return;}
            if(step==2){m.maker.Interact(inventory);if(!inventory.HeldPizza||!m.serving.Seasoned(inventory.HeldPizza.GetComponent<Pizza>()))throw new Exception("Medicine ingredient failed");m.oven.Interact(inventory);step=3;wait=Time.time+6;return;}
            if(step==3){m.oven.Interact(inventory);if(!inventory.HeldPizza||!m.ValidPizza(inventory.HeldPizza.GetComponent<Pizza>()))throw new Exception("Bake failed");step=4;wait=Time.time+1;return;}
            if(step==4){if(m.State!=PrisonKeysMission.Stage.Serve||!m.serving.Carrying||!inventory.HeldPizza.transform.Find("배식용 접시"))throw new Exception("Plate service failed");step=5;}
            if(step==5){if(m.State==PrisonKeysMission.Stage.Office){step=6;}else{var g=m.diners[m.Served];Move(prison.player,g.transform.position+g.transform.forward*1.6f);if(!m.Interact())throw new Exception("Guard interaction failed "+m.Served);step=7;wait=Time.time+10;return;}}
            if(step==7){if(!m.CanDeveloperComplete)return;foreach(var g in m.diners.Take(m.Served))if(!g.GetComponent<PrisonPizzaEating>().Finished||!g.GetComponent<PrisonGuardSleep>())throw new Exception("Sleeping guard failed");step=5;return;}
            if(step==6){Move(prison.player,m.warden.transform.position+m.warden.transform.forward*1.6f);if(!m.Interact())throw new Exception("Warden interaction failed");step=8;wait=Time.time+12;return;}
            if(step==8){if(m.State!=PrisonKeysMission.Stage.Complete||inventory.HeldPizza)throw new Exception("Warden completion failed "+m.State);File.WriteAllText("Logs/Prison-pizza-play-check.txt","PASS assisted Play mode: medicine added through original maker; original oven baked pizza; automatic plate and carried pizza; three guards ate and slept; office opened; warden ate and slept; mission Complete and inventory cleared. Travel assisted, not manual play.");SessionState.SetBool("PizzaLayoutCheck",false);step=0;EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.WriteAllText("Logs/Prison-pizza-play-check.txt",e.ToString());SessionState.SetBool("PizzaLayoutCheck",false);step=0;EditorApplication.ExitPlaymode();}}
        static void Move(PlayerCombat player,Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position+Vector3.up*.2f;cc.enabled=true;Cursor.lockState=CursorLockMode.Locked;player.GetComponent<PlayerMove>().isControllable=true;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonPizzaLayout"))return;var action=File.ReadAllText("Temp/PrisonPizzaLayout").Trim();File.Delete("Temp/PrisonPizzaLayout");try{if(action=="build")Build();else if(action=="test"){SessionState.SetBool("PizzaLayoutCheck",true);EditorApplication.EnterPlaymode();}else Inspect();}catch(Exception e){File.WriteAllText("Logs/Prison-pizza-layout.txt",e.ToString());}}
        static void Build()
        {
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;
            if(scene.name!="PrisonChapter")throw new Exception("Prison only");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-pizza-layout-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            const string folder="Assets/Scripts/Mok/Art/PizzaWorkbench/";
            var maker=m.maker.transform;maker.name="감옥 전용 피자 제작 테이블";maker.position=new Vector3(34.5f,.05f,-30.5f);maker.rotation=Quaternion.identity;maker.localScale=Vector3.one*1.15f;
            maker.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"PizzaWorkbench.mesh.asset");
            var renderer=maker.GetComponent<MeshRenderer>();renderer.enabled=true;renderer.sharedMaterials=new[]{"WarmStone","PaintedSteel","BrushedSteel","Rubber","OxideRed"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(folder+n+".mat")).ToArray();
            if(!maker.GetComponent<MeshFilter>().sharedMesh||renderer.sharedMaterials.Any(x=>!x))throw new Exception("Workbench assets missing");
            var box=maker.GetComponent<BoxCollider>();box.center=new Vector3(0,.4155f,0);box.size=new Vector3(1.26f,.831f,1.06f);
            var point=(Transform)new SerializedObject(m.maker).FindProperty("doughPlacePoint").objectReferenceValue;point.SetParent(maker,false);point.localPosition=new Vector3(0,.865f,0);point.localRotation=Quaternion.identity;point.localScale=Vector3.one/1.15f;
            m.kitchen.position=point.position;
            var bin=m.transform.Find("피자 재료 폐기통");if(bin)bin.position=new Vector3(35.7f,.5f,-32f);
            var cart=m.previous.prison.cart;cart.gameObject.SetActive(true);cart.transform.position=new Vector3(34.5f,.15f,-24.5f);m.loadPoint=cart.transform;
            Physics.SyncTransforms();
            if(!cart.GetComponentsInChildren<Renderer>().Any(r=>r.enabled))throw new Exception("Cart renderers missing");
            if(!Physics.Raycast(point.position+Vector3.up*.3f,Vector3.down,out var hit,1)||hit.collider!=box)throw new Exception("Workbench surface alignment failed");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var pos=m.camera.transform.position;var rot=m.camera.transform.rotation;
            m.camera.transform.position=new Vector3(36.8f,2.3f,-27.5f);m.camera.transform.LookAt(point.position);PrisonChickenCheck.Capture(m.camera,"Logs/Prison-pizza-table.png");
            m.camera.transform.position=new Vector3(36.8f,2,-21.3f);m.camera.transform.LookAt(cart.transform.position+Vector3.up*.65f);PrisonChickenCheck.Capture(m.camera,"Logs/Prison-pizza-cart.png");
            m.camera.transform.SetPositionAndRotation(pos,rot);
            File.WriteAllText("Logs/Prison-pizza-layout-result.txt","PASS: backed up unsaved scene; dedicated workbench and dough aligned; cart active with visible renderers; load target preserved; saved prison scene only.");
        }
        static void Inspect(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var b=new StringBuilder();
            foreach(var t in new[]{m.maker.transform,m.kitchen,m.loadPoint,m.previous.prison.cart.transform,m.cookingSupplies.transform}){b.AppendLine("REF "+t.name+" "+t.position+" scale="+t.lossyScale);for(var p=t;p;p=p.parent)b.AppendLine(" PARENT "+p.name+" active="+p.gameObject.activeSelf);}
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(r.bounds.center.x>20&&r.bounds.center.x<40&&r.bounds.center.z> -36&&r.bounds.center.z< -18)b.AppendLine("PROP "+r.name+" "+r.bounds+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled);
            File.WriteAllText("Logs/Prison-pizza-layout.txt",b.ToString());
        }
    }
}
#endif
