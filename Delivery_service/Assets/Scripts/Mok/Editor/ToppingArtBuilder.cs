#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class ToppingArtBuilder {
const string Folder="Assets/Scripts/Mok/Resources/MokPizzaArt";
static readonly List<Vector3> vertices=new List<Vector3>();
static List<int>[] triangles; static Material[] palette;
static ToppingArtBuilder(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
if(!File.Exists("Temp/Mok-topping-build"))return;File.Delete("Temp/Mok-topping-build");
try{Build();File.WriteAllText("Temp/Mok-topping-build.txt","PASS: Toppings and trays built.");}catch(Exception e){File.WriteAllText("Temp/Mok-topping-build.txt",e.ToString());Debug.LogException(e);}}
        static Material Mat(string name, string hex, float gloss = .15f)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", gloss);
            material.SetColor("_EmissionColor", Color.black); material.DisableKeyword("_EMISSION");
            return material;
        }
        static void Begin() { vertices.Clear(); triangles = Enumerable.Range(0, palette.Length).Select(_ => new List<int>()).ToArray(); }
        static void Face(int mat, params Vector3[] p)
        {
            int start = vertices.Count; vertices.AddRange(p);
            for (int i = 1; i < p.Length - 1; i++) { triangles[mat].Add(start); triangles[mat].Add(start + i); triangles[mat].Add(start + i + 1); }
        }
        static Vector3 Ring(float r, float y, float angle) => new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
        static void Disc(int mat, Vector3 center, float radius, float aspect = 1, int sides = 32, float noise = 0)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p = Ring(radius * (1 + noise * Mathf.Sin(i * 2.3f)), 0, a), q = Ring(radius * (1 + noise * Mathf.Sin((i + 1) * 2.3f)), 0, b);
                p.z *= aspect; q.z *= aspect; Face(mat, center, center + q, center + p);
            }
        }
        static void Box(int mat, Vector3 center, Vector3 size, float yaw = 0)
        {
            Vector3 s = size / 2; Quaternion rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 V(float x, float y, float z) => center + rotation * new Vector3(x * s.x, y * s.y, z * s.z);
            Face(mat,V(-1,1,-1),V(-1,1,1),V(1,1,1),V(1,1,-1));
            Face(mat,V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),V(-1,-1,1));
            Face(mat,V(-1,-1,-1),V(-1,1,-1),V(1,1,-1),V(1,-1,-1));
            Face(mat,V(1,-1,1),V(1,1,1),V(-1,1,1),V(-1,-1,1));
            Face(mat,V(-1,-1,1),V(-1,1,1),V(-1,1,-1),V(-1,-1,-1));
            Face(mat,V(1,-1,-1),V(1,1,-1),V(1,1,1),V(1,-1,1));
        }
        static GameObject Finish(string name, Transform parent)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.subMeshCount = palette.Length;
            for (int i = 0; i < palette.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                // Use Mesh setters so Unity also refreshes the GPU buffers of loaded prefabs.
                existing.Clear(); existing.vertices = mesh.vertices; existing.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
                existing.normals = mesh.normals; existing.bounds = mesh.bounds;
                EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing;
            }
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterials = palette;
            return obj;
        }

