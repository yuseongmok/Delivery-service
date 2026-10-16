using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
public sealed class WantedHUD : MonoBehaviour {
    public Text label;
    void LateUpdate(){
        float remaining=0;bool seen=false;
        if(!PoliceControl.IsArrestInProgress)
            foreach(var police in FindObjectsByType<PoliceControl>(FindObjectsSortMode.None))
                if(police.isActiveAndEnabled){remaining=Mathf.Max(remaining,police.RemainingChaseSeconds);seen|=police.HasVisualContact;}
        label.text=remaining>0?$"수배 중  ·  {Mathf.CeilToInt(remaining)}초":"";
        label.color=new Color(1f,.25f,.2f,seen?1f:Mathf.Lerp(.12f,1f,(Mathf.Sin(Time.time*7f)+1f)*.5f));
    }
}
}
