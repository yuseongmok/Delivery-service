#if UNITY_EDITOR
using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;
namespace DeliveryService.Mok.Editor{
[InitializeOnLoad] static class PrisonInformantSetup{
static PrisonInformantSetup(){EditorApplication.update+=Tick;}
static void Tick(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonInformantSetup"))return;File.Delete("Temp/PrisonInformantSetup");var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;
EditorSceneManager.SaveScene(scene,"Logs/Prison-before-informant-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
var intro=Object.FindFirstObjectByType<PrisonIntroduction>();if(intro.informantEntrance)return;
var entrance=intro.gameObject.AddComponent<PrisonInformantIntroduction>();entrance.intro=intro;intro.informantEntrance=entrance;
var population=Object.FindFirstObjectByType<PrisonPopulation>();var go=Object.Instantiate(population.inmateTemplate,intro.transform);go.name="최형욱에게 부탁을 마친 죄수";
foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
foreach(var joint in go.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(joint);
foreach(var body in go.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
var agent=go.GetComponent<NavMeshAgent>();if(!agent)agent=go.AddComponent<NavMeshAgent>();agent.radius=.25f;agent.height=1.8f;agent.stoppingDistance=.25f;
var p=intro.informant.position+intro.informant.forward*1.6f+intro.informant.right*.9f;if(NavMesh.SamplePosition(p,out var hit,1,NavMesh.AllAreas))p=hit.position;
go.transform.position=p;go.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(intro.informant.position-p,Vector3.up));
var animator=go.GetComponentInChildren<Animator>();animator.enabled=true;animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");animator.Rebind();animator.Update(0);go.SetActive(true);
entrance.petitioner=go.transform;entrance.agent=agent;entrance.animator=animator;EditorUtility.SetDirty(intro);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-informant-setup.txt","Saved dedicated petitioner beside current informant position.");
}
}}
#endif
