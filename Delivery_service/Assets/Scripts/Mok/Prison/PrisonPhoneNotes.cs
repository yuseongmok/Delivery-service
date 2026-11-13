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
            var originalMatrix=GUI.matrix;var originalColor=GUI.color;float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var s=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=23,wordWrap=true,padding=new RectOffset(20,20,15,15)};
            GUI.color=new Color(.05f,.10f,.15f);GUI.DrawTexture(new Rect(55,200,340,530),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(65,210,320,65),m.introduction?"기록 · 정보거래":"기록 · 민기를 찾아서",s);
            GUI.color=new Color(1,.65f,.3f);GUI.Label(new Rect(65,280,320,65),selection==0?"▶ 현재 목표     알아낸 정보":"현재 목표     ▶ 알아낸 정보",s);GUI.color=Color.white;
            string body=selection==0?m.dialogue.objectives[(int)m.State]:m.Delivered<3?"배식하며 민기의 소식을 묻자.":"다리에 총상을 입은 새 수감자는 의무실에 있다. 간수의 시선을 돌려 들어가자.";
            if(m.introduction)body=selection==0?m.introduction.Objective:m.introduction.State==PrisonIntroduction.Stage.Complete?m.introduction.dialogue.information:"민기와 같은 방에 수감되었다. 운동장에서 최형욱을 만나 보자.";
            var chicken=FindFirstObjectByType<PrisonChickenMission>();if(chicken&&chicken.Active)body=selection==0?chicken.Objective:chicken.State==PrisonChickenMission.Stage.Complete?"간수복은 간수장실에 있다. 작업실에 수면제를 가진 죄수가 있다.":"최형욱은 치킨을 대가로 정보를 주겠다고 했다. 낮잠 중인 주방장을 깨우지 말자.";
            var medicine=FindFirstObjectByType<PrisonMedicineMission>();if(medicine&&medicine.Active)body=selection==0?medicine.Objective:medicine.HasMedicine?"수면제 확보. 민기와 자기 방으로 돌아가 다음 계획을 의논하자.":medicine.dialogue.clue;
            var keys=FindFirstObjectByType<PrisonKeysMission>();if(keys&&keys.Active)body=selection==0?keys.Objective:"간수의 열쇠로 창고를 열어 재료를 확보한다. 페페로니 피자를 구워 카트에 싣고 간수들에게 대접해 간수장실로 들어가자.";
            var office=FindFirstObjectByType<PrisonOfficeSearch>();if(office&&office.Active)body=selection==0?office.Objective:office.Notes; GUI.Label(new Rect(65,365,320,300),body,s);GUI.matrix=originalMatrix;GUI.color=originalColor;
        }
    }
}
