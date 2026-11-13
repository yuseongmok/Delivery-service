#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class PrisonVanDoors {
    struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Mix(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    public static void Split(MeshFilter filter,Transform car,Transform hinge,bool left=false){
        var source=filter.sharedMesh;using var data=MeshUtility.AcquireReadOnlyMeshData(source);var m=data[0];int count=m.vertexCount;
        using var pos=new NativeArray<Vector3>(count,Allocator.Temp);using var normals=new NativeArray<Vector3>(count,Allocator.Temp);using var uv=new NativeArray<Vector2>(count,Allocator.Temp);
        m.GetVertices(pos);m.GetNormals(normals);m.GetUVs(0,uv);
        var body=new List<Vertex>[m.subMeshCount];var door=new List<Vertex>[m.subMeshCount];
        Plane[] planes={new Plane(left?Vector3.left:Vector3.right,new Vector3(left?-.72f:.72f,0,0)),new Plane(Vector3.up,new Vector3(0,.48f,0)),new Plane(Vector3.down,new Vector3(0,2.05f,0)),new Plane(Vector3.forward,new Vector3(0,0,.25f)),new Plane(Vector3.back,new Vector3(0,0,1.55f))};
        for(int s=0;s<m.subMeshCount;s++){
            body[s]=new List<Vertex>();door[s]=new List<Vertex>();using var indices=new NativeArray<int>(m.GetSubMesh(s).indexCount,Allocator.Temp);m.GetIndices(indices,s);
            for(int i=0;i<indices.Length;i+=3){
                var polygon=new List<Vertex>();for(int k=0;k<3;k++){int j=indices[i+k];polygon.Add(new Vertex{p=car.InverseTransformPoint(filter.transform.TransformPoint(pos[j])),n=car.InverseTransformDirection(filter.transform.TransformDirection(normals[j])),uv=uv[j]});}
                foreach(var plane in planes){var inside=Clip(polygon,plane,true);var outside=Clip(polygon,plane,false);Triangulate(outside,body[s]);polygon=inside;if(polygon.Count==0)break;}
                Triangulate(polygon,door[s]);
            }
        }
        filter.sharedMesh=SaveMesh(source.name+(left?"_DriverOpenableBody":"_OpenableBody"),body,v=>filter.transform.InverseTransformPoint(car.TransformPoint(v)));
        var g=new GameObject(filter.name+" passenger panel");g.transform.SetParent(hinge,false);
        g.AddComponent<MeshFilter>().sharedMesh=SaveMesh(source.name+(left?"_DriverDoor":"_PassengerDoor"),door,v=>v-hinge.localPosition);
        g.AddComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;
    }
    static List<Vertex> Clip(List<Vertex> polygon,Plane plane,bool positive){
        var result=new List<Vertex>();if(polygon.Count==0)return result;
        for(int i=0;i<polygon.Count;i++){
            var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=plane.GetDistanceToPoint(a.p),db=plane.GetDistanceToPoint(b.p);
            bool ia=positive?da>=0:da<0,ib=positive?db>=0:db<0;
            if(ia)result.Add(a);if(ia!=ib)result.Add(Vertex.Mix(a,b,da/(da-db)));
        }return result;
    }
    static void Triangulate(List<Vertex> polygon,List<Vertex> target){for(int i=1;i<polygon.Count-1;i++){target.Add(polygon[0]);target.Add(polygon[i]);target.Add(polygon[i+1]);}}
    static Mesh SaveMesh(string name,List<Vertex>[] groups,Func<Vector3,Vector3> position){
        var all=groups.SelectMany(g=>g).ToArray();var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.vertices=all.Select(v=>position(v.p)).ToArray();mesh.normals=all.Select(v=>v.n).ToArray();mesh.uv=all.Select(v=>v.uv).ToArray();mesh.subMeshCount=groups.Length;
        int offset=0;for(int s=0;s<groups.Length;s++){mesh.SetTriangles(Enumerable.Range(offset,groups[s].Count).ToArray(),s);offset+=groups[s].Count;}
        mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/DeliveryPrisonChapter/Escape"+"/"+name+".asset");return mesh;
    }

}}
#endif