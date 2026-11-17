#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    public static class PrisonKitchenDoorSetup
    {
        public static void Build()
        {
            try
            {
                if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved edits: stop.");
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Prison-before-kitchen-door-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();
                var q=UnityEngine.Object.FindFirstObjectByType<PrisonChickenQuest>();
                if(q.kitchenAccess)throw new Exception("Kitchen access already installed.");
                var root=new GameObject("주방 양문 및 입장 시작 영역");root.transform.SetParent(m.transform,true);root.transform.position=new Vector3(30,0,-20);
                var gate=root.AddComponent<PrisonKitchenAccess>();gate.mission=m;
                gate.leftDoor=Door(root.transform,"주방 왼쪽 문",-1.55f,1);
                gate.rightDoor=Door(root.transform,"주방 오른쪽 문",1.55f,-1);
                gate.obstacle=root.AddComponent<UnityEngine.AI.NavMeshObstacle>();gate.obstacle.carving=true;gate.obstacle.center=new Vector3(0,1.4f,0);gate.obstacle.size=new Vector3(3.1f,2.8f,.3f);
                if(q.kitchenEntranceWall){q.kitchenEntranceWall.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(q.kitchenEntranceWall);}
                q.kitchenAccess=gate;PrefabUtility.RecordPrefabInstancePropertyModifications(q);EditorUtility.SetDirty(q);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/KitchenDoor-build.txt","PASS: saved closed Synty double doors, colliders, inside-only entry bounds; legacy distance auto-start bypassed; backed up PrisonChapter.");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/KitchenDoor-build.txt",e.ToString());if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static Transform Door(Transform root,string name,float x,int direction)
        {
            var hinge=new GameObject(name).transform;hinge.SetParent(root,false);hinge.localPosition=new Vector3(x,0,0);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Door_01.prefab");
            if(!asset)throw new Exception("Door asset missing");
            var leaf=(GameObject)PrefabUtility.InstantiatePrefab(asset,hinge);
            foreach(var col in leaf.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
            var renderers=leaf.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            leaf.transform.localScale=new Vector3(1.53f/b.size.x,2.77f/b.size.y,.14f/b.size.z);
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            leaf.transform.position+=hinge.position+new Vector3(direction*.765f,1.385f,0)-b.center;
            var collider=hinge.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(direction*.765f,1.385f,0);collider.size=new Vector3(1.53f,2.77f,.16f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(leaf.transform);
            return hinge;
        }
        public static void Inspect()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
            var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();
            var q=UnityEngine.Object.FindFirstObjectByType<PrisonChickenQuest>();
            string s="entrance="+m.entrance.transform.position+" rotation="+m.entrance.transform.eulerAngles+" checkpoint="+m.checkpoint.position+" station="+m.station.transform.position+"\n";
            s+="wall="+(q.kitchenEntranceWall?q.kitchenEntranceWall.name+" pos="+q.kitchenEntranceWall.transform.position+" scale="+q.kitchenEntranceWall.transform.lossyScale:"null")+"\n";
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>Vector3.Distance(r.bounds.center,m.entrance.transform.position)<8))s+=r.name+" centre="+r.bounds.center+" size="+r.bounds.size+"\n";
            File.WriteAllText("Logs/KitchenDoor-inspect.txt",s);EditorApplication.Exit(0);
        }
    }
}
#endif
