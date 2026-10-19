#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class EastTrafficSetup {
    static EastTrafficSetup(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-east-traffic-setup"))return;File.Delete("Temp/Mok-east-traffic-setup");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-east-traffic-setup.txt",e.ToString());Debug.LogException(e);}}
    [MenuItem("Tools/Delivery Service/Mok/Apply East Traffic")]
    static void Run(){
        var previous=SceneManager.GetActiveScene();
        for(int i=0;i<SceneManager.sceneCount;i++){var open=SceneManager.GetSceneAt(i);if(open.isDirty)EditorSceneManager.SaveScene(open,"Temp/"+open.name+"-before-east-traffic.unity",true);}
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try {
            EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-east-traffic.unity",true);
            var root=scene.GetRootGameObjects().First(g=>g.name=="Mok East City").transform;
            if(root.Find("Reckless traffic routes")!=null)throw new Exception("Already installed; edit existing routes instead.");
            var cars=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarMov>()).Where(c=>c.transform.position.x>195).OrderBy(c=>c.name).ToArray();
            if(cars.Length!=16)throw new Exception("Expected 16 east cars, got "+cars.Length);
            var routes=new GameObject("Reckless traffic routes").transform;routes.SetParent(root,false);
            // 각 사각 블록 주변 도로만 사용하며 짧은 역주행과 교차로 차선 변경을 섞는다.
            float[,] blocks={{210,290,-110,-5},{290,370,-110,100},{370,440,-5,100},{210,370,-5,100},{370,440,-110,-5}};
            for(int i=0;i<5;i++){
                float l=blocks[i,0],r=blocks[i,1],b=blocks[i,2],t=blocks[i,3];
                Vector3 V(float x,float z)=>new Vector3(x,.05f,z);
                var points=new[]{V(l+3,b+12),V(l+3,t-12),V(l+3,t-3),V(l+12,t-3),V(r-12,t+3),V(r-3,t+3),V(r-3,t-12),V(r-3,b+12),V(r-3,b+3),V(r-12,b+3),V(l+12,b-3),V(l+3,b-3)};
                if(i%2==1)Array.Reverse(points);
                var list=new List<Vector3>();
                for(int p=0;p<points.Length;p++){var a=points[p];var end=points[(p+1)%points.Length];int steps=Mathf.CeilToInt(Vector3.Distance(a,end)/25);for(int j=0;j<steps;j++)list.Add(Vector3.Lerp(a,end,j/(float)steps));}
                var nodes=list.Select((pos,n)=>{var go=new GameObject("Reckless "+(i+1)+" / "+n);go.transform.SetParent(routes,false);go.transform.position=pos;return go.AddComponent<WayPoint>();}).ToArray();
                for(int n=0;n<nodes.Length;n++)nodes[n].nextWaypoints.Add(nodes[(n+1)%nodes.Length]);
                var car=cars[i*3];int start=(i*7)%nodes.Length;var next=nodes[(start+1)%nodes.Length];
                car.transform.SetPositionAndRotation(nodes[start].transform.position,Quaternion.LookRotation(next.transform.position-nodes[start].transform.position));
                var so=new SerializedObject(car);so.FindProperty("currentTarget").objectReferenceValue=next;so.FindProperty("rotationSpeed").floatValue=10;so.ApplyModifiedPropertiesWithoutUndo();
                var driver=car.gameObject.AddComponent<EastRecklessDriver>();driver.cruiseSpeed=20+i;driver.turnSpeed=12;
                PrefabUtility.RecordPrefabInstancePropertyModifications(car);PrefabUtility.RecordPrefabInstancePropertyModifications(car.transform);
            }
            root.Find("East citizens").gameObject.AddComponent<EastJaywalking>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/Mok-east-traffic-setup.txt","PASS: 5/16 reckless cars; speeds 20..24, corner 12; 18 jaywalkers; saved JJinCity; original fleet 50 preserved.");
        }finally{SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
}
#endif
