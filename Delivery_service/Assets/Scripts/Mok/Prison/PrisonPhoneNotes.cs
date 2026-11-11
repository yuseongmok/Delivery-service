using UnityEngine;

namespace DeliveryService.Mok
{
    // 감옥에서는 같은 M키 휴대폰에 현재 목표와 알아낸 정보를 보여 준다.
    public sealed class PrisonPhoneNotes : MonoBehaviour
    {
        PhoneController phone;
        int selection;
        void Start(){phone=GetComponentInChildren<PhoneController>(true);}
        void Update()
        {
            if(!phone||!phone.IsPhoneOpen())return;
            if(Input.GetKeyDown(KeyCode.UpArrow))selection=0;
            if(Input.GetKeyDown(KeyCode.DownArrow))selection=1;
        }
        void OnGUI()
        {
            var m=PrisonMission.Current;if(!phone||!phone.IsPhoneOpen()||!m)return;
            float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var s=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=23,wordWrap=true,padding=new RectOffset(20,20,15,15)};
            GUI.color=new Color(.05f,.10f,.15f);GUI.DrawTexture(new Rect(55,200,340,530),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(65,210,320,65),"기록 · 민기를 찾아서",s);
            GUI.color=new Color(1,.65f,.3f);GUI.Label(new Rect(65,280,320,65),selection==0?"▶ 현재 목표     알아낸 정보":"현재 목표     ▶ 알아낸 정보",s);GUI.color=Color.white;
            string body=selection==0?m.dialogue.objectives[(int)m.State]:m.Delivered<3?"배식하며 민기의 소식을 묻자.":"다리에 총상을 입은 새 수감자는 의무실에 있다. 간수의 시선을 돌려 들어가자.";
            GUI.Label(new Rect(65,365,320,300),body,s);
        }
    }
}
