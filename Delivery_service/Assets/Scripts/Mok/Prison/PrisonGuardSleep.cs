using UnityEngine;
namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(2900)]
    public sealed class PrisonGuardSleep:MonoBehaviour
    {
        TextMesh[] letters;Animator animator;NPCControl npc;PrisonPizzaEating eating;
        void Start(){npc=GetComponent<NPCControl>();eating=GetComponent<PrisonPizzaEating>();animator=GetComponent<Animator>();letters=new TextMesh[3];for(int i=0;i<3;i++){var go=new GameObject("잠든 간수 Z "+i);go.transform.SetParent(transform,false);var t=go.AddComponent<TextMesh>();t.text="Z";t.font=MokUIFont.Font;t.fontSize=64;t.characterSize=.02f;t.anchor=TextAnchor.MiddleCenter;go.GetComponent<Renderer>().sharedMaterial=t.font.material;letters[i]=t;go.SetActive(false);}}
        void LateUpdate(){if(letters==null)return;var bind=FindFirstObjectByType<PrisonWardenRestraint>();bool asleep=eating&&eating.Finished&&(npc.IsKnockedDown||(bind&&bind.warden.gameObject==gameObject&&bind.Bound));var mission=PrisonMission.Current;if(!mission)return;var view=mission.player.View;foreach(var c in Camera.allCameras)if(c.enabled&&c.GetComponent<AudioListener>()&&c.GetComponent<AudioListener>().enabled){view=c;break;}
            var visual=GetComponentInChildren<PrisonWardenVisual>();var head=visual&&visual.visualAnimator?visual.visualAnimator.GetBoneTransform(HumanBodyBones.Head):animator.GetBoneTransform(HumanBodyBones.Head);var start=head.position+Vector3.up*.25f;bool visible=asleep&&Time.timeScale>0;var delta=start-view.transform.position;
            if(visible&&Physics.Raycast(view.transform.position,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform))visible=false;
            for(int i=0;i<3;i++){var t=letters[i];t.gameObject.SetActive(visible);if(!visible)continue;float phase=Mathf.Repeat(Time.time/2.6f+i/3f,1);t.transform.position=start+Vector3.up*(phase*.65f)+view.transform.right*(phase*.12f);t.transform.rotation=view.transform.rotation;t.transform.localScale=Vector3.one*Mathf.Lerp(.55f,1.3f,phase);t.color=new Color(.8f,.92f,1,Mathf.Clamp01(phase*7)*Mathf.Clamp01((1-phase)*5));}
        }
        void OnDisable(){if(letters!=null)foreach(var t in letters)if(t)t.gameObject.SetActive(false);}
    }
}
