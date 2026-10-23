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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
            if(command=="build-tutorial")StoryTutorialBuilder.Build();
            if(command=="tune-tutorial")StoryTutorialBuilder.Tune();
            if(command=="inspect-tutorial")StoryTutorialBuilder.Inspect();
            if(command=="polish-tutorial")StoryTutorialBuilder.Polish();
            if(command=="test-chain"){SessionState.SetBool(Key+".chain",true);StartTest();}
            if(command=="test-tutorial"){SessionState.SetBool(Key+".tutorial",true);StartTest();}
            if(command=="test-input"){SessionState.SetBool(Key+".input",true);StartTest();}
            if(command=="vault-polish")BankPrologueBuilder.PolishVault();
            if(command=="partner-reach")BankPrologueBuilder.RefinePartnerReach();
            if(command=="entrance-polish")BankPrologueBuilder.RefineEntrance();
            if(command=="shop-arrival")BankPrologueBuilder.BuildShopArrival();
            if(command=="shop-arrival-polish")BankPrologueBuilder.PolishShopArrival();
            if(command=="test-shop-arrival"){SessionState.SetBool(Key+".shop",true);StartTest();}
            if(command=="pursuit")BankPrologueBuilder.BuildPursuit();
            if(command=="pursuit-polish")BankPrologueBuilder.PolishPursuit();
            if(command=="test-pursuit"){SessionState.SetBool(Key+".pursuit",true);StartTest();}
            if(command=="test-police"){SessionState.SetBool(Key+".police",true);StartTest();}
            if(command=="police-tactics")BankPrologueBuilder.BuildPoliceTactics();
            if(command=="dress-bank")BankPrologueBuilder.DressBank();
            if(command=="combat")BankPrologueBuilder.BuildCombat();
            if(command=="combat-polish")BankPrologueBuilder.PolishCombat();
            if(command=="inspect-performance")BankPrologueBuilder.InspectPerformance();
            if(command=="fix-exit")BankPrologueBuilder.FixBlockedExit();
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
            .Concat(new[]{"Yun.Achievements.first_packaged_pizza","Yun.Achievements.first_honest_income"})
            .Select(k=>new SavedValue{key=k,value=PlayerPrefs.GetInt(k),exists=PlayerPrefs.HasKey(k)}).ToArray()};
        File.WriteAllText("Temp/BankPrologue-prefs.json",JsonUtility.ToJson(save));
        EditorSceneManager.OpenScene(SessionState.GetBool(Key+".tutorial",false)?StoryTutorialBuilder.Main:BankPrologueBuilder.ScenePath);
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
            if(EditorApplication.timeSinceStartup-start>600)throw new Exception("Test timed out");
            if(routines.Count>0){
                var current=routines.Peek();
                if(current.MoveNext()){if(current.Current is IEnumerator child)routines.Push(child);return;}
                routines.Pop();if(routines.Count>0)return;
            }
            SessionState.SetBool(Key+".ok",true);
        } catch(Exception e) {log+="FAIL: "+e+"\n";Debug.LogException(e);}
        EditorApplication.update-=Tick;File.WriteAllText("Temp/BankPrologue-check.txt",log);Directory.CreateDirectory("Logs");File.WriteAllText("Logs/BankPrologue-check.txt",log);Debug.Log(log);EditorApplication.ExitPlaymode();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
    static void Set(object obj,string key,object value)=>obj.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static IEnumerator WalkDoor(CharacterController controller,System.Action done){
        float until=Time.time+6;
        while(controller.transform.position.z>-56&&Time.time<until){controller.Move(new Vector3(0,-2,-3)*Time.deltaTime);yield return null;}
        done();
    }
    static IEnumerator WalkPath(ProloguePlayer player,Vector3 destination){
        var path=new NavMeshPath();
        Check(NavMesh.SamplePosition(player.transform.position,out var from,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(from.position,destination,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"escape corridor has continuous navigation");
        var body=player.GetComponent<CharacterController>();
        foreach(var corner in path.corners.Skip(1)){
            float deadline=Time.time+30;
            while(Time.time<deadline){
                var delta=corner-player.transform.position;delta.y=0;if(delta.magnitude<.35f)break;
                player.transform.rotation=Quaternion.LookRotation(delta);
                body.Move((delta.normalized*4+Vector3.down*3)*Time.deltaTime);yield return null;
            }
            var distance=corner-player.transform.position;distance.y=0;
            Check(distance.magnitude<.5f,"player physically walks corridor corner without teleporting");
        }
    }
    static void Capture(Camera cam,string name){
        var target=new RenderTexture(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;
        var bank=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        var canvas=bank?bank.objective.canvas:StoryTutorial.Current.objective.canvas;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var distance=canvas.planeDistance;
        try{cam.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.3f;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/BankPrologue-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;cam.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static IEnumerator Run(){
        yield return null;yield return null;
        if(SessionState.GetBool(Key+".tutorial",false)){SessionState.SetBool(Key+".tutorial",false);yield return RunTutorial();yield break;}
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.autoEnterGame=false;
        if(SessionState.GetBool(Key+".input",false)){SessionState.SetBool(Key+".input",false);yield return RunKeyboard(d);yield break;}
        if(SessionState.GetBool(Key+".shop",false)){SessionState.SetBool(Key+".shop",false);yield return RunShopArrival(d);yield break;}
        if(SessionState.GetBool(Key+".pursuit",false)){SessionState.SetBool(Key+".pursuit",false);yield return RunPursuit(d);yield break;}
        if(SessionState.GetBool(Key+".police",false)){SessionState.SetBool(Key+".police",false);yield return RunPolice(d);yield break;}
        if(d.drive){yield return RunFinale(d);
            if(SessionState.GetBool(Key+".chain",false)){
                SessionState.SetBool(Key+".chain",false);
                typeof(BankPrologue).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{d.gameScene});
                float limit=Time.realtimeSinceStartup+45;while(!StoryTutorial.Active&&Time.realtimeSinceStartup<limit)yield return null;
                yield return null;yield return null;
                Check(StoryTutorial.Active,"actual prologue scene load hands off to story tutorial");
                yield return RunTutorial();
            }yield break;}
        Check(d!=null&&d.cash.Length==3&&d.officers.Length==40,"saved scene references and actor pool");
        Check(d.IsCinematic&&!d.player.Fire()&&!d.Collect(0),"opening locks gameplay");
        float reveal=Time.time+1.5f;while(Time.time<reveal)yield return null;
        Capture(d.filmCamera,"opening");
        d.SetPaused(true);float before=Time.time;yield return null;Check(Time.timeScale==0&&!d.player.Fire()&&Mathf.Abs(Time.time-before)<.01f,"pause blocks gameplay and clock");d.SetPaused(false);
        // 컷신 생략은 실제 코루틴의 종착점을 사용한다.
        if(!d.accomplice||!d.accomplice.performance)Set(d,"skip",true);
        bool entranceSeen=false;
        while(d.Current==BankPrologue.Phase.Opening){
            if(!entranceSeen&&d.officers[0].Active){
                entranceSeen=true;
                Check(!d.CanPlay&&d.officers[0].ShotsFired==0,"police entrance keeps gameplay and hostile fire locked");
                float untilEntrance=Time.time+1;
                while(Time.time<untilEntrance&&d.Current==BankPrologue.Phase.Opening)yield return null;
                Capture(d.filmCamera,"police-entrance");
            }
            yield return null;
        }
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
    static IEnumerator RunPursuit(BankPrologue d){
        d.AcceptInput=false;d.StopAllCoroutines();d.player.enabled=false;d.accomplice.enabled=false;d.fade.alpha=0;d.bars.alpha=0;
        foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}foreach(var o in d.roadPolice)o.gameObject.SetActive(false);
        d.player.GetComponent<CharacterController>().enabled=false;Set(d,"<Current>k__BackingField",BankPrologue.Phase.Driving);
        d.drive.ExternalInput=true;d.drive.SetInput(1,0);d.drive.Begin();
        float until=Time.time+5;while(Time.time<until)yield return null;
        var cars=UnityEngine.Object.FindObjectsByType<ProloguePursuitCar>(FindObjectsSortMode.None);
        Check(cars.Length==2&&cars.All(c=>c.Pursuing&&c.Travelled>30&&c.siren.isPlaying&&c.headlights.enabled),"both pursuit cars catch up with sirens and headlights");
        foreach(var c in cars)Check(d.getawayCar.position.x-c.transform.position.x>6&&d.getawayCar.position.x-c.transform.position.x<40,"pursuit stays behind within audible range: "+c.name+" gap "+(d.getawayCar.position.x-c.transform.position.x).ToString("F1"));
        d.filmCamera.transform.position=d.getawayCar.position+d.getawayCar.forward*7+Vector3.up*4;d.filmCamera.transform.LookAt(d.getawayCar.position-d.getawayCar.forward*15+Vector3.up);Capture(d.filmCamera,"police-pursuit");
        d.SetPaused(true);var positions=cars.Select(c=>c.transform.position).ToArray();yield return null;yield return null;
        Check(cars.Select((c,i)=>Vector3.Distance(c.transform.position,positions[i])<.001f&&!c.siren.isPlaying).All(v=>v),"pause freezes pursuit and audio");d.SetPaused(false);yield return null;yield return null;
        Check(cars.All(c=>c.siren.isPlaying),"sirens resume after pause");
        d.drive.Stop();yield return null;yield return null;Check(cars.All(c=>!c.Pursuing&&!c.siren.isPlaying&&!c.headlights.enabled),"stopping drive shuts down pursuit audio and lights");
    }
    static IEnumerator RunPolice(BankPrologue d){
        d.AcceptInput=false;d.player.enabled=false;Set(d,"skip",true);
        while(d.Current==BankPrologue.Phase.Opening)yield return null;
        Set(d,"waveTime",Time.time+1000);d.accomplice.enabled=false;
        foreach(var o in d.officers){o.Stop();o.gameObject.SetActive(false);}
        Check(d.officers.All(o=>o.brain)&&d.roadPolice.All(o=>!o.brain),"bank roles assigned; road guards unchanged");
        var cop=d.officers[0];d.player.Teleport(new Vector3(-6,.05f,-9));
        cop.transform.position=new Vector3(-6,0,-16);cop.Activate();cop.brain.flanker=false;
        float until=Time.time+12;while(cop.brain.CoverVisits==0&&Time.time<until)yield return null;
        Check(cop.brain.CoverVisits>0,"police reach protected cover using navigation");
        until=Time.time+.35f;while(Time.time<until)yield return null;
        d.filmCamera.transform.position=cop.transform.position+new Vector3(4,2,-3);d.filmCamera.transform.LookAt(cop.transform.position+Vector3.up);
        Capture(d.filmCamera,"police-cover");
        until=Time.time+15;while(cop.ShotsFired==0&&Time.time<until)yield return null;
        Check(cop.ShotsFired>0,"police peek, aim and fire from cover");
        cop.ReceiveHit(10,cop.transform.position+Vector3.up,Vector3.right);yield return null;yield return null;
        Check(cop.ragdoll.IsDown&&cop.brain.CoverIndex<0,"hit ragdoll releases cover reservation");
        until=Time.time+8;while((cop.ragdoll.IsDown||cop.brain.Current==ProloguePoliceBrain.Action.Recover)&&Time.time<until)yield return null;
        Check(!cop.ragdoll.IsDown&&cop.GetComponent<NavMeshAgent>().enabled,"police recover after hit");
        cop.Stop();cop.gameObject.SetActive(false);
        var flank=d.officers[1];d.player.Teleport(new Vector3(-6,.05f,-9));flank.transform.position=new Vector3(-6,0,-16);flank.Activate();flank.brain.flanker=true;
        until=Time.time+12;while(flank.brain.FlankMoves==0&&Time.time<until)yield return null;
        Check(flank.brain.FlankMoves>0,"flanking role selects lateral navigation route");
        flank.Stop();flank.gameObject.SetActive(false);
        var observer=d.officers[2];d.player.Teleport(new Vector3(0,.05f,2));observer.transform.SetPositionAndRotation(new Vector3(0,0,7),Quaternion.Euler(0,180,0));observer.Activate();
        until=Time.time+.4f;while(Time.time<until)yield return null;
        Check(observer.ClearShot(),"exposed player detected");var remembered=observer.brain.LastSeen;int shots=observer.ShotsFired;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,4);wall.transform.localScale=new Vector3(20,3,.4f);
        d.player.Teleport(new Vector3(1,.05f,2));Physics.SyncTransforms();
        until=Time.time+1.2f;while(Time.time<until)yield return null;
        Check(!observer.ClearShot()&&observer.ShotsFired==shots&&Vector3.Distance(observer.brain.LastSeen,remembered)<.01f,"wall blocks fire and hidden player tracking");
        observer.Stop();observer.gameObject.SetActive(false);UnityEngine.Object.Destroy(wall);
        until=Time.time+1;while(Time.time<until)yield return null;
        var squad=d.GetComponent<ProloguePoliceSquad>();Check(squad.PermitShot()&&squad.PermitShot()&&!squad.PermitShot(),"squad limits simultaneous gunfire");
    }
    static IEnumerator WaitLesson(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    static IEnumerator ApproachDoor(StoryTutorial t){
        var body=t.inventory.GetComponent<CharacterController>();var interaction=t.inventory.GetComponent<PlayerInteraction>();
        var start=t.inventory.transform.position;NavMesh.SamplePosition(start,out var startNav,3,NavMesh.AllAreas);var queue=new System.Collections.Generic.Queue<Vector2Int>();
        var parents=new System.Collections.Generic.Dictionary<Vector2Int,Vector2Int>();var positions=new System.Collections.Generic.Dictionary<Vector2Int,Vector3>();
        var zero=Vector2Int.zero;queue.Enqueue(zero);parents[zero]=zero;positions[zero]=start;Vector2Int end=zero;bool found=false;
        // 실제 충돌체를 살펴 주차된 바이크를 돌아가는 도보 경로를 찾는다. 런타임 게임에는 설치하지 않는다.
        while(queue.Count>0){var key=queue.Dequeue();var p=positions[key];var eye=p+Vector3.up*1.5f;var aim=t.destination.transform.position-eye;
            if(Physics.Raycast(eye,aim.normalized,out var hit,interaction.InteractionDistance,interaction.InteractionLayers)&&hit.collider.GetComponentInParent<DeliveryPoint>()==t.destination){end=key;found=true;break;}
            foreach(var offset in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){
                var next=key+offset;if(Mathf.Abs(next.x)>20||Mathf.Abs(next.y)>20||parents.ContainsKey(next))continue;
                var guess=start+new Vector3(next.x*.5f,0,next.y*.5f);
                guess.y=startNav.position.y;
                if(!NavMesh.SamplePosition(guess,out var n,.4f,NavMesh.AllAreas))continue;
                var floor=n.position;
                if(Physics.OverlapCapsule(floor+Vector3.up*.5f,floor+Vector3.up*1.35f,.34f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(t.inventory.transform)&&c.gameObject!=t.inventory.gameObject))continue;
                parents[next]=key;positions[next]=floor;queue.Enqueue(next);
            }
        }
        Check(found,"walkable interaction approach exists around parked motorcycle");
        var route=new System.Collections.Generic.List<Vector3>();for(var k=end;k!=zero;k=parents[k])route.Add(positions[k]);route.Reverse();
        foreach(var p in route){float until=Time.time+3;while(Time.time<until){var delta=p-body.transform.position;delta.y=0;if(delta.magnitude<.18f)break;body.Move((delta.normalized*2+Vector3.down*3)*Time.deltaTime);yield return null;}}
        var facing=t.destination.transform.position-t.inventory.transform.position;facing.y=0;
        t.inventory.transform.rotation=Quaternion.LookRotation(facing);
        interaction.InteractionCamera.transform.LookAt(t.destination.transform.position);
        var camera=interaction.InteractionCamera;
        bool reachable=Physics.Raycast(camera.transform.position,camera.transform.forward,out var actual,interaction.InteractionDistance,interaction.InteractionLayers)&&actual.collider.GetComponentInParent<DeliveryPoint>()==t.destination;
        File.WriteAllText("Logs/Tutorial-door.txt","player "+body.transform.position+" hit "+(actual.collider?actual.collider.name:"none")+" distance "+actual.distance);
        Check(reachable,"dismounted player physically reaches first delivery interaction surface");
    }
    static IEnumerator RunTutorial(){
        var t=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();
        Check(t&&StoryTutorial.Active&&t.actor.gameObject.activeInHierarchy,"prologue tutorial activates saved Mingi and lesson");
        yield return WaitLesson(.5f);
        Check(t.pc.GetComponent<ShopPC>()!=null,"opening objective targets actual shop PC");
        Check(Physics.Raycast(t.actor.position+Vector3.up*.1f,Vector3.down,out var floor,.25f,~0,QueryTriggerInteraction.Ignore),"Mingi feet settle on shop floor without floating");
        var cam=t.inventory.GetComponent<PlayerInteraction>().InteractionCamera;var cp=cam.transform.position;var cr=cam.transform.rotation;
        cam.transform.position=t.actor.position+new Vector3(-2.6f,1.5f,2.2f);cam.transform.LookAt(t.actor.position+Vector3.up*.9f);Capture(cam,"tutorial-grounded");cam.transform.SetPositionAndRotation(cp,cr);
        Check(t.orders.StoryLesson&&t.day.timeMultiplier==0,"lesson isolates orders and freezes day clock");
        t.day.OnClick_OpenShop();yield return WaitLesson(.5f);
        Check(t.orders.PendingOrders.Count==1&&PizzaRecipes.Title(t.orders.PendingOrders[0])=="치즈피자","opening produces exactly one cheese order");
        Check(t.orders.TryAcceptOrder(t.orders.PendingOrders[0]),"nearby tutorial destination is registered and accepts order");
        yield return WaitLesson(.3f);int cash=MoneyManager.Instance.currentMoney;
        t.dough.Interact(t.inventory);t.maker.Interact(t.inventory);yield return WaitLesson(.3f);
        Check(t.CurrentStep==StoryTutorial.Step.Sauce,"actual dough placement advances to sauce");
        t.sauce.Interact(t.inventory);t.maker.Interact(t.inventory);yield return WaitLesson(.3f);
        Check(t.CurrentStep==StoryTutorial.Step.Cheese,"actual sauce advances to cheese");
        t.cheese.Interact(t.inventory);t.maker.Interact(t.inventory);yield return WaitLesson(.3f);
        Check(t.CurrentStep==StoryTutorial.Step.Bake&&MoneyManager.Instance.currentMoney==cash,"basic lesson ingredients are free and cheese advances to oven");
        t.maker.Interact(t.inventory);t.oven.Interact(t.inventory);yield return WaitLesson(7);
        t.oven.Interact(t.inventory);yield return WaitLesson(.3f);
        Check(t.inventory.HasPizza()&&t.inventory.HeldPizza.GetComponent<Pizza>().IsBaked,"actual oven cooks and returns pizza");
        t.packing.Interact(t.inventory);yield return WaitLesson(.5f);t.packing.Interact(t.inventory);yield return WaitLesson(.5f);t.packing.Interact(t.inventory);yield return WaitLesson(.5f);
        Check(t.pickup.PackedCount==1&&t.CurrentStep==StoryTutorial.Step.Pack,"closed package remains a pickup objective instead of restarting dough");
        t.pickup.Interact(t.inventory);yield return WaitLesson(.3f);Check(t.CurrentStep==StoryTutorial.Step.Load,"packaged pizza advances to cargo");
        t.cargo.Interact(t.inventory);yield return WaitLesson(2);Check(t.cargo.StoredCount==1,"cargo accepts real packaged pizza");
        yield return WaitLesson(10);
        Check(Vector3.Distance(t.actor.position,t.outsideStand.position)<.6f,"Mingi walks through shop doorway to bike on physical floor");
        t.navigation.ResumeDelivery();yield return WaitLesson(.3f);
        var bike=t.cargo.bikeController;Check(bike.EnterBike(t.inventory.gameObject),"real bike mounting accepts cargo and switches rider/camera");yield return WaitLesson(.3f);
        Check(t.CurrentStep==StoryTutorial.Step.Deliver,"navigation and riding advance to delivery");
        var probe=bike.gameObject.AddComponent<TutorialRideProbe>();probe.bike=bike;probe.points=t.navigation.DisplayPath.ToArray();
        float rideEnd=Time.time+180;int recoveries=0;
        while(!probe.done&&Time.time<rideEnd){
            if(!bike.isDriven){yield return WaitLesson(6);Check(++recoveries<=3,"rider can recover after traffic collision");bike.EnterBike(t.inventory.gameObject);}
            yield return null;
        }
        File.WriteAllText("Logs/Tutorial-ride.txt","position "+bike.transform.position+" destination "+t.destination.transform.position+" progress "+probe.index+" / "+probe.points.Length+" riding "+bike.isDriven+" recoveries "+recoveries+" path "+string.Join(";",probe.points.Select(p=>p.ToString())));
        Check(probe.done&&bike.isDriven,"real motorcycle physics follows first delivery route without teleporting");
        UnityEngine.Object.Destroy(probe);bike.ExitBike();yield return WaitLesson(.5f);
        t.cargo.Interact(t.inventory);yield return WaitLesson(2);
        var cc=t.inventory.GetComponent<CharacterController>();
        yield return ApproachDoor(t);
        Set(t.orders,"deliveryClockMinutes",10000d);
        t.destination.Interact(t.inventory);yield return WaitLesson(.3f);
        Check(!t.orders.HasActiveOrder&&t.CurrentStep==StoryTutorial.Step.Review,"actual first delivery completes order and triggers customer dialogue");
        var review=ShopReputation.Instance.Reviews[0];Check(review.stars==5&&review.payment>0&&review.text.Contains("오랜만"),"late tutorial delivery retains payment and story five-star review");
        Check(t.orders.PendingOrders.Count==0,"normal orders stay suppressed until return");
        var app=UnityEngine.Object.FindObjectsByType<ReviewPhoneApp>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        for(var parent=app.transform;parent!=null;parent=parent.parent)parent.gameObject.SetActive(true);app.Open();yield return WaitLesson(8);
        Check(t.CurrentStep==StoryTutorial.Step.Return,"review app advances return-to-shop objective");
        cc.enabled=false;t.inventory.transform.position=t.insideStand.position+Vector3.forward*1.6f;yield return WaitLesson(15);
        File.WriteAllText("Logs/Tutorial-guide-return.txt","actor "+t.actor.position+" target "+t.insideStand.position+" route index "+typeof(StoryTutorial).GetField("guideIndex",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(t)+" nearby "+string.Join(",",Physics.OverlapSphere(t.actor.position+Vector3.up,.6f).Select(c=>c.name)));
        Check(Vector3.Distance(t.actor.position,t.insideStand.position)<1.4f,"Mingi returns inside through doorway before final conversation");
        Check(t.Completed&&!StoryTutorial.Active&&!t.orders.StoryLesson&&t.day.timeMultiplier>0,"return dialogue completes and restores normal day and order rules");
        yield return WaitLesson(1);Check(t.orders.PendingOrders.Count==OrderManager.PendingCapacity,"normal order queue refills after lesson");
    }
    static IEnumerator RunKeyboard(BankPrologue d){
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try{
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Space,.15f);
            float openingEnd=Time.time+10;
            while(d.Current==BankPrologue.Phase.Opening&&Time.time<openingEnd)yield return null;
            Check(d.CanPlay,"Space input skips opening and hands control back");
            Set(d.player,"immuneUntil",Time.time+1000);
            // 제어 함수를 직접 호출하지 않고 입력 이벤트로 일반 Update를 검증한다.
            var startPosition=d.player.transform.position;
            float until=Time.time+.6f;
            while(Time.time<until){InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            Check(Vector3.Distance(startPosition,d.player.transform.position)>1,"physical W input moves player through normal Update after opening");
            Check(d.player.Fire(),"rifle fires before keyboard reload test");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.R));
            until=Time.time+1;while(!d.player.Reloading&&Time.time<until)yield return null;
            Check(d.player.Reloading,"physical R input starts reload through normal Update");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            until=Time.time+3;while(d.player.Reloading&&Time.time<until)yield return null;
            Check(d.player.Ammo==d.player.magazineSize&&!d.player.Reloading,"keyboard reload refills magazine");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Escape,.15f);
            Check(d.Paused&&Time.timeScale==0,"Escape input pauses gameplay");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Enter,.15f);
            Check(!d.Paused&&Time.timeScale==1,"Enter input resumes gameplay");
            // 탑승 전제는 준비하되 탑승 호출 자체는 E 입력으로만 실행한다.
            d.StopAllCoroutines();
            foreach(var officer in d.officers){officer.Stop();officer.gameObject.SetActive(false);}
            Set(d,"<Current>k__BackingField",BankPrologue.Phase.Escape);
            Set(d,"<RouteStep>k__BackingField",0);
            d.player.Teleport(d.exitPoint.position);
            var agent=d.accomplice.GetComponent<NavMeshAgent>();
            agent.Warp(d.accomplice.boardingPoint.position);
            Check(d.accomplice.ReadyToBoard,"companion ready at passenger door");
            yield return null;yield return null;
            Check(d.RouteStep<d.escapeRoute.Length&&d.prompt.text.Contains("E  "),"boarding prompt is shown even when intermediate route markers were missed");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.E,.15f);
            Check(d.Current==BankPrologue.Phase.Getaway,"E input starts vehicle boarding even with missed route markers");
            until=Time.time+15;while(d.Current==BankPrologue.Phase.Getaway&&Time.time<until)yield return null;
            Check(d.Current==BankPrologue.Phase.Driving&&d.drive.Driving,"boarding animation hands over driving control");
            var car=d.getawayCar.GetComponent<Rigidbody>();
            startPosition=car.position;
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.W,1);
            Check(Vector3.Distance(startPosition,car.position)>1&&d.drive.Speed>0,"W input accelerates vehicle");
            var rotation=car.rotation;
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.D,.3f);
            Check(Quaternion.Angle(rotation,car.rotation)>1,"D input steers vehicle");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Space,.5f);
            Check(Mathf.Abs(d.drive.Speed)<.1f,"Space input brakes vehicle");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.C,1);
            Check(Vector3.Dot(d.filmCamera.transform.position-car.position,car.transform.forward)>0,"C input moves camera to rear-view position");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.R,.15f);
            Check(Mathf.Abs(car.position.z+65)<.1f&&Mathf.Abs(d.drive.Speed)<.1f,"R input restores vehicle to road");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.S,.5f);
            Check(d.drive.Speed<0,"S input reverses vehicle");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Escape,.15f);
            Check(d.Paused,"Escape pauses driving");
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.R,.15f);
            float timeout=Time.realtimeSinceStartup+12;
            while(d&&Time.realtimeSinceStartup<timeout)yield return null;
            var restarted=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
            Check(!d&&restarted&&restarted.Current==BankPrologue.Phase.Opening&&Time.timeScale==1,"paused R input reloads prologue scene");
            restarted.StopAllCoroutines();
            Set(restarted,"<Current>k__BackingField",BankPrologue.Phase.Robbery);
            restarted.Fail();
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.R,.15f);
            timeout=Time.realtimeSinceStartup+12;
            while(restarted&&Time.realtimeSinceStartup<timeout)yield return null;
            var finalDirector=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
            Check(!restarted&&finalDirector&&finalDirector.Current==BankPrologue.Phase.Opening,"failed-screen R input restarts prologue");
            string lobby=System.IO.Path.GetFileNameWithoutExtension(finalDirector.lobbyScene);
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.Escape,.15f);
            yield return HoldKey(keyboard,UnityEngine.InputSystem.Key.L,.15f);
            timeout=Time.realtimeSinceStartup+15;
            while(finalDirector&&Time.realtimeSinceStartup<timeout)yield return null;
            Check(!finalDirector&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==lobby&&Time.timeScale==1,"paused L input returns to lobby");
        }finally{InputSystem.RemoveDevice(keyboard);}
    }
    static IEnumerator HoldKey(Keyboard keyboard,UnityEngine.InputSystem.Key key,float seconds){
        float until=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;}
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        int frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
    }
    static IEnumerator RunShopArrival(BankPrologue d){
        d.StopAllCoroutines();d.AcceptInput=false;d.player.enabled=false;d.accomplice.gameObject.SetActive(false);
        Set(d,"<Current>k__BackingField",BankPrologue.Phase.CityReveal);Set(d,"skip",false);
        d.fade.alpha=0;d.cityVista.SetActive(true);d.getawayCar.SetPositionAndRotation(d.cityCarStart.position,d.cityCarStart.rotation);
        d.StartCoroutine(d.shopArrival.Play());
        float until=Time.time+.5f;while(Time.time<until)yield return null;Capture(d.filmCamera,"shop-arrival-car-refined");
        d.SetPaused(true);var position=d.getawayCar.position;yield return null;yield return null;Check(d.getawayCar.position==position,"arrival dialogue pauses car movement");d.SetPaused(false);
        int stage=1;until=Time.time+d.story.carConversation.Sum(l=>l.seconds)+d.story.shopConversation.Sum(l=>l.seconds)+15;
        while(!d.shopArrival.Completed&&Time.time<until){
            if(d.shopArrival.Stage>stage){stage=d.shopArrival.Stage;float wait=Time.time+.8f;while(Time.time<wait)yield return null;Capture(d.filmCamera,"shop-arrival-refined-"+stage);}
            yield return null;
        }
        Check(d.shopArrival.Completed&&d.shopArrival.Opened,"dialogue, approach and shutter sequence completes");
        Set(d,"skip",true);yield return d.shopArrival.Play();
        Check(d.shopArrival.Completed&&d.shopArrival.slats.All(s=>s.localPosition.y>3),"skip leaves shutter fully open without blocking transition");
        Check(UnityEngine.Object.FindFirstObjectByType<MoneyManager>()==null,"shop cinematic does not start main-game economy");
    }
    static IEnumerator RunFinale(BankPrologue d){
        d.AcceptInput=false;d.player.enabled=false;
        Check(d.player.ragdoll.bodies.Length>=10&&d.officers.All(o=>o.ragdoll&&o.ragdoll.bodies.Length>=10),"saved citizen ragdoll joints reused for player and all police");
        Check(d.cityVista&&d.cityCarStart&&d.cityWideCamera&&d.logo&&d.roadPolice.Length==18,"saved city vista, logo and three roadblocks assigned");
        if(d.accomplice.performance){
            float openingCapture=Time.time+5.6f;while(Time.time<openingCapture)yield return null;
            Capture(d.filmCamera,"vault-performance");
            File.WriteAllText("Logs/Bank-performance-pose.txt", "Opening pose " + d.accomplice.animator.GetBoneTransform(HumanBodyBones.RightHand).position + " wheel " + d.accomplice.performance.wheel.position);
        }else Set(d,"skip",true);
        while(d.Current==BankPrologue.Phase.Opening)yield return null;
        Set(d,"waveTime",Time.time+1000);
        foreach(var o in d.officers)o.Stop();
        var target=d.officers[2];target.Activate();
        d.player.Teleport(new Vector3(0,.05f,2));
        target.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,5));
        Physics.SyncTransforms();yield return null;
        if(d.player.combatFeedback){
            d.player.view.transform.LookAt(target.transform.position+Vector3.up*1.3f);
            var e=d.player.view.transform.localEulerAngles;Set(d.player,"pitch",e.x>180?e.x-360:e.x);
            int ammo=d.player.Ammo;
            Check(d.player.Fire()&&d.player.Ammo==ammo-1,"actual rifle shot consumes one round");
            Check(target.Health==35&&target.ragdoll.IsDown&&!target.Dead,"raycast hit applies damage and starts police ragdoll");
            Check(d.player.combatFeedback.ConfirmedHits==1&&d.player.combatFeedback.hitMarker.alpha>0,"only accepted damage confirms the hit marker");
            Check(d.player.gunFeedback.ShotCount==1&&d.player.gunFeedback.flame.gameObject.activeSelf&&d.player.RecoilAmount>1,"shot triggers visible flame and recoil");
            Check(!d.player.Fire()&&d.player.gunFeedback.ShotCount==1,"fire interval blocks duplicate sound and flashes");
            Capture(d.player.view,"rifle-feedback");
        }else Check(target.ReceiveHit(35)&&target.ragdoll.IsDown&&!target.Dead,"nonlethal shot starts police ragdoll");
        Check(target.ragdoll.bodies.All(b=>!b.isKinematic),"police bones simulate physics");
        float until=Time.time+.5f;while(Time.time<until)yield return null;
        Capture(d.player.view,"police-ragdoll");
        Check(target.ReceiveHit(35)&&target.Dead&&target.ragdoll.Permanent,"fatal shot keeps physical corpse instead of rigid fall");
        if(d.player.combatFeedback){
            Check(!d.player.gunFeedback.flame.gameObject.activeSelf,"muzzle flame switches off after the shot");
            int confirmed=d.player.combatFeedback.ConfirmedHits;
            var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=d.player.view.transform.position+Vector3.left*3;cover.transform.localScale=Vector3.one;
            d.player.view.transform.LookAt(cover.transform.position);Physics.SyncTransforms();
            Check(d.player.Fire()&&d.player.combatFeedback.SurfaceHits==1&&d.player.combatFeedback.ConfirmedHits==confirmed,"wall impact plays surface feedback without a false confirmed hit");
            UnityEngine.Object.Destroy(cover);
            d.SetPaused(true);Check(!d.player.Fire(),"pause prevents shooting feedback");d.SetPaused(false);
        }
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
        bool peakSeen=false;
        until=Time.time+50;bool cashCaptured=false;while(d.Current==BankPrologue.Phase.Robbery&&Time.time<until){
            if(d.BattleBeat==BankPrologue.CombatBeat.FinalPush)peakSeen=true;
            if(!cashCaptured&&d.accomplice.CollectionProgress>.45f){
                d.filmCamera.transform.position=d.accomplice.transform.position+new Vector3(2,2,-1);
                d.filmCamera.transform.LookAt(d.accomplice.transform.position+new Vector3(0,1,.4f));
                Capture(d.filmCamera,"cash-performance");cashCaptured=true;
            }
            yield return null;
        }
        Check(d.Collected==d.cash.Length&&d.Current==BankPrologue.Phase.Escape,"accomplice navigates to cash and finishes collection while player defends");
        Check(peakSeen,"cash collection triggers final police pressure before escape");
        if(d.accomplice.performance){
            var acting=d.accomplice.performance;
            Check(acting.BagCarried&&acting.DepositedBundles==15,"fifteen bundles deposited and bag shouldered before escape");
            Check(acting.SoundEvents>=20,"foley events accompany vault, cash and bag actions");
            int reactions=acting.StartleCount;
            typeof(ProloguePartnerPerformance).GetMethod("HearShot",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(acting,new object[]{acting.transform.position+Vector3.forward});
            Check(acting.StartleCount>reactions,"nearby gunshot triggers companion reaction without interrupting escape");
        }
        // 금고에 멈춰 있는 플레이어를 두고 공범이 먼저 탈출하지 않는지 확인한다.
        d.player.Teleport(new Vector3(0,.05f,11));
        until=Time.time+5;while(Time.time<until)yield return null;
        Check(Vector3.Distance(d.accomplice.transform.position,d.player.transform.position)<3,"accomplice waits beside player instead of escaping ahead");
        foreach(var p in d.escapeRoute){
            yield return WalkPath(d.player,p.position);
            until=Time.time+18;while(Vector3.Distance(d.accomplice.transform.position,p.position)>3&&Time.time<until)yield return null;
            Check(Vector3.Distance(d.accomplice.transform.position,p.position)<3,"accomplice follows player around corridor "+p.name);
        }
        var controller=d.player.GetComponent<CharacterController>();
        // 정지시킨 경찰이 입구를 가리지 않도록 지형 통과 검증에서는 제외한다.
        foreach(var o in d.officers)o.gameObject.SetActive(false);
        d.player.Teleport(new Vector3(14,.05f,-45));
        bool walked=false;d.StartCoroutine(WalkDoor(controller,()=>walked=true));
        while(!walked)yield return null;
        Check(d.player.transform.position.z<-55.5f,"actual player controller walks through bank doorway and pavement without teleporting: "+d.player.transform.position);
        d.player.Teleport(d.exitPoint.position);
        until=Time.time+30;while(!d.accomplice.ReadyToBoard&&Time.time<until)yield return null;
        Check(d.accomplice.ReadyToBoard,"accomplice follows bank escape corridors to passenger door");
        d.player.Teleport(d.exitPoint.position);Check(d.BoardCar(),"boarding begins short transition");
        if(d.accomplice.performance){
            float doorCapture=Time.time+1.2f;while(Time.time<doorCapture)yield return null;Capture(d.filmCamera,"boarding-performance");
            doorCapture=Time.time+1;while(Time.time<doorCapture)yield return null;Capture(d.filmCamera,"seating-performance");
        }
        until=Time.time+5;while(d.Current==BankPrologue.Phase.Getaway&&Time.time<until)yield return null;
        Check(d.Current==BankPrologue.Phase.Driving&&d.drive.Driving&&!d.IsCinematic,"car hands control to player");
        if(d.accomplice.performance)Check(d.accomplice.performance.EnteredCar&&Quaternion.Angle(d.accomplice.performance.passengerDoor.localRotation,Quaternion.identity)<1,"accomplice enters through opening passenger door and shuts it before driving");
        d.drive.ExternalInput=true;d.drive.SetInput(1,0);
        Vector3 startPosition=d.getawayCar.position;
        var officerStart=d.roadPolice[0].transform.position;
        until=Time.time+3;while(Time.time<until)yield return null;
        Check(d.getawayCar.position.x>startPosition.x+15,"throttle actually moves the escape car");
        Check(Vector3.Distance(officerStart,d.roadPolice[0].transform.position)>1,"roadblock police run toward approaching car");
        var pursuit=UnityEngine.Object.FindObjectsByType<ProloguePursuitCar>(FindObjectsSortMode.None);
        if(pursuit.Length>0){
            Check(pursuit.Length==2&&pursuit.All(c=>c.Pursuing&&c.Travelled>4&&c.siren.isPlaying),"two police cars move with active spatial sirens");
            var camP=d.filmCamera.transform.position;var camR=d.filmCamera.transform.rotation;
            d.filmCamera.transform.position=d.getawayCar.position+d.getawayCar.forward*7+Vector3.up*4;d.filmCamera.transform.LookAt(d.getawayCar.position-d.getawayCar.forward*15+Vector3.up);
            Capture(d.filmCamera,"police-pursuit");d.filmCamera.transform.SetPositionAndRotation(camP,camR);
            d.SetPaused(true);var positions=pursuit.Select(c=>c.transform.position).ToArray();yield return null;yield return null;
            Check(pursuit.Select((c,i)=>Vector3.Distance(c.transform.position,positions[i])<.001f&&!c.siren.isPlaying).All(v=>v),"pause freezes pursuing cars and sirens");d.SetPaused(false);
        }
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
        yield return null;
        Check(pursuit.All(c=>!c.Pursuing&&!c.siren.isPlaying),"pursuit and sirens end before city reveal");
        until=Time.time+3;while(Time.time<until)yield return null;
        Capture(d.filmCamera,"city-arrival");
        int arrivalStage=0;
        until=Time.time+30+d.story.carConversation.Sum(l=>l.seconds)+d.story.shopConversation.Sum(l=>l.seconds);while(d.logo.alpha<.9f&&Time.time<until){
            if(d.shopArrival&&d.shopArrival.Stage>arrivalStage){arrivalStage=d.shopArrival.Stage;yield return null;Capture(d.filmCamera,"shop-arrival-"+arrivalStage);}
            yield return null;
        }
        if(d.shopArrival)Check(d.shopArrival.Completed&&d.shopArrival.Opened,"car conversation and pizza shop shutter lead into logo");
        Check(d.logo.alpha>=.9f&&d.cityVista.activeSelf,"panorama completes and reveals game logo");
        Check(d.filmCamera.transform.position.y-d.getawayCar.position.y<10,"city reveal uses low diagonal entrance framing");
        Capture(d.filmCamera,"city-logo");
        until=Time.time+10;while(d.Current!=BankPrologue.Phase.Complete&&Time.time<until)yield return null;
        Check(d.Current==BankPrologue.Phase.Complete&&Time.timeScale==1,"logo reaches main-game handoff");
        Check(Application.CanStreamedLevelBeLoaded(d.gameScene)&&UnityEngine.Object.FindFirstObjectByType<MoneyManager>()==null,"main game available and economy not running during prologue");
    }
}
[DefaultExecutionOrder(30000)]
public sealed class TutorialRideProbe:MonoBehaviour {
 public MotorcycleController bike;public Vector3[] points;public int index=1;public bool done;
 void Set(string n,object v)=>typeof(MotorcycleController).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(bike,v);
 void Update(){
  if(!bike||points==null||points.Length<2)return;
  var delta=points[Mathf.Min(index,points.Length-1)]-bike.transform.position;delta.y=0;
  if(delta.magnitude<(index==points.Length-1?5f:2.2f)){index++;if(index>=points.Length)done=true;}
  if(done){Set("moveInput",0f);Set("isBraking",true);return;}
  float angle=Vector3.SignedAngle(bike.transform.forward,delta,Vector3.up);
  Set("moveInput",bike.CurrentSpeed>3?0f:1f);Set("isBraking",bike.CurrentSpeed>4);
  Set("smoothedSteerInput",Mathf.Clamp(angle/25,-1,1));
  typeof(MotorcycleController).GetMethod("RotationPhysics",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(bike,null);
 }
}
}
#endif



