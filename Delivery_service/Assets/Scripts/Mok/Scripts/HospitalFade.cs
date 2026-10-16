using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class HospitalFade : MonoBehaviour {
    public CanvasGroup overlay;
    public float Alpha => overlay!=null?overlay.alpha:0;
    public IEnumerator DarkenAfter(float delay){yield return new WaitForSeconds(delay);yield return FadeTo(1,1f);}
    public IEnumerator WakeUp(){yield return new WaitForSecondsRealtime(.35f);yield return FadeTo(0,1.3f);}
    IEnumerator FadeTo(float target,float duration){
        if(overlay==null)yield break;float start=overlay.alpha;overlay.blocksRaycasts=true;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime){overlay.alpha=Mathf.SmoothStep(start,target,t/duration);yield return null;}
        overlay.alpha=target;overlay.blocksRaycasts=target>0;
    }
    void OnDisable(){if(overlay!=null){overlay.alpha=0;overlay.blocksRaycasts=false;}}
}
}
