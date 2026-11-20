using UnityEngine;
namespace DeliveryService.Mok {
// Keeps the existing first-person dialogue controls, with a gentle conversational lens.
public sealed class RacerConversationPresentation : MonoBehaviour {
 static RacerConversationPresentation current;public static bool Active=>current&&current.conversation&&current.conversation.IsDialogueActive;
 PhoneController phone;RacerNPCInteraction conversation;Camera view;float originalFov;bool focused;
 void Awake(){conversation=GetComponent<RacerNPCInteraction>();phone=FindFirstObjectByType<PhoneController>();}
 void LateUpdate(){
 bool talking=conversation&&conversation.IsDialogueActive;if(talking)current=this;else if(current==this)current=null;if(talking&&phone)phone.ClosePhone();
 if(talking&&!focused){view=Camera.main;if(view){originalFov=view.fieldOfView;focused=true;}}
 if(!focused||!view)return;
 if(!talking){view.fieldOfView=Mathf.Lerp(view.fieldOfView,originalFov,1-Mathf.Exp(-Time.deltaTime*6));if(Mathf.Abs(view.fieldOfView-originalFov)<.05f)Restore();return;}
 view.fieldOfView=Mathf.Lerp(view.fieldOfView,Mathf.Min(originalFov,54),1-Mathf.Exp(-Time.deltaTime*4));
 }
 void Restore(){if(view)view.fieldOfView=originalFov;focused=false;view=null;}
 void OnDisable(){if(current==this)current=null;Restore();}
}
}
