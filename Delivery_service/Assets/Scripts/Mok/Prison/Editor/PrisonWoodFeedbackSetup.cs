#if UNITY_EDITOR
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Linq;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad] static class PrisonWoodFeedbackSetup{
 static PrisonWoodFeedbackSetup(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonWoodFeedbackSetup"))return;File.Delete("Temp/PrisonWoodFeedbackSetup");var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;
 EditorSceneManager.SaveScene(scene,"Logs/Prison-before-wood-feedback-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var work=Object.FindFirstObjectByType<PrisonFirstWork>();var fx=work.GetComponent<PrisonWoodFeedback>();if(fx)return;fx=work.gameObject.AddComponent<PrisonWoodFeedback>();fx.work=work;
 Material Mat(string name,Color color){string path="Assets/DeliveryPrisonChapter/Introduction/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,path);}return m;}
 var skin=Mat("작업 손 피부",new Color(.58f,.39f,.26f));var cloth=Mat("작업 죄수복 소매",new Color(.72f,.22f,.055f));var wood=Mat("판자 파편",new Color(.47f,.29f,.13f));
 Transform Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
 fx.hands=new Transform[2];fx.sleeves=new Transform[2];for(int i=0;i<2;i++){
 var g=new GameObject(i==0?"왼손 · 손잡이 아래":"오른손 · 손잡이 위");g.transform.SetParent(work.axe,false);g.transform.localPosition=new Vector3(0,i==0?-.10f:.08f,0);fx.hands[i]=g.transform;
 float side=i==0?-1:1;
 Part("손바닥",g.transform,new Vector3(side*.05f,0,0),new Vector3(.055f,.105f,.075f),skin);
 for(int f=0;f<4;f++)for(int joint=0;joint<3;joint++){
 float angle=(joint*65+15)*Mathf.Deg2Rad;var finger=Part("손잡이를 감싼 손가락",g.transform,new Vector3(side*Mathf.Cos(angle)*.035f,(f-1.5f)*.023f,Mathf.Sin(angle)*.035f),new Vector3(.039f,.019f,.024f),skin);finger.localRotation=Quaternion.Euler(0,-side*(joint*65+15),0);
 }
 var thumb=Part("엄지",g.transform,new Vector3(side*.024f,.048f,-.027f),new Vector3(.064f,.027f,.03f),skin);thumb.localRotation=Quaternion.Euler(0,0,side*25);
 fx.sleeves[i]=Part("주황색 소매 "+i,work.transform,Vector3.zero,Vector3.one,cloth,PrimitiveType.Cylinder);g.SetActive(false);fx.sleeves[i].gameObject.SetActive(false);
 }
 fx.chips=new Transform[16];for(int i=0;i<fx.chips.Length;i++){fx.chips[i]=Part("나무 파편 "+i,work.transform,Vector3.zero,new Vector3(.016f+(i%3)*.008f,.012f,.045f+(i%4)*.012f),wood);fx.chips[i].gameObject.SetActive(false);}
 fx.impactClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DeliveryPrisonChapter/KitchenArt/CookingAudio/Chop.wav");EditorUtility.SetDirty(fx);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Prison-wood-feedback-setup.txt","Saved two grips, orange sleeves, 16 pooled chips and single impact sound.");
 }
}}
#endif
