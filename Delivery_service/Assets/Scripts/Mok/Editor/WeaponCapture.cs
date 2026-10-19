#if UNITY_EDITOR
using UnityEngine;using UnityEditor;using System.IO;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class WeaponCapture {
 static WeaponCapture(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-weapon-capture"))return;File.Delete("Temp/Mok-weapon-capture");try{Run();}catch(System.Exception e){File.WriteAllText("Temp/Mok-weapon-capture.txt",e.ToString());}}
 public static void Capture(Camera c,string path){var rt=new RenderTexture(1200,900,24);rt.Create();var prev=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1200,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);}}
 static void Run(){var merchant=Object.FindFirstObjectByType<WeaponMerchant>();var c=new GameObject("Weapon capture",typeof(Camera)).GetComponent<Camera>();c.enabled=false;c.fieldOfView=48;c.nearClipPlane=.05f;var center=merchant.transform.parent.position;c.transform.position=center+merchant.transform.parent.rotation*new Vector3(6,3.4f,-8);c.transform.LookAt(center+Vector3.up*1.4f);Capture(c,"Temp/Mok-weapon-shop.png");
 var light=new GameObject("Capture lamp",typeof(Light)).GetComponent<Light>();light.type=LightType.Point;light.intensity=15;light.range=10;light.transform.position=new Vector3(-1,-9998,-2);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.07f,.1f,.14f);c.farClipPlane=10;
 for(int k=0;k<4;k++){var go=(GameObject)PrefabUtility.InstantiatePrefab(WeaponCatalog.Model((WeaponKind)k));go.transform.position=new Vector3(0,-10000,0);go.transform.rotation=Quaternion.Euler(0,55,0);var rs=go.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);c.transform.position=bounds.center+new Vector3(.5f,.3f,-1)*Mathf.Max(.45f,bounds.size.magnitude*1.4f);c.transform.LookAt(bounds.center);Capture(c,"Temp/Mok-weapon-"+((WeaponKind)k)+".png");Object.DestroyImmediate(go);}Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(c.gameObject);File.WriteAllText("Temp/Mok-weapon-capture.txt","PASS");}
}
}
#endif
