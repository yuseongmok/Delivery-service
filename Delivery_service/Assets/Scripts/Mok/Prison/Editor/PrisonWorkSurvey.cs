#if UNITY_EDITOR
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using System.IO; using System.Linq;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] static class PrisonWorkSurvey {
static PrisonWorkSurvey(){EditorApplication.update+=Tick;}
static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonWorkSurvey"))return;File.Delete("Temp/PrisonWorkSurvey");var intro=Object.FindFirstObjectByType<PrisonIntroduction>();var s="CELL "+intro.cellSpawn.position+" MINGI "+intro.cellMingi.position+" CAMERA "+intro.cellCamera.position+"\n";
foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){var p=c.bounds.center;if((p.x>24&&p.x<47&&p.z>20&&p.z<45)||(Vector3.Distance(p,intro.cellSpawn.position)<8))s+=c.name+" center="+p+" size="+c.bounds.size+"\n";}
s+="SKY "+RenderSettings.skybox+" sun "+RenderSettings.sun+"\n";foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional))s+="LIGHT "+l.name+" "+l.intensity+" "+l.transform.eulerAngles+"\n";var mesh=intro.firstWork.axe.GetComponent<MeshFilter>().sharedMesh;s+="AXE "+mesh.bounds+"\n";foreach(var v in mesh.vertices.Where(v=>v.x>mesh.bounds.max.x-.01f||v.x<mesh.bounds.min.x+.01f))s+="EDGE "+v+"\n";s+="SHADER "+RenderSettings.skybox.shader.name+"\n";File.WriteAllText("Logs/Prison-work-survey.txt",s);
}
}}
#endif

