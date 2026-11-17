#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static class PrisonChickenPlateSetup
    {
        const string Folder = "Assets/DeliveryPrisonChapter/KitchenArt/PlatedChicken";
        const string Prefab = Folder + "/잘라 담은 양념치킨.prefab";
        public static void Run()
        {
            try
            {
                if (EditorSceneManager.GetActiveScene().isDirty) throw new Exception("Unsaved scene: stop before editing.");
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Prison-before-plated-chicken-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DeliveryPrisonChapter/KitchenArt/로우폴리 둥근 소품.asset");
                if(!mesh)throw new Exception("Existing faceted food mesh missing");
                var sauce=Material("양념 튀김",new Color(.82f,.245f,.055f));
                var crust=Material("바삭한 단면",new Color(.96f,.57f,.14f));
                var bone=Material("치킨 뼈",new Color(.94f,.87f,.69f));
                var sesame=Material("참깨",new Color(.97f,.87f,.55f));
                sauce.SetFloat("_Smoothness",.48f);
                var glaze=Material("양념 하이라이트",new Color(.96f,.39f,.09f));glaze.SetFloat("_Smoothness",.55f);
                var root=new GameObject("접시에 담긴 여덟 조각 양념치킨");
                var plateAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plate_01.prefab");
                if(!plateAsset)throw new Exception("Synty plate missing");
                var plate=(GameObject)PrefabUtility.InstantiatePrefab(plateAsset);
                plate.transform.SetParent(root.transform,false);
                var bounds=Bounds(plate);
                plate.transform.localScale*=.68f/Mathf.Max(bounds.size.x,bounds.size.z);
                bounds=Bounds(plate);plate.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                float top=Bounds(plate).max.y;
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;
                    Vector3 p=new Vector3(Mathf.Cos(angle)*.18f,top+.043f+(i%3)*.008f,Mathf.Sin(angle)*.18f);
                    var piece=new GameObject(i%4==0?"양념 닭다리":i%4==1?"양념 윙":"바삭한 순살 조각").transform;
                    piece.SetParent(root.transform,false);piece.localPosition=p;
                    piece.localRotation=Quaternion.Euler(i%2==0?5:-4,-angle*Mathf.Rad2Deg+30,0);
                    if(i%4==0)
                    {
                        Part(piece,"닭다리 두툼한 살",mesh,new Vector3(-.025f,0,0),new Vector3(.14f,.11f,.115f),sauce);
                        Part(piece,"닭다리 가늘어지는 부분",mesh,new Vector3(.042f,-.005f,0),new Vector3(.13f,.06f,.065f),sauce);
                        Part(piece,"짧은 뼈",mesh,new Vector3(.112f,-.005f,0),new Vector3(.045f,.025f,.022f),bone);
                        Part(piece,"뼈 마디 1",mesh,new Vector3(.135f,-.002f,.009f),new Vector3(.027f,.03f,.025f),bone);
                        Part(piece,"뼈 마디 2",mesh,new Vector3(.135f,-.002f,-.009f),new Vector3(.027f,.03f,.025f),bone);
                    }
                    else if(i%4==1)
                    {
                        Part(piece,"윙 봉",mesh,new Vector3(-.032f,0,0),new Vector3(.15f,.075f,.082f),sauce);
                        var wing=Part(piece,"접힌 날개",mesh,new Vector3(.045f,.008f,.025f),new Vector3(.13f,.055f,.065f),sauce);
                        wing.localRotation=Quaternion.Euler(0,-55,0);
                        Part(piece,"바삭한 날개 끝",mesh,new Vector3(.083f,.01f,.064f),new Vector3(.065f,.035f,.04f),crust);
                    }
                    else
                    {
                        Part(piece,"불규칙한 순살",mesh,Vector3.zero,new Vector3(.125f,.085f,.115f),sauce);
                        Part(piece,"돌출된 튀김옷",mesh,new Vector3(.037f,-.008f,.03f),new Vector3(.07f,.065f,.07f),sauce);
                    }
                    // Small angular crust clusters break up the smooth, meatball-like silhouette.
                    for(int j=0;j<13;j++)
                    {
                        float a=j*2.399f+i*.31f;
                        float radius=.025f+(j%3)*.014f;
                        Vector3 pos=new Vector3(Mathf.Cos(a)*radius-.012f,.037f-(radius-.025f)*.5f,Mathf.Sin(a)*radius*.8f);
                        float size=.009f+(j%3)*.002f;
                        float rx=i%4==0?.07f:i%4==1?.075f:.0625f;
                        float ry=i%4==0?.055f:i%4==1?.0375f:.0425f;
                        float rz=i%4==0?.0575f:i%4==1?.041f:.0575f;
                        float cx=i%4==0?-.025f:i%4==1?-.032f:0;
                        pos.x=cx+Mathf.Cos(a)*radius*.8f;pos.z=Mathf.Sin(a)*radius*.65f;
                        pos.y=ry*Mathf.Sqrt(Mathf.Max(.12f,1-Mathf.Pow((pos.x-cx)/rx,2)-Mathf.Pow(pos.z/rz,2)))-size*.25f;
                        Part(piece,"바삭한 튀김옷 "+j,mesh,pos,new Vector3(size*1.3f,size,size),j%4==0?crust:j%3==0?glaze:sauce);
                    }
                    for(int j=0;j<6;j++)
                    {
                        float a=j*2.4f+i;
                                                float rx=i%4==0?.07f:i%4==1?.075f:.0625f, ry=i%4==0?.055f:i%4==1?.0375f:.0425f;
                        float rz=i%4==0?.0575f:i%4==1?.041f:.0575f, cx=i%4==0?-.025f:i%4==1?-.032f:0;
                        float sx=Mathf.Cos(a)*.028f,sz=Mathf.Sin(a)*.022f;
                        float sy=ry*Mathf.Sqrt(1-Mathf.Pow(sx/rx,2)-Mathf.Pow(sz/rz,2));
                        var seed=Part(piece,"볶은 참깨 "+j,mesh,new Vector3(cx+sx,sy,sz),new Vector3(.009f,.003f,.004f),sesame);
                        seed.localRotation=Quaternion.Euler(0,j*37,0);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);Object.DestroyImmediate(root);
                var mission=Object.FindFirstObjectByType<PrisonChickenMission>();if(!mission)throw new Exception("Prison cooking missing");
                Attach(mission.cooking.cookedChicken.transform,false);
                Attach(mission.carrying.transform,true);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/PrisonChickenPlateSetup.txt","PASS: scene backed up; Synty plate + eight pieces saved as prefab; cooked/carry references preserved; only PrisonChapter scene saved.");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/PrisonChickenPlateSetup.txt",e.ToString());if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void Attach(Transform root,bool carried)
        {
            var previous=root.Find("Plated chicken visual");if(previous)Object.DestroyImmediate(previous.gameObject);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),root);
            visual.name="Plated chicken visual";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
            // Keep the existing root's position/rotation and all mission references intact.
            Vector3 scale=root.lossyScale;float size=carried?.76f:1f;
            visual.transform.localScale=new Vector3(size/Mathf.Abs(scale.x),size/Mathf.Abs(scale.y),size/Mathf.Abs(scale.z));
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }
        static Material Material(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(m);return m;
        }
        static Bounds Bounds(GameObject root){var renderers=root.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        static Transform Part(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 scale,Material material)
        {
            var p=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));p.transform.SetParent(parent,false);p.transform.localPosition=position;p.transform.localScale=scale;
            p.GetComponent<MeshFilter>().sharedMesh=mesh;p.GetComponent<MeshRenderer>().sharedMaterial=material;return p.transform;
        }
    }
}
#endif


