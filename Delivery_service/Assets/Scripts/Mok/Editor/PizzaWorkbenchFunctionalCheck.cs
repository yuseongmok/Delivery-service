#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Mok.Editor
{
    // Opt-in check in an isolated, unsaved scene; never starts the city managers.
    [InitializeOnLoad]
    public static class PizzaWorkbenchFunctionalCheck
    {
        const string Key="Mok.Workbench.Check";
        static readonly string Report=Path.GetFullPath("Temp/PizzaWorkbench-functional-check.txt");
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static PizzaMaker maker;
        static ToppingInventory inventory;
        static PlayerInteraction interaction;
        static Camera camera;
        static string log;
        static PizzaWorkbenchFunctionalCheck()
        {
            EditorApplication.playModeStateChanged+=Changed;
        }
        [MenuItem("Tools/Delivery Service/Mok/Verify Pizza Workbench")]
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            if(SceneManager.sceneCount!=1||SceneManager.GetActiveScene().isDirty)throw new Exception("Save the single city scene first.");
            var source=UnityEngine.Object.FindObjectsByType<PizzaMaker>(FindObjectsSortMode.None).Single();
            SessionState.SetString(Key+".scene",source.gameObject.scene.path);
            var clone=UnityEngine.Object.Instantiate(source.gameObject);
            clone.name="WorkbenchUnderTest";
            var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(clone,test);
            var original=source.gameObject.scene;
            SceneManager.SetActiveScene(test);
            EditorSceneManager.CloseScene(original,true);
            var player=new GameObject("WorkbenchTestPlayer");
            var inv=player.AddComponent<ToppingInventory>();
            var hold=new GameObject("HoldPoint").transform;hold.SetParent(player.transform);hold.position=clone.transform.position+Vector3.up*4;
            Set(inv,"holdPoint",hold);
            var cam=new GameObject("WorkbenchTestCamera").AddComponent<Camera>();
            var action=player.AddComponent<PlayerInteraction>();
            Set(action,"playerCamera",cam);Set(action,"interactLayer",(LayerMask)(1<<clone.layer));
            action.enabled=false;
            SessionState.SetBool(Key,true);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;
            if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;
        }
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
        static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
        static void Check(bool value,string message){if(!value)throw new Exception(message);log+="PASS: "+message+"\n";}
        static void ClickTable()
        {
            camera.transform.position=maker.transform.TransformPoint(new Vector3(.52f,1.8f,.39f));
            camera.transform.rotation=Quaternion.LookRotation(-maker.transform.up,maker.transform.forward);
            Physics.SyncTransforms();
            Check(Physics.Raycast(camera.transform.position,camera.transform.forward,out var hit,3f,1<<maker.gameObject.layer)&&hit.collider.GetComponentInParent<PizzaMaker>()==maker,"ray hits replacement table within 3 m");
            typeof(PlayerInteraction).GetMethod("TryInteract",Flags).Invoke(interaction,null);
        }
        static void Run()
        {
            log="Real Play Mode check in isolated scene, using existing PlayerInteraction/PizzaMaker/Pizza/Inventory and project ingredient assets.\n";
            try
            {
                maker=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();
                inventory=UnityEngine.Object.FindFirstObjectByType<ToppingInventory>();
                interaction=UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
                camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
                var point=Get<Transform>(maker,"doughPlacePoint");
                Check(point.parent==maker.transform&&Vector3.Distance(point.localPosition,new Vector3(0,.865f,0))<.0001f,"original PizzaPoint is preserved");
                Check(maker.GetComponent<MeshRenderer>().sharedMaterials.All(m=>m.shader.name=="Universal Render Pipeline/Lit"),"all replacement materials respond to lighting");
                var data=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d.toppingPrefab!=null).ToArray();
                var dough=data.First(d=>d.toppingType==ToppingType.Dough);
                inventory.AddItem(dough);ClickTable();
                var pizza=Get<Pizza>(maker,"currentPizza");
                Check(pizza!=null&&pizza.IsPlaced&&!inventory.HasItem()&&pizza.transform.parent==point&&pizza.transform.localPosition==Vector3.zero,"dough placed at original point and removed from inventory");
                foreach(var topping in data.Where(d=>d.toppingType!=ToppingType.Dough).GroupBy(d=>d.toppingType).Select(g=>g.First()))
                {
                    int before=pizza.GetComponentsInChildren<Transform>().Length;
                    inventory.AddItem(topping);ClickTable();
                    Check(!inventory.HasItem()&&pizza.currentToppings.Contains(topping.toppingName)&&pizza.GetComponentsInChildren<Transform>().Length>before,"topping data and visible prefab added: "+topping.toppingType);
                }
                ClickTable();
                Check(inventory.HeldPizza==pizza.gameObject&&pizza.transform.parent==inventory.HoldPoint&&Get<Pizza>(maker,"currentPizza")==null,"finished pizza picked up by aiming at table edge");
                ClickTable();
                Check(!inventory.HasItem()&&pizza.transform.parent==point&&Get<Pizza>(maker,"currentPizza")==pizza,"held pizza can be placed back");
                log+="NOTE: Empty-handed interaction aimed at the pizza itself calls Pizza.Interact, which only accepts toppings; pickup requires aiming at the table. This is existing behavior.\nRESULT: PASS\n";
            }
            catch(Exception e){log+="RESULT: FAIL\n"+e;Debug.LogException(e);}
            finally{File.WriteAllText(Report,log);Debug.Log(log);EditorApplication.ExitPlaymode();}
        }
        static void Restore()
        {
            SessionState.SetBool(Key,false);
            EditorSceneManager.OpenScene(SessionState.GetString(Key+".scene","Assets/Scenes/JJinCity.unity"),OpenSceneMode.Single);
            var target=UnityEngine.Object.FindFirstObjectByType<PizzaMaker>();
            Selection.activeGameObject=target.gameObject;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}
#endif

