using UnityEngine;
namespace DeliveryService.Mok {
// 타격 구간에서만 짧게 남는 방망이 끝 잔상이다.
public sealed class BatSwingVisual:MonoBehaviour {
 TrailRenderer trail;Material material;int lastFrame;
 public static void Set(Transform weapon,float phase){
  var effect=weapon.GetComponent<BatSwingVisual>()??weapon.gameObject.AddComponent<BatSwingVisual>();
  if(!effect.trail)effect.Awake();
  effect.lastFrame=Time.frameCount;effect.trail.emitting=phase>=.3f&&phase<=.66f&&Time.timeScale>0;
 }
 void Awake(){
  if(trail)return;
  var tip=new GameObject("방망이 잔상");tip.transform.SetParent(transform,false);tip.transform.localPosition=new Vector3(0,.8f,0);
  trail=tip.AddComponent<TrailRenderer>();trail.time=.09f;trail.minVertexDistance=.025f;trail.widthMultiplier=.075f;trail.emitting=false;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.renderQueue=3000;material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");trail.sharedMaterial=material;
  trail.startColor=new Color(1,.84f,.55f,.3f);trail.endColor=new Color(1,.84f,.55f,0);
 }
 void LateUpdate(){if(trail&&(Time.timeScale<=0||Time.frameCount>lastFrame+1))trail.emitting=false;}
 void OnDisable(){if(trail){trail.emitting=false;trail.Clear();}}
 void OnDestroy(){if(material)Destroy(material);}
 public static void Pose(float p,bool heavy,bool reverse,out Vector3 position,out Quaternion rotation){
  var rest=new Vector3(.32f,-.35f,.65f);
  var wind=heavy?new Vector3(.25f,.15f,.62f):new Vector3(.55f,-.12f,.65f);
  var hit=heavy?new Vector3(.03f,-.15f,.85f):new Vector3(-.05f,-.13f,.86f);
  var end=heavy?new Vector3(-.03f,-.48f,.75f):new Vector3(-.55f,-.18f,.7f);
  var r0=Quaternion.Euler(15,0,-18);var r1=heavy?Quaternion.Euler(-35,0,-8):Quaternion.Euler(25,-40,-75);
  var r2=heavy?Quaternion.Euler(80,0,5):Quaternion.Euler(65,5,0);var r3=heavy?Quaternion.Euler(110,0,8):Quaternion.Euler(25,40,75);
  if(!heavy&&reverse){wind.x=-wind.x;hit.x=-hit.x;end.x=-end.x;r1=new Quaternion(r1.x,-r1.y,-r1.z,r1.w);r2=new Quaternion(r2.x,-r2.y,-r2.z,r2.w);r3=new Quaternion(r3.x,-r3.y,-r3.z,r3.w);}
  p=Mathf.Clamp01(p);float t;
  if(p<.3f){t=Mathf.SmoothStep(0,1,p/.3f);position=Vector3.Lerp(rest,wind,t);rotation=Quaternion.Slerp(r0,r1,t);}
  else if(p<.52f){t=Mathf.Pow((p-.3f)/.22f,1.6f);position=Vector3.Lerp(wind,hit,t);rotation=Quaternion.Slerp(r1,r2,t);}
  else if(p<.68f){t=Mathf.SmoothStep(0,1,(p-.52f)/.16f);position=Vector3.Lerp(hit,end,t);rotation=Quaternion.Slerp(r2,r3,t);}
  else{t=Mathf.SmoothStep(0,1,(p-.68f)/.32f);position=Vector3.Lerp(end,rest,t);rotation=Quaternion.Slerp(r3,r0,t);}

 }
}}
