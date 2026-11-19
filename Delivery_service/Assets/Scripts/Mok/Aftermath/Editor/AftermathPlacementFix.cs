#if UNITY_EDITOR
using System;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{public static class AftermathPlacementFix{public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-placement.unity",true);var f=Object.FindFirstObjectByType<AftermathFilm>();var set=f.shots[7].set;var ts=set.GetComponentsInChildren<Transform>(true);var canvas=ts.First(t=>t.name=="TV 뉴스 화면");var anchor=new GameObject("TV 화면 고정 위치").transform;anchor.SetParent(set.transform);anchor.position=new Vector3(4,2.65f,10.859f);canvas.SetParent(anchor,false);var rt=(RectTransform)canvas;rt.anchoredPosition3D=Vector3.zero;rt.localRotation=Quaternion.identity;
var box=f.shots[13].motions.First(m=>!m.actor);box.subject.position=new Vector3(1,1.43f,11.15f);box.from.position=box.subject.position;box.to.position=new Vector3(1,1.18f,11.03f);
Object.DestroyImmediate(ts.First(t=>t.name=="포장 픽업 작업대").gameObject);
var pack=f.shots[9];pack.cameraFrom.position=new Vector3(-.4f,2.05f,10.1f);pack.cameraTo.position=new Vector3(-.2f,2,10.2f);pack.lookAt.position=new Vector3(1,1.5f,11.6f);pack.motions[0].rightContact.position=new Vector3(.68f,1.47f,11.45f);
f.shots[13].cameraFrom.position=new Vector3(-.7f,2.05f,10);f.shots[13].cameraTo.position=new Vector3(-.6f,2,10);f.shots[13].lookAt.position=new Vector3(1,1.4f,11.2f);
f.shots[14].motions.First(m=>!m.actor).carryOffset=new Vector3(0,1.18f,.53f);
EditorSceneManager.SaveScene(scene);AftermathReview.Record();}}}
#endif
