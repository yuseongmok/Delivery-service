#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeliveryService.Mok.Editor
{
    public static class PizzaWorkbenchInstaller
    {
        const string Folder="Assets/Scripts/Mok/Art/PizzaWorkbench/";
        [MenuItem("Tools/Delivery Service/Mok/Apply Pizza Workbench to Open Scene")]
        public static void ApplyToOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before replacing the workbench.");
            var makers=UnityEngine.Object.FindObjectsByType<PizzaMaker>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(makers.Length!=1) throw new InvalidOperationException("Open a scene with exactly one PizzaMaker.");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"PizzaWorkbench.mesh.asset");
            var materials=new[]{"WarmStone","PaintedSteel","BrushedSteel","Rubber","OxideRed"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Folder+n+".mat")).ToArray();
            if(mesh==null||materials.Any(m=>m==null)) throw new InvalidOperationException("Workbench assets are missing.");
            var maker=makers[0];var filter=maker.GetComponent<MeshFilter>();var renderer=maker.GetComponent<MeshRenderer>();var collider=maker.GetComponent<BoxCollider>();
            if(filter==null||renderer==null||collider==null)throw new InvalidOperationException("Expected original workbench MeshFilter, MeshRenderer and BoxCollider.");
            Undo.RecordObjects(new UnityEngine.Object[]{filter,renderer,collider},"Replace pizza workbench visual");
            filter.sharedMesh=mesh;renderer.sharedMaterials=materials;renderer.receiveShadows=true;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            collider.center=new Vector3(0,.4155f,0);collider.size=new Vector3(1.26f,.831f,1.06f);
            foreach(var item in new UnityEngine.Object[]{filter,renderer,collider}) {PrefabUtility.RecordPrefabInstancePropertyModifications(item);EditorUtility.SetDirty(item);}
            EditorSceneManager.MarkSceneDirty(maker.gameObject.scene);Selection.activeGameObject=maker.gameObject;
            Debug.Log("Workbench visual replaced. PizzaMaker, PizzaPoint, layer and transform preserved. Undo is available; save the scene when ready.");
        }
        public static void VerifyBatch()
        {
            try {
                EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
                var maker=UnityEngine.Object.FindObjectsByType<PizzaMaker>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
                var point=(Transform)new SerializedObject(maker).FindProperty("doughPlacePoint").objectReferenceValue;
                var filter=maker.GetComponent<MeshFilter>();var renderer=maker.GetComponent<MeshRenderer>();var box=maker.GetComponent<BoxCollider>();
                if(filter.sharedMesh!=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"PizzaWorkbench.mesh.asset"))throw new Exception("Replacement mesh missing");
                if(renderer.sharedMaterials.Length!=5||renderer.sharedMaterials.Any(m=>m.shader.name!="Universal Render Pipeline/Lit"))throw new Exception("Lit materials missing");
                if(point==null||point.parent!=maker.transform||Vector3.Distance(point.localPosition,new Vector3(0,.865f,0))>.0001f)throw new Exception("PizzaPoint changed");
                if(maker.gameObject.layer!=6||box.size!=new Vector3(1.26f,.831f,1.06f))throw new Exception("Interaction geometry mismatch");
                Physics.SyncTransforms();
                Vector3 origin=maker.transform.TransformPoint(new Vector3(0,1.3f,0));
                if(!Physics.Raycast(origin,-maker.transform.up,out var hit,3f,1<<6)||hit.collider.GetComponentInParent<PizzaMaker>()!=maker)throw new Exception("Interaction ray cannot hit workbench");
                File.WriteAllText(Environment.GetEnvironmentVariable("MOK_WORKBENCH_OUTPUT")+"/verification.txt","PASS: saved scene loads; replacement mesh and five Lit materials; original PizzaMaker/PizzaPoint and layer; collider raycast. Scene not saved, no Play mode/save-data mutation.");
                EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
