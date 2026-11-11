#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonMissionCheck
    {
        static Vector3[] residentStarts;
        static PrisonResident[] movingResidents;
        static float savedSensitivity;
        static bool hadSensitivity;
        static int comfortStage;
        static double comfortNext;
        public static void RunComfort()
        {
            PrisonCommonSetup.Comfort();
            hadSensitivity=PlayerPrefs.HasKey("DeliveryService.MouseSensitivity");
            savedSensitivity=DeliveryService.Yun.Settings.GameSettingsPanel.MouseSensitivity;
            File.WriteAllText("Logs/Prison-comfort-check.txt", "Sensitivity and resident Play Mode checks\n");
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
            SessionState.SetBool("Prison.Comfort",true);
            EditorApplication.EnterPlaymode();
        }
        static void ComfortAssert(bool passed,string message)
        {
            File.AppendAllText("Logs/Prison-comfort-check.txt",(passed?"PASS ":"FAIL ")+message+"\n");
            if(!passed)throw new Exception(message);
        }
        static void ComfortTick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<comfortNext)return;
            try{
            if(comfortStage==0){
                var panel=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Yun.Settings.GameSettingsPanel>(FindObjectsInactive.Include);
                panel.gameObject.SetActive(true);
                var slider=panel.transform.Find("Window/Sensitivity Slider").GetComponent<UnityEngine.UI.Slider>();
                slider.value=1.7f;
                ComfortAssert(Mathf.Abs(DeliveryService.Yun.Settings.GameSettingsPanel.MouseSensitivity-1.7f)<.001f,"slider changes shared sensitivity");
                ComfortAssert(Time.timeScale==0,"settings pause");panel.Close();
                ComfortAssert(Time.timeScale==1,"settings resume");
                ComfortAssert(Mathf.Abs(PlayerPrefs.GetFloat("DeliveryService.MouseSensitivity")-1.7f)<.001f,"sensitivity saved");
                movingResidents=UnityEngine.Object.FindObjectsByType<PrisonResident>(FindObjectsSortMode.None).Where(r=>r.route.Length>0).ToArray();
                residentStarts=movingResidents.Select(r=>r.transform.position).ToArray();
                ComfortAssert(movingResidents.Length==5,"five roaming residents, delivery recipients remain stationary");
                comfortStage=1;comfortNext=EditorApplication.timeSinceStartup+30;return;
            }
            if(comfortStage==1){
                for(int i=0;i<movingResidents.Length;i++)ComfortAssert(Vector3.Distance(residentStarts[i],movingResidents[i].transform.position)>2f,"resident moved: "+movingResidents[i].name);
                UnityEngine.SceneManagement.SceneManager.LoadScene("JJinCity");comfortStage=2;comfortNext=EditorApplication.timeSinceStartup+12;return;
            }
            var cityPanel=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Yun.Settings.GameSettingsPanel>(FindObjectsInactive.Include);
            cityPanel.gameObject.SetActive(true);
            var citySlider=cityPanel.transform.Find("Window/Sensitivity Slider").GetComponent<UnityEngine.UI.Slider>();
            ComfortAssert(Mathf.Abs(citySlider.value-1.7f)<.001f,"city reads prison setting after scene change");
            citySlider.value=savedSensitivity;cityPanel.Close();
            if(!hadSensitivity)PlayerPrefs.DeleteKey("DeliveryService.MouseSensitivity");PlayerPrefs.Save();
            File.AppendAllText("Logs/Prison-comfort-check.txt","RESULT: PASS\n");
            EditorApplication.update-=ComfortTick;if(Application.isBatchMode)EditorApplication.Exit(0);else EditorApplication.ExitPlaymode();
            }catch(Exception e){File.AppendAllText("Logs/Prison-comfort-check.txt",e+"\n");PlayerPrefs.SetFloat("DeliveryService.MouseSensitivity",savedSensitivity);if(!hadSensitivity)PlayerPrefs.DeleteKey("DeliveryService.MouseSensitivity");PlayerPrefs.Save();EditorApplication.update-=ComfortTick;if(Application.isBatchMode)EditorApplication.Exit(1);else EditorApplication.ExitPlaymode();}
        }
        const string Key="Prison.Check", Log="Logs/Prison-check.txt";
        static double next;
        static int stage;
        static bool error;
        static PrisonMissionCheck(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Prison.Comfort",false)){SessionState.SetBool("Prison.Comfort",false);savedSensitivity=PlayerPrefs.GetFloat("DeliveryService.MouseSensitivity",1f);hadSensitivity=PlayerPrefs.HasKey("DeliveryService.MouseSensitivity");comfortStage=0;comfortNext=EditorApplication.timeSinceStartup+5;EditorApplication.update+=ComfortTick;}};}
        public static void RunCity(){SessionState.SetBool("Prison.Check.City",true);Run();}
        public static void Run()
        {
            if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save scene before test");
            File.WriteAllText(Log,"Prison mission Play Mode checks. Interaction and travel positions seeded by test.\n");
            EditorSceneManager.OpenScene(SessionState.GetBool("Prison.Check.City",false)?"Assets/Scenes/JJinCity.unity":"Assets/Scenes/PrisonChapter.unity");
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){stage=0;error=false;next=Time.time+3;EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;Application.runInBackground=true;}
            if(state==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;SessionState.SetBool(Key,false);SessionState.SetBool("Prison.Check.City",false);}
        }
        static void OnLog(string message,string stack,LogType type){if(type==LogType.Exception){error=true;File.AppendAllText(Log,"EXCEPTION "+message+"\n"+stack+"\n");}}
        static void Check(bool condition,string name){File.AppendAllText(Log,(condition?"PASS ":"FAIL ")+name+"\n");if(!condition)error=true;}
        static void Move(PrisonMission m,Vector3 at){var cc=m.player.GetComponent<CharacterController>();cc.enabled=false;m.player.transform.position=at;cc.enabled=true;Cursor.lockState=CursorLockMode.Locked;}
        static void Path(Vector3 from,Vector3 to,string name)
        {
            var path=new NavMeshPath();bool a=NavMesh.SamplePosition(from,out var start,2,NavMesh.AllAreas),b=NavMesh.SamplePosition(to,out var end,2,NavMesh.AllAreas);
            bool result=a&&b&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            Check(result,"walkable path "+name+" "+from+" -> "+to);
        }
        static int rayRetries;
        static bool RayPoint(PrisonMission m,Vector3 origin,Vector3 target,PrisonMissionPoint expected,string name)
        {
            Physics.SyncTransforms();
            var hits=Physics.RaycastAll(origin,(target-origin).normalized,3,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Where(h=>!h.transform.IsChildOf(m.player.transform));
            var hit=hits.FirstOrDefault();
            bool reaches=hit.collider&&hit.collider.GetComponentInParent<PrisonMissionPoint>()==expected;
            if(!reaches&&hit.collider&&hit.collider.GetComponentInParent<PrisonResident>()&&rayRetries++<5){File.AppendAllText(Log,"WAIT for passerby to clear: "+name+"\n");return false;}
            rayRetries=0;Check(reaches,name+" hit="+(hit.collider?hit.collider.name:"none"));return true;
        }
        static void Capture(Camera camera,Vector3 at,Vector3 look,string file)
        {
            var p=camera.transform.position;var q=camera.transform.rotation;var target=camera.targetTexture;var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;
            camera.transform.position=at;camera.transform.LookAt(look);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/"+file+".png",tex.EncodeToPNG());
            camera.targetTexture=target;camera.transform.SetPositionAndRotation(p,q);RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
        static float originalTime,originalMoney;
        static int originalDay;
        static void City()
        {
            var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            if(stage++==0)
            {
                originalTime=clock.currentTime;originalDay=clock.currentDay;originalMoney=MoneyManager.Instance.currentMoney;
                Check(clock.IsNightPreparation,"story ready without closing shop");
                Check(BrokerMission.Current.CanStart,"broker available without waiting for night");
                clock.OnClick_ConfirmClose();
                next=Time.time+2;return;
            }
            Check(clock.currentDay==originalDay&&!clock.IsTransitioning,"no next day transition");
            Check(MoneyManager.Instance.currentMoney==originalMoney,"no daily charge");
            Check(!clock.dayText.gameObject.activeSelf&&!clock.timeText.gameObject.activeSelf,"calendar labels hidden");
            typeof(BrokerMission).GetProperty("State").SetValue(BrokerMission.Current,BrokerMission.Phase.Complete);
            Check(SpecialOrderMission.Current.DayUnlocked&&SpecialOrderMission.Current.CanStart,"special order unlocks same session");
            typeof(SpecialOrderMission).GetProperty("State").SetValue(SpecialOrderMission.Current,SpecialOrderMission.Phase.Complete);
            Check(BackdoorMission.Current.DayUnlocked&&BackdoorMission.Current.CanStart,"backdoor unlocks same session");
            typeof(BackdoorMission).GetProperty("State").SetValue(BackdoorMission.Current,BackdoorMission.Phase.Complete);
            Check(MechanicMission.Current.DayUnlocked&&MechanicMission.Current.CanStart,"mechanic unlocks same session");
            typeof(MechanicMission).GetProperty("State").SetValue(MechanicMission.Current,MechanicMission.Phase.Complete);
            Check(VenueFinale.Current.Begin()&&VenueFinale.Current.State==VenueFinale.Phase.Opening,"finale starts without next day wait");
            File.AppendAllText(Log,error?"RESULT: FAIL\n":"RESULT: PASS\n");File.Copy(Log,"Logs/Story-calendar-check.txt",true);EditorApplication.ExitPlaymode();
        }
        static void Tick()
        {
            if(Time.time<next||!EditorApplication.isPlaying)return;
            next=Time.time+1;
            try
            {
                if(SessionState.GetBool("Prison.Check.City",false)){City();return;}
                var m=PrisonMission.Current;if(!m)throw new Exception("No PrisonMission");
                switch(stage++)
                {
                    case 0:
                        
                        Cursor.lockState=CursorLockMode.Locked;
                        Check(m.player.View&&m.player.GetComponent<PlayerWeapon>()&&m.player.GetComponent<PlayerTacticalStance>()&&m.player.avatar,"player combat, stance, avatar and camera preserved");
                        Check(!UnityEngine.Object.FindFirstObjectByType<DayNightCycle>(),"no prison calendar");
                        var shared=m.GetComponent<PrisonSharedPresentation>();
                        Check(shared&&shared.navigation&&shared.marker.GetComponentInChildren<StoryTargetDiamond>(),"shared city marker and navigation connected");
                        Check(m.player.GetComponent<InteractHoverManager>().enabled,"city interaction outline enabled");
                        Check(m.player.GetComponentInChildren<PhoneController>().GetComponentInChildren<Canvas>(true).gameObject.activeSelf,"phone canvas enabled");
                        var settings=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Yun.Settings.GameSettingsPanel>(FindObjectsInactive.Include);
                        Check(settings,"city ESC settings present");
                        settings.gameObject.SetActive(true);
                        Check(Time.timeScale==0&&!m.player.GetComponent<PlayerMove>().enabled,"settings pauses player and time");
                        settings.Close();
                        Check(Time.timeScale==1&&m.player.GetComponent<PlayerMove>().enabled,"settings resumes player");
                        var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();
                        Check(planner&&planner.enabled,"shared phone apps initialized");
                        planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Missions);
                        planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Messages);
                        planner.Show(DeliveryService.Mok.Navigation.NavigationPlanner.Screen.Home);
                        foreach(var guard in m.guards)Check(guard.GetComponent<NavMeshAgent>().isOnNavMesh,"guard on NavMesh: "+guard.name);
                        Path(m.player.transform.position,new Vector3(30,0,-29.8f),"cell to kitchen");
                        Path(new Vector3(30,0,-27),new Vector3(4.8f,0,-25),"cart to cellblock");
                        Path(new Vector3(0,0,-11),new Vector3(-28,0,4),"cellblock to infirmary");
                        Capture(m.player.View,new Vector3(29,2,-24),new Vector3(30,1,-31),"Prison-kitchen-play");
                        Capture(m.player.View,new Vector3(-28,2,7),new Vector3(-30,1,4),"Prison-mingi-play");
                        Capture(m.player.View,new Vector3(0,1.7f,-28),new Vector3(7,1,-25),"Prison-cells-play");
                        Move(m,new Vector3(30,1.1f,-30));
                        RayPoint(m,new Vector3(30,1.7f,-30),m.cooking.transform.position,m.cooking,"cooking F target");
                        Check(m.Use(m.cooking),"start cooking");next+=4;break;
                    case 1:Check(m.State==PrisonMission.Phase.Load,"cooking finishes");m.LoadCart();Check(m.cart.Count==3,"three meals loaded");break;
                    case 2:case 3:case 4:
                        int index=m.Delivered;Move(m,m.recipients[index].transform.position+Vector3.left*1.5f);m.cart.transform.position=m.player.transform.position+Vector3.forward;
                        if(!RayPoint(m,m.player.transform.position+Vector3.up*1.5f,m.recipients[index].transform.position+Vector3.up,m.recipients[index],"recipient F target "+index)){stage--;next=Time.time+1;break;}
                        Check(m.Use(m.recipients[index]),"serve recipient "+index);next+=1;break;
                    case 5:
                        Capture(m.player.View,new Vector3(5,1.7f,-25),new Vector3(7.2f,1,-25),"Prison-prisoner-close");
                        Check(m.Delivered==3&&m.cart.Count==0&&m.State==PrisonMission.Phase.Infirmary,"three deliveries unlock infirmary");
                        foreach(var g in m.guards)g.Distract(new Vector3(-19,0,-10));
                        RayPoint(m,new Vector3(-28.7f,1.7f,4),new Vector3(-30,.75f,4),m.infirmary,"Mingi bed F target");
                        Move(m,new Vector3(-29,1.1f,4));Check(m.Use(m.infirmary),"talk to Mingi");next+=5;break;
                    case 6:Check(m.Use(m.infirmary),"Mingi dialogue 2");next+=5;break;
                    case 7:foreach(var g in m.guards)g.Distract(new Vector3(-19,0,-10));Check(m.Use(m.infirmary),"Mingi dialogue 3");break;
                    case 8:
                        Check(m.State==PrisonMission.Phase.Return,"return objective");m.Fail(false);Check(m.Failing,"stealth detection starts retry");next+=4;break;
                    case 9:
                        Check(!m.Failing&&m.State==PrisonMission.Phase.Infirmary&&m.Delivered==3,"stealth retry keeps delivery progress");
                        // 완료 동작은 위 대화 흐름 검사와 분리해 복귀 단계에서 확인한다.
                        typeof(PrisonMission).GetProperty("State").SetValue(m,PrisonMission.Phase.Return);
                        Move(m,new Vector3(-11.5f,1.1f,-25));Check(m.Use(m.cell),"return to own cell");Check(m.State==PrisonMission.Phase.Complete,"mission complete");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("PrisonChapter");next+=3;break;
                    case 10:
                        var attackedGuard=m.guards[2];
                        Move(m,attackedGuard.transform.position+attackedGuard.transform.forward*2+Vector3.up);
                        Check(attackedGuard.GetComponent<CitizenCombat>().ReceiveKick(m.player,Vector3.forward,30),"guard hit intercepted");
                        Check(m.Failing,"guard retaliates and restarts");next+=4;break;
                    case 11:
                        Check(!m.Failing&&m.State==PrisonMission.Phase.Cook&&m.Delivered==0&&m.cart.Count==0,"assault resets entire mission");
                        File.AppendAllText(Log,error?"RESULT: FAIL\n":"RESULT: PASS\n");EditorApplication.ExitPlaymode();break;

                }
            }
            catch(Exception e){File.AppendAllText(Log,"TEST ERROR "+e+"\n");EditorApplication.ExitPlaymode();}
        }
    }
}
#endif





