#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DeliveryService.Mok.Editor
{
    // 편집기 전용 실제 충돌 이동 검사. 위치 순간이동이나 임무 단계 변경은 하지 않는다.
    [InitializeOnLoad]
    public static class PrisonWalkthroughCheck
    {
        static PrisonWalkthroughCheck(){EditorApplication.update+=Tick;}
        static void Tick(){
            if(!EditorApplication.isPlaying||!PrisonMission.Current)return;
            if(File.Exists("Temp/Prison-walk-start")){
                try{File.Delete("Temp/Prison-walk-start");}catch(IOException){return;}
                if(!UnityEngine.Object.FindFirstObjectByType<PrisonWalkthroughDriver>())PrisonMission.Current.player.gameObject.AddComponent<PrisonWalkthroughDriver>();
            }
        }
    }
    public class PrisonWalkthroughDriver:MonoBehaviour
    {
        PrisonMission mission; CharacterController body; Camera view;
        Vector3[] corners;int corner;float stamp,progressAt;Vector3 lastProgress;
        Vector3? aim;bool release;bool crouch;bool moving;float speed=4;
        string action="idle";
        void Start(){mission=PrisonMission.Current;body=GetComponent<CharacterController>();view=mission.player.View;foreach(var c in GetComponentsInChildren<CameraMove>())c.isControllable=false;Application.runInBackground=true;}
        void Update(){
            if(release){InputSystem.QueueStateEvent(Keyboard.current,crouch?new KeyboardState(Key.LeftCtrl):new KeyboardState());release=false;}
            if(File.Exists("Temp/Prison-walk-command")){
                var args=File.ReadAllText("Temp/Prison-walk-command").Trim().Split(' ');File.Delete("Temp/Prison-walk-command");
                action=string.Join(" ",args);aim=null;
                if(args[0]=="walk"){
                    var target=new Vector3(float.Parse(args[1]),0,float.Parse(args[2]));var path=new NavMeshPath();
                    if(NavMesh.SamplePosition(new Vector3(transform.position.x,body.bounds.min.y+.1f,transform.position.z),out var start,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(target,out var end,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){corners=path.corners;corner=1;moving=true;lastProgress=transform.position;progressAt=Time.time;}
                    else action="NO PATH "+target;
                }
                if(args[0]=="look"||args[0]=="use"){
                    moving=false;aim=new Vector3(float.Parse(args[1]),float.Parse(args[2]),float.Parse(args[3]));
                    if(args[0]=="use")Invoke(nameof(PressF),.25f);
                }
                if(args[0]=="crouch"){crouch=args[1]=="1";InputSystem.QueueStateEvent(Keyboard.current,crouch?new KeyboardState(Key.LeftCtrl):new KeyboardState());}
                if(args[0]=="stop")moving=false;
                if(args[0]=="cheat"){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.LeftCtrl,Key.LeftShift,Key.F12));release=true;}
            }
            if(moving&&!mission.Failing){
                var delta=corners[corner]-transform.position;delta.y=0;
                if(delta.magnitude<.28f){if(++corner>=corners.Length){moving=false;action="ARRIVED";} }
                else{
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),240*Time.deltaTime);
                    // 이동은 실제 CharacterController 충돌을 거치며 카트는 기존 Rigidbody 제어를 그대로 사용한다.
                    body.Move(delta.normalized*speed*(crouch?.5f:1)*Time.deltaTime);
                    if(Vector3.Distance(lastProgress,transform.position)>.3f){lastProgress=transform.position;progressAt=Time.time;}
                    else if(Time.time-progressAt>4){moving=false;action="BLOCKED toward "+corners[corner];}
                }
            }
            if(Time.time>stamp){stamp=Time.time+.5f;Physics.Raycast(view.transform.position,view.transform.forward,out var hit,3,~0,QueryTriggerInteraction.Ignore);
                File.WriteAllText("Logs/Prison-walk-state.txt",$"{action}\nphase={mission.State} served={mission.Delivered} busy={mission.Busy} failing={mission.Failing}\nplayer={transform.position} camera={view.transform.position} cart={mission.cart.transform.position} kinematic={mission.cart.controller.cartRigidbody.isKinematic}\nray={(hit.collider?hit.collider.name:"none")} canAct={mission.player.CanAct}\nsubtitle={mission.Subtitle}\n");}
        }
        void PressF(){InputSystem.QueueStateEvent(Keyboard.current,crouch?new KeyboardState(Key.LeftCtrl,Key.F):new KeyboardState(Key.F));release=true;}
        void LateUpdate(){if(aim.HasValue){var direction=aim.Value-view.transform.position;var flat=Vector3.ProjectOnPlane(direction,Vector3.up);if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(flat);view.transform.rotation=Quaternion.LookRotation(direction);}else if(moving)view.transform.localRotation=Quaternion.identity;}
    }
}
#endif


