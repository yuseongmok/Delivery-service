#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
namespace DeliveryService.Mok.NewCity.Editor
{
 [InitializeOnLoad] internal static class HarborFinaleAudit
 {
  static HarborFinaleAudit(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/HarborFinaleAudit"))return;File.Delete("Temp/HarborFinaleAudit");Run();}
  static void Run()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))return;Physics.SyncTransforms();var log=new StringBuilder();
   foreach(var pair in new[]{(new Vector3(0,1.65f,11),new Vector3(0,1.65f,-6),"카운터 뒤 → 정문"),(new Vector3(0,1.65f,11),new Vector3(6,1.65f,-6),"카운터 뒤 → 정면 유리"),(new Vector3(7,1.65f,11),new Vector3(16,1.65f,7),"후퇴 통로 → 측면 출입구"),(new Vector3(-11.4f,1.65f,27),new Vector3(-3.6f,1.65f,27),"마지막 시야 → 민기")})
    log.AppendLine(pair.Item3+": "+(Physics.Linecast(pair.Item1,pair.Item2,out var hit)?"BLOCKED "+hit.collider.name:"CLEAR"));
   foreach(var end in new[]{new Vector3(0,0,11),new Vector3(6.3f,0,11),new Vector3(9.2f,0,20),new Vector3(-12,0,37)}){var path=new NavMeshPath();bool ok=NavMesh.SamplePosition(new Vector3(0,0,5),out var a,1,-1)&&NavMesh.SamplePosition(end,out var b,1,-1)&&NavMesh.CalculatePath(a.position,b.position,-1,path)&&path.status==NavMeshPathStatus.PathComplete;log.AppendLine("홀→"+end+": "+ok);}
   var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();var pos=c.transform.position;var rot=c.transform.rotation;var target=c.targetTexture;var active=RenderTexture.active;
   foreach(var shot in new[]{(new Vector3(-40,12,-89),new Vector3(-66,5,-70),"terrain"),(new Vector3(-43,1.7f,-70),new Vector3(-66,2,-70),"tunnel"),(new Vector3(0,1.65f,11.3f),new Vector3(0,1.5f,-8),"defense"),(new Vector3(6.3f,1.65f,10.7f),new Vector3(9.2f,1.5f,18),"retreat"),(new Vector3(9.2f,1.65f,19.3f),new Vector3(8,1.5f,23),"cart"),(new Vector3(-12,1.65f,35),new Vector3(-12,1.5f,27),"exit")}){var rt=new RenderTexture(1600,900,24);var t=new Texture2D(1600,900,TextureFormat.RGB24,false);try{c.transform.position=shot.Item1;c.transform.LookAt(shot.Item2);c.targetTexture=rt;c.ResetCullingMatrix();c.ResetWorldToCameraMatrix();c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes("Logs/FinaleAudit-"+shot.Item3+".png",t.EncodeToPNG());}finally{c.targetTexture=target;RenderTexture.active=active;Object.DestroyImmediate(rt);Object.DestroyImmediate(t);}}
   c.transform.SetPositionAndRotation(pos,rot);File.WriteAllText("Logs/FinaleAudit.txt",log.ToString());
  }
 }
}
#endif



