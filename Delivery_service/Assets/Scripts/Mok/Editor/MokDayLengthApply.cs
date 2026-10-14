#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class MokDayLengthApply {
static MokDayLengthApply(){if(File.Exists("Temp/Mok-day20-request"))EditorApplication.update+=Apply;}
static void Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;EditorApplication.update-=Apply;
var cycle=Object.FindFirstObjectByType<DayNightCycle>();if(cycle==null)return;
Undo.RecordObject(cycle,"Set 20-minute day");cycle.timeMultiplier=54f;PrefabUtility.RecordPrefabInstancePropertyModifications(cycle);EditorUtility.SetDirty(cycle);EditorSceneManager.MarkSceneDirty(cycle.gameObject.scene);EditorSceneManager.SaveScene(cycle.gameObject.scene);File.Delete("Temp/Mok-day20-request");File.WriteAllText("Temp/Mok-day20-result.txt","PASS: live and saved DayNightCycle timeMultiplier=54; 18 game hours / 54 = 1200 real seconds.");
EditorApplication.delayCall+=DeliveryService.Mok.Editor.SequentialOrdersCheck.Start;
}}
#endif
