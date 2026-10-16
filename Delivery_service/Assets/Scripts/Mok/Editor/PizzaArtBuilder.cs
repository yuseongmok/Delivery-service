#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PizzaArtBuilder
    {
        const string Folder = "Assets/Scripts/Mok/Resources/MokPizzaArt";
        static readonly List<Vector3> vertices = new List<Vector3>();
        static List<int>[] triangles;
        static Material[] palette;
        static PizzaArtBuilder()
        {
            if (File.Exists("Temp/Mok-pizza-art-build")) EditorApplication.delayCall += () =>
            {
                File.Delete("Temp/Mok-pizza-art-build");
                try { Build(); File.WriteAllText("Temp/Mok-pizza-art-build.txt", "PASS: Pizza stage meshes, lit materials and hinged box prefabs created."); }
                catch (Exception e) { File.WriteAllText("Temp/Mok-pizza-art-build.txt", e.ToString()); Debug.LogException(e); }
            };
        }
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
        static void Crust(Transform parent, bool baked)
        {
            Begin();
            var profile = new[] {new Vector2(0,-.055f),new Vector2(.405f,-.055f),new Vector2(.46f,-.035f),new Vector2(.476f,-.005f),new Vector2(.462f,.025f),new Vector2(.435f,.035f),new Vector2(.405f,.017f),new Vector2(.38f,-.013f),new Vector2(0,-.013f)};
            for (int i = 0; i < 36; i++) for (int j = 0; j < profile.Length - 1; j++)
            {
                Vector3 P(int k, int v) { float a=k*Mathf.PI*2/36; float irregular=1+.008f*Mathf.Sin(k*2.1f); return Ring(profile[v].x*irregular,profile[v].y,a); }
                int mat = baked ? (j >= 3 && j <= 5 && i % 8 == 0 ? 4 : 3) : (i % 5 == 0 ? 1 : 0);
                Face(mat,P(i,j),P(i,j+1),P(i+1,j+1),P(i+1,j));
            }
            if (baked) for(int i=0;i<23;i++) {float a=i*2.39996f; var c=Ring(.435f,.036f,a); Disc(i%3==0?5:4,c,.009f+(i%3)*.003f,.65f,7);}
            Finish(baked ? "BakedCrust" : "RawCrust",parent);
        }
        [MenuItem("Tools/Delivery Service/Mok/Build Pizza Art")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var library = AssetDatabase.LoadAssetAtPath<PizzaIngredientLibrary>(Folder + "/Ingredients.asset");
            if (library == null) { library = ScriptableObject.CreateInstance<PizzaIngredientLibrary>(); AssetDatabase.CreateAsset(library, Folder + "/Ingredients.asset"); }
            library.ingredients = AssetDatabase.FindAssets("t:PizzaToppingData").Select(g => AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            EditorUtility.SetDirty(library);
            palette = new[] { Mat("DoughCream","#DCC79E"),Mat("DoughFlour","#E9D6B1"),Mat("SauceTomato","#A83C29",.25f),Mat("GoldenCrust","#B57734"),Mat("ToastAmber","#8D4B23"),Mat("ToastBrown","#60321D"),Mat("CheeseIvory","#E8D8A3"),Mat("CheeseWarm","#D5BA73"),Mat("MeltedGold","#E4C277",.28f),Mat("CaramelCheese","#B07A39"),Mat("Kraft","#AB8760"),Mat("BoxInside","#CDB38B"),Mat("InkRed","#783C30"),Mat("InkCream","#E6D7B6") };
            var pizza = new GameObject("PizzaStages");
            try
            {
                Crust(pizza.transform,false); Crust(pizza.transform,true);
                Begin(); Disc(2,new Vector3(0,-.010f,0),.407f,1,40,.012f);
                Finish("Sauce",pizza.transform);
                Begin();
                for(int i=0;i<310;i++) {float a=i*2.39996f;float r=.369f*Mathf.Sqrt((i+.5f)/310); Box(i%4==0?7:6,Ring(r,-.001f+(i%3)*.003f,a),new Vector3(.044f+(i%4)*.006f,.009f,.018f+(i%2)*.004f),i*71);}
                Finish("ShreddedCheese",pizza.transform);
                Begin(); Disc(8,new Vector3(0,-.002f,0),.389f,1,48,.025f);
                for(int i=0;i<38;i++) {float a=i*2.39996f,r=.353f*Mathf.Sqrt((i+.5f)/38); Disc(i%4==0?9:7,Ring(r,.0002f,a),.013f+(i%4)*.005f,.7f,7,.16f);}
                for(int i=0;i<14;i++)
                {
                    var c=Ring(.33f*Mathf.Sqrt((i+.5f)/14),-.001f,i*2.39996f);
                    float radius=.018f+(i%3)*.007f;
                    for(int j=0;j<8;j++)Face(i%3==0?9:8,c+Vector3.up*.006f,c+Ring(radius,0,(j+1)*Mathf.PI/4),c+Ring(radius,0,j*Mathf.PI/4));
                }
                Finish("MeltedCheese",pizza.transform);
                pizza.transform.Find("BakedCrust").gameObject.SetActive(false);
                foreach(var n in new[]{"Sauce","ShreddedCheese","MeltedCheese"}) pizza.transform.Find(n).gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(pizza,Folder+"/PizzaStages.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(pizza); }
            var box = new GameObject("Box");
            try
            {
                Begin(); Box(10,new Vector3(0,.009f,0),new Vector3(1,.018f,1));
                Box(11,new Vector3(0,.019f,0),new Vector3(.97f,.002f,.97f));
                Box(10,new Vector3(-.49f,.054f,0),new Vector3(.02f,.09f,1)); Box(10,new Vector3(.49f,.054f,0),new Vector3(.02f,.09f,1));
                Box(10,new Vector3(0,.054f,-.49f),new Vector3(.96f,.09f,.02f)); Box(10,new Vector3(0,.054f,.49f),new Vector3(.96f,.09f,.02f));
                // Small printed front stripe and side vents.
                Box(12,new Vector3(0,.055f,-.501f),new Vector3(.48f,.018f,.001f));
                for(int i=0;i<3;i++) Box(5,new Vector3(.501f,.06f,-.16f+i*.16f),new Vector3(.001f,.018f,.055f));
                Finish("BoxBase",box.transform);
                var hinge = new GameObject("LidHinge").transform; hinge.SetParent(box.transform,false); hinge.localPosition=new Vector3(0,.105f,.495f);
                Begin(); Box(10,new Vector3(0,0,-.495f),new Vector3(1.016f,.014f,1.016f));
                Box(11,new Vector3(0,-.008f,-.495f),new Vector3(.98f,.002f,.98f));
                // Printed emblem: a low-poly pizza slice, framed by clean red bands.
                Box(12,new Vector3(0,.008f,-.12f),new Vector3(.85f,.001f,.018f)); Box(12,new Vector3(0,.008f,-.87f),new Vector3(.85f,.001f,.018f));
                Disc(12,new Vector3(0,.009f,-.49f),.255f,1,28);
                Face(13,new Vector3(-.15f,.010f,-.36f),new Vector3(.15f,.010f,-.36f),new Vector3(0,.010f,-.69f));
                Box(13,new Vector3(0,.010f,-.34f),new Vector3(.32f,.002f,.035f));
                Disc(12,new Vector3(-.055f,.011f,-.42f),.025f,1,8); Disc(12,new Vector3(.055f,.011f,-.43f),.025f,1,8); Disc(12,new Vector3(0,.011f,-.54f),.025f,1,8);
                Finish("BoxLid",hinge);
                PrefabUtility.SaveAsPrefabAsset(box,Folder+"/Box.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(box); }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
