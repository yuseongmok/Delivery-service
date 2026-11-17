#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class ChickenCookingFlowCheck
    {
        const string Key="Mok.ChickenCookingFlow.Check";
        static IEnumerator routine;
        static int frame=-1;
        static double began;
        static string log="";
        static ChickenCookingFlowCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS: "+message+"\n";}
        public static void Begin(){File.WriteAllText("Temp/ChickenCookingFlowCheck","run");Poll();}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Key,false) && File.Exists("Temp/ChickenCookingFlowCheck"))
            {
                File.Delete("Temp/ChickenCookingFlowCheck");
                var scene=SceneManager.GetActiveScene();
                SessionState.SetBool(Key+".keepScene",scene.name=="PrisonChapter");
                if(scene.isDirty && scene.name!="PrisonChapter"){File.WriteAllText("Logs/ChickenCookingFlowCheck.txt","BLOCKED: active scene has unsaved edits.");return;}
                SessionState.SetString(Key+".scene",scene.path);
                SessionState.SetBool(Key+".background",Application.runInBackground);
                var list=PlayerPrefs.GetString("PendingToppingList","");
                var names=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g)).toppingName).Concat(list.Split(',')).Where(n=>!string.IsNullOrEmpty(n)).Distinct();
                string economy=Path.Combine(Application.persistentDataPath,"economySave.json");
                var backup=new DailyRentCheck.Backup{values=names.SelectMany(n=>new[]{"Stock_"+n,"Pending_"+n}).Concat(new[]{"ChallengeBestDays"}).Select(k=>new DailyRentCheck.SavedKey{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray(),listExists=PlayerPrefs.HasKey("PendingToppingList"),list=list,economyPath=economy,economyExists=File.Exists(economy),economy=File.Exists(economy)?File.ReadAllText(economy):null};
                File.WriteAllText("Temp/ChickenUIReview-save-backup.json",JsonUtility.ToJson(backup));
                EditorSceneManager.SaveScene(scene,"Logs/Challenge-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                SessionState.SetBool(Key+".maximized",gameView.maximized);if(!Application.isBatchMode)gameView.maximized=true;
                if(!SessionState.GetBool(Key+".keepScene",false))EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool(Key,false)||routine==null||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup-began>600)throw new Exception("Timed out");if(routine.MoveNext())return;}
            catch(Exception e){log+="FAIL: "+e+"\n";}
            File.WriteAllText("Logs/ChickenCookingFlowCheck.txt",log);routine=null;EditorApplication.ExitPlaymode();
        }
        static void Changed(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;began=EditorApplication.timeSinceStartup;log="";routine=Run();}
            if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Restore;
        }
        static void Complete(object mission)
        {
            var property=mission.GetType().GetProperty("State");
            property.SetValue(mission,Enum.Parse(property.PropertyType,"Complete"));
        }
        static readonly string Output="../Documents/Chicken_Cooking_Flow/";
        static void Set(object o,string p,object v)=>o.GetType().GetProperty(p).SetValue(o,v);
        static void Invoke(object o,string m,params object[] args)=>o.GetType().GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(o,args);
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);
            var assembly=typeof(EditorWindow).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance").GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,new object[]{Enum.Parse(kind,"FixedResolution"),1280,720,"Chicken UI review"},null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int n=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null)-1;
            var gv=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            gv.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(gv,n);
            gv.Show();gv.Focus();
            GameModeSession.Select(false);if(!SessionState.GetBool(Key+".keepScene",false))SceneManager.LoadScene("PrisonChapter");for(int i=0;i<40;i++)yield return null;
            var m=UnityEngine.Object.FindFirstObjectByType<PrisonChickenMission>();var intro=m.prison.introduction;
            if(File.Exists("Temp/PrisonCameraStartCheck")){File.Delete("Temp/PrisonCameraStartCheck");float startCheckuntil=Time.time+100;while(intro.State!=PrisonIntroduction.Stage.Yard && Time.time<startCheckuntil)yield return null;Check(intro.State==PrisonIntroduction.Stage.Yard,"normal prison introduction hands control to player");for(int i=0;i<30;i++)yield return null;ScreenCapture.CaptureScreenshot(Output+"camera-prison-start.png");for(int i=0;i<5;i++)yield return null;var startCheckview=m.prison.player.View;var startCheckat=startCheckview.transform.position;var startCheckbody=m.prison.player.GetComponent<CharacterController>();for(int i=0;i<60;i++){startCheckbody.Move(m.prison.player.transform.forward*Time.deltaTime);yield return null;}Check(Vector3.Distance(startCheckat,startCheckview.transform.position)>.3f,"gameplay camera follows walking after introduction");Check(startCheckview.enabled && !intro.filmCamera.enabled,"film camera disabled after introduction");ScreenCapture.CaptureScreenshot(Output+"camera-prison-walk.png");for(int i=0;i<10;i++)yield return null;yield break;}
            intro.StopAllCoroutines();Invoke(intro,"SetCinematic",false);Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(intro,"Fade",0f);Set(intro,"Card","");Set(intro,"Subtitle","");intro.mugshotActor.gameObject.SetActive(false);
            if(File.Exists("Temp/KeysContinuousCheck")){File.Delete("Temp/KeysContinuousCheck");m.enabled=false;var flow=PrisonKeysFlowCheck.Run();while(flow.MoveNext())yield return null;yield break;}
            if(File.Exists("Temp/LockInteractionCheck"))
            {
                File.Delete("Temp/LockInteractionCheck");m.enabled=false;
                var lockMission=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();lockMission.previous.enabled=false;lockMission.Begin();yield return null;lockMission.FinishTheft();
                lockMission.keyGuard.enabled=false;var lockAgent=lockMission.keyGuard.GetComponent<UnityEngine.AI.NavMeshAgent>();lockAgent.isStopped=true;
                lockMission.keyGuard.transform.rotation=Quaternion.LookRotation(lockMission.keyGuard.transform.position-lockMission.keyLock.transform.position);
                var lockPlayer=m.prison.player;var lockController=lockPlayer.GetComponent<CharacterController>();lockController.enabled=false;lockPlayer.transform.position=lockMission.keyLock.transform.position-lockMission.keyLock.transform.forward*1.1f;lockController.enabled=true;
                Check(lockMission.Interact(),"warehouse interaction starts");var action=lockMission.GetComponent<PrisonLockInteraction>();
                for(int i=0;i<40;i++)yield return null;
                Vector2 KeyPoint(int index){var t=lockMission.keyLock.keyModels[index].transform;var screen=lockMission.camera.WorldToScreenPoint(t.position-t.forward*.078f);return new Vector2(screen.x/Screen.width*1920,(1-screen.y/Screen.height)*1080);}
                Check(action.Playing && lockMission.SelectingKey,"key selection active");
                ScreenCapture.CaptureScreenshot(Output+"warehouse-key-selection.png");for(int i=0;i<4;i++)yield return null;
                Vector2 lockMouse=KeyPoint(0);Mouse(lockMouse,false);yield return null;Mouse(lockMouse,true);yield return null;
                for(float wait=0;wait<1.1f;wait+=Time.deltaTime){Mouse(lockMouse,false);yield return null;}
                Check(action.State==PrisonLockInteraction.Phase.Turn,"selected key inserted with animation");
                lockMouse=KeyPoint(0);Mouse(lockMouse,false);yield return null;Mouse(lockMouse,true);yield return null;
                for(int i=0;i<20;i++){lockMouse.x+=4;Mouse(lockMouse,true);yield return null;}
                Mouse(lockMouse,false);for(int i=0;i<45;i++)yield return null;
                Check(action.State==PrisonLockInteraction.Phase.Choose && lockMission.State==PrisonKeysMission.Stage.Unlock,"wrong key rejects and returns to selection");
                lockMouse=KeyPoint(lockMission.keyLock.correctKey);Mouse(lockMouse,false);yield return null;Mouse(lockMouse,true);yield return null;
                lockMission.keyGuard.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(lockPlayer.transform.position-lockMission.keyGuard.transform.position,Vector3.up));yield return null;
                Check(action.GuardLooking,"guard turning toward player gives advance warning");
                // 삽입 중 취소해도 훔친 열쇠와 미션 단계는 유지한다.
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());Mouse(lockMouse,false);yield return null;
                Check(!action.Playing && !lockMission.SelectingKey && lockPlayer.View.enabled && lockMission.State==PrisonKeysMission.Stage.Unlock,"F interrupts insertion immediately and preserves stolen keys");
                lockMission.keyGuard.transform.rotation=Quaternion.LookRotation(lockMission.keyGuard.transform.position-lockMission.keyLock.transform.position);
                Check(lockMission.Interact(),"lock resumes after hiding");for(int i=0;i<35;i++)yield return null;
                lockMouse=KeyPoint(lockMission.keyLock.correctKey);Mouse(lockMouse,false);yield return null;Mouse(lockMouse,true);yield return null;
                for(float wait=0;wait<1.1f;wait+=Time.deltaTime){Mouse(lockMouse,false);yield return null;}
                Check(action.State==PrisonLockInteraction.Phase.Turn,"correct key fully inserted (phase="+action.State+", playing="+action.Playing+", insertion="+action.Insertion+")");
                Time.timeScale=0;float lockProgress=action.Rotation;yield return null;yield return null;Check(action.Rotation==lockProgress,"pause holds lock progress");Time.timeScale=1;
                ScreenCapture.CaptureScreenshot(Output+"warehouse-key-inserted.png");for(int i=0;i<5;i++)yield return null;
                lockMouse=KeyPoint(lockMission.keyLock.correctKey);Mouse(lockMouse,false);yield return null;Mouse(lockMouse,true);yield return null;
                for(int i=0;i<70 && action.Playing;i++){lockMouse.x+=4;Mouse(lockMouse,true);yield return null;}
                Mouse(lockMouse,false);for(int i=0;i<70;i++)yield return null;
                Check(!action.Playing && lockPlayer.View.enabled && !lockMission.SelectingKey,"turn completion returns movement and camera");
                Check(lockMission.State==PrisonKeysMission.Stage.Gather && lockMission.keyLock.Opened && lockMission.supplies.All(x=>x.activeSelf),"door opens and four warehouse ingredients activate");
                yield break;
            }
            if(File.Exists("Temp/PickpocketCheck"))
            {
                File.Delete("Temp/PickpocketCheck");m.enabled=false;
                var keys=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();
                keys.previous.enabled=false;keys.Begin();yield return null;
                var theftPlayer=m.prison.player;var theftController=theftPlayer.GetComponent<CharacterController>();theftController.enabled=false;
                theftPlayer.transform.position=keys.keyGuard.transform.position-keys.keyGuard.transform.forward*1.1f;theftController.enabled=true;
                yield return null;
                Check(keys.CanSteal(),"rear approach accepted");
                Check(keys.Interact(),"F interaction starts physical theft");
                var theft=keys.GetComponent<PrisonPickpocket>();
                for(int k=0;k<40;k++)yield return null;
                Check(theft.Playing && !theftPlayer.View.enabled,"theft camera active");
                float heldProgress=theft.Progress;Time.timeScale=0;yield return null;yield return null;Check(theft.Progress==heldProgress && theft.Playing,"pause preserves theft progress");Time.timeScale=1;
                ScreenCapture.CaptureScreenshot(Output+"pickpocket-preview.png");
                for(int k=0;k<5;k++)yield return null;
                theft.End(false);yield return null;
                Check(keys.State==PrisonKeysMission.Stage.Steal && keys.keyRing.activeSelf && theftPlayer.View.enabled,"cancel restores camera and leaves keys on guard");
                keys.Interact();for(int k=0;k<30;k++)yield return null;
                Vector3 sp=keys.camera.WorldToScreenPoint(keys.keyRing.transform.position);
                Vector2 pos=new Vector2(sp.x/Screen.width*1920,(1-sp.y/Screen.height)*1080);
                Mouse(pos,false);yield return null;Mouse(pos,true);yield return null;
                for(int k=0;k<240 && theft.Playing;k++){pos.y-=2;Mouse(pos,true);yield return null;}
                Mouse(pos,false);yield return null;
                Check(keys.State==PrisonKeysMission.Stage.Unlock && !keys.keyRing.activeSelf,"slow mouse extraction succeeds and unlock stage begins");
                Check(theftPlayer.View.enabled && keys.keyGuard.enabled,"success restores theftPlayer view and guard patrol");
                keys.TheftCaught();for(int k=0;k<100;k++)yield return null;
                Check(!keys.Failing && keys.State==PrisonKeysMission.Stage.Steal && keys.keyRing.activeSelf,"checkpoint resets stolen keys and mission");
                theftController.enabled=false;theftPlayer.transform.position=keys.keyGuard.transform.position-keys.keyGuard.transform.forward*1.1f;theftController.enabled=true;
                Check(keys.Interact(),"retry allows another theft");for(int k=0;k<30;k++)yield return null;
                sp=keys.camera.WorldToScreenPoint(keys.keyRing.transform.position);pos=new Vector2(sp.x/Screen.width*1920,(1-sp.y/Screen.height)*1080);
                Mouse(pos,false);yield return null;Mouse(pos,true);yield return null;
                for(int k=0;k<8;k++){pos.x+=k%2==0?40:-40;Mouse(pos,true);yield return null;}
                Check(theft.Noise>=.6f,"rapid mouse movement raises guard warning");
                for(int k=0;k<180 && theft.Playing;k++){pos.x+=k%2==0?40:-40;Mouse(pos,true);yield return null;}
                Mouse(pos,false);for(int k=0;k<100;k++)yield return null;
                Check(!theft.Playing && !keys.Failing && keys.State==PrisonKeysMission.Stage.Steal,"ignoring noise warning fails and resets checkpoint");
                Check(keys.keyLock!=null && keys.supplies.Length==4,"existing warehouse lock and four ingredients retained");
                yield break;
            }
            if(File.Exists("Temp/DenseKitchenPreview"))
            {
                File.Delete("Temp/DenseKitchenPreview");m.prison.player.View.enabled=false;m.camera.enabled=true;
                m.camera.fieldOfView=78;m.camera.transform.position=new Vector3(30,2.25f,-21.2f);m.camera.transform.LookAt(new Vector3(31,.1f,-28.5f));
                for(int k=0;k<30;k++)yield return null;
                ScreenCapture.CaptureScreenshot(Output+"kitchen-dense-game.png");
                for(int k=0;k<8;k++)yield return null;
                Check(true,"dense kitchen runtime preview captured");yield break;
            }
            if(File.Exists("Temp/ChickenMusicLifetimeCheck"))
            {
                File.Delete("Temp/ChickenMusicLifetimeCheck");
                yield return null;yield return null;
                var audio=PrisonCookingAudio.For(PrisonChickenQuest.Current);
                Check(m.State==PrisonChickenMission.Stage.Kitchen && audio.MusicPlaying,"music starts on mission acceptance before station use");
                m.enabled=false; // Isolate music phase checks from combat/arrival triggers.
                foreach(var phase in new[]{PrisonChickenMission.Stage.Cook,PrisonChickenMission.Stage.Carry,PrisonChickenMission.Stage.Fight,PrisonChickenMission.Stage.Deliver})
                {
                    Set(m,"State",phase);yield return null;yield return null;
                    Check(audio.MusicPlaying,"music continues during "+phase);
                }
                Check(audio.MusicStartCount==1,"one uninterrupted music playback across mission phases");
                Time.timeScale=0;yield return null;yield return null;Check(!audio.MusicPlaying,"music pauses with game");
                Time.timeScale=1;yield return null;yield return null;Check(audio.MusicPlaying && audio.MusicStartCount==1,"music resumes without restarting");
                Set(m,"Failing",true);yield return null;yield return null;Check(!audio.MusicPlaying,"music stops on mission failure");
                Set(m,"Failing",false);yield return null;yield return null;Check(audio.MusicPlaying,"music restarts after retry");
                Set(m,"State",PrisonChickenMission.Stage.Conversation);yield return null;yield return null;Check(!audio.MusicPlaying,"music stops on handover dialogue");
                Set(m,"State",PrisonChickenMission.Stage.Complete);yield return null;Check(!audio.MusicPlaying,"music remains stopped after mission complete");
                yield break;
            }
            log+="INITIAL: "+m.State+" cookingEnabled="+m.cooking.enabled+" collected="+m.Collected[0]+"\n";
            if(File.Exists("Temp/ChefSleepPreview"))
            {
                File.Delete("Temp/ChefSleepPreview");
                var sleeper=m.chefReaction;m.prison.player.View.enabled=false;m.camera.enabled=true;sleeper.playerView=m.camera.transform;
                m.camera.transform.position=sleeper.head.position+new Vector3(1.5f,1,1.5f);m.camera.transform.LookAt(sleeper.head.position+Vector3.up*.35f);
                for(int frame=0;frame<20;frame++)yield return null;
                ScreenCapture.CaptureScreenshot(Output+"chef-sleep-z-final.png");
                Check(sleeper.GetComponentsInChildren<TextMesh>().Any(t=>t.text=="Z"&&t.gameObject.activeInHierarchy),"final-sized sleep Z visible");
                for(int frame=0;frame<8;frame++)yield return null;
                yield break;
            }
