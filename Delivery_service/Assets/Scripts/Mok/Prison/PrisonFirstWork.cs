using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonFirstWork : MonoBehaviour
    {
        public Transform station, approach, axe;
        public Vector3 bladeContact;
        public GameObject[] boards;
        public int Completed { get; private set; }
        public int Required => boards.Length;
        public bool Returning { get; private set; }
        public bool Sleeping { get; private set; }
        public bool Working { get; private set; }
        public bool Swinging => swinging;
        public bool ReadyToSleep { get; set; }
        public Vector3 BladeWorld => axe.TransformPoint(bladeContact);
        PrisonIntroduction intro;
        PrisonWoodFeedback feedback;
        float strength;
        bool swinging;
        Vector3 axePosition; Quaternion axeRotation;
        Vector3[] boardPositions;
        public Transform Target => Returning ? intro.cellSpawn : approach;
        public string Objective => Returning ? "첫날의 일과 · 방으로 돌아가 쉬기" : $"첫날의 일과 · 작업실에서 판자 쪼개기 ({Completed}/{Required})";
        public string Prompt => Returning ? "[F] 잠자리에 들기" : Working ? "좌클릭을 눌러 힘을 모으고, 놓아서 내리치기 · [F] 중단" : "[F] 판자 작업하기";
        public void Begin(PrisonIntroduction owner){intro=owner;feedback=GetComponent<PrisonWoodFeedback>();Completed=0;Returning=Sleeping=ReadyToSleep=false;axePosition=axe.localPosition;axeRotation=axe.localRotation;boardPositions=new Vector3[boards.Length];for(int i=0;i<boards.Length;i++){boardPositions[i]=boards[i].transform.localPosition;boards[i].SetActive(true);}ReadyBoard();}
        bool Near(Transform target)=>Vector3.Distance(intro.mission.player.transform.position,target.position)<1.8f;
        void ReadyBoard(){if(Completed<Required)boards[Completed].transform.position=station.position-Vector3.up*.04f;}
        void Update(){
            if(!intro||Sleeping||Time.timeScale<=0||intro.State!=PrisonIntroduction.Stage.Work)return;
            if(Returning){if(ReadyToSleep&&Near(intro.cellSpawn)&&Keyboard.current.fKey.wasPressedThisFrame)Sleeping=true;return;}
            if(swinging)return;
            if(!Working){if(Near(approach)&&Keyboard.current.fKey.wasPressedThisFrame){Working=true;intro.WorkView(true,station.position);strength=0;Pose(0);if(feedback)feedback.Open(intro.filmCamera);}return;}
            if(Keyboard.current.fKey.wasPressedThisFrame){EndView();return;}
            if(Mouse.current.leftButton.isPressed){strength=Mathf.Min(1,strength+Time.deltaTime*.85f);Pose(strength);}
            if(Mouse.current.leftButton.wasReleasedThisFrame)StartCoroutine(Strike());
        }
        void Pose(float power){axe.localPosition=axePosition;axe.localRotation=Quaternion.AngleAxis(-75*power,Vector3.right)*axeRotation;}
        IEnumerator Strike(){
            swinging=true;float power=strength;
            for(float t=0;t<.2f;t+=Time.deltaTime){Pose(power*(1-Mathf.Pow(t/.2f,2)));yield return null;}
            Pose(0);if(feedback)feedback.Hit(power>=.65f);else PrisonCookingAudio.Cue("cut");
            if(power>=.65f){
                var board=boards[Completed].transform;var halves=board.GetComponentsInChildren<MeshRenderer>();
                for(float t=0;t<.28f;t+=Time.deltaTime){for(int i=0;i<halves.Length;i++){halves[i].transform.localPosition+=Vector3.right*(i==0?-1:1)*Time.deltaTime*.48f;halves[i].transform.Rotate(Vector3.forward,(i==0?1:-1)*Time.deltaTime*28,Space.Self);}yield return null;}
                yield return new WaitForSeconds(.25f);board.gameObject.SetActive(false);Completed++;
            }
            strength=0;swinging=false;
            if(Completed>=Required){EndView();Returning=true;intro.WorkEvening();}else ReadyBoard();
        }
        public void EndView(){if(feedback)feedback.Close();Working=false;strength=0;if(axe){axe.localPosition=axePosition;axe.localRotation=axeRotation;}if(intro)intro.WorkView(false,Vector3.zero);}
        public void Cancel(){StopAllCoroutines();if(Working)EndView();Sleeping=true;}
        void OnGUI(){if(!Working||Time.timeScale<=0)return;var old=GUI.color;float w=240*Screen.height/900f;var r=new Rect((Screen.width-w)/2,Screen.height*.82f,w,12);GUI.color=new Color(.08f,.1f,.12f,.9f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=strength>=.65f?new Color(.4f,.85f,.6f):new Color(1,.65f,.2f);r.width*=strength;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(0,Screen.height*.85f,Screen.width,45),strength>=.65f?"놓아서 내리치기":"좌클릭을 눌러 도끼 들어 올리기",new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=22,alignment=TextAnchor.MiddleCenter});GUI.color=old;}
    }
}


