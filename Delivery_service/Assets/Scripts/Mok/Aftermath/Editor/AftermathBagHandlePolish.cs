#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathBagHandlePolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-bag-handle-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>();var h=film.hideout;
                h.bag.SetPositionAndRotation(h.hiddenBag,Quaternion.identity);
                var old=h.bag.Find("양손 운반 손잡이");if(old)Object.DestroyImmediate(old.gameObject);
                var root=new GameObject("양손 운반 손잡이").transform;root.SetParent(h.bag);root.position=h.hiddenBag;root.rotation=Quaternion.identity;root.localScale=Vector3.one;
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/Aftermath/오래된 나무.mat");
                if(!material)material=h.bag.GetComponentInChildren<Renderer>().sharedMaterial;
                Vector3 grip=h.hiddenBag+new Vector3(-.10f,.43f,0);
                void Strap(string name,Vector3 from,Vector3 to,float width)
                {
                    var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(root);
                    part.transform.position=(from+to)*.5f;part.transform.rotation=Quaternion.FromToRotation(Vector3.up,to-from);
                    var size=new Vector3(width,Vector3.Distance(from,to),width);
                    var scale=root.lossyScale;part.transform.localScale=new Vector3(size.x/scale.x,size.y/scale.y,size.z/scale.z);
                    part.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(part.GetComponent<Collider>());
                }
                Strap("손 사이로 보이는 가죽 손잡이",grip+Vector3.back*.16f,grip+Vector3.forward*.16f,.028f);
                foreach(float z in new[]{-.16f,.16f})
                    Strap("가방에 연결된 손잡이 끈",h.hiddenBag+new Vector3(-.10f,.24f,z),grip+Vector3.forward*z,.023f);
                h.rightGrip.position=grip+Vector3.back*.085f;
                h.leftGrip.position=grip+Vector3.forward*.085f;
                foreach(var motion in film.shots[2].motions)
                    if(motion.actor==film.firstPerson.hideoutPlayer){motion.to.position=new Vector3(500.70f,0,-.8f);motion.moveDelay=.45f;}
                foreach(var motion in film.shots[3].motions)
                    if(motion.actor==film.firstPerson.hideoutPlayer){motion.from.position=motion.to.position=new Vector3(500.70f,0,-.8f);}
                EditorSceneManager.SaveScene(scene);AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
