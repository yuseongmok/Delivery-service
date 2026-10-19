using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Navigation {
public sealed class NavigationMapStyle : MonoBehaviour {
 public const int MapLayer=20;
 public Camera trackingCamera;
 public Camera overviewCamera;
 void OnEnable(){if(Application.isPlaying&&GetComponent<NavigationRenderBudget>()==null)gameObject.AddComponent<NavigationRenderBudget>();RenderPipelineManager.beginCameraRendering+=Prepare;Configure(trackingCamera);Configure(overviewCamera);}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Prepare;}
 void Prepare(ScriptableRenderContext context,Camera camera){if(camera==trackingCamera||camera==overviewCamera)Configure(camera);else camera.cullingMask&=~(1<<MapLayer);}
 public static void Configure(Camera camera){if(camera==null)return;camera.cullingMask=(1<<MapLayer)|(1<<17);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.34f,.43f,.47f);camera.orthographic=true;camera.allowHDR=false;camera.allowMSAA=false;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.volumeLayerMask=0;}
}
}
