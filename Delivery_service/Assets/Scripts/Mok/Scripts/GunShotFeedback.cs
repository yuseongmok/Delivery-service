using UnityEngine;using UnityEngine.Rendering;
namespace DeliveryService.Mok {
// 총구 불꽃은 총마다 재사용하고, 화면 진동은 렌더링할 때만 더해 조준 입력을 보존한다.
public sealed class GunShotFeedback:MonoBehaviour {
 static Mesh mesh;static Material material;Transform flame;Light lamp;float fired=-100;Camera view;bool applied;Vector3 position;Quaternion rotation;
 public int Shots{get;private set;}
 public static void Fire(Transform gun,Vector3 muzzle,Camera camera=null){var f=gun.GetComponent<GunShotFeedback>()??gun.gameObject.AddComponent<GunShotFeedback>();f.Ensure();f.view=camera;f.fired=Time.time;f.Shots++;f.flame.localPosition=muzzle;f.flame.localRotation=Quaternion.Euler(0,0,Random.Range(0,360));f.flame.localScale=Vector3.one*Random.Range(.8f,1.1f);f.flame.gameObject.SetActive(true);f.lamp.enabled=true;}
 void Ensure(){if(flame)return;if(!mesh){mesh=new Mesh{name="권총 별모양 총구 불꽃"};var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();for(int j=0;j<4;j++){float a=j*Mathf.PI/4;var side=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);int n=v.Count;v.Add(Vector3.zero);v.Add(side*.055f+Vector3.forward*.06f);v.Add(Vector3.forward*.23f);v.Add(-side*.055f+Vector3.forward*.06f);t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});}mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(1,.75f,.22f));}var g=new GameObject("총구 불꽃",typeof(MeshFilter),typeof(MeshRenderer));flame=g.transform;flame.SetParent(transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;lamp=g.AddComponent<Light>();lamp.color=new Color(1,.65f,.2f);lamp.range=2;lamp.intensity=2;lamp.shadows=LightShadows.None;}
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Before;RenderPipelineManager.endCameraRendering+=After;}
 void Update(){if(flame&&Time.time-fired>.06f)flame.gameObject.SetActive(false);}
 void Before(ScriptableRenderContext c,Camera cam){if(cam!=view||Time.timeScale<=0||Time.time-fired>.15f||applied)return;float t=(Time.time-fired)/.15f,e=(1-t)*(1-t);position=cam.transform.localPosition;rotation=cam.transform.localRotation;applied=true;cam.transform.localPosition=position+new Vector3(Mathf.Sin(t*27)*.0025f,Mathf.Cos(t*22)*.004f,0)*e;cam.transform.localRotation=rotation*Quaternion.Euler(-.45f*e,Mathf.Sin(t*25)*.2f*e,0);}
 void After(ScriptableRenderContext c,Camera cam){if(cam==view)Restore();}
 void Restore(){if(applied&&view){view.transform.localPosition=position;view.transform.localRotation=rotation;}applied=false;}
 void OnDisable(){Restore();if(flame)flame.gameObject.SetActive(false);RenderPipelineManager.beginCameraRendering-=Before;RenderPipelineManager.endCameraRendering-=After;}
}
}
