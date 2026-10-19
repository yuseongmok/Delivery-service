#if UNITY_EDITOR
using System.Text;
using UnityEngine;
namespace DeliveryService.Mok {
// 주행 검사에서만 추가하는 충돌 기록기. 저장된 씬에는 남기지 않는다.
public class BikeContactProbe : MonoBehaviour {
 public static readonly StringBuilder Report=new StringBuilder();
 void OnCollisionEnter(Collision c){
 string path=c.transform.name;for(var p=c.transform.parent;p!=null;p=p.parent)path=p.name+"/"+path;
 float normalSpeed=0;foreach(var contact in c.contacts)normalSpeed=Mathf.Max(normalSpeed,Mathf.Abs(Vector3.Dot(c.relativeVelocity,contact.normal)));
 Report.AppendLine($"{Time.time:F2} {path} tag={c.gameObject.tag} speed={c.relativeVelocity.magnitude:F2} normalSpeed={normalSpeed:F2} normal={(c.contactCount>0?c.GetContact(0).normal:Vector3.zero)} at={transform.position}");System.IO.File.WriteAllText("Temp/Mok-bike-contacts.txt",Report.ToString());
 }
}
}
#endif

