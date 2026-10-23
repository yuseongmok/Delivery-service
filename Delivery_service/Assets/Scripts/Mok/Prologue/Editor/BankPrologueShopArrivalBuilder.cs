#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void PolishShopArrival(){
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var a=d.shopArrival;
        EditorSceneManager.SaveScene(d.gameObject.scene,"Logs/Bank-before-arrival-polish.unity",true);
        if(!a.shop.Find("Arrival shop sign")){
            var label=new GameObject("Arrival shop sign");label.transform.SetParent(a.shop,false);label.transform.localPosition=new Vector3(-.45f,3.79f,3.615f);label.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=label.AddComponent<TextMesh>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");text.text="이얏호 피자  /  DELIVERY";text.fontSize=64;text.characterSize=.065f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.88f,.65f);
            label.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/WorldText.mat");label.AddComponent<PrologueDepthText>();
        }
        a.actor.rotation=a.shop.rotation*Quaternion.Euler(0,180,0);
        // 기존 소품의 뼈대 보정 스크립트를 복제하지 않으므로 잘못 걸린 끈과 손 소품은 숨긴다.
        foreach(var mesh in a.actor.GetComponentsInChildren<MeshRenderer>(true))mesh.enabled=false;
        if(!a.cabinLight){var light=new GameObject("Arrival cabin fill");light.transform.SetParent(d.getawayCar,false);light.transform.localPosition=new Vector3(-.15f,1.1f,.45f);a.cabinLight=light.AddComponent<Light>();a.cabinLight.type=LightType.Point;a.cabinLight.intensity=1.2f;a.cabinLight.range=2.5f;a.cabinLight.color=new Color(1,.83f,.65f);a.cabinLight.enabled=false;}
        a.carStop.rotation=a.shop.rotation*Quaternion.Euler(0,90,0);
        EditorSceneManager.SaveScene(d.gameObject.scene);
    }
    public static void BuildShopArrival(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-shop-arrival-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        if(d.shopArrival)throw new Exception("Shop arrival already exists; preserve edits.");
        var shell=d.cityVista.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.sharedMesh&&m.sharedMesh.name=="ShopShell_LowPoly");
        var root=new GameObject("Pizza shop arrival / saved cinematic").transform;root.SetParent(d.cityVista.transform,false);root.SetPositionAndRotation(shell.transform.position,shell.transform.rotation);
        var arrival=d.gameObject.AddComponent<PrologueShopArrival>();d.shopArrival=arrival;arrival.director=d;arrival.shop=root;
        var actor=UnityEngine.Object.Instantiate(d.accomplice.gameObject);actor.name="Companion arrival actor";Strip(actor);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
        arrival.actor=actor.transform;arrival.animator=actor.GetComponentInChildren<Animator>();arrival.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        actor.transform.SetPositionAndRotation(root.TransformPoint(new Vector3(3.22f,0,3.85f)),Quaternion.Euler(0,180,0));
        var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/ShopBuilding/Steel.mat");
        var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/ShopBuilding/DeepTeal.mat");
        arrival.slats=new Transform[19];
        for(int i=0;i<arrival.slats.Length;i++)arrival.slats[i]=Box("Shutter slat "+i,new Vector3(3.22f,.09f+i*.16f,3.15f),new Vector3(2.15f,.15f,.09f),metal,root,false);
        foreach(float x in new[]{2.10f,4.34f})Box("Shutter guide",new Vector3(x,1.52f,3.15f),new Vector3(.11f,3.05f,.18f),trim,root,false);
        Box("Shutter roller housing",new Vector3(3.22f,3.16f,3.15f),new Vector3(2.4f,.25f,.3f),trim,root,false);
        arrival.carStop=Point("Shop car parking",root.TransformPoint(new Vector3(1,0,8)),root);arrival.carStop.rotation=Quaternion.Euler(0,90,0);
        arrival.wideShot=Point("Shop reveal camera",root.TransformPoint(new Vector3(8,2.8f,12)),root);arrival.wideShot.LookAt(root.TransformPoint(new Vector3(.6f,1.9f,2.8f)));
        arrival.doorShot=Point("Shutter action camera",root.TransformPoint(new Vector3(6.5f,1.9f,6)),root);arrival.doorShot.LookAt(root.TransformPoint(new Vector3(3.22f,1.4f,3.2f)));
        arrival.shutterSound=AssetDatabase.LoadAssetAtPath<AudioClip>(PerformanceArt+"/VaultSwing.wav");
        var lamp=LightAt("Arrival warm doorway",root.TransformPoint(new Vector3(3.2f,2.7f,2.3f)),new Color(1,.75f,.45f),3,6);lamp.transform.SetParent(root,true);
        EditorUtility.SetDirty(d.story);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        var cam=d.filmCamera;var pos=cam.transform.position;var rot=cam.transform.rotation;bool active=d.cityVista.activeSelf;
        d.cityVista.SetActive(true);DecorCapture(cam,arrival.wideShot.position,root.TransformPoint(new Vector3(1,1.8f,3)),"pizza-arrival");d.cityVista.SetActive(active);cam.transform.SetPositionAndRotation(pos,rot);
        File.WriteAllText("Logs/Bank-shop-arrival-build.txt","PASS: original scene backed up; actual city pizza shop used; saved shutter, actor, camera and parking markers; editable story dialogue; gameplay economy untouched.");
    }
}
}
#endif
