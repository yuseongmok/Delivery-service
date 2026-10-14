#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MokRepairFinish {
[InitializeOnLoadMethod]static void Init(){if(File.Exists("Temp/Mok-repair-finish-request"))EditorApplication.delayCall+=Run;}
static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/Mok-repair-finish-request");var r=GameObject.Find("repair_shop/Mok Repair Garage");var nav=r.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(nav==null)nav=r.AddComponent<UnityEngine.AI.NavMeshObstacle>();nav.shape=UnityEngine.AI.NavMeshObstacleShape.Box;nav.center=new Vector3(0,2.5f,0);nav.size=new Vector3(15,5,8);nav.carving=true;r.transform.Find("ServiceFloorLabel").localEulerAngles=new Vector3(90,180,0);EditorSceneManager.MarkSceneDirty(r.scene);EditorSceneManager.SaveScene(r.scene);File.WriteAllText("Temp/Mok-repair-finish.txt","Pedestrian navmesh carving added; floor label faces entrance. Saved.");DeliveryService.Mok.Editor.RepairGarageCheck.Start();}
}
#endif
