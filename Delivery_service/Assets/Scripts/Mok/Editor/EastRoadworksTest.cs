#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class EastRoadworksTest {
static void Check(bool ok,string label,Action<string> log){if(!ok)throw new Exception(label);log("PASS: "+label);}
public static IEnumerator RunCore(Action<string> log){
var root=GameObject.Find("Mok East City/Roadworks and parking").transform;
Check(root.childCount==8&&root.GetComponentsInChildren<NavMeshObstacle>().Length==40,"공사 8구간·주차 차량 32대 충돌 및 길찾기 장애물",log);
Physics.SyncTransforms();foreach(Transform site in root){foreach(float side in new[]{-1f,1f}){Vector3 from=site.position+new Vector3(side*3,.8f,-11);var hits=Physics.SphereCastAll(from,.65f,Vector3.forward,22,~0,QueryTriggerInteraction.Ignore);Check(!hits.Any(h=>h.transform.IsChildOf(root)),site.name+" 오토바이 통로 "+side,log);}Check(Physics.RaycastAll(site.position+new Vector3(0,.65f,-12),Vector3.forward,24,~0,QueryTriggerInteraction.Ignore).Any(h=>h.transform.IsChildOf(site)),site.name+" 공사 충돌체",log);}
var manager=UnityEngine.Object.FindFirstObjectByType<OrderManager>();manager.enabled=false;var policy=manager.GetComponent<DistrictDeliveryPolicy>();var nav=UnityEngine.Object.FindFirstObjectByType<NavigationManager>();var old=nav.NavigationTarget;var origin=new GameObject("Roadworks navigation test");nav.SetNavigationTarget(origin.transform);var shop=UnityEngine.Object.FindObjectsByType<DeliveryService.Mok.Navigation.NavigationPlace>(FindObjectsSortMode.None).First(p=>p.kind==DeliveryService.Mok.Navigation.NavigationPlaceKind.Pizza);
foreach(var p in policy.eastCity){typeof(OrderManager).GetField("currentTargetDeliveryID",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,p.DeliveryPointID);origin.transform.position=shop.Destination.position;nav.ResumeDelivery();Check(nav.DisplayPath.Count>0,"공사 배치 후 배달 경로 "+p.DeliveryPointID,log);bool access=false;foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})if(Physics.Raycast(p.transform.position+Vector3.up*.85f+dir*2,-dir,out var hit,3,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<DeliveryPoint>()==p)access=true;Check(access,"배달함 접근 "+p.DeliveryPointID,log);}
nav.SetNavigationTarget(old);nav.StopGuidance();UnityEngine.Object.Destroy(origin);
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;typeof(DayNightCycle).GetMethod("UpdateLighting",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(cycle,null);Capture();
if(File.Exists("Temp/Mok-roadworks-preview-only")){File.Delete("Temp/Mok-roadworks-preview-only");yield break;}
var regression=EastTrafficTest.RunCore(log);while(regression.MoveNext())yield return regression.Current;
}
static void Capture(){var go=new GameObject("Roadworks preview");var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=65;c.farClipPlane=1800;c.nearClipPlane=.05f;c.cullingMask=~((1<<20)|(1<<17));c.transform.position=new Vector3(294,6,-82);c.transform.LookAt(new Vector3(290,0,-62));var rt=new RenderTexture(1280,800,24);rt.Create();c.targetTexture=rt;var old=RenderTexture.active;try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Temp/Mok-roadworks-preview.png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}finally{RenderTexture.active=old;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}}
}
}
#endif
