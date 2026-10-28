#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;
using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class PrologueEscapeUpgrade {
    static string log;
    public static void Apply(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-escape-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
        SceneManager.SetActiveScene(scene);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        var p=d.GetComponent<PrologueEscapePerformance>();if(!p)p=d.gameObject.AddComponent<PrologueEscapePerformance>();p.director=d;p.supportDelay=3;
        p.pursuitCars=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProloguePursuitCar>(true)).ToArray();
        if(!p.driverDoor){
            var hinge=new GameObject("Driver door hinge - first person entry").transform;hinge.SetParent(d.getawayCar,false);hinge.localPosition=new Vector3(-.95f,.65f,.8f);p.driverDoor=hinge;
            var split=typeof(BankPrologueBuilder).GetMethod("SplitPassengerDoor",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var f in d.getawayCar.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.transform==d.getawayCar||m.name.Contains("Glass")).Where(m=>!m.transform.IsChildOf(d.accomplice.performance.passengerDoor)).ToArray())split.Invoke(null,new object[]{f,d.getawayCar,hinge,true});
            // 차체와 같은 재질의 안쪽 패널로 문 뒷면을 막는다.
            var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Driver inner door trim";panel.transform.SetParent(hinge,false);panel.transform.localPosition=new Vector3(.03f,.13f,-.7f);panel.transform.localScale=new Vector3(.05f,.3f,1.25f);
            panel.GetComponent<Renderer>().sharedMaterial=d.getawayCar.GetComponent<Renderer>().sharedMaterials[0];UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        }
        if(!p.partnerGun){
            var source=d.officers.First(o=>o.gun).gun;
            var gun=UnityEngine.Object.Instantiate(source.gameObject,d.getawayCar);gun.name="Mingi support firearm";gun.transform.localScale=source.lossyScale;gun.transform.localPosition=new Vector3(.98f,1.17f,-.3f);gun.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var c in gun.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            p.partnerGun=gun.transform;p.feedback=gun.GetComponentInChildren<PrologueGunFeedback>(true);
            var muzzle=new GameObject("Support muzzle").transform;muzzle.SetParent(gun.transform,false);muzzle.position=gun.transform.position-gun.transform.parent.forward*.45f;p.muzzle=muzzle;
            if(!p.feedback){
                p.feedback=gun.AddComponent<PrologueGunFeedback>();
                var flash=new GameObject("Support flash").AddComponent<Light>();flash.transform.SetParent(muzzle,false);flash.range=4;flash.intensity=2;flash.color=new Color(1,.6f,.2f);p.feedback.flash=flash;
                var flame=GameObject.CreatePrimitive(PrimitiveType.Sphere);flame.name="Support muzzle flame";flame.transform.SetParent(muzzle,false);flame.transform.localScale=Vector3.one*.15f;UnityEngine.Object.DestroyImmediate(flame.GetComponent<Collider>());p.feedback.flame=flame.transform;
                var original=d.officers.FirstOrDefault(o=>o.gunFeedback);if(original)p.feedback.shot=original.gunFeedback.shot;
            }
            gun.SetActive(false);
        }
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Bank city horizon");
        if(!root){
            root=new GameObject("Bank city horizon");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Environments/Custom/SM_Env_Skyline_01.prefab");
            if(!prefab)throw new Exception("City skyline asset missing");
            var horizon=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
            var bounds=new Bounds();bool first=true;foreach(var r in horizon.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            horizon.transform.localScale*=900/Mathf.Max(bounds.size.x,bounds.size.z);
            bounds=new Bounds();first=true;foreach(var r in horizon.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            horizon.transform.position+=new Vector3(110,-10,-65)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var c in horizon.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        }
        // 본편의 남색 하늘과 도시 스카이라인을 사용해 기본 회색 수평선을 없앤다.
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true))){c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.055f,.12f,.22f);c.farClipPlane=Mathf.Max(c.farClipPlane,1500);EditorUtility.SetDirty(c);}
        EditorUtility.SetDirty(p);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Prologue-escape-apply.txt","PASS scene backed up and saved; first-person driver door; Mingi support weapon; city skyline; pursuit cars="+p.pursuitCars.Length);
    }
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Prologue-escape-check.txt",log);}
    static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static void Capture(Camera camera,string name){var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var t=new Texture2D(1280,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes("Logs/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);}}
    public static IEnumerator Verify(){
        log="Prologue escape refinement\n";yield return null;
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();Assert(d!=null,"bank loaded");d.AcceptInput=false;d.StopAllCoroutines();d.BeginRobbery();Set(d.player,"immuneUntil",Time.time+1000);
        var p=d.GetComponent<PrologueEscapePerformance>();Assert(p&&p.driverDoor&&p.partnerGun&&p.pursuitCars.Length>0,"saved scene references");
        foreach(var o in d.officers.Where(o=>o.Active&&o.brain)){o.brain.ResetPlan();o.brain.Tick();Assert(o.brain.CoverIndex<0,"police no longer claim cover");}
        var cam=d.player.view;Assert(PrologueEscapePerformance.VisibleSpawn(cam,cam.transform.position+cam.transform.forward*2-Vector3.up),"visible spawn rejected");Assert(!PrologueEscapePerformance.VisibleSpawn(cam,cam.transform.position-cam.transform.forward*4-Vector3.up),"offscreen spawn allowed");
        foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}d.accomplice.StopAllCoroutines();
        Set(d,"<Current>k__BackingField",BankPrologue.Phase.Escape);
        var cc=d.player.GetComponent<CharacterController>();cc.enabled=false;d.player.transform.position=d.exitPoint.position;cc.enabled=true;
        var agent=d.accomplice.GetComponent<NavMeshAgent>();agent.enabled=false;d.accomplice.transform.position=d.accomplice.performance.passengerStand.position;agent.enabled=true;agent.Warp(d.accomplice.boardingPoint.position);
        yield return null;
        Assert(d.BoardCar(),"normal boarding action accepted");
        float until=Time.time+1.5f;while(Time.time<until)yield return null;Capture(d.filmCamera,"Prologue-first-person-entry");
        Assert(p.Boarding&&Vector3.Distance(d.filmCamera.transform.position,d.getawayCar.position)<4,"boarding camera remains at player doorway");
        d.SetPaused(true);var held=d.filmCamera.transform.position;yield return null;Assert(d.filmCamera.transform.position==held,"boarding pause retains view");d.SetPaused(false);
        until=Time.time+10;while(d.Current!=BankPrologue.Phase.Driving&&Time.time<until)yield return null;
        Assert(d.drive.Driving&&d.accomplice.Boarded,"driver and companion seated before driving");Assert(Quaternion.Angle(p.driverDoor.localRotation,Quaternion.identity)<1,"driver door closed");
        d.drive.ExternalInput=true;d.drive.SetInput(1,0);
        until=Time.time+18;while(!p.PursuitEnded&&Time.time<until){if(p.SupportShots==2){var saved=d.filmCamera.transform.rotation;d.filmCamera.transform.rotation=d.getawayCar.rotation*Quaternion.Euler(0,85,0);Capture(d.filmCamera,"Prologue-partner-support");d.filmCamera.transform.rotation=saved;}yield return null;}
        Assert(p.PursuitEnded&&p.SupportShots>=p.pursuitCars.Length*5,"Mingi gunfire ends every pursuing police car");Assert(p.pursuitCars.All(c=>c.DisabledByPartner&&!c.Pursuing&&!c.siren.isPlaying),"pursuit and sirens stop without disappearing");
        Assert(d.drive.Driving&&d.Current==BankPrologue.Phase.Driving,"support does not interrupt player driving");Capture(d.filmCamera,"Prologue-support-driving");
        Assert(d.filmCamera.clearFlags==CameraClearFlags.SolidColor&&GameObject.Find("Bank city horizon"),"saved city background replaces default horizon");
        until=Time.time+20;while(d.Current==BankPrologue.Phase.Driving&&Time.time<until)yield return null;
        Assert(d.Current==BankPrologue.Phase.CityReveal,"full speed escape reaches city after support gunfire");
        d.drive.SetInput(0,0);
    }
}
}
#endif
