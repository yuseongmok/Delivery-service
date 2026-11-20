using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// Stages the starting grid without changing vehicle physics or race results.
public sealed class RaceStartPresentation : MonoBehaviour {
 public static bool Active {get;private set;}
 public float fadeSeconds=.55f, opponentSeconds=2.6f;
 float opacity;Camera view;Transform cameraRig;Vector3 localPosition;Quaternion localRotation;float fov;bool framing;Vector3 shotPosition;Quaternion shotRotation;
 public IEnumerator Cover(){Active=true;yield return Fade(1);}
 public IEnumerator RevealOpponent(Transform opponent){
 var bike=FindFirstObjectByType<MotorcycleController>();
 if(bike&&bike.bikeCamera){view=bike.bikeCamera.GetComponentInChildren<Camera>();cameraRig=bike.bikeCamera.transform;localPosition=cameraRig.localPosition;localRotation=cameraRig.localRotation;if(view)fov=view.fieldOfView;}
 if(cameraRig&&opponent){
 var center=opponent.position+Vector3.up*1.1f;
 var from=opponent.TransformPoint(new Vector3(-3.8f,1.5f,4.8f));
 var to=opponent.TransformPoint(new Vector3(-3.1f,1.6f,4.1f));
 framing=true;shotPosition=from;shotRotation=Quaternion.LookRotation(center-from);if(view)view.fieldOfView=48;
 yield return Fade(0);
 for(float t=0;t<opponentSeconds;t+=Time.deltaTime){shotPosition=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t/opponentSeconds));shotRotation=Quaternion.LookRotation(center-shotPosition);yield return null;}
 yield return Fade(1);
 }
 RestoreView();yield return Fade(0);yield return new WaitForSeconds(.4f);Active=false;
 }
 void LateUpdate(){if(framing&&cameraRig)cameraRig.SetPositionAndRotation(shotPosition,shotRotation);}
 IEnumerator Fade(float target){float initial=opacity;for(float t=0;t<fadeSeconds;t+=Time.deltaTime){opacity=Mathf.Lerp(initial,target,t/fadeSeconds);yield return null;}opacity=target;}
 void RestoreView(){framing=false;if(cameraRig){cameraRig.localPosition=localPosition;cameraRig.localRotation=localRotation;}if(view)view.fieldOfView=fov;}
 void OnDisable(){StopAllCoroutines();RestoreView();Active=false;opacity=0;}
 void OnGUI(){if(opacity<=0)return;var old=GUI.color;int depth=GUI.depth;GUI.depth=-1000;GUI.color=new Color(0,0,0,opacity);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;GUI.depth=depth;}
}
}
