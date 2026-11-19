#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        public static void FinishArt()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-finish.unity",true);
                film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;
                font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");dark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"차콜.mat");cream=AssetDatabase.LoadAssetAtPath<Material>(Folder+"서류.mat");
                var forest=film.shots[0].set.transform;var trade=film.shots[3].set.transform;var shop=film.shots[6].set.transform;
                foreach(var t in forest.GetComponentsInChildren<Transform>(true))if(t.name=="SM_Gen_Env_Mountain_02")t.position+=Vector3.forward*90;
                // Both palms support the underside, near the side edges, rather than entering the lid.
                var box=shop.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Box");
                foreach(var t in box.GetComponentsInChildren<Transform>())
                {
                    if(t.name=="피자 상자 왼손 받침")t.position=box.position+new Vector3(-.33f,-.025f,-.17f);
                    if(t.name=="피자 상자 오른손 받침")t.position=box.position+new Vector3(.33f,-.025f,-.17f);
                }
                var tv=film.newsHeadline.GetComponentInParent<Canvas>(true).transform;
                tv.position+=Vector3.up*.09f;
                film.shots[6].lookAt.position+=Vector3.up*.09f;
                film.shots[6].cameraFrom.position+=Vector3.up*.09f;film.shots[6].cameraTo.position+=Vector3.up*.09f;
                film.shots[6].fieldOfView=42;
                var cards=trade.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("새 신분증 ")).OrderBy(t=>t.name).ToArray();
                for(int i=0;i<cards.Length;i++)
                {
                    var card=cards[i];
                    var portrait=new GameObject("새 신분증 사진",typeof(MeshFilter),typeof(MeshRenderer));portrait.transform.SetParent(card.parent);portrait.transform.position=card.position+new Vector3(-.078f,.008f,0);portrait.transform.rotation=Quaternion.Euler(90,0,0);portrait.transform.localScale=new Vector3(.073f,.10f,1);
                    var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);portrait.GetComponent<MeshFilter>().sharedMesh=quad.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(quad);
                    var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"수배사진"+i+".png");AssetDatabase.CreateAsset(mat,Folder+"신분증 사진 "+i+".mat");portrait.GetComponent<Renderer>().sharedMaterial=mat;
                    Box("신분증 정보선",card.parent,card.position+new Vector3(.045f,.009f,.03f),new Vector3(.10f,.002f,.008f),dark);
                    Box("신분증 정보선",card.parent,card.position+new Vector3(.045f,.009f,0),new Vector3(.10f,.002f,.008f),dark);
                }
                foreach(var text in trade.GetComponentsInChildren<TextMesh>(true)) {text.characterSize=.0028f;text.transform.position+=new Vector3(.04f,0,-.055f);}
                var prison=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
                var intro=prison.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrisonIntroduction>(true)).Single();
                for(int i=0;i<2;i++)
                {
                    var copy=CloneVisual(i==0?intro.mugshotActor:intro.mingi.animator.transform,root);copy.SetActive(true);
                    foreach(var r in copy.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
                    var a=copy.AddComponent<AftermathActor>();a.animator=copy.GetComponentInChildren<Animator>(true);a.animator.Rebind();a.animator.Update(0);
                    var tex=Portrait(a,i);shop.GetComponentsInChildren<RawImage>(true)[i].texture=tex;Object.DestroyImmediate(copy);
                }
                EditorSceneManager.CloseScene(prison,true);
                // A longer lens hides the incidental furniture and focuses on the seated silhouette.
                film.shots[7].cameraFrom.position=new Vector3(1.8f,1.35f,5.0f);film.shots[7].cameraTo.position=new Vector3(1.5f,1.35f,4.8f);film.shots[7].lookAt.position=new Vector3(0,1,3);film.shots[7].fieldOfView=38;
                // Improve dusk contrast without modifying the original new-city lighting.
                RenderSettings.ambientIntensity=.7f;
                foreach(var l in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))if(l.type==LightType.Directional){l.intensity=.6f;l.color=new Color(.85f,.89f,1);}
                foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Capture();File.WriteAllText("Logs/Aftermath-finish.txt","PASS art refinement saved");EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("Logs/Aftermath-finish.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif

