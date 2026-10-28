#if UNITY_EDITOR
using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class VenueCarInteriorUpgrade {
 const string Art="Assets/Scripts/Mok/Art/VenueFinale/";
 static Material Mat(string name,Color color){string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.16f);EditorUtility.SetDirty(m);return m;}
 public static void Preview(){var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();bool active=m.car.gameObject.activeSelf;var rotation=m.door.localRotation;var go=new GameObject("Door inspection camera");var camera=go.AddComponent<Camera>();var rt=new RenderTexture(1000,700,24);var previous=RenderTexture.active;try{m.car.gameObject.SetActive(true);camera.targetTexture=rt;camera.nearClipPlane=.03f;camera.fieldOfView=50;camera.transform.position=m.car.TransformPoint(new Vector3(-3,1.8f,-1.7f));camera.transform.LookAt(m.car.TransformPoint(new Vector3(-.65f,.8f,.1f)));foreach(var angle in new[]{0,70}){m.door.localRotation=Quaternion.Euler(0,angle,0);camera.Render();RenderTexture.active=rt;var image=new Texture2D(1000,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,700),0,0);image.Apply();File.WriteAllBytes("Logs/Venue-door-"+angle+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}}finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);m.car.gameObject.SetActive(active);m.door.localRotation=rotation;}}
 public static void Apply(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();if(!EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Venue-before-door-lining-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
 var car=m.car;var old=car.Find("Cabin lining");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);old=m.door.Find("Door inner lining");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);m.door.localRotation=Quaternion.identity;
 var cabin=new GameObject("Cabin lining").transform;cabin.SetParent(car,false);var door=new GameObject("Door inner lining").transform;door.SetParent(m.door,false);
 var trim=Mat("Cabin charcoal trim",new Color(.055f,.065f,.07f));var cloth=Mat("Cabin fabric",new Color(.09f,.105f,.115f));var metal=Mat("Cabin muted steel",new Color(.23f,.25f,.26f));var rubber=Mat("Cabin rubber seals",new Color(.025f,.03f,.035f));
 GameObject Box(string name,Transform parent,Vector3 point,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetPositionAndRotation(car.TransformPoint(point),car.rotation);g.transform.SetParent(parent,true);g.transform.localScale=new Vector3(size.x/car.lossyScale.x,size.y/car.lossyScale.y,size.z/car.lossyScale.z);g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 void Beam(string name,Vector3 a,Vector3 b,float width,Material material){var g=Box(name,cabin,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),material);g.transform.rotation=car.rotation*Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
 Box("Cabin floor",cabin,new Vector3(0,.39f,-.05f),new Vector3(1.7f,.08f,1.9f),trim);
 Box("Driver floor mat",cabin,new Vector3(-.43f,.44f,.38f),new Vector3(.62f,.025f,.66f),rubber);
 Box("Seat underside",cabin,new Vector3(-.45f,.5f,-.38f),new Vector3(.65f,.16f,.7f),trim);
 Box("Lower dashboard bulkhead",cabin,new Vector3(0,.65f,.86f),new Vector3(1.65f,.48f,.08f),trim);
 Box("Driver sill",cabin,new Vector3(-.89f,.46f,.075f),new Vector3(.15f,.12f,1.45f),trim);
 Box("Sill scuff plate",cabin,new Vector3(-.9f,.525f,.075f),new Vector3(.1f,.015f,1.16f),metal);
 Beam("Rear door jamb",new Vector3(-.86f,.5f,-.65f),new Vector3(-.83f,.94f,-.65f),.055f,trim);
 Beam("Front door jamb",new Vector3(-.87f,.5f,.8f),new Vector3(-.83f,.94f,.72f),.045f,trim);
 Box("Door backing",door,new Vector3(-.86f,.7f,.075f),new Vector3(.075f,.43f,1.37f),trim);
 Box("Door fabric inset",door,new Vector3(-.81f,.73f,.06f),new Vector3(.025f,.24f,.92f),cloth);
 Box("Armrest",door,new Vector3(-.755f,.72f,.08f),new Vector3(.13f,.06f,.58f),trim);
 Box("Interior handle",door,new Vector3(-.735f,.85f,.4f),new Vector3(.04f,.035f,.18f),metal);
 Box("Door lower pocket",door,new Vector3(-.79f,.55f,-.12f),new Vector3(.07f,.09f,.65f),rubber);
 EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Venue-car-interior.txt","Saved matching charcoal inner door, armrest, handle, seals, jambs, sill, floor and seat backing; no gameplay colliders added.");
 }
}}
#endif