Check(!m.cooking.rawChicken.activeSelf,"board chicken hidden before ingredients");
            m.ResetKitchen();
            Check(!m.cooking.rawChicken.activeSelf,"board chicken hidden on recipe reset");
            var pickupPlayer=m.prison.player;var pickupCC=pickupPlayer.GetComponent<CharacterController>();
            pickupCC.enabled=false;pickupPlayer.transform.position=m.ingredients[0].transform.position+Vector3.back;pickupCC.enabled=true;
            Check(m.Use(m.ingredients[0]),"raw chicken pickup interaction accepted");
            Check(m.cooking.rawChicken.activeSelf,"board chicken appears on raw chicken pickup");
            m.ResetKitchen();
            Check(!m.cooking.rawChicken.activeSelf,"retry hides board chicken again");
            Check(m.Use(m.ingredients[0]),"raw chicken can be collected again after retry");
            for(int i=0;i<m.Collected.Length;i++)m.Collected[i]=true;
            m.Say("민기: 주방에 들어가 치킨을 만들자.",8);
            var startHud=UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>();
            float acceptedAt=Time.time;float startLimit=Time.time+20;bool sawStart=false;
            while(Time.time<startLimit)
            {
                if(startHud && startHud.DisplayedTitle=="갓 튀긴 치킨" && startHud.GetComponent<CanvasGroup>().alpha>.95f && startHud.GetComponentsInChildren<Text>().Any(t=>t.text=="미션 시작"))
                {sawStart=true;ScreenCapture.CaptureScreenshot(Output+"prison-mission-start.png");break;}
                yield return null;
            }
            Check(sawStart,"prison chicken start banner");
            Check(Time.time-acceptedAt<2.6f,"start banner within 2.6s including preceding completion card, despite walking dialogue");
            log+="START LATENCY: "+(Time.time-acceptedAt).ToString("F2")+"s\n";
            for(int i=0;i<8;i++)yield return null;
            var player=m.prison.player;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=m.station.transform.position+Vector3.back;cc.enabled=true;
            Invoke(m,"Lock",true,m.cookView);m.cooking.Begin();
            var q=PrisonChickenQuest.Current;Check(q,"Woong cooking present");
            Check(PrisonCookingSounds.Current && PrisonCookingSounds.Current.snore && PrisonCookingSounds.Current.chop,"cooking sound assets assigned");
            var cookingAudio=PrisonCookingAudio.For(q);
            var phone=player.GetComponentInChildren<PhoneController>(true);
            Check(phone,"phone controller present");phone.OpenPhone();Check(!phone.IsPhoneOpen(),"phone Open blocked in prison");
            phone.TogglePhone();Check(!phone.IsPhoneOpen(),"phone Toggle blocked in prison");
            var market=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Stocks.StockMarket>();
            Check(market&&!market.MarketOpen,"prison stock trading closed");
            var view=UnityEngine.Object.FindFirstObjectByType<PrisonSharedPresentation>();
            yield return null;yield return null;
            Check(cookingAudio.MusicPlaying,"cooking music begins at cooking station");
            int previous=-1;float started=Time.time, cutFraction=0;bool press=false;int sauceIndex=0;
            var visited=new System.Collections.Generic.HashSet<int>();
            var captured=new System.Collections.Generic.HashSet<int>();
            var heard=new System.Collections.Generic.HashSet<int>();
            while(q.IsPlayingCustomCooking && Time.time-started<150)
            {
                int stage=(int)q.currentStep;
                if(cookingAudio.LoopPlaying && !heard.Contains(stage))
                {
                    heard.Add(stage);
                    if(stage!=6)
                    {
                        Mouse(new Vector2(960,540),false);
                        UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                        float releasedUntil=Time.time+.2f;while(Time.time<releasedUntil)yield return null;
                        Check(!cookingAudio.LoopPlaying,"action loop stops when input stops in stage "+(stage+1));
                    }
                    else
                    {
                        Time.timeScale=0;yield return null;yield return null;
                        Check(!cookingAudio.LoopPlaying,"frying sound pauses with game");Check(!cookingAudio.MusicPlaying,"cooking music pauses with game");Time.timeScale=1;
                    }
                }
                if(stage!=previous){log+="ENTER: "+q.currentStep+" at "+(Time.time-started).ToString("F1")+"s\n";previous=stage;cutFraction=0;press=false;visited.Add(stage);}
                if(view.subtitle && !string.IsNullOrEmpty(view.subtitle.text))throw new Exception("Cooking subtitle overlap: "+view.subtitle.text);
                Vector2 cursor=new Vector2(960,540);bool hold=false;
                UnityEngine.InputSystem.Key? key=null;
                if(stage==0)
                {
                    var a=Field<Vector2>(q,"cutStartGui");var b=Field<Vector2>(q,"cutEndGui");
                    if(cutFraction>1){cutFraction=0;hold=false;}
                    else {cursor=Vector2.Lerp(a,b,cutFraction);hold=true;cutFraction+=.1f;}
                }
                else if(stage==1||stage==2||stage==4){cursor=new Vector2(560+800*Field<float>(q,"bowlX"),740);hold=true;}
                else if(stage==3){float angle=Time.time*12;cursor=new Vector2(960+Mathf.Cos(angle)*220,540+Mathf.Sin(angle)*180);}
                else if(stage==5)
                {
                    int slot=Field<int>(q,"currentDialSlot");var numbers=Field<int[][]>(q,"safeDials");var indices=Field<int[]>(q,"dialIndices");
                    int target=new[]{1,8,0}[slot];
                    if(!press)key=numbers[slot][indices[slot]]==target?UnityEngine.InputSystem.Key.Space:UnityEngine.InputSystem.Key.D;
                    press=!press;
                }
                else if(stage==6)
                {
                    float value=Field<float>(q,"fryCursor");float zone=Field<float>(q,"fryZoneCenter");float half=Field<float>(q,"fryZoneHalfWidth");
                    if(!press&&Mathf.Abs(value-zone)<half*.4f){key=UnityEngine.InputSystem.Key.Space;press=true;}else press=false;
                }
                else if(stage==7)
                {
                    int cell=(sauceIndex++/3)%84;
                    cursor=new Vector2(410+(cell%14+.5f)*1100/14,160+(cell/14+.5f)*490/6);hold=true;
                }
                Mouse(cursor,hold);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,key.HasValue?new UnityEngine.InputSystem.LowLevel.KeyboardState(key.Value):new UnityEngine.InputSystem.LowLevel.KeyboardState());
                float elapsed=Time.time-started;
                if(!captured.Contains(stage) && (stage!=0||Field<int>(q,"cutCurrentCount")>0))
                {
                    ScreenCapture.CaptureScreenshot(Output+"flow-"+(stage+1)+".png");captured.Add(stage);
                }
                yield return null;
            }
            Mouse(new Vector2(960,540),false);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            Check(visited.Count==8,"all 8 stages reached using inputs only");
            Check(cookingAudio.ChopCount==8,"exactly one chop sound for each of eight cuts");
            Check(cookingAudio.SliceCount==8,"one slice layer accompanies each chop");
            foreach(int step in new[]{1,2,3,4,6,7})Check(heard.Contains(step),"action sound played in stage "+(step+1));
            Check(!cookingAudio.LoopPlaying,"action loop stopped on cooking completion");
            yield return null;Check(cookingAudio.MusicPlaying,"mission music continues after cooking completion");
            Check(q.currentStep==PrisonChickenQuest.CustomStep.Done,"cooking completed without stage overrides");
            Check(m.State==PrisonChickenMission.Stage.Carry,"cooking returns to mission with chicken");
            for(int i=0;i<6;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Output+"plated-chicken-carry.png");
            for(int i=0;i<8;i++)yield return null;
            var hud=UnityEngine.Object.FindFirstObjectByType<MissionMilestoneHUD>();Check(hud,"shared milestone HUD installed");
            m.Say("",.01f);
            for(int i=0;i<50;i++)yield return null;
            // Completion UI is a separate state-fixture test, not a claim to play the delivery fight.
            Set(m,"State",PrisonChickenMission.Stage.Complete);
            float completedAt=Time.time;float limit=Time.time+25;bool sawComplete=false;
            while(Time.time<limit){if(hud.DisplayedTitle=="갓 튀긴 치킨"&&hud.GetComponent<CanvasGroup>().alpha>.95f&&hud.GetComponentsInChildren<Text>().Any(t=>t.text=="미션 완료")){sawComplete=true;ScreenCapture.CaptureScreenshot(Output+"prison-mission-complete.png");break;}yield return null;}
            Check(sawComplete,"prison completion banner state fixture");
            Check(Time.time-completedAt<.6f,"completion banner within 0.6s despite next mission dialogue");
            log+="COMPLETE LATENCY: "+(Time.time-completedAt).ToString("F2")+"s\n";
            for(int i=0;i<8;i++)yield return null;
            var chef=m.chefReaction;Check(chef && chef.GetComponent<PrisonChefSleep>(),"chef sleep effect installed");
            Check(chef.IsSleeping,"chef initially asleep");
            player.View.enabled=false;m.camera.enabled=true;chef.playerView=m.camera.transform;
            m.camera.transform.position=chef.head.position+new Vector3(1.5f,1.0f,1.5f);m.camera.transform.LookAt(chef.head.position+Vector3.up*.35f);
            for(int i=0;i<8;i++)yield return null;
            Check(chef.GetComponentsInChildren<TextMesh>().Any(t=>t.text=="Z"&&t.gameObject.activeInHierarchy),"sleeping Z effect visible above chef");
            ScreenCapture.CaptureScreenshot(Output+"chef-sleep-z.png");
            for(int i=0;i<8;i++)yield return null;
            Set(m,"State",PrisonChickenMission.Stage.Cook);m.cooking.Begin();
            yield return null;yield return null;Check(cookingAudio.MusicPlaying,"music restarts for cooking retry");
            var wake=chef.Wake(m.camera);wake.MoveNext();yield return null;yield return null;
            Check(!cookingAudio.MusicPlaying,"music stops immediately when chef wakes");
            m.cooking.Close();Set(m,"State",PrisonChickenMission.Stage.Complete);
            Check(!chef.IsSleeping,"wake disables sleeping state");
            Check(!chef.GetComponentsInChildren<TextMesh>().Any(t=>t.text=="Z" && t.gameObject.activeInHierarchy),"Z letters hidden on waking");
            chef.Sleep();
            var prisonScene=SceneManager.GetActiveScene();
            var outside=SceneManager.CreateScene("Prison feature policy regression");SceneManager.SetActiveScene(outside);
            Check(!PrisonFeaturePolicy.Active,"prison restriction released outside prison scene");
            phone.OpenPhone();Check(phone.IsPhoneOpen(),"phone opens outside prison");phone.ClosePhone();
            SceneManager.SetActiveScene(prisonScene);SceneManager.UnloadSceneAsync(outside);
            log+="Cooking played with automated mouse/keyboard events. Mission completion banner checked separately using a state fixture.\n";
        }
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(target);
        static void Mouse(Vector2 gui,bool hold)
        {
            var state=new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(gui.x/1920*Screen.width,(1-gui.y/1080)*Screen.height)};
            if(hold)state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,state);
        }
        static void Shot(Camera camera,Vector3 position,Vector3 target,string name){
            var p=camera.transform.position;var q=camera.transform.rotation;
            camera.transform.position=position;camera.transform.LookAt(target);Capture(camera,name);
            camera.transform.SetPositionAndRotation(p,q);
        }
        static void Capture(Camera camera,string name){
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.001f;}
            Canvas.ForceUpdateCanvases();
            var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1720,966,24);rt.antiAliasing=4;camera.targetTexture=rt;camera.aspect=1720f/966;
            try{camera.Render();RenderTexture.active=rt;var t=new Texture2D(1720,966,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1720,966),0,0);t.Apply();File.WriteAllBytes(Output+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);foreach(var c in canvases){if(c){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}}}
        }
        static void Restore()
        {
            try{
                var b=JsonUtility.FromJson<DailyRentCheck.Backup>(File.ReadAllText("Temp/ChickenUIReview-save-backup.json"));
                foreach(var v in b.values){if(v.exists)PlayerPrefs.SetInt(v.key,v.value);else PlayerPrefs.DeleteKey(v.key);}
                if(b.listExists)PlayerPrefs.SetString("PendingToppingList",b.list);else PlayerPrefs.DeleteKey("PendingToppingList");PlayerPrefs.Save();
                if(b.economyExists)File.WriteAllText(b.economyPath,b.economy);else if(File.Exists(b.economyPath))File.Delete(b.economyPath);
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).maximized=SessionState.GetBool(Key+".maximized",false);
                if(!SessionState.GetBool(Key+".keepScene",false))EditorSceneManager.OpenScene(SessionState.GetString(Key+".scene","Assets/Scenes/Lobby.unity"));
            }finally{Application.runInBackground=SessionState.GetBool(Key+".background",false);SessionState.SetBool(Key,false);Time.timeScale=1;if(Application.isBatchMode)EditorApplication.Exit(log.Contains("FAIL:")?1:0);}
        }
    }
}
#endif
