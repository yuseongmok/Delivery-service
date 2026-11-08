using UnityEngine;
namespace DeliveryService.Mok {
// 프리팹 원점이 아니라 실제 탄창과 슬라이드에서 손잡이·총구 위치를 구한다.
public sealed class HandgunGeometry:MonoBehaviour {
 public Vector3 Grip{get;private set;}public Vector3 Muzzle{get;private set;}public bool Found{get;private set;}
 public static HandgunGeometry Get(Transform gun){var g=gun.GetComponent<HandgunGeometry>();if(!g)g=gun.gameObject.AddComponent<HandgunGeometry>();return g;}
 void Awake(){Grip=new Vector3(0,.24f,.035f);Muzzle=new Vector3(0,.30f,.25f);foreach(var m in GetComponentsInChildren<MeshFilter>(true)){if(!m.sharedMesh)continue;var b=m.sharedMesh.bounds;if(m.name.ToLowerInvariant().Contains("magazin")){Grip=transform.InverseTransformPoint(m.transform.TransformPoint(b.center))+Vector3.up*.018f;Found=true;}if(m.name=="Slide"){var c=transform.InverseTransformPoint(m.transform.TransformPoint(b.center));float front=float.NegativeInfinity;for(int i=0;i<8;i++){var v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));front=Mathf.Max(front,transform.InverseTransformPoint(m.transform.TransformPoint(v)).z);}Muzzle=new Vector3(c.x,c.y,front+.015f);}}}
}
}
