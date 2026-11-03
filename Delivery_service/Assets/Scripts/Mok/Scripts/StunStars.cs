using UnityEngine;
namespace DeliveryService.Mok {
// 머리의 회전에 끌려가지 않는 별 세 개. 회복·비활성화 때 즉시 숨긴다.
public sealed class StunStars:MonoBehaviour {
 public Transform head;public PrologueRagdoll prologue;public NPCControl citizen;
 Transform orbit;Transform[] stars;static Mesh mesh;static Material material;
 public bool Visible=>orbit&&orbit.gameObject.activeSelf;
 void Awake(){
  if(!head){var a=GetComponentInChildren<Animator>();if(a&&a.isHuman)head=a.GetBoneTransform(HumanBodyBones.Head);}
  if(!prologue)prologue=GetComponent<PrologueRagdoll>();if(!citizen)citizen=GetComponent<NPCControl>();
  if(!mesh){mesh=new Mesh();mesh.name="기절 별";var v=new Vector3[11];var tri=new int[60];for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;v[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*(i%2==0?.08f:.034f);int k=i*6;tri[k]=0;tri[k+1]=i+1;tri[k+2]=(i+1)%10+1;tri[k+3]=tri[k+2];tri[k+4]=tri[k+1];tri[k+5]=0;}mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();}
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(1,.82f,.1f);}
  orbit=new GameObject("기절 표시").transform;stars=new Transform[3];
  for(int i=0;i<3;i++){var o=new GameObject("별",typeof(MeshFilter),typeof(MeshRenderer));o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;o.transform.SetParent(orbit,false);stars[i]=o.transform;}
  orbit.gameObject.SetActive(false);
 }
 void LateUpdate(){bool down=head&&((prologue&&prologue.IsDown)||(citizen&&(citizen.IsDead||citizen.IsKnockedDown)));orbit.gameObject.SetActive(down);if(!down)return;
  orbit.position=head.position+Vector3.up*.3f;var cam=Camera.main;
  if(prologue){var officer=prologue.GetComponent<PrologueOfficer>();if(officer&&officer.director)cam=officer.director.filmCamera.enabled?officer.director.filmCamera:officer.director.player.view;}
  for(int i=0;i<3;i++){float a=Time.time*3+i*Mathf.PI*2/3;stars[i].localPosition=new Vector3(Mathf.Cos(a)*.22f,Mathf.Sin(a*2)*.045f,Mathf.Sin(a)*.22f);if(cam)stars[i].rotation=cam.transform.rotation;}
 }
 void OnDisable(){if(orbit)orbit.gameObject.SetActive(false);}
 void OnDestroy(){if(orbit)Destroy(orbit.gameObject);}
}}
