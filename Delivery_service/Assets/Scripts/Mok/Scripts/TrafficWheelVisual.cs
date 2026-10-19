using UnityEngine;
namespace DeliveryService.Mok {
// 실제 이동 거리를 바퀴 둘레로 환산한다. 주행 및 충돌 계산에는 관여하지 않는다.
public sealed class TrafficWheelVisual : MonoBehaviour {
    public Transform[] wheels;
    public float[] radii;
    Quaternion[] rest;Vector3[] axes;float[] angles;Vector3 previous;
    void OnEnable(){
        previous=transform.position;
        if(wheels==null)return;
        rest=new Quaternion[wheels.Length];axes=new Vector3[wheels.Length];angles=new float[wheels.Length];
        for(int i=0;i<wheels.Length;i++)if(wheels[i]!=null){rest[i]=wheels[i].localRotation;axes[i]=wheels[i].parent.InverseTransformDirection(transform.right).normalized;}
    }
    void LateUpdate(){
        Vector3 delta=Vector3.ProjectOnPlane(transform.position-previous,Vector3.up);previous=transform.position;
        if(rest==null||radii==null||radii.Length!=wheels.Length||delta.sqrMagnitude<.00000001f||delta.magnitude>10f)return;
        float distance=delta.magnitude*(Vector3.Dot(delta,transform.forward)>=0?1f:-1f);
        for(int i=0;i<wheels.Length;i++)if(wheels[i]!=null){
            angles[i]=Mathf.Repeat(angles[i]+distance/Mathf.Max(.01f,radii[i])*Mathf.Rad2Deg,360f);
            wheels[i].localRotation=Quaternion.AngleAxis(angles[i],axes[i])*rest[i];
        }
    }
}
}
