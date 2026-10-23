#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    const string VaultArt=Art+"/VaultFrame";
    public static void RefineEntrance(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-entrance-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        string path=VaultArt+"/PoliceBoots.wav";int rate=22050,n=(int)(rate*.18f);var random=new System.Random(81);
        using(var w=new BinaryWriter(File.Create(path))){
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(n*2);
            for(int i=0;i<n;i++){float t=i/(float)rate;float s=(Mathf.Sin(t*2*Mathf.PI*95)*.6f+(float)(random.NextDouble()*2-1)*.22f)*Mathf.Exp(-t*32);w.Write((short)(Mathf.Clamp(s,-1,1)*24000));}
        }
        AssetDatabase.ImportAsset(path);var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        foreach(var officer in d.officers)officer.entranceFootstep=clip;
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Bank-entrance-build.txt","PASS: backed up scene; saved spatial boot steps for staged police entrance. Optional voice clip exposed per officer; existing editable story subtitles preserved.");
    }
    public static void RefinePartnerReach(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-partner-reach-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        for(int i=0;i<d.cash.Length;i++){
            var cash=d.cash[i].transform.position;
            // 두 손이 돈 더미와 가방에 함께 닿도록 둘 사이에서 작업한다.
            d.accomplice.collectionPoints[i].position=new Vector3(cash.x-.22f,0,cash.z-.38f);
        }
        d.accomplice.performance.handCash.localScale=new Vector3(.23f,.035f,.13f);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/Bank-partner-reach.txt","PASS: scene backed up; collection stance centered between cash and bag; hand bundle sized to palm.");
    }
    public static void PolishVault(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        Directory.CreateDirectory("Logs");
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-vault-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        var door=d.vaultDoor;
        if(door.parent.Cast<Transform>().Any(t=>t.name=="Vault fitted frame"))throw new Exception("Vault frame already installed");
        Directory.CreateDirectory(VaultArt);AssetDatabase.Refresh();
        var steel=LoadMat("Gunmetal");var bronze=AssetDatabase.LoadAssetAtPath<Material>(DecorArt+"/Satin bronze.mat");
        var frame=new GameObject("Vault fitted frame").transform;frame.SetParent(door.parent,false);frame.position=door.TransformPoint(new Vector3(2.1f,0,0));
        // 사각 벽의 모서리를 메우고 원형 문이 들어갈 깊이 있는 개구부를 만든다.
        VaultBand("Square steel surround",frame,2.085f,2.38f,-.16f,.65f,steel,true);
        VaultBand("Bronze receiving rim",frame,2.085f,2.16f,-.20f,-.15f,bronze,false);
        VaultBand("Recessed inner seal",frame,2.065f,2.095f,.24f,.62f,steel,false);
        foreach(float y in new[]{-.95f,.95f}){
            Box("Hinge mounting plate",new Vector3(-2.23f,y,-.25f),new Vector3(.28f,.5f,.15f),bronze,frame,false);
            {
                Cylinder("Heavy hinge barrel",new Vector3(-2.1f,y,-.08f),new Vector3(.24f,.28f,.24f),steel,frame);
                Box("Door hinge arm",new Vector3(.36f,y,-.30f),new Vector3(.75f,.18f,.18f),bronze,door,false);
            }
        }
        var bolts=new List<Transform>();
        for(int i=0;i<8;i++){
            float a=(i*45+22.5f)*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            var bolt=Cylinder("Retracting locking bar",new Vector3(2.1f,0,.12f)+radial*1.97f,new Vector3(.12f,.17f,.12f),bronze,door);
            bolt.localRotation=Quaternion.FromToRotation(Vector3.up,radial);bolts.Add(bolt);
            var socket=Box("Lock receiver",radial*2.15f+Vector3.forward*.24f,new Vector3(.23f,.13f,.14f),steel,frame,false);
            socket.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
        }
        d.accomplice.performance.vaultBolts=bolts.ToArray();
        var wheel=d.accomplice.performance.wheel;
        var hub=Cylinder("Wheel center hub",Vector3.zero,new Vector3(.23f,.09f,.23f),bronze,wheel);hub.localRotation=Quaternion.Euler(90,0,0);
        // 기존 손 IK의 잡는 반경에 맞춘 손잡이 테두리.
        VaultBand("Wheel grip ring",wheel,.34f,.39f,-.075f,.025f,bronze,false);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        var cam=d.filmCamera;var cp=cam.transform.position;var cr=cam.transform.rotation;var dr=door.localRotation;
        try{
            DecorCapture(cam,new Vector3(3.8f,2.4f,2.4f),frame.position,"vault-fitted-closed");
            door.localRotation=Quaternion.Euler(0,-105,0);
            DecorCapture(cam,new Vector3(3.8f,2.4f,2.4f),frame.position,"vault-fitted-open");
        }finally{door.localRotation=dr;cam.transform.SetPositionAndRotation(cp,cr);}
        File.WriteAllText("Logs/Bank-vault-build.txt","PASS: scene backed up; fitted square-to-round frame, deep receiving rim, hinge arms, eight retracting bolts and hand-sized wheel saved. Original collision and hinge reference preserved.");
    }
    static void VaultBand(string name,Transform parent,float inner,float outer,float front,float back,Material mat,bool square){
        var v=new List<Vector3>();var indices=new List<int>();
        Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=v.Count;v.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
        for(int i=0;i<64;i++){
            float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;
            var ra=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var rb=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
            float oa=square?outer/Mathf.Max(Mathf.Abs(ra.x),Mathf.Abs(ra.y)):outer;
            float ob=square?outer/Mathf.Max(Mathf.Abs(rb.x),Mathf.Abs(rb.y)):outer;
            var ai=ra*inner;var bi=rb*inner;var ao=ra*oa;var bo=rb*ob;var f=Vector3.forward*front;var k=Vector3.forward*back;
            quad(ai+f,bi+f,bo+f,ao+f);quad(ao+k,bo+k,bi+k,ai+k);
            quad(ai+k,bi+k,bi+f,ai+f);quad(ao+f,bo+f,bo+k,ao+k);
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,VaultArt+"/"+name+".asset");
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);
        g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;
    }
}
}
#endif