// Mesh geometry is baked into assets, with flat face normals and no runtime colliders.
static void Cylinder(int mat,Vector3 c,float r,float height,int sides=12){
Disc(mat,c+Vector3.up*height,r,1,sides);
for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
Face(mat,c+Ring(r,0,a),c+Ring(r,height,a),c+Ring(r,height,b),c+Ring(r,0,b));}}
static Vector3 Turn(Vector3 p,float yaw)=>Quaternion.Euler(0,yaw,0)*p;
// A raised, bevelled chocolate tile, flat shaded on every face.
static void BevelBox(int mat,Vector3 c,Vector3 size,float yaw){
Vector3 P(float x,float z,float h,float inset)=>c+Turn(new Vector3(x*(size.x/2-inset),h,z*(size.z/2-inset)),yaw);
Face(mat,P(-1,-1,size.y,.003f),P(-1,1,size.y,.003f),P(1,1,size.y,.003f),P(1,-1,size.y,.003f));
var corners=new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)};
for(int i=0;i<4;i++){var a=corners[i];var b=corners[(i+1)%4];Face(mat,P(a.x,a.y,0,0),P(b.x,b.y,0,0),P(b.x,b.y,size.y,.003f),P(a.x,a.y,size.y,.003f));}}
static void Gum(int mat,Vector3 c,float rx,float rz,float h,float yaw){
Vector3 P(int i,float r,float y){float a=i*Mathf.PI/4;return c+Turn(new Vector3(Mathf.Cos(a)*rx*r,y,Mathf.Sin(a)*rz*r),yaw);}
for(int i=0;i<8;i++){Face(mat,P(i,1,0),P(i,.82f,h),P(i+1,.82f,h),P(i+1,1,0));Face(mat,c+Vector3.up*h,P(i+1,.82f,h),P(i,.82f,h));}}
static void Piece(ToppingType type,Vector3 c,float yaw,bool baked,int index){
switch(type){
case ToppingType.Pepperoni:
    Cylinder(baked?1:0,c,.043f,baked?.009f:.007f);
    for(int i=0;i<5;i++){var p=c+Turn(Ring(.012f+(i%2)*.013f,baked?.0095f:.0075f,i*2.4f),yaw);Disc(baked?3:2,p,.0035f,1,5);}
    if(baked)for(int i=0;i<3;i++)Disc(1,c+Turn(Ring(.035f,.010f,i*2.1f),yaw),.004f,1,5);
    break;
case ToppingType.Pepper:
    // Tapered curved red chilli with a contrasting green calyx and stem.
    var centers=new[]{new Vector3(-.012f,.013f,-.044f),new Vector3(-.005f,.016f,-.023f),new Vector3(.003f,.014f,.004f),new Vector3(.012f,.010f,.029f),new Vector3(.028f,.003f,.050f)};
    var radii=new[]{.012f,.016f,.014f,.009f,.0008f};
    for(int j=0;j<centers.Length-1;j++)for(int k=0;k<8;k++){
        Vector3 P(int q,int v){float a=v*Mathf.PI/4;return c+Turn(centers[q]+new Vector3(Mathf.Cos(a)*radii[q],Mathf.Sin(a)*radii[q]*.65f,0),yaw);}
        Face(baked?20:19,P(j,k),P(j,k+1),P(j+1,k+1),P(j+1,k));}
    Box(4,c+Turn(new Vector3(-.014f,.015f,-.046f),yaw),new Vector3(.026f,.015f,.012f),yaw);
    Box(4,c+Turn(new Vector3(-.019f,.018f,-.060f),yaw),new Vector3(.008f,.008f,.025f),yaw-22);
    break;
case ToppingType.Pineapple:
    // Curved wedge of fruit: broad yellow face, pale core, radial fibres and rind edge.
    float h=baked?.019f:.025f;
    Vector3 W(float radius,float angle,float y)=>c+Turn(new Vector3(-Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius-.038f),yaw);
    for(int j=0;j<8;j++){
        float a=-.85f+j*1.7f/8,b=a+1.7f/8;
        Face(baked?7:6,W(.012f,a,h),W(.012f,b,h),W(.075f,b,h),W(.075f,a,h));
        Face(21,W(.075f,a,0),W(.075f,a,h),W(.075f,b,h),W(.075f,b,0));
        Face(baked?7:6,W(.012f,b,0),W(.012f,b,h),W(.012f,a,h),W(.012f,a,0));
        if(j%2==0)Face(2,W(.025f,a,h+.0005f),W(.025f,a+.025f,h+.0005f),W(.066f,a+.025f,h+.0005f),W(.066f,a,h+.0005f));
    }
    Face(6,W(.012f,-.85f,0),W(.012f,-.85f,h),W(.075f,-.85f,h),W(.075f,-.85f,0));
    Face(6,W(.075f,.85f,0),W(.075f,.85f,h),W(.012f,.85f,h),W(.012f,.85f,0));
    break;
case ToppingType.Chocolate:
    // Six raised squares and deep seams read as a chocolate bar even at gameplay distance.
    if(baked)Disc(8,c+Vector3.up*.001f,.056f,.78f,12,.08f);
    Box(8,c+Vector3.up*.005f,new Vector3(.069f,.010f,.095f),yaw);
    for(int row=0;row<3;row++)for(int col=0;col<2;col++){
        var q=c+Turn(new Vector3((col-.5f)*.033f,.010f,(row-1)*.03f),yaw);
        BevelBox(9,q,new Vector3(.028f,baked?.012f:.019f,.025f),yaw);
    }
    break;
case ToppingType.Jelly:
    // Face-up gummy bear: separate ears, muzzle, arms and feet preserve an unmistakable silhouette.
    int m=index%2==0?10:11;float squash=baked?.72f:1;
    void G(float x,float z,float rx,float rz,float height,int material){Gum(material,c+Turn(new Vector3(x,0,z),yaw),rx,rz,height*squash,yaw);}
    G(0,.005f,.021f,.027f,.026f,m); // belly
    G(-.017f,-.045f,.012f,.012f,.025f,m);G(.017f,-.045f,.012f,.012f,.025f,m);
    G(0,-.027f,.024f,.023f,.032f,m); // head
    G(-.027f,-.002f,.011f,.018f,.022f,m);G(.027f,-.002f,.011f,.018f,.022f,m);
    G(-.014f,.032f,.012f,.018f,.021f,m);G(.014f,.032f,.012f,.018f,.021f,m);
    foreach(float side in new[]{-1f,1f})Disc(8,c+Turn(new Vector3(side*.009f,.033f*squash,-.032f),yaw),.003f,1,6);
    Gum(11,c+Turn(new Vector3(0,.030f*squash,-.021f),yaw),.007f,.005f,.004f*squash,yaw);
    break;
case ToppingType.Cheese:Box(index%3==0?14:13,c+Vector3.up*.004f,new Vector3(.046f,.008f,.014f),yaw);break;
}}
static void Save(GameObject root,string name){try{PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}}
[MenuItem("Tools/Delivery Service/Mok/Build Toppings and Trays")]
public static void Build(){
palette=new[]{Mat("ToppingPepperoni","#A74230"),Mat("ToppingRoastedRed","#743222"),Mat("ToppingFat","#D7B685"),Mat("ToppingSear","#99602D"),Mat("ToppingPepper","#4C723C"),Mat("ToppingRoastedPepper","#3D5029"),Mat("ToppingPineapple","#E4B94F"),Mat("ToppingRoastedPineapple","#D8AD46"),Mat("ToppingChocolate","#4D2C24"),Mat("ToppingChocolateLight","#80553E"),Mat("ToppingJellyBerry","#C1517E",.32f),Mat("ToppingJellyOrange","#E39B42",.32f),Mat("TraySteel","#677978",.22f),Mat("CheeseIvory","#E8D8A3"),Mat("CheeseWarm","#D5BA73"),Mat("TrayRim","#94A09A",.22f),Mat("TrayInset","#394B4C"),Mat("DoughCream","#DCC79E"),Mat("SauceTomato","#A83C29",.25f),Mat("ChilliRed","#C83F2D"),Mat("ChilliRoasted","#A63A29"),Mat("PineappleRind","#AA8236")};
var types=new[]{ToppingType.Pepperoni,ToppingType.Pepper,ToppingType.Pineapple,ToppingType.Chocolate,ToppingType.Jelly};
foreach(var type in types)foreach(bool baked in new[]{false,true}){
var root=new GameObject(type+(baked?"Baked":"Raw"));Begin();
// Top of shredded cheese is 0.0095 m. Every topping starts above 0.016 m.
int count=type==ToppingType.Pepperoni?12:10;float phase=Array.IndexOf(types,type)*.63f;
for(int i=0;i<count;i++){float a=i*2.39996f+phase;float r=.325f*Mathf.Sqrt((i+.5f)/count);Piece(type,Ring(r,.016f,a),i*71,baked,i);}
Finish(root.name+"Mesh",root.transform);Save(root,root.name);
}
foreach(ToppingType type in Enum.GetValues(typeof(ToppingType))){
var root=new GameObject("Tray"+type);Begin();
Box(16,new Vector3(0,.012f,0),new Vector3(.45f,.024f,.31f));
foreach(float s in new[]{-1f,1f}){
Box(12,new Vector3(s*.221f,.066f,0),new Vector3(.015f,.11f,.32f));
Box(12,new Vector3(0,.066f,s*.151f),new Vector3(.45f,.11f,.015f));
Box(15,new Vector3(s*.228f,.124f,0),new Vector3(.024f,.012f,.34f));
Box(15,new Vector3(0,.124f,s*.158f),new Vector3(.48f,.012f,.024f));}
Box(16,new Vector3(0,.065f,-.161f),new Vector3(.12f,.038f,.003f));
if(type==ToppingType.Dough){for(int i=0;i<4;i++)Cylinder(17,new Vector3((i%2)*.006f,.033f+i*.025f,0),.137f,.022f,18);}
else if(type==ToppingType.Sauce){Box(18,new Vector3(0,.094f,0),new Vector3(.426f,.018f,.286f));}
else if(type==ToppingType.Cheese||type==ToppingType.Pepperoni){int count=type==ToppingType.Cheese?140:32;
for(int i=0;i<count;i++){float a=i*2.39996f,r=Mathf.Sqrt((i+.5f)/count);
Piece(type,new Vector3(Mathf.Cos(a)*r*.17f,.092f+(i%3)*.005f,Mathf.Sin(a)*r*.1f),i*71,false,i);}}
else { // Fewer, separated pieces make the silhouette readable in the supply tray.
for(int i=0;i<8;i++)Piece(type,new Vector3((i%4-1.5f)*.098f,.092f,(i/4-.5f)*.133f),(i%3-1)*9,false,i);
}
Finish(root.name+"Mesh",root.transform);Save(root,root.name);
// The carried tray uses the same new artwork, so the ingredient remains recognisable.
if(type!=ToppingType.Dough){var held=new GameObject("Held"+type);var tray=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Tray"+type+".prefab"),held.transform);tray.transform.localScale=Vector3.one*.65f;Save(held,held.name);}
}
AssetDatabase.SaveAssets();
}
}}
#endif
