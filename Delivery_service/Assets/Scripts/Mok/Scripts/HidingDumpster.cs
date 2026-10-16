using UnityEngine;
namespace DeliveryService.Mok {
public sealed class HidingDumpster : MonoBehaviour {
    public Transform lid;
    public Vector3 ExitPosition=>transform.TransformPoint(new Vector3(0,.1f,1.8f));
    float openedUntil;
    public void Open(){openedUntil=Time.time+.45f;}
    void Update(){if(lid!=null)lid.localRotation=Quaternion.Slerp(lid.localRotation,Quaternion.Euler(Time.time<openedUntil?-65:0,0,0),Time.deltaTime*12);}
}
}
