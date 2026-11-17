#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonLockArtPolish
    {
        static PrisonLockArtPolish(){EditorApplication.update+=Poll;}
        static void Poll(){const string trigger="Temp/PrisonLockArtPolish";if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(trigger))return;File.Delete(trigger);try{Build();}catch(Exception e){File.WriteAllText("Logs/PrisonLockArtPolish.txt",e.ToString());}}
        static void Build(){
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();if(!m||m.gameObject.scene.name!="PrisonChapter")throw new Exception("Open PrisonChapter first");
            EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Prison-before-lock-art-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            const string folder="Assets/DeliveryPrisonChapter/KeysMission";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Key_01.prefab");
            var source=prefab.GetComponentInChildren<MeshFilter>();var original=source.sharedMesh;var bounds=original.bounds;var size=bounds.size;
            int axis=size.x>size.y&&size.x>size.z?0:size.y>size.z?1:2;
            var verts=original.vertices;float center=bounds.center[axis],extent=bounds.extents[axis];
            float Width(bool positive){var points=verts.Where(v=>positive?v[axis]>center+extent*.35f:v[axis]<center-extent*.35f).ToArray();var box=new Bounds(points[0],Vector3.zero);foreach(var p in points)box.Encapsulate(p);var s=box.size;s[axis]=0;return s.magnitude;}
            Vector3 along=Vector3.zero;along[axis]=Width(true)>Width(false)?1:-1;var rotation=Quaternion.FromToRotation(along,Vector3.back);
            var mesh=Object.Instantiate(original);mesh.name="Synty prison key normalized";mesh.vertices=verts.Select(v=>rotation*(v-bounds.center)*(.25f/size[axis])).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            string meshPath=folder+"/창고 Synty 열쇠.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);
            for(int i=0;i<m.keyLock.keyModels.Length;i++){
                var root=m.keyLock.keyModels[i].transform;foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                var child=root.Find("Synty 열쇠");if(!child){child=new GameObject("Synty 열쇠").transform;child.SetParent(root,false);child.gameObject.AddComponent<MeshFilter>();child.gameObject.AddComponent<MeshRenderer>();}
                child.GetComponent<MeshFilter>().sharedMesh=mesh;child.GetComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;child.GetComponent<Renderer>().enabled=true;
            }
            Material Mat(string name,Color color,float metallic){string p=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,p);}mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",.35f);return mat;}
            var brass=Mat("창고 자물쇠 황동",new Color(.43f,.31f,.12f),.65f);var steel=Mat("창고 자물쇠 강철",new Color(.42f,.46f,.49f),.8f);var dark=Mat("창고 열쇠 구멍",new Color(.025f,.03f,.035f),0);
            foreach(var r in m.keyLock.door.GetComponentsInChildren<Renderer>(true))if(r.name=="문 잠금장치")r.enabled=false;
            var old=m.keyLock.door.Find("창고 자물쇠 상세");if(old)Object.DestroyImmediate(old.gameObject);
            var body=new GameObject("창고 자물쇠 상세").transform;body.SetParent(m.keyLock.door,false);body.SetPositionAndRotation(m.keyLock.transform.position,m.keyLock.transform.rotation);
            Part(body,PrimitiveType.Cube,Vector3.zero,new Vector3(.17f,.13f,.065f),brass);
            Part(body,PrimitiveType.Cube,new Vector3(0,0,-.035f),new Vector3(.145f,.108f,.014f),brass);
            Part(body,PrimitiveType.Cylinder,new Vector3(0,0,-.046f),new Vector3(.027f,.003f,.027f),dark).localRotation=Quaternion.Euler(90,0,0);
            Part(body,PrimitiveType.Cube,new Vector3(0,-.015f,-.05f),new Vector3(.012f,.029f,.003f),dark);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Vector3 p=new Vector3(Mathf.Cos(a)*.057f,.072f+Mathf.Sin(a)*.065f,0),q=new Vector3(Mathf.Cos(b)*.057f,.072f+Mathf.Sin(b)*.065f,0);var link=Part(body,PrimitiveType.Cylinder,(p+q)*.5f,new Vector3(.017f,(q-p).magnitude*.55f,.017f),steel);link.localRotation=Quaternion.FromToRotation(Vector3.up,q-p);}
            foreach(float x in new[]{-.057f,.057f})Part(body,PrimitiveType.Cylinder,new Vector3(x,.058f,0),new Vector3(.017f,.025f,.017f),steel);
            var action=m.GetComponent<PrisonLockInteraction>();
            if(action.detailedGrip)Object.DestroyImmediate(action.detailedGrip.gameObject);
            var grip=new GameObject("열쇠를 집는 전용 손").AddComponent<PrisonLockGrip>();grip.transform.SetParent(m.transform,false);action.detailedGrip=grip;
            var skin=Mat("열쇠 손 피부",new Color(.64f,.43f,.29f),0);skin.SetFloat("_Smoothness",.1f);
            var cloth=AssetDatabase.LoadAssetAtPath<Material>(folder+"/소매치기 죄수 소매.mat");
            Part(grip.transform,PrimitiveType.Sphere,new Vector3(0,-.071f,-.037f),new Vector3(.075f,.085f,.035f),skin);
            void Finger(Vector3[] points,float width){for(int i=0;i<points.Length-1;i++){Vector3 p=points[i],q=points[i+1];var bone=Part(grip.transform,PrimitiveType.Capsule,(p+q)*.5f,new Vector3(width,Vector3.Distance(p,q)*.5f+width*.15f,width*.85f),skin);bone.localRotation=Quaternion.FromToRotation(Vector3.up,q-p);}}
            Finger(new[]{new Vector3(.023f,-.047f,-.035f),new Vector3(.025f,-.016f,-.018f),new Vector3(.012f,0,0)},.017f);
            Finger(new[]{new Vector3(-.033f,-.072f,-.032f),new Vector3(-.032f,-.03f,-.022f),new Vector3(-.012f,0,0)},.021f);
            for(int i=0;i<3;i++){float x=.006f-i*.018f;Finger(new[]{new Vector3(x,-.043f,-.037f),new Vector3(x,-.028f,-.012f),new Vector3(x,-.06f,.003f),new Vector3(x,-.075f,-.019f)},.016f-i*.001f);}
            grip.sleeve=Part(grip.transform,PrimitiveType.Cylinder,new Vector3(0,-.17f,-.04f),new Vector3(.075f,.07f,.065f),cloth);
            grip.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/PrisonLockArtPolish.txt","PASS saved Synty keys and detailed padlock; original interaction anchors preserved");
        }
        static Mesh FingerMesh(bool sphere){
            string path="Assets/DeliveryPrisonChapter/KeysMission/"+(sphere?"열쇠 손바닥":"열쇠 손가락")+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            float[] heights={-1,-.7f,0,.7f,1},radii={0,.38f,.5f,.38f,0};
            Vector3 Point(int row,int side){float angle=side*Mathf.PI/4;return new Vector3(Mathf.Cos(angle)*radii[row],heights[row]*(sphere?.5f:1),Mathf.Sin(angle)*radii[row]);}
            void Tri(Vector3 a,Vector3 b,Vector3 c){int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);}
            for(int row=0;row<4;row++)for(int side=0;side<8;side++){var a=Point(row,side);var b=Point(row,(side+1)%8);var c=Point(row+1,side);var d=Point(row+1,(side+1)%8);Tri(a,c,b);Tri(b,c,d);}
            mesh=new Mesh{name=sphere?"Low poly grip palm":"Low poly grip finger"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform Part(Transform parent,PrimitiveType type,Vector3 p,Vector3 size,Material material){var g=GameObject.CreatePrimitive(type);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(type==PrimitiveType.Sphere||type==PrimitiveType.Capsule)g.GetComponent<MeshFilter>().sharedMesh=FingerMesh(type==PrimitiveType.Sphere);Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
    }
}
#endif
