#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonCctvWireSetup
    {
        static PrisonCctvWireSetup(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonCctvWireSetup"))return;var action=File.ReadAllText("Temp/PrisonCctvWireSetup").Trim();File.Delete("Temp/PrisonCctvWireSetup");try{if(action=="polish")Polish();else Build();}catch(Exception e){File.WriteAllText("Logs/Prison-wire-setup.txt",e.ToString());}}
        static void Polish(){
            var p=Object.FindFirstObjectByType<PrisonCctvWirePanel>();var scene=p.gameObject.scene;if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-wire-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            p.RefreshDiagram();var tool=p.cutter.Find("Synty 전선 절단기");tool.localRotation=Quaternion.Euler(0,0,180)*Quaternion.Euler(90,0,0);var filter=tool.GetComponentInChildren<MeshFilter>(true);var mesh=filter.sharedMesh;var materials=filter.GetComponent<Renderer>().sharedMaterials;
            var convert=p.cutter.worldToLocalMatrix*filter.transform.localToWorldMatrix;var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=convert.MultiplyPoint3x4(vertices[i]);float top=float.NegativeInfinity;foreach(var v in vertices)top=Mathf.Max(top,v.y);var shift=new Vector3(0,.025f-top,0);for(int i=0;i<vertices.Length;i++)vertices[i]+=shift;
            var pivot=new Vector3(0,-.075f,0);p.leftJaw.localPosition=p.rightJaw.localPosition=pivot;
            for(int side=0;side<2;side++){var half=Object.Instantiate(mesh);half.name="CCTV cutter jaw "+side;var v=(Vector3[])vertices.Clone();for(int i=0;i<v.Length;i++)v[i]-=pivot;half.vertices=v;var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=convert.MultiplyVector(normals[i]).normalized;half.normals=normals;
                for(int sub=0;sub<mesh.subMeshCount;sub++){var keep=new System.Collections.Generic.List<int>();var tris=mesh.GetTriangles(sub);for(int i=0;i<tris.Length;i+=3){float x=(vertices[tris[i]].x+vertices[tris[i+1]].x+vertices[tris[i+2]].x)/3;if((x<0)==(side==0)){keep.Add(tris[i]);keep.Add(tris[i+1]);keep.Add(tris[i+2]);}}half.SetTriangles(keep,sub);}half.RecalculateBounds();string path="Assets/DeliveryPrisonChapter/CctvWire/절단기 턱 "+side+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(half,saved);Object.DestroyImmediate(half);half=saved;}else AssetDatabase.CreateAsset(half,path);var jaw=side==0?p.leftJaw:p.rightJaw;var mf=jaw.GetComponent<MeshFilter>();if(!mf)mf=jaw.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=half;var mr=jaw.GetComponent<MeshRenderer>();if(!mr)mr=jaw.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=materials;
            }
            tool.gameObject.SetActive(false);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-wire-polish.txt","PASS saved original Synty cutter split into moving jaws; cutting point aligned; diagram fitted");
        }
        static void Build(){
            var m=Object.FindFirstObjectByType<PrisonCctvMission>();var scene=m.gameObject.scene;if(scene.name!="PrisonChapter"||m.wirePanel)throw new Exception("Prison only; already installed?");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-wire-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            const string folder="Assets/DeliveryPrisonChapter/CctvWire";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject("CCTV 배전판 · 전선 절단").transform;root.SetParent(m.transform);root.position=new Vector3(-37.3f,1.65f,-32.5f);var panel=root.gameObject.AddComponent<PrisonCctvWirePanel>();panel.mission=m;m.wirePanel=panel;
            Material Mat(string name,Color c){var path=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};mat.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(mat,path);}return mat;}
            var dark=Mat("배전판 안쪽",new Color(.035f,.055f,.06f));var metal=Mat("배전판 도장",new Color(.24f,.31f,.32f));var steel=Mat("배전판 테두리",new Color(.53f,.6f,.6f));var paper=Mat("배선도 종이",new Color(.83f,.78f,.64f));var copper=Mat("전선 구리",new Color(.78f,.42f,.16f));
            Transform At(string name,Vector3 at,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);t.localPosition=at;return t;}
            Transform Box(string name,Vector3 at,Vector3 size,Material material,Transform parent=null,bool solid=false){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(parent?parent:root,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(t.GetComponent<Collider>());return t;}
            TextMesh Label(string name,string text,Vector3 at,float size,Transform parent=null){var t=At(name,at,parent);t.localRotation=Quaternion.Euler(0,180,0);var label=t.gameObject.AddComponent<TextMesh>();label.text=text;label.font=MokUIFont.Font;label.fontSize=64;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.9f,.9f,.8f);var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/OfficeSearch/벽에 가려지는 글자.mat");t.GetComponent<Renderer>().sharedMaterial=mat?mat:label.font.material;return label;}
            Box("매립 금속 함체",Vector3.zero,new Vector3(1.55f,1.18f,.15f),metal,null,true);Box("배전판 내부",new Vector3(0,0,.09f),new Vector3(1.43f,1.05f,.035f),dark);
            foreach(float x in new[]{-.75f,.75f})Box("접힌 세로 테두리",new Vector3(x,0,.15f),new Vector3(.045f,1.18f,.17f),steel);
            foreach(float y in new[]{-.57f,.57f})Box("접힌 가로 테두리",new Vector3(0,y,.15f),new Vector3(1.55f,.045f,.17f),steel);
            panel.hinge=At("배전판 문 경첩",new Vector3(-.77f,0,.24f));Box("열리는 배전판 덮개",new Vector3(.77f,0,0),new Vector3(1.52f,1.15f,.055f),metal,panel.hinge,true);
            Box("배전판 문 손잡이",new Vector3(1.37f,0,.07f),new Vector3(.07f,.24f,.07f),steel,panel.hinge);
            Label("덮개 표지","CCTV\n영상 전원 제어반",new Vector3(.77f,.05f,.04f),.03f,panel.hinge);
            for(int i=0;i<4;i++)Box("덮개 통풍구",new Vector3(.77f,-.29f-i*.045f,.033f),new Vector3(.65f,.016f,.015f),dark,panel.hinge);
            panel.wholeWires=new GameObject[3];panel.cutWires=new GameObject[3];panel.wireTargets=new Collider[3];
            var colors=new[]{new Color(.85f,.12f,.07f),new Color(.07f,.35f,.95f),new Color(.1f,.75f,.27f)};
            void Wire(Transform parent,string name,Vector3[] points,Material material,float width=.026f){var t=At(name,Vector3.zero,parent);var l=t.gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=material;l.widthMultiplier=width;l.numCapVertices=3;l.numCornerVertices=3;l.positionCount=points.Length;l.SetPositions(points);}
            for(int i=0;i<3;i++){float x=-.49f+i*.3f;var mat=Mat(PrisonCctvWirePanel.Names[i],colors[i]);
                Box("상단 단자",new Vector3(x,.36f,.17f),new Vector3(.14f,.12f,.09f),steel);Box("하단 단자",new Vector3(x,-.36f,.17f),new Vector3(.14f,.12f,.09f),steel);
                Label("단자 번호",new[]{"R1","B2","C3"}[i],new Vector3(x,.47f,.23f),.022f);
                var whole=At(PrisonCctvWirePanel.Names[i]+" 전선",new Vector3(x,0,.23f));Wire(whole,"절단 전 피복",new[]{new Vector3(0,.34f,0),new Vector3(-.025f,.18f,.02f),Vector3.zero,new Vector3(.025f,-.18f,.02f),new Vector3(0,-.34f,0)},mat);
                var col=whole.gameObject.AddComponent<BoxCollider>();col.size=new Vector3(.12f,.62f,.08f);panel.wireTargets[i]=col;panel.wholeWires[i]=whole.gameObject;
                var cut=At("잘린 "+PrisonCctvWirePanel.Names[i],whole.localPosition);Wire(cut,"위쪽 끊긴 선",new[]{new Vector3(0,.34f,0),new Vector3(-.025f,.18f,.02f),new Vector3(-.07f,.055f,.055f)},mat);Wire(cut,"아래쪽 끊긴 선",new[]{new Vector3(.07f,-.055f,.055f),new Vector3(.025f,-.18f,.02f),new Vector3(0,-.34f,0)},mat);
                Box("노출된 구리",new Vector3(-.07f,.04f,.055f),new Vector3(.013f,.035f,.013f),copper,cut);Box("노출된 구리",new Vector3(.07f,-.04f,.055f),new Vector3(.013f,.035f,.013f),copper,cut);cut.gameObject.SetActive(false);panel.cutWires[i]=cut.gameObject;
            }
            Box("정비 배선도",new Vector3(.48f,0,.16f),new Vector3(.42f,.57f,.015f),paper);panel.diagram=Label("영상 전원 연결 단서","",new Vector3(.48f,0,.175f),.0115f);panel.diagram.color=new Color(.08f,.11f,.12f);panel.RefreshDiagram();
            panel.viewpoint=At("배전판 근접 시점",new Vector3(0,0,1.85f));panel.viewpoint.LookAt(root.position+Vector3.forward*.15f);
            panel.checkpoint=At("배전판 재시작",new Vector3(0,-1.65f,1.55f));panel.checkpoint.rotation=Quaternion.Euler(0,180,0);
            var cam=At("배전판 조작 카메라",Vector3.zero);panel.camera=cam.gameObject.AddComponent<Camera>();panel.camera.enabled=false;panel.camera.fieldOfView=48;panel.camera.nearClipPlane=.02f;cam.gameObject.AddComponent<AudioListener>().enabled=false;
            panel.cutter=At("전선 절단기",Vector3.zero);panel.leftJaw=At("절단기 왼쪽 턱",Vector3.zero,panel.cutter);panel.rightJaw=At("절단기 오른쪽 턱",Vector3.zero,panel.cutter);
            var tool=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Items/SM_Item_Wirecutter_01.prefab"),panel.cutter);tool.name="Synty 전선 절단기";tool.transform.localPosition=new Vector3(0,-.08f,0);tool.transform.localRotation=Quaternion.Euler(90,0,0);foreach(var c in tool.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);panel.cutter.gameObject.SetActive(false);
            panel.sound=root.gameObject.AddComponent<AudioSource>();panel.sound.playOnAwake=false;panel.sound.spatialBlend=0;
            panel.openSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/차문 열기.mp3");panel.cutSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/절단 사운드.mp3");panel.alarmSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/은행경보.mp3");
            panel.warning=At("배전판 경보등",new Vector3(0,.6f,.5f)).gameObject.AddComponent<Light>();panel.warning.color=Color.red;panel.warning.range=4;panel.warning.enabled=false;
            var textPath=folder+"/CCTV 전선 절단 대사.asset";panel.dialogue=AssetDatabase.LoadAssetAtPath<PrisonCctvWireText>(textPath);if(!panel.dialogue){panel.dialogue=ScriptableObject.CreateInstance<PrisonCctvWireText>();AssetDatabase.CreateAsset(panel.dialogue,textPath);}
            m.console=root;m.dialogue.opened="민기: 모니터 옆 배전판을 열어. 영상 전원만 끊으면 카메라가 꺼질 거야.";EditorUtility.SetDirty(m.dialogue);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-wire-setup.txt","PASS saved panel / hinged door / three wires / circuit clue / wire cutter / checkpoint / sounds");
        }
    }
}
#endif
