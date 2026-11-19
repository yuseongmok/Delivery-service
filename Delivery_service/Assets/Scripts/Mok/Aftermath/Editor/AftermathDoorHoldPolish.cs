#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathDoorHoldPolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-door-hold-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>();var h=film.hideout;
                h.cabinetDoor.localRotation=Quaternion.identity;
                var material=h.cabinetDoor.GetComponentsInChildren<MeshRenderer>(true).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.y).First().sharedMaterial;
                foreach(var renderer in h.cabinetDoor.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var set=film.shots[2].set.transform;
                var old=set.Find("보관함 오른쪽 고정문");if(old)Object.DestroyImmediate(old.gameObject);
                var oldDress=h.cabinetDoor.Find("손에 맞춘 문");if(oldDress)Object.DestroyImmediate(oldDress.gameObject);
                h.cabinetDoor.position=new Vector3(500.85f,.25f,1.5f);
                var dress=new GameObject("손에 맞춘 문").transform;dress.SetParent(h.cabinetDoor,false);
                void Box(string name,Transform parent,Vector3 position,Vector3 size)
                {
                    var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());
                }
                Box("왼쪽 여닫이문",dress,new Vector3(500.525f,1.1f,1.5f),new Vector3(.65f,1.7f,.08f));
                Box("보관함 오른쪽 고정문",set,new Vector3(501.275f,1.1f,1.5f),new Vector3(.85f,1.7f,.08f));
                Box("세로 손잡이",dress,new Vector3(500.30f,1.35f,1.40f),new Vector3(.035f,.20f,.035f));
                foreach(float y in new[]{1.26f,1.44f})Box("손잡이 받침",dress,new Vector3(500.30f,y,1.435f),new Vector3(.035f,.025f,.07f));
                h.doorGrip.position=new Vector3(500.30f,1.35f,1.40f);
                h.openAngle=-90;
                var pose=film.GetComponent<AftermathDoorHand>();if(!pose)pose=film.gameObject.AddComponent<AftermathDoorHand>();pose.hideout=h;
                EditorSceneManager.SaveScene(scene);AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
