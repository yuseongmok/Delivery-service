#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]
public static class BankPrologueCheck {
    const string Key="Mok.BankPrologue.Check";
    static IEnumerator routine;
    static System.Collections.Generic.Stack<IEnumerator> routines;
    static string log;
    static double start;
    static int frame;
    [Serializable] public class SavedValue { public string key; public int value; public bool exists; }
    [Serializable] public class SavedPrefs { public SavedValue[] values; public string pending; public bool exists; }
    static double nextRequest;
    static BankPrologueCheck() { EditorApplication.playModeStateChanged+=Changed; EditorApplication.update+=Request; }
    // 명시적으로 만든 요청 파일만 처리하며 플레이 중에는 씬을 수정하지 않는다.
    static void Request() {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextRequest)return;
        nextRequest=EditorApplication.timeSinceStartup+1;
        const string path="Temp/BankPrologue-request.txt";
        if(!File.Exists(path))return;
        string command=File.ReadAllText(path).Trim();File.Delete(path);
        try {
            if(command=="test")StartTest();
            if(command=="partner")BankPrologueBuilder.InstallPartner();
            if(command=="expand")BankPrologueBuilder.Expand();
            if(command=="finale")BankPrologueBuilder.UpgradeEscape();
            if(command=="vista-polish")BankPrologueBuilder.PolishFinale();
            if(command=="depth-text")BankPrologueBuilder.FixWorldText();
            if(command=="exterior-preview"){
                var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var p=d.filmCamera.transform.position;var r=d.filmCamera.transform.rotation;
                d.filmCamera.transform.position=new Vector3(-22,11,-78);d.filmCamera.transform.LookAt(new Vector3(0,4,-49));
                Capture(d.filmCamera,"bank-exterior");d.filmCamera.transform.SetPositionAndRotation(p,r);
            }
            if(command=="city-preview"){
                var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var cam=d.filmCamera;var p=cam.transform.position;var r=cam.transform.rotation;
                var carP=d.getawayCar.position;var carR=d.getawayCar.rotation;float far=cam.farClipPlane;bool fog=RenderSettings.fog;
                d.cityVista.SetActive(true);RenderSettings.fog=false;cam.farClipPlane=3000;
                d.getawayCar.SetPositionAndRotation(d.cityCarStart.position,d.cityCarStart.rotation);
                cam.transform.position=d.getawayCar.position-d.getawayCar.forward*10+Vector3.up*5;cam.transform.LookAt(d.getawayCar.position+d.getawayCar.forward*6);
                Capture(cam,"city-preview");cam.transform.SetPositionAndRotation(d.cityWideCamera.position,d.cityWideCamera.rotation);Capture(cam,"panorama-preview");
                d.getawayCar.SetPositionAndRotation(carP,carR);cam.transform.SetPositionAndRotation(p,r);cam.farClipPlane=far;RenderSettings.fog=fog;d.cityVista.SetActive(false);
            }
            if(command=="preview"){
                var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
                var p=d.filmCamera.transform.position;var r=d.filmCamera.transform.rotation;
                d.filmCamera.transform.SetPositionAndRotation(d.shots[3].position,d.shots[3].rotation);
                Capture(d.filmCamera,"street-preview");
                d.filmCamera.transform.SetPositionAndRotation(p,r);
            }
            if(command=="polish"){
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);
                if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
                var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
                BankPrologueBuilder.InstallGuns(d);
                BankPrologueBuilder.DressStreet();
                foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)))
                    if(text.text=="현금")UnityEngine.Object.DestroyImmediate(text.gameObject);
                EditorSceneManager.SaveScene(scene);
                if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.AlignViewToObject(d.shots[0]);
            }
        }catch(Exception e){File.WriteAllText("Temp/BankPrologue-request-error.txt",e.ToString());Debug.LogException(e);}
    }
    public static void BuildAndTest() { if(!File.Exists(BankPrologueBuilder.ScenePath))BankPrologueBuilder.Build(); StartTest(); }
    [MenuItem("Tools/Delivery Service/Mok/Verify Bank Prologue")]
    public static void StartTest() {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open scene edits before verification.");
        var pending=PlayerPrefs.GetString("PendingToppingList","");
        var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName)
            .Concat(pending.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
        var save=new SavedPrefs{pending=pending,exists=PlayerPrefs.HasKey("PendingToppingList"),values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n})
            .Select(k=>new SavedValue{key=k,value=PlayerPrefs.GetInt(k),exists=PlayerPrefs.HasKey(k)}).ToArray()};
        File.WriteAllText("Temp/BankPrologue-prefs.json",JsonUtility.ToJson(save));
        EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath);
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".ok",false);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state) {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode) {
            log="Bank prologue Play Mode verification\n";start=EditorApplication.timeSinceStartup;frame=-1;
            routine=Run();routines=new System.Collections.Generic.Stack<IEnumerator>();routines.Push(routine);Application.runInBackground=true;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode) {
            var save=JsonUtility.FromJson<SavedPrefs>(File.ReadAllText("Temp/BankPrologue-prefs.json"));
            foreach(var v in save.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
            if(save.exists)PlayerPrefs.SetString("PendingToppingList",save.pending);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
            SessionState.SetBool(Key,false);
            if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool(Key+".ok",false)?0:1);
        }
    }
    static void Tick() {
        if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
        try {
            if(EditorApplication.timeSinceStartup-start>180)throw new Exception("Test timed out");
            if(routines.Count>0){
                var current=routines.Peek();
                if(current.MoveNext()){if(current.Current is IEnumerator child)routines.Push(child);return;}
                routines.Pop();if(routines.Count>0)return;
            }
            SessionState.SetBool(Key+".ok",true);
        } catch(Exception e) {log+="FAIL: "+e+"\n";Debug.LogException(e);}
        EditorApplication.update-=Tick;File.WriteAllText("Temp/BankPrologue-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
    static void Set(object obj,string key,object value)=>obj.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static void Capture(Camera cam,string name){
        var target=new RenderTexture(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;
        var canvas=UnityEngine.Object.FindFirstObjectByType<BankPrologue>().objective.canvas;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var distance=canvas.planeDistance;
        try{cam.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.3f;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Temp/BankPrologue-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;cam.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static IEnumerator Run(){
        yield return null;yield return null;
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.autoEnterGame=false;
        if(d.drive){yield return RunFinale(d);yield break;}
        Check(d!=null&&d.cash.Length==3&&d.officers.Length==40,"saved scene references and actor pool");
        Check(d.IsCinematic&&!d.player.Fire()&&!d.Collect(0),"opening locks gameplay");
        float reveal=Time.time+1.5f;while(Time.time<reveal)yield return null;
        Capture(d.filmCamera,"opening");
        d.SetPaused(true);float before=Time.time;yield return null;Check(Time.timeScale==0&&!d.player.Fire()&&Mathf.Abs(Time.time-before)<.01f,"pause blocks gameplay and clock");d.SetPaused(false);
        // 컷신 생략은 실제 코루틴의 종착점을 사용한다.
        Set(d,"skip",true);while(d.Current==BankPrologue.Phase.Opening)yield return null;
        Check(d.Current==BankPrologue.Phase.Robbery&&d.player.view.enabled&&!d.filmCamera.enabled&&d.Alarm,"cutscene hands control back and starts alarm");
        Check(d.story && d.player.magazine && d.player.bolt && d.player.magazineSize==30,"editable story and rifle reload parts assigned");
        Set(d,"waveTime",0f);yield return null;yield return null;Check(d.SpawnedPolice==12,"first reinforcement wave");Set(d,"waveTime",0f);yield return null;yield return null;Check(d.SpawnedPolice==16,"second reinforcement wave");Set(d,"waveTime",0f);yield return null;yield return null;Check(d.SpawnedPolice==16,"active police cap");Set(d,"waveTime",Time.time+1000);
        Check(d.officers.Take(2).All(o=>o.Active&&o.GetComponent<NavMeshAgent>().isOnNavMesh),"police spawn on baked navigation");
        var path=new NavMeshPath();Check(NavMesh.CalculatePath(new Vector3(13,0,-54),new Vector3(0,0,12),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"street to vault navigation connected");
        float routeLength=0;for(int k=1;k<path.corners.Length;k++)routeLength+=Vector3.Distance(path.corners[k-1],path.corners[k]);Check(routeLength>85 && path.corners.Length>=6,"winding escape route: "+routeLength.ToString("F1")+" metres, "+path.corners.Length+" corners");
        var officer=d.officers[0];var initial=officer.transform.position;
        float until=Time.time+1.2f;while(Time.time<until)yield return null;
        Check(Vector3.Distance(initial,officer.transform.position)>.3f,"police actually approach");
        d.player.Teleport(new Vector3(0,.05f,2));d.player.transform.rotation=Quaternion.identity;
        officer.GetComponent<NavMeshAgent>().Warp(new Vector3(0,0,7));officer.transform.rotation=Quaternion.Euler(0,180,0);
        Physics.SyncTransforms();yield return null;
        Capture(d.player.view,"combat");
        Check(officer.ClearShot(),"police can see exposed player");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,4.5f);wall.transform.localScale=new Vector3(3,3,.3f);
        Physics.SyncTransforms();Check(!officer.ClearShot(),"walls block police shots");UnityEngine.Object.Destroy(wall);yield return null;
        until=Time.time+6;while(officer.ShotsFired==0&&Time.time<until)yield return null;
        Check(officer.ShotsFired>0,"police actually fire during gameplay");
        Capture(d.player.view,"combat");
        foreach(var o in d.officers)if(o.gameObject.activeSelf)o.GetComponent<NavMeshAgent>().enabled=false;
        d.player.Teleport(new Vector3(0,.05f,2));d.player.transform.rotation=Quaternion.identity;
        officer.transform.position=new Vector3(0,.05f,5);officer.transform.rotation=Quaternion.Euler(0,180,0);
        Physics.SyncTransforms();yield return null;
        Check(d.player.Fire()&&officer.Health==35,"player raycast damages police");
        until=Time.time+.25f;while(Time.time<until)yield return null;
        Check(d.player.Fire()&&officer.Dead&&!officer.GetComponent<Collider>().enabled,"second shot defeats police and removes blocker");
        var magStart=d.player.magazine.localPosition;Check(d.player.Reload(),"reload starts");until=Time.time+1.1f;while(Time.time<until)yield return null;Check(Vector3.Distance(magStart,d.player.magazine.localPosition)>.1f&&!d.player.Fire(),"magazine animates and reload blocks fire");Capture(d.player.view,"reload");until=Time.time+1.3f;while(Time.time<until)yield return null;
        Check(d.player.Ammo==30&&!d.player.Reloading,"reload completes");
        int oldHealth=d.player.Health;
        Check(d.player.ReceiveHit(14)&&!d.player.ReceiveHit(14)&&d.player.Health==oldHealth-14,"damage and short invulnerability");
        Check(!d.Collect(0)&&!d.BoardCar(),"remote collection and premature escape blocked");
        d.player.Teleport(new Vector3(0,.05f,11.8f));d.player.transform.rotation=Quaternion.identity;
        Capture(d.player.view,"vault");
        for(int i=0;i<3;i++){d.player.Teleport(new Vector3(d.cash[i].transform.position.x,.05f,12.8f));Check(d.Collect(i)&&!d.Collect(i),"cash "+i+" collected exactly once");}
        Check(d.Current==BankPrologue.Phase.Escape&&d.Collected==3,"all cash unlocks getaway objective");
        d.SetPaused(true);Check(!d.BoardCar(),"paused boarding rejected");d.SetPaused(false);
        foreach(var waypoint in d.escapeRoute){d.player.Teleport(waypoint.position);yield return null;yield return null;}
        d.player.transform.rotation=Quaternion.Euler(0,180,0);Capture(d.player.view,"expanded-lobby");
        Check(d.RouteStep==3,"all escape route checkpoints reached");
        d.player.Teleport(d.exitPoint.position);
        Check(d.BoardCar()&&d.Current==BankPrologue.Phase.Getaway&&!d.player.Fire(),"boarding enters cinematic and disables firing");
        until=Time.time+3;while(Time.time<until)yield return null;
        Capture(d.filmCamera,"getaway");
        Set(d,"skip",true);while(d.Current!=BankPrologue.Phase.Complete)yield return null;
        Check(!d.CanPlay&&Time.timeScale==1,"getaway reaches transition without replay");
        Check(Application.CanStreamedLevelBeLoaded(d.gameScene)&&Application.CanStreamedLevelBeLoaded(d.lobbyScene),"main game and lobby build routes available");
        Check(UnityEngine.Object.FindFirstObjectByType<MoneyManager>()==null&&UnityEngine.Object.FindFirstObjectByType<PoliceManager>()==null,"prologue does not run main-game economy or wanted system");
    }
    static IEnumerator RunFinale(BankPrologue d){
        Check(d.player.ragdoll.bodies.Length>=10&&d.officers.All(o=>o.ragdoll&&o.ragdoll.bodies.Length>=10),"saved citizen ragdoll joints reused for player and all police");
        Check(d.cityVista&&d.cityCarStart&&d.cityWideCamera&&d.logo&&d.roadPolice.Length==18,"saved city vista, logo and three roadblocks assigned");
        Set(d,"skip",true);while(d.Current==BankPrologue.Phase.Opening)yield return null;
        Set(d,"waveTime",Time.time+1000);
        foreach(var o in d.officers)o.Stop();
        var target=d.officers[2];target.Activate();
        d.player.Teleport(new Vector3(0,.05f,2));
        target.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,5));
        Physics.SyncTransforms();yield return null;
        Check(target.ReceiveHit(35)&&target.ragdoll.IsDown&&!target.Dead,"nonlethal shot starts police ragdoll");
        Check(target.ragdoll.bodies.All(b=>!b.isKinematic),"police bones simulate physics");
        float until=Time.time+.5f;while(Time.time<until)yield return null;
        Capture(d.player.view,"police-ragdoll");
        Check(target.ReceiveHit(35)&&target.Dead&&target.ragdoll.Permanent,"fatal shot keeps physical corpse instead of rigid fall");
        foreach(var o in d.officers)o.Stop();
        Check(d.player.ReceiveHit(8)&&d.player.KnockedDown&&!d.player.Fire()&&!d.Collect(0),"player hit starts ragdoll and blocks actions");
        Check(!d.player.ReceiveHit(8),"downed player cannot be repeatedly hit");
        until=Time.time+.6f;while(Time.time<until)yield return null;
        Capture(d.filmCamera,"player-ragdoll");
        Check(Vector3.Distance(d.filmCamera.transform.position,d.player.ragdoll.animator.GetBoneTransform(HumanBodyBones.Head).position)<.3f,"ragdoll camera stays at first-person head position");
        File.WriteAllText("Temp/Bank-ragdoll-details.txt",string.Join("\n",d.player.ragdoll.boneColliders.Select(c=>c.name+" "+c.bounds+" enabled "+c.enabled))+"\n"+string.Join("\n",d.player.avatarRenderers.Select(r=>r.name+" "+r.bounds+" enabled "+r.enabled)));
        until=Time.time+3;while(d.player.KnockedDown&&Time.time<until)yield return null;
        yield return null;
        Check(!d.player.KnockedDown&&d.player.view.enabled&&d.player.GetComponent<CharacterController>().enabled,"player stands up and regains camera and movement");
        Check(d.accomplice&&!d.Collect(0),"saved accomplice owns cash collection instead of player");
        Check(d.accomplice.animator.parameters.Any(p=>p.name=="MoveSpeed"),"accomplice has compatible walk and run animation controller");
        d.filmCamera.transform.position=new Vector3(3,2.4f,10);
        d.filmCamera.transform.LookAt(d.accomplice.transform.position+Vector3.up);
        Capture(d.filmCamera,"accomplice");
        until=Time.time+50;while(d.Current==BankPrologue.Phase.Robbery&&Time.time<until)yield return null;
        Check(d.Collected==d.cash.Length&&d.Current==BankPrologue.Phase.Escape,"accomplice navigates to cash and finishes collection while player defends");
        foreach(var p in d.escapeRoute){d.player.Teleport(p.position);yield return null;yield return null;}
        until=Time.time+30;while(!d.accomplice.ReadyToBoard&&Time.time<until)yield return null;
        Check(d.accomplice.ReadyToBoard,"accomplice follows bank escape corridors to passenger door");
        d.player.Teleport(d.exitPoint.position);Check(d.BoardCar(),"boarding begins short transition");
        until=Time.time+5;while(d.Current==BankPrologue.Phase.Getaway&&Time.time<until)yield return null;
        Check(d.Current==BankPrologue.Phase.Driving&&d.drive.Driving&&!d.IsCinematic,"car hands control to player");
        d.drive.ExternalInput=true;d.drive.SetInput(1,0);
        Vector3 startPosition=d.getawayCar.position;
        var officerStart=d.roadPolice[0].transform.position;
        until=Time.time+3;while(Time.time<until)yield return null;
        Check(d.getawayCar.position.x>startPosition.x+15,"throttle actually moves the escape car");
        Check(Vector3.Distance(officerStart,d.roadPolice[0].transform.position)>1,"roadblock police run toward approaching car");
        var steeringStart=d.getawayCar.rotation;d.drive.SetInput(1,.5f);
        until=Time.time+.35f;while(Time.time<until)yield return null;
        Check(Quaternion.Angle(steeringStart,d.getawayCar.rotation)>3,"steering changes car heading");
        d.drive.ResetToRoad();d.drive.SetInput(1,0);
        Capture(d.filmCamera,"driving");
        d.SetPaused(true);var paused=d.getawayCar.GetComponent<Rigidbody>().position;yield return null;yield return null;
        Check(d.getawayCar.GetComponent<Rigidbody>().position==paused&&!d.FinishDriving(),"pause freezes driving and blocks transition");d.SetPaused(false);
        until=Time.time+4;while(!d.roadPolice.Any(o=>o.Dead)&&Time.time<until)yield return null;
        Check(d.roadPolice.Any(o=>o.Dead&&o.ragdoll.IsDown),"car impact launches roadblock police into ragdoll");
        Capture(d.filmCamera,"roadblock");
        until=Time.time+20;while(d.Current==BankPrologue.Phase.Driving&&Time.time<until)yield return null;
        Check(d.Current==BankPrologue.Phase.CityReveal&&!d.drive.Driving,"road finish triggers city arrival once");
        until=Time.time+3;while(Time.time<until)yield return null;
        Capture(d.filmCamera,"city-arrival");
        until=Time.time+20;while(d.logo.alpha<.9f&&Time.time<until)yield return null;
        Check(d.logo.alpha>=.9f&&d.cityVista.activeSelf,"panorama completes and reveals game logo");
        Check(d.filmCamera.transform.position.y-d.getawayCar.position.y<10,"city reveal uses low diagonal entrance framing");
        Capture(d.filmCamera,"city-logo");
        until=Time.time+10;while(d.Current!=BankPrologue.Phase.Complete&&Time.time<until)yield return null;
        Check(d.Current==BankPrologue.Phase.Complete&&Time.timeScale==1,"logo reaches main-game handoff");
        Check(Application.CanStreamedLevelBeLoaded(d.gameScene)&&UnityEngine.Object.FindFirstObjectByType<MoneyManager>()==null,"main game available and economy not running during prologue");
    }
}
}
#endif



