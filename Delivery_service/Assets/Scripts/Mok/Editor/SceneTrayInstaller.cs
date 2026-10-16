#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class SceneTrayInstaller
    {
        static SceneTrayInstaller() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!File.Exists("Temp/Mok-install-scene-trays")) return;
            File.Delete("Temp/Mok-install-scene-trays");
            try { Install(); } catch(Exception e) { File.WriteAllText("Temp/Mok-install-scene-trays.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Tools/Delivery Service/Mok/Apply Ingredient Models to Scene")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/JJinCity.unity") throw new Exception("Open JJinCity first.");
            string backup="Temp/JJinCity-before-scene-trays-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
            if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Could not back up current scene");
            int count=0; string log="Scene backup: "+backup+"\n";
            foreach(var topping in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Topping>(true)))
            {
                if(topping.GetComponentInParent<Pizza>()!=null) continue;
                var serialized=new SerializedObject(topping);
                var data=serialized.FindProperty("toppingData").objectReferenceValue as PizzaToppingData;
                if(data==null) continue;
                var existing=topping.GetComponentsInChildren<Renderer>(true);
                Undo.RecordObjects(existing,"Hide old ingredient models");
                var visual=topping.GetComponent<ToppingTrayVisual>();
                if(visual==null) visual=Undo.AddComponent<ToppingTrayVisual>(topping.gameObject);
                Undo.RecordObject(visual,"Set persistent tray model");
                visual.Initialize(data);
                if(visual.Model==null) throw new Exception("No model for "+topping.name);
                foreach(var r in existing) { EditorUtility.SetDirty(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
                EditorUtility.SetDirty(visual);PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                foreach(var t in visual.Model.GetComponentsInChildren<Transform>(true))
                {PrefabUtility.RecordPrefabInstancePropertyModifications(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
                if(!PrefabUtility.IsPartOfPrefabInstance(visual.Model)) throw new Exception("Model must keep a prefab link");
                count++;log+="PASS: Persistent prefab "+data.toppingType+" at "+visual.Model.position+"\n";
            }
            if(count!=8) throw new Exception("Expected 8 supply stations, found "+count);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save scene");
            log+="PASS: JJinCity saved with 8 linked ingredient models.\n";
            File.WriteAllText("Temp/Mok-install-scene-trays.txt",log);Debug.Log(log);
        }
    }
}
#endif
