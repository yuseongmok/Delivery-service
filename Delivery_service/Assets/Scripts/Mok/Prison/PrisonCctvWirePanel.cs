using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonCctvWirePanel:MonoBehaviour
    {
        public PrisonCctvMission mission;
        public PrisonCctvWireText dialogue;
        [Range(0,2)] public int correctWire=2;
        public Transform hinge,viewpoint,checkpoint,cutter,leftJaw,rightJaw;
        public Collider[] wireTargets;
        public GameObject[] wholeWires,cutWires;
        public Camera camera;
        public AudioSource sound;
        public AudioClip openSound,cutSound,alarmSound;
        public Light warning;
        public TextMesh diagram;
        public bool Playing{get;private set;}
        public bool Ready=>Playing&&!animating;
        public int Selected{get;private set;}=-1;
        public static readonly string[] Names={"빨강 · R1","파랑 · B2","초록 · C3"};
        PlayerCombat Player=>mission.office.previous.previous.prison.player;
        bool animating,previousView,previousListener;
        Quaternion closed;string feedback="";
        void Awake(){closed=hinge.localRotation;camera.enabled=false;camera.GetComponent<AudioListener>().enabled=false;warning.enabled=false;cutter.gameObject.SetActive(false);RefreshDiagram();}
        public void RefreshDiagram(){diagram.characterSize=.010f;diagram.text="영상 전원\n"+Names[correctWire].Split('·')[1].Trim()+"\n\n다른 선: 경보";}
        public bool Begin(){if(Playing||!mission.BeginWireInteraction())return false;Playing=true;animating=true;Selected=-1;sound.volume=1;feedback="";RefreshDiagram();previousView=Player.View.enabled;previousListener=Player.View.GetComponent<AudioListener>().enabled;camera.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);Player.View.enabled=false;Player.View.GetComponent<AudioListener>().enabled=false;camera.enabled=true;camera.GetComponent<AudioListener>().enabled=previousListener;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;StartCoroutine(Open());return true;}
        IEnumerator Open(){if(openSound)sound.PlayOneShot(openSound,.35f);var p=camera.transform.position;var q=camera.transform.rotation;for(float t=0;t<.65f;t+=Time.deltaTime){float a=Mathf.SmoothStep(0,1,t/.65f);hinge.localRotation=closed*Quaternion.Euler(0,-115*a,0);camera.transform.SetPositionAndRotation(Vector3.Lerp(p,viewpoint.position,a),Quaternion.Slerp(q,viewpoint.rotation,a));yield return null;}camera.transform.SetPositionAndRotation(viewpoint.position,viewpoint.rotation);hinge.localRotation=closed*Quaternion.Euler(0,-115,0);animating=false;}
        void Update(){if(!Playing||Time.timeScale<=0)return;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(!Ready)return;if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame){Cancel();return;}if(Mouse.current==null)return;Selected=-1;var ray=camera.ScreenPointToRay(Mouse.current.position.ReadValue());if(Physics.Raycast(ray,out var hit,4,~0,QueryTriggerInteraction.Ignore)){for(int i=0;i<wireTargets.Length;i++)if(hit.collider==wireTargets[i])Selected=i;}if(Selected>=0&&Mouse.current.leftButton.wasPressedThisFrame)Choose(Selected);}
        public bool Choose(int wire){if(!Ready||Time.timeScale<=0||wire<0||wire>=wholeWires.Length)return false;Selected=wire;animating=true;StartCoroutine(Cut(wire));return true;}
        IEnumerator Cut(int index){
            cutter.gameObject.SetActive(true);var end=wireTargets[index].transform.position+transform.forward*.05f;var from=end-transform.up*.35f+transform.forward*.16f;cutter.rotation=transform.rotation;
            for(float t=0;t<.35f;t+=Time.deltaTime){cutter.position=Vector3.Lerp(from,end,Mathf.SmoothStep(0,1,t/.35f));yield return null;}
            cutter.position=end;
            for(float t=0;t<.2f;t+=Time.deltaTime){float a=1-Mathf.Clamp01(t/.2f);leftJaw.localRotation=Quaternion.Euler(0,0,18*a);rightJaw.localRotation=Quaternion.Euler(0,0,-18*a);yield return null;}
            if(cutSound)sound.PlayOneShot(cutSound,.55f);wholeWires[index].SetActive(false);cutWires[index].SetActive(true);bool success=index==correctWire;feedback=success?dialogue.success:dialogue.failed;
            yield return new WaitForSeconds(.25f);cutter.gameObject.SetActive(false);
            if(!success){warning.enabled=true;if(alarmSound){sound.clip=alarmSound;sound.loop=true;sound.volume=.4f;sound.Play();}for(float t=0;t<1.4f;t+=Time.deltaTime){warning.intensity=3+Mathf.PingPong(t*12,8);yield return null;}}
            else yield return new WaitForSeconds(.4f);
            RestoreView();mission.ResolveWire(success);
        }
        public void Cancel(){if(!Ready)return;StopAllCoroutines();RestoreView();hinge.localRotation=closed;mission.EndWireInteraction();}
        public void Abort(){if(!Playing)return;StopAllCoroutines();RestoreView();mission.EndWireInteraction();}
        void RestoreView(){if(!Playing)return;sound.Stop();sound.loop=false;warning.enabled=false;cutter.gameObject.SetActive(false);camera.enabled=false;camera.GetComponent<AudioListener>().enabled=false;Player.View.enabled=previousView;Player.View.GetComponent<AudioListener>().enabled=previousListener;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Playing=false;animating=false;Selected=-1;}
        public void ResetPanel(){hinge.localRotation=closed;for(int i=0;i<wholeWires.Length;i++){wholeWires[i].SetActive(true);cutWires[i].SetActive(false);}warning.enabled=false;}
        void OnDisable(){if(Playing){RestoreView();mission.EndWireInteraction();}}
        void OnGUI(){if(!Playing||Time.timeScale<=0)return;float scale=Screen.height/1080f;var old=GUI.color;var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(28*scale),alignment=TextAnchor.MiddleCenter,wordWrap=true};GUI.color=new Color(.03f,.045f,.055f,.93f);GUI.DrawTexture(new Rect(Screen.width*.2f,Screen.height*.81f,Screen.width*.6f,Screen.height*.15f),Texture2D.whiteTexture);GUI.color=new Color(1,.82f,.48f);GUI.Label(new Rect(0,Screen.height*.06f,Screen.width,50*scale),dialogue.title,style);GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.21f,Screen.height*.82f,Screen.width*.58f,65*scale),Ready?(Selected>=0?Names[Selected]+" 전선 자르기":dialogue.hint):(string.IsNullOrEmpty(feedback)?"배전판 조작 중…":feedback),style);style.fontSize=Mathf.RoundToInt(21*scale);GUI.Label(new Rect(0,Screen.height*.91f,Screen.width,35*scale),dialogue.controls,style);GUI.color=old;}
    }
}
