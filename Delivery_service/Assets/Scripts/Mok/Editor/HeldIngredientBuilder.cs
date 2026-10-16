#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class HeldIngredientBuilder
    {
        const string Folder="Assets/Scripts/Mok/Resources/MokPizzaArt";
        static HeldIngredientBuilder()
        {
            if(File.Exists("Temp/Mok-held-build"))EditorApplication.delayCall+=()=>
            {
                File.Delete("Temp/Mok-held-build");
                try {Build();File.WriteAllText("Temp/Mok-held-build.txt","PASS: Held ingredient visuals built.");}
                catch(Exception e){File.WriteAllText("Temp/Mok-held-build.txt",e.ToString());Debug.LogException(e);}
            };
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");
        static void Part(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,float yaw=0)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        [MenuItem("Tools/Delivery Service/Mok/Build Held Ingredient Models")]
        public static void Build()
        {
            var green=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PepperGreen.mat");
            if(green==null){green=new Material(Shader.Find("Universal Render Pipeline/Lit"));green.SetColor("_BaseColor",new Color(.28f,.4f,.18f));green.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(green,Folder+"/PepperGreen.mat");}
            foreach(var type in new[]{ToppingType.Sauce,ToppingType.Cheese,ToppingType.Pepper,ToppingType.Pepperoni,ToppingType.Pineapple,ToppingType.Chocolate,ToppingType.Jelly})
            {
                var root=new GameObject("Held"+type);
                try {
                    if(type==ToppingType.Sauce)
                    {
                        Part(root.transform,"Tin",PrimitiveType.Cylinder,new Vector3(0,.04f,0),new Vector3(.18f,.055f,.18f),Mat("BoxInside"));
                        Part(root.transform,"Red Label",PrimitiveType.Cylinder,new Vector3(0,.035f,0),new Vector3(.182f,.029f,.182f),Mat("InkRed"));
                        Part(root.transform,"Sauce",PrimitiveType.Cylinder,new Vector3(0,.0955f,0),new Vector3(.16f,.001f,.16f),Mat("SauceTomato"));
                    }
                    else
                    {
                        var paper=Mat("BoxInside");
                        Part(root.transform,"Tray",PrimitiveType.Cube,new Vector3(0,-.015f,0),new Vector3(.3f,.015f,.23f),paper);
                        foreach(float side in new[]{-1f,1f}){
                            Part(root.transform,"Tray rim",PrimitiveType.Cube,new Vector3(side*.147f,.005f,0),new Vector3(.009f,.045f,.23f),paper);
                            Part(root.transform,"Tray rim",PrimitiveType.Cube,new Vector3(0,.005f,side*.112f),new Vector3(.3f,.045f,.009f),paper);
                        }
                        int count=type==ToppingType.Cheese?46:14;
                        for(int i=0;i<count;i++)
                        {
                            float a=i*2.39996f,r=Mathf.Sqrt((i+.5f)/count);
                            var pos=new Vector3(Mathf.Cos(a)*r*.11f,.005f+(i%3)*.003f,Mathf.Sin(a)*r*.078f);
                            var size=new Vector3(.036f,.017f,.03f);var primitive=PrimitiveType.Cube;Material material;
                            switch(type){
                                case ToppingType.Cheese:material=Mat(i%3==0?"CheeseWarm":"CheeseIvory");size=new Vector3(.038f,.006f,.011f);break;
                                case ToppingType.Pepperoni:material=Mat("SauceTomato");primitive=PrimitiveType.Cylinder;size=new Vector3(.047f,.003f,.047f);break;
                                case ToppingType.Pepper:material=green;size=new Vector3(.042f,.012f,.013f);break;
                                case ToppingType.Pineapple:material=Mat("MeltedGold");break;
                                case ToppingType.Chocolate:material=Mat("ToastBrown");break;
                                default:material=Mat("InkRed");size=new Vector3(.028f,.024f,.027f);break;
                            }
                            Part(root.transform,type.ToString(),primitive,pos,size,material,i*71);
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Held"+type+".prefab");
                } finally {UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
