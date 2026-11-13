#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    // 씬 연결과 실패 복구를 플레이 모드에서 검증한다. 입력 조작 검증과 구분해 기록한다.
    [InitializeOnLoad]
    public static class PrisonChickenCheck
    {
        const string Log="Logs/Prison-chicken-check.txt";
        static bool running;
        static int stage;
        static float wait;
        static bool overflow;
        static readonly System.Collections.Generic.HashSet<ChickenCooking.Step> seen=new();
        static double deadline;
        static PrisonChickenCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ChickenCheck",false)){SessionState.SetBool("ChickenCheck",false);running=true;stage=0;deadline=EditorApplication.timeSinceStartup+100;}};}
        public static void Run(){if(!PrisonChapterCheck.Active)EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Play-mode mission state / checkpoint validation\n");SessionState.SetBool("ChickenCheck",true);EditorApplication.EnterPlaymode();}
        static void Assert(bool okay,string message){File.AppendAllText(Log,(okay?"PASS ":"FAIL ")+message+"\n");if(!okay)throw new Exception(message);}
        static void Place(PrisonChickenMission m,Vector3 position){var cc=m.prison.player.GetComponent<CharacterController>();cc.enabled=false;m.prison.player.transform.position=position;cc.enabled=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        static void Set(object owner,string property,object value){owner.GetType().GetProperty(property).SetValue(owner,value);}
        public static void ContinueChapter(){File.WriteAllText(Log,"Continuous chapter test; previous mission state retained\n");running=true;stage=1;wait=Time.time+.5f;deadline=EditorApplication.timeSinceStartup+200;}
        static void Tick()
        {
            if(!running||!EditorApplication.isPlaying)return;
            try{
                var m=Object.FindFirstObjectByType<PrisonChickenMission>();if(!m)return;
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout stage "+stage);
                if(stage==0){wait=Time.time+2;stage=1;return;}
                if(Time.time<wait)return;
                if(stage==1){if(PrisonChapterCheck.Active){m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.1f;stage=2;return;}
                    var intro=m.prison.introduction;intro.StopAllCoroutines();typeof(PrisonIntroduction).GetMethod("SetCinematic",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(intro,new object[]{false});Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(intro,"Fade",0f);Set(intro,"Card","");intro.mugshotActor.gameObject.SetActive(false);
                    m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.1f;
                    stage=2;return;
                }
                if(stage==2){
                    Assert(m.State==PrisonChickenMission.Stage.Kitchen,"intro completion unlocks kitchen");
                    Place(m,m.checkpoint.position);Assert(m.Use(m.entrance),"kitchen entry interaction");
                    foreach(var ingredient in m.ingredients){var path=new NavMeshPath();NavMesh.SamplePosition(m.checkpoint.position,out var from,3,NavMesh.AllAreas);Assert(NavMesh.SamplePosition(new Vector3(ingredient.transform.position.x,0,ingredient.transform.position.z)+Vector3.forward,out var to,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"route to "+ingredient.name);Place(m,ingredient.transform.position+new Vector3(0,.1f,-1.5f));Assert(m.Use(ingredient),"collect "+ingredient.name);}
                    Place(m,m.station.transform.position+new Vector3(0,.1f,-1.5f));Assert(m.Use(m.station),"open cooking station");Assert(m.cooking.Open&&!m.prison.player.CanAct,"cooking blocks attacks and movement");Capture(m.camera,"Logs/Prison-chicken-cooking.png");m.LeaveCooking();Assert(m.prison.player.CanAct,"leaving cooking restores controls");
                    stage=3;wait=Time.time+1.1f;return;
                }
                if(stage>=3&&stage<=5){m.MakeNoise(stage==3);stage++;wait=Time.time+3.5f;return;}
                if(stage==6){if(m.Failing)return;Assert(m.Noise==0&&m.State==PrisonChickenMission.Stage.Cook,"three noises retry kitchen");Assert(!m.Collected[0]&&m.Remaining>350&&m.prison.player.CanAct,"retry resets supplies timer and controls");m.RemainingForCheck();stage=7;wait=Time.time+5;return;}
                if(stage==7){if(m.Failing)return;Assert(m.State==PrisonChickenMission.Stage.Cook&&m.Remaining>350,"timeout retries");foreach(var p in m.ingredients){Place(m,p.transform.position+Vector3.back);m.Use(p);}Place(m,m.station.transform.position+Vector3.back);Assert(m.Use(m.station),"reopen after retry");overflow=true;seen.Clear();stage=11;return;}
                if(stage==11){if(m.State==PrisonChickenMission.Stage.Carry){Assert(seen.Count==8,"all cooking stages completed with queued mouse/keyboard inputs");Assert(m.prison.player.CanAct,"cooking input completion restores controls");Release();Place(m,m.ambushPoint.position+Vector3.up);stage=8;wait=Time.time+1;}else DriveCooking(m);return;}
                if(stage==8){Assert(m.State==PrisonChickenMission.Stage.Fight,"delivery route starts ambush");foreach(var e in m.enemies){Assert(e.combat&&e.combat.Health>0&&e.combat.GetComponent<NavMeshAgent>().isOnNavMesh,"active melee ambusher on navigation");e.combat.ReceiveBullet(m.prison.player,Vector3.forward,1000);}stage=9;wait=Time.time+1;return;}
                if(stage==9){Assert(m.State==PrisonChickenMission.Stage.Deliver,"defeating ambushers unlocks delivery");Place(m,m.prison.introduction.informant.position+Vector3.back*2);m.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonChickenMission).GetMethod("Deliver",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null));stage=10;wait=Time.time+3;return;}
                if(stage==10){Assert(m.State==PrisonChickenMission.Stage.Complete&&m.prison.player.CanAct,"information conversation completes and restores controls");Capture(m.prison.player.View,"Logs/Prison-chicken-delivered.png");File.AppendAllText(Log,"NOTE real input events drive cooking; travel seeded for checkpoint validation\n");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
            }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
        }
        static void Release(){InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());}
        static void DriveCooking(PrisonChickenMission mission){
            var c=mission.cooking;
            if(!c.Open){Release();Assert(overflow&&!mission.Collected[1],"overfilling flour discards ingredient and leaves station");overflow=false;Place(mission,mission.ingredients[1].transform.position+Vector3.back);Assert(mission.Use(mission.ingredients[1]),"recollect spilled flour");Place(mission,mission.station.transform.position+Vector3.back);Assert(mission.Use(mission.station),"resume same recipe after spill");return;}
            seen.Add(c.Current);
            var mouse=new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)};
            bool press=false,space=false;
            switch(c.Current){
                case ChickenCooking.Step.Cut:mouse.position=c.CutScreenPosition;press=Mathf.Repeat(Time.time,.6f)<.15f;break;
                case ChickenCooking.Step.Flour:press=overflow||c.Amount<.98f;break;
                case ChickenCooking.Step.Water:case ChickenCooking.Step.Oil:press=c.Amount<.98f;break;
                case ChickenCooking.Step.Mix:float angle=Time.time*4;mouse.position+=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*140;break;
                case ChickenCooking.Step.Heat:press=Mathf.Abs(c.Temperature-180)<1;break;
                case ChickenCooking.Step.Fry:space=Mathf.Abs(c.Needle-.5f)<.05f;break;
                case ChickenCooking.Step.Sauce:press=true;break;
            }
            if(press)mouse=mouse.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(Mouse.current,mouse);InputSystem.QueueStateEvent(Keyboard.current,space?new KeyboardState(Key.Space):new KeyboardState());
        }
        static void RemainingForCheck(this PrisonChickenMission mission){Set(mission,"Remaining",.05f);}
        public static void Capture(Camera camera,string path){var previous=camera.targetTexture;var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);}
    }
}
#endif
