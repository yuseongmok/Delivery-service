using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok
{
    // 실제 원통형 메시를 별도 카메라로 촬영해 배전판 UI에 합성한다.
    public sealed class PrisonCableDisplay
    {
        GameObject root;Camera camera;RenderTexture texture;Material rubber; readonly List<Mesh> meshes=new List<Mesh>();
        Vector2[][] source;int lastCut=-2;
        public Texture Render(Vector2[][] paths,int cut)
        {
            if(!root){
                root=new GameObject("배전판 케이블 렌더"){hideFlags=HideFlags.HideAndDontSave};root.transform.position=Vector3.one*10000;
                rubber=new Material(Shader.Find("Universal Render Pipeline/Lit"));rubber.SetColor("_BaseColor",new Color(.045f,.05f,.055f));rubber.SetFloat("_Smoothness",.6f);rubber.SetFloat("_Metallic",.15f);
                camera=new GameObject("Cable camera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.transform.localPosition=new Vector3(950,-465,-900);camera.orthographic=true;camera.orthographicSize=350;camera.aspect=990f/700;camera.nearClipPlane=1;camera.farClipPlane=1100;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.18f,.19f);camera.depth=-100;camera.enabled=true;
                texture=new RenderTexture(1188,840,24,RenderTextureFormat.ARGB32){antiAliasing=4};texture.Create();camera.targetTexture=texture;
                var light=new GameObject("Cable softbox",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(30,-35,0);
            }
            if(source!=paths||lastCut!=cut){
                source=paths;lastCut=cut;
                for(int i=root.transform.childCount-1;i>=0;i--){var child=root.transform.GetChild(i);if(child.GetComponent<MeshFilter>()){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}}
                foreach(var mesh in meshes)Object.Destroy(mesh);meshes.Clear();
                for(int i=0;i<paths.Length;i++){
                    var samples=new List<Vector3>();var p=paths[i];
                    for(int j=0;j<p.Length-1;j++){
                        for(int k=0;k<14;k++){
                            float t=k/14f;
                            if(i==cut&&j==0&&t>.2f&&t<.6f){if(samples.Count>1){Tube(samples,i);samples.Clear();}continue;}
                            var a=p[Mathf.Max(0,j-1)];var b=p[j];var c=p[j+1];var d=p[Mathf.Min(p.Length-1,j+2)];
                            // 절단 지점은 직선으로 유지하고 선 묶음 안쪽은 둥글게 굽힌다.
                            Vector2 q=j==0?Vector2.Lerp(b,c,t):.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                            samples.Add(new Vector3(q.x,-q.y,-i*24));
                        }
                    }
                    samples.Add(new Vector3(p[p.Length-1].x,-p[p.Length-1].y,-i*24));Tube(samples,i);
                }
                // 렌더 파이프라인이 다음 프레임에 전용 텍스처를 렌더한다.
            }
            return texture;
        }
        void Tube(List<Vector3> points,int index){
            if(points.Count<2)return;const int sides=10;var vertices=new Vector3[points.Count*sides];var triangles=new int[(points.Count-1)*sides*6];
            for(int i=0;i<points.Count;i++){
                var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(tangent,Vector3.forward).normalized;
                for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;vertices[i*sides+j]=points[i]+(side*Mathf.Cos(angle)+Vector3.forward*Mathf.Sin(angle))*9;
                    if(i==0)continue;int a=(i-1)*sides+j,b=(i-1)*sides+(j+1)%sides,c=i*sides+j,d=i*sides+(j+1)%sides;int at=((i-1)*sides+j)*6;triangles[at]=a;triangles[at+1]=c;triangles[at+2]=b;triangles[at+3]=b;triangles[at+4]=c;triangles[at+5]=d;}
            }
            var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject("검정 고무 케이블 "+index,typeof(MeshFilter),typeof(MeshRenderer)){layer=31};go.transform.SetParent(root.transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=rubber;
        }
        public void SetVisible(bool visible){if(root&&root.activeSelf!=visible)root.SetActive(visible);}
        public void Dispose(){if(root)Object.Destroy(root);foreach(var mesh in meshes)if(mesh)Object.Destroy(mesh);if(rubber)Object.Destroy(rubber);if(texture){texture.Release();Object.Destroy(texture);}}
    }
}

