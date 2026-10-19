#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.Rendering;using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEngine;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok {
// 전용 명령행 인수가 있는 개발용 실행 파일에서만 실행한다. 일반 게임에서는 생성하지 않는다.
public sealed class StandalonePerformanceProbe:MonoBehaviour {
 string output;int rendered;void CountRender(ScriptableRenderContext c,Camera camera){if(camera.CompareTag("MainCamera"))rendered++;}
 readonly List<string> lines=new List<string>();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Install(){var arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--mok-profile="));var request=Path.GetFullPath(Path.Combine(Application.dataPath,"../MokPerformanceRequest.txt"));if(arg==null&&File.Exists(request))arg="--mok-profile="+File.ReadAllText(request).Trim();if(arg==null)return;var go=new GameObject("Standalone performance probe");DontDestroyOnLoad(go);go.AddComponent<StandalonePerformanceProbe>().output=arg.Substring("--mok-profile=".Length);Application.runInBackground=true;Screen.SetResolution(1920,1080,FullScreenMode.Windowed);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(5);RenderPipelineManager.endCameraRendering+=CountRender;lines.Add("SAVE SANDBOX "+Application.companyName+" / "+Application.productName);
  lines.Add($"DEVICE {SystemInfo.processorType} / {SystemInfo.graphicsDeviceName} / {Screen.width}x{Screen.height} / quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
  var p=FindFirstObjectByType<PlayerCombat>();var view=p.View;var clock=FindFirstObjectByType<DayNightCycle>();clock.isShopOpen=true;clock.timeMultiplier=0;var phone=FindFirstObjectByType<PhoneController>();var planner=FindFirstObjectByType<NavigationPlanner>();var nav=FindFirstObjectByType<NavigationManager>();
  p.GetComponent<PlayerMove>().enabled=false;p.GetComponent<CharacterController>().enabled=false;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;
  lines.Add("CENSUS cars="+FindObjectsByType<CarMov>(FindObjectsSortMode.None).Length+" citizens="+FindObjectsByType<NPCControl>(FindObjectsSortMode.None).Length);
  var points=new[]{new Vector3(0,0,50),new Vector3(290,0,-30),new Vector3(-255,0,0)};
  for(int sample=0;sample<7;sample++){
   int region=sample<6?sample/2:1;bool map=sample%2==1||sample==6;clock.currentTime=sample==6?21:12;
   p.transform.position=points[region];view.transform.position=p.transform.position+Vector3.up*1.65f;view.transform.rotation=Quaternion.Euler(0,region==2?270:0,0);nav.SetNavigationTarget(p.transform);phone.ClosePhone();if(map)phone.TogglePhone();yield return null;if(map)planner.Show(NavigationPlanner.Screen.Map);
   yield return new WaitForSecondsRealtime(3);rendered=0;var frames=new List<double>();double start=Time.realtimeSinceStartupAsDouble,previous=start;
   while(Time.realtimeSinceStartupAsDouble-start<6){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-previous)*1000);previous=now;}
   frames.Sort();lines.Add($"SCENARIO {region} {(sample==6?"night":"day")} {(map?"map-open":"phone-hidden")} frames={frames.Count} median={frames[frames.Count/2]:F2}ms p95={frames[(int)(frames.Count*.95)]:F2}ms medianFPS={1000/frames[frames.Count/2]:F1} mainCameraRenders={rendered}");File.WriteAllLines(output,lines);if(sample==1){ScreenCapture.CaptureScreenshot(output+".png");yield return new WaitForSecondsRealtime(.5f);}
  }
  RenderPipelineManager.endCameraRendering-=CountRender;lines.Add("COMPLETE: no density/quality/physics-frequency changes");File.WriteAllLines(output,lines);Application.Quit();
 }
}
}
#endif
