#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    const string CombatArt=Art+"/CombatFeedback";
    public static void PolishCombat(){
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        if(EditorApplication.isPlayingOrWillChangePlaymode||!d)throw new Exception("Open bank in Edit mode.");
        EditorSceneManager.SaveScene(d.gameObject.scene,"Logs/Bank-before-combat-polish.unity",true);
        var guns=new List<PrologueGunFeedback>{d.player.gunFeedback};guns.AddRange(d.officers.Select(o=>o.gunFeedback));guns.AddRange(d.roadPolice.Select(o=>o.gunFeedback));
        string report="";
        foreach(var effect in guns.Distinct()){
            var root=effect.transform.parent;var points=new List<Vector3>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
                if(filter.transform.IsChildOf(effect.transform)||!filter.sharedMesh)continue;
                using var data=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh);
                using var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);data[0].GetVertices(vertices);
                foreach(var vertex in vertices)points.Add(root.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
            }
            float front=points.Max(v=>v.z);var tip=points.Where(v=>v.z>front-.003f).ToArray();
            var center=tip.Aggregate(Vector3.zero,(a,b)=>a+b)/tip.Length;
            effect.transform.localPosition=center+Vector3.forward*.004f;effect.transform.localRotation=Quaternion.identity;
            report+=root.name+" muzzle "+effect.transform.localPosition+"\n";
        }
        // 뒤에서 보아도 원반처럼 보이지 않는 별 모양 섬광과 길쭉한 불꽃.
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(CombatArt+"/MuzzleFlame.asset");
        var verts=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<16;i++){
            float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;float ra=i%2==0?.1f:.035f,rb=i%2==0?.035f:.1f;int n=verts.Count;
            verts.Add(new Vector3(0,0,.015f));verts.Add(new Vector3(Mathf.Cos(a)*ra,Mathf.Sin(a)*ra,.015f));verts.Add(new Vector3(Mathf.Cos(b)*rb,Mathf.Sin(b)*rb,.015f));tris.AddRange(new[]{n,n+1,n+2});
        }
        for(int i=0;i<3;i++){
            var rotation=Quaternion.Euler(0,0,i*60);int n=verts.Count;
            foreach(var point in new[]{new Vector3(-.025f,0,0),new Vector3(-.065f,0,.07f),new Vector3(0,0,.26f),new Vector3(.065f,0,.07f),new Vector3(.025f,0,0)})verts.Add(rotation*point);
            tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n,n+3,n+4});
        }
        mesh.Clear();mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var material=AssetDatabase.LoadAssetAtPath<Material>(CombatArt+"/Flame orange.mat");material.SetColor("_BaseColor",new Color(1.7f,.35f,.035f));EditorUtility.SetDirty(material);
        foreach(var effect in guns.Distinct())effect.flame.Find("White core").localPosition=Vector3.back*.01f;
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(d.gameObject.scene);File.WriteAllText("Logs/Bank-combat-muzzles.txt",report);
    }
    public static void BuildCombat(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before editing.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        Directory.CreateDirectory("Logs");EditorSceneManager.SaveScene(d.gameObject.scene,"Logs/Bank-before-combat-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        Directory.CreateDirectory(CombatArt);AssetDatabase.Refresh();
        if(d.player.combatFeedback)throw new Exception("Combat feedback already installed.");
        var mesh=new Mesh{name="Low poly muzzle flame"};var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<8;i++){
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int n=vertices.Count;
            vertices.Add(Vector3.zero);vertices.Add(new Vector3(Mathf.Cos(a)*.085f,Mathf.Sin(a)*.085f,.065f));
            vertices.Add(new Vector3(Mathf.Cos(b)*.085f,Mathf.Sin(b)*.085f,.065f));vertices.Add(new Vector3(0,0,i%2==0?.38f:.28f));
            triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
        }
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,CombatArt+"/MuzzleFlame.asset");
        var orange=CombatMat("Flame orange",new Color(2.5f,.8f,.08f));var core=CombatMat("Flame core",new Color(3,2.8f,1.7f));
        var rifle=CombatAudio("Rifle shot",0,.48f);
        var feedback=d.gameObject.AddComponent<PrologueCombatFeedback>();feedback.director=d;d.player.combatFeedback=feedback;
        feedback.bodyHit=CombatAudio("Cloth impact",1,.2f);feedback.fatalHit=CombatAudio("Heavy impact",2,.29f);feedback.wallHit=CombatAudio("Stone impact",3,.18f);
        var particle=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Impact dust"};
        particle.SetColor("_BaseColor",Color.white);particle.SetFloat("_Surface",1);particle.SetFloat("_SrcBlend",5);particle.SetFloat("_DstBlend",10);particle.SetFloat("_ZWrite",0);particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particle.renderQueue=3000;
        AssetDatabase.CreateAsset(particle,CombatArt+"/ImpactDust.mat");feedback.particleMaterial=particle;
        var marker=new GameObject("Confirmed shot marker",typeof(RectTransform),typeof(CanvasGroup));marker.transform.SetParent(d.objective.canvas.transform,false);
        var rect=(RectTransform)marker.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(40,40);rect.anchoredPosition=Vector2.zero;
        feedback.hitMarker=marker.GetComponent<CanvasGroup>();feedback.hitMarker.alpha=0;feedback.hitMarker.blocksRaycasts=false;
        feedback.markerStrokes=new Image[4];int index=0;
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}){
            var g=new GameObject("Hit tick",typeof(RectTransform),typeof(Image));g.transform.SetParent(rect,false);
            var image=g.GetComponent<Image>();image.color=new Color(1,.95f,.79f);image.raycastTarget=false;
            image.rectTransform.sizeDelta=new Vector2(2,7);image.rectTransform.anchoredPosition=new Vector2(x*8,y*8);image.rectTransform.localRotation=Quaternion.Euler(0,0,-x*y*45);
            feedback.markerStrokes[index++]=image;
        }
        d.player.gunFeedback=InstallGunFeedback(d.player.muzzle,true,mesh,orange,core,rifle);
        foreach(var o in d.officers)o.gunFeedback=InstallGunFeedback(o.muzzle,false,mesh,orange,core,rifle);
        foreach(var o in d.roadPolice)if(!o.gunFeedback)o.gunFeedback=InstallGunFeedback(o.muzzle,false,mesh,orange,core,rifle);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(d.gameObject.scene);
        File.WriteAllText("Logs/Bank-combat-build.txt","PASS: saved player/police muzzle flashes, rifle audio, pooled impact feedback and confirmed-hit marker.");
    }
    static PrologueGunFeedback InstallGunFeedback(Light light,bool player,Mesh mesh,Material orange,Material core,AudioClip shot){
        var effect=light.gameObject.AddComponent<PrologueGunFeedback>();effect.flash=light;effect.playerGun=player;effect.shot=shot;
        light.intensity=player?5:3.5f;light.range=player?3:2;light.shadows=LightShadows.None;light.enabled=false;
        var flame=new GameObject("Muzzle flame").transform;flame.SetParent(light.transform,false);flame.localScale=Vector3.one*(player?1:.75f);effect.flame=flame;
        for(int i=0;i<2;i++){
            var g=new GameObject(i==0?"Outer flame":"White core");g.transform.SetParent(flame,false);g.layer=2;
            g.transform.localScale=i==0?Vector3.one:new Vector3(.45f,.45f,.72f);
            g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=i==0?orange:core;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        flame.gameObject.SetActive(false);return effect;
    }
    static Material CombatMat(string name,Color color){
        var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Cull",0);AssetDatabase.CreateAsset(m,CombatArt+"/"+name+".mat");return m;
    }
    static AudioClip CombatAudio(string name,int type,float seconds){
        string path=CombatArt+"/"+name+".wav";int rate=44100,n=(int)(rate*seconds);var rng=new System.Random(719+type);float low=0;
        using(var w=new BinaryWriter(File.Create(path))){
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(n*2);
            for(int i=0;i<n;i++){
                float t=i/(float)rate,noise=(float)(rng.NextDouble()*2-1);low=Mathf.Lerp(low,noise,.13f);float s;
                if(type==0)s=noise*.48f*Mathf.Exp(-t*60)+Mathf.Sin(t*2*Mathf.PI*105)*.38f*Mathf.Exp(-t*25)+low*.2f*Mathf.Exp(-t*8)+(t>.045f?noise*.12f*Mathf.Exp(-(t-.045f)*25):0);
                else if(type==3)s=noise*.45f*Mathf.Exp(-t*65)+Mathf.Sin(t*5100)*.2f*Mathf.Exp(-t*50);
                else s=low*.5f*Mathf.Exp(-t*25)+Mathf.Sin(t*2*Mathf.PI*(type==1?145:85))*.5f*Mathf.Exp(-t*22)+noise*.15f*Mathf.Exp(-t*70);
                s*=Mathf.Clamp01(t/.001f)*Mathf.Clamp01((seconds-t)/.025f);w.Write((short)(Mathf.Clamp(s,-1,1)*30000));
            }
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
}
#endif
