#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    const string PerformanceArt=Art+"/PartnerPerformance";
    public static void PolishPerformance(){
        EditorSceneManager.OpenScene(ScenePath);
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        EditorSceneManager.SaveScene(d.gameObject.scene,"Logs/Bank-before-performance-polish.unity",true);
        for(int i=0;i<d.cash.Length;i++){
            var pos=d.cash[i].transform.position;pos.z=13.45f;d.cash[i].transform.position=pos;
            d.cash[i].transform.localScale=Vector3.one*.45f;
            foreach(Transform bundle in d.cash[i].transform){var b=bundle.localPosition;b.x=0;b.z=0;bundle.localPosition=b;}
            d.accomplice.collectionPoints[i].position=new Vector3(pos.x,0,13.05f);
        }
        d.accomplice.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var renderer in d.accomplice.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
        var a=d.accomplice;var p=a.performance;
        p.handCash.localScale=new Vector3(.34f,.04f,.18f);
        // FBX 뼈대의 확대 배율을 소품에 상속하지 않도록 월드 크기를 고정한다.
        p.shoulderMount.SetParent(null,true);p.shoulderMount.localScale=Vector3.one;
        p.shoulderMount.SetPositionAndRotation(a.transform.TransformPoint(new Vector3(0,1.05f,-.3f)),a.transform.rotation);
        p.shoulderMount.SetParent(a.animator.GetBoneTransform(HumanBodyBones.Chest),true);
        a.bag.localPosition=Vector3.zero;a.bag.localRotation=Quaternion.identity;a.bag.localScale=Vector3.one;
        if(!p.shoulderStraps){
            var straps=new GameObject("Duffel shoulder straps").transform;
            straps.SetPositionAndRotation(a.transform.TransformPoint(new Vector3(0,1.33f,0)),a.transform.rotation);
            foreach(int side in new[]{-1,1}){
                Box("Front strap",new Vector3(side*.13f,0,.13f),new Vector3(.045f,.47f,.025f),LoadMat("Gunmetal"),straps,false);
                Box("Shoulder strap",new Vector3(side*.13f,.23f,-.08f),new Vector3(.045f,.025f,.43f),LoadMat("Gunmetal"),straps,false);
            }
            straps.SetParent(a.animator.GetBoneTransform(HumanBodyBones.Chest),true);p.shoulderStraps=straps.gameObject;
        }
        EditorSceneManager.SaveScene(d.gameObject.scene);
    }
    public static void BuildPerformance(){
        EditorSceneManager.OpenScene(ScenePath);
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var scene=d.gameObject.scene;
        Directory.CreateDirectory("Logs");
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-performance-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed.");
        Directory.CreateDirectory(PerformanceArt);AssetDatabase.Refresh();
        var a=d.accomplice;
        if(a.performance)throw new Exception("Performance already installed; preserve scene edits.");
        var p=a.gameObject.AddComponent<ProloguePartnerPerformance>();a.performance=p;p.partner=a;
        var wheel=Point("Vault wheel pivot",d.vaultDoor.TransformPoint(new Vector3(2.1f,0,-.45f)),d.vaultDoor);
        foreach(var spoke in d.vaultDoor.GetComponentsInChildren<Transform>().Where(t=>t.name=="Wheel spoke").ToArray())spoke.SetParent(wheel,true);
        p.wheel=wheel;
        // 손이 닿는 높이의 금고 손잡이. 문과 금속 테두리는 기존 외형을 유지한다.
        wheel.localPosition=new Vector3(2.1f,-.55f,-.45f);
        UnityEngine.Object.DestroyImmediate(a.bag.gameObject);
        var mount=new GameObject("Bag shoulder mount").transform;mount.SetParent(a.animator.GetBoneTransform(HumanBodyBones.Chest),false);
        mount.localPosition=new Vector3(0,-.22f,-.22f);p.shoulderMount=mount;
        var bag=new GameObject("Openable cash duffel").transform;bag.SetParent(mount,false);a.bag=bag;
        var fabric=LoadMat("BankGreen");var trim=LoadMat("Gunmetal");var money=LoadMat("Cash");
        Box("Bag floor",Vector3.zero,new Vector3(.5f,.06f,.3f),fabric,bag,false);
        foreach(int side in new[]{-1,1}){
            Box("Bag end",new Vector3(side*.24f,.11f,0),new Vector3(.045f,.24f,.3f),fabric,bag,false);
            Box("Bag side",new Vector3(0,.11f,side*.14f),new Vector3(.5f,.24f,.03f),fabric,bag,false);
            Box("Carry handle",new Vector3(side*.15f,.3f,0),new Vector3(.035f,.2f,.03f),trim,bag,false);
        }
        var lid=new GameObject("Zipped flap hinge").transform;lid.SetParent(bag,false);lid.localPosition=new Vector3(0,.23f,.14f);p.bagLid=lid;
        Box("Bag flap",new Vector3(0,0,-.14f),new Vector3(.5f,.025f,.29f),fabric,lid,false);
        Box("Zipper seam",new Vector3(0,.017f,-.14f),new Vector3(.48f,.009f,.015f),trim,lid,false);
        p.bagMouth=new GameObject("Bag opening").transform;p.bagMouth.SetParent(bag,false);p.bagMouth.localPosition=new Vector3(0,.24f,0);
        p.bagContents=new GameObject[15];
        for(int i=0;i<15;i++){var notes=Box("Stored cash "+i,new Vector3((i%2==0?-.11f:.11f),.035f+(i/2)*.021f,0),new Vector3(.2f,.018f,.13f),money,bag,false);p.bagContents[i]=notes.gameObject;notes.gameObject.SetActive(false);}
        p.handCash=Box("Cash in hand",Vector3.zero,new Vector3(.23f,.035f,.13f),money,a.transform,false);p.handCash.gameObject.SetActive(false);
        p.passengerDoor=new GameObject("Passenger door hinge").transform;p.passengerDoor.SetParent(d.getawayCar,false);p.passengerDoor.localPosition=new Vector3(.95f,.65f,.8f);
        foreach(var filter in d.getawayCar.GetComponentsInChildren<MeshFilter>().Where(m=>m.transform==d.getawayCar||m.name.Contains("Glass")).ToArray())SplitPassengerDoor(filter,d.getawayCar,p.passengerDoor);
        p.doorHandle=Point("Passenger handle",d.getawayCar.TransformPoint(new Vector3(1.02f,.9f,-.45f)),p.passengerDoor);
        Box("Inner door trim",new Vector3(-.03f,.15f,-.7f),new Vector3(.04f,.28f,1.25f),trim,p.passengerDoor,false);
        Box("Passenger seat cushion",new Vector3(.48f,.48f,-.05f),new Vector3(.65f,.14f,.7f),trim,d.getawayCar,false);
        Box("Passenger seat back",new Vector3(.48f,.8f,-.4f),new Vector3(.65f,.65f,.15f),trim,d.getawayCar,false);
        p.passengerSeat=Point("Passenger seated root",d.getawayCar.TransformPoint(new Vector3(.48f,-.2f,-.1f)),d.getawayCar);p.passengerSeat.localRotation=Quaternion.identity;
        p.passengerStand=Point("Passenger stand",d.getawayCar.TransformPoint(new Vector3(1.7f,0,0)),d.getawayCar);p.passengerStand.rotation=Quaternion.LookRotation(-d.getawayCar.right);
        a.boardingPoint.position=p.passengerStand.position;
        for(int i=0;i<d.cash.Length;i++){d.cash[i].transform.localScale=Vector3.one*.65f;var pos=d.cash[i].transform.position;pos.z=13.65f;d.cash[i].transform.position=pos;}
        p.vaultLatch=Foley("VaultLatch",1.4f,0);p.vaultSwing=Foley("VaultSwing",2.4f,1);
        p.paperRustle=Foley("CashRustle",.23f,2);p.zipper=Foley("BagZip",.6f,3);p.cloth=Foley("BagCloth",.5f,4);
        p.doorOpen=Foley("CarDoorOpen",.6f,5);p.doorClose=Foley("CarDoorClose",.4f,6);
        EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/Bank-performance-build.txt","PASS: saved articulated vault wheel, openable bag, hand cash, passenger door mesh, seat and seven foley clips.");
    }
    static AudioClip Foley(string name,float seconds,int type){
        string path=PerformanceArt+"/"+name+".wav";int rate=22050,n=(int)(rate*seconds);var random=new System.Random(41+type);
        using(var writer=new BinaryWriter(File.Create(path))){
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+n*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(n*2);
            float previous=0;
            for(int i=0;i<n;i++){
                float t=i/(float)rate,p=t/seconds,noise=(float)(random.NextDouble()*2-1);float high=noise-previous;previous=noise;
                float envelope=Mathf.Sin(Mathf.PI*p);float sample=0;
                if(type==0){float pulse=t%(.14f);sample=(Mathf.Sin(t*2400)+noise*.4f)*Mathf.Exp(-pulse*80)*envelope*.22f;}
                if(type==1)sample=(Mathf.Sin(t*(230+25*Mathf.Sin(t*3)))+.4f*Mathf.Sin(t*720)+noise*.15f)*envelope*.16f;
                if(type==2)sample=high*.16f*envelope*(.45f+.55f*Mathf.Abs(Mathf.Sin(t*85)));
                if(type==3)sample=(high*.15f+Mathf.Sin(t*4000)*.04f)*envelope;
                if(type==4)sample=noise*.12f*envelope*(.5f+.5f*Mathf.Sin(t*33));
                if(type==5)sample=(noise*.18f+Mathf.Sin(t*850)*.15f)*Mathf.Exp(-t*18)+Mathf.Sin(t*310)*envelope*.05f;
                if(type==6)sample=(Mathf.Sin(t*420)*.5f+noise*.2f)*Mathf.Exp(-t*17);
                writer.Write((short)(Mathf.Clamp(sample,-1,1)*28000));
            }
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Mix(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    static void SplitPassengerDoor(MeshFilter filter,Transform car,Transform hinge,bool left=false){
        var source=filter.sharedMesh;using var data=MeshUtility.AcquireReadOnlyMeshData(source);var m=data[0];int count=m.vertexCount;
        using var pos=new NativeArray<Vector3>(count,Allocator.Temp);using var normals=new NativeArray<Vector3>(count,Allocator.Temp);using var uv=new NativeArray<Vector2>(count,Allocator.Temp);
        m.GetVertices(pos);m.GetNormals(normals);m.GetUVs(0,uv);
        var body=new List<Vertex>[m.subMeshCount];var door=new List<Vertex>[m.subMeshCount];
        Plane[] planes={new Plane(left?Vector3.left:Vector3.right,new Vector3(left?-.72f:.72f,0,0)),new Plane(Vector3.up,new Vector3(0,.48f,0)),new Plane(Vector3.down,new Vector3(0,1.45f,0)),new Plane(Vector3.forward,new Vector3(0,0,-.65f)),new Plane(Vector3.back,new Vector3(0,0,.8f))};
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
        mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,PerformanceArt+"/"+name+".asset");return mesh;
    }
    public static void InspectPerformance(){
        EditorSceneManager.OpenScene(ScenePath);
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        string s="Car scale "+d.getawayCar.lossyScale+"\n";
        foreach(var m in d.getawayCar.GetComponentsInChildren<MeshFilter>()){
            var b=m.sharedMesh.bounds;
            s+=m.name+" local "+m.transform.localPosition+" bounds "+b+" vertices "+m.sharedMesh.vertexCount+" readable "+m.sharedMesh.isReadable+"\n";
        }
        foreach(var bone in new[]{HumanBodyBones.Head,HumanBodyBones.Chest,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand})
            s+=bone+" "+d.accomplice.animator.GetBoneTransform(bone).position+"\n";
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Bank-performance-inspect.txt",s);
    }
}
}
#endif
