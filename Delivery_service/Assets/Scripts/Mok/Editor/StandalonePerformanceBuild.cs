#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEditor.Build.Reporting;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class StandalonePerformanceBuild {
 static StandalonePerformanceBuild(){EditorApplication.update+=Poll;}
 static void Poll(){if(SessionState.GetBool("Mok.PerformanceRegression.Check",false)||SessionState.GetBool("Mok.Performance.Check",false)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-standalone-build"))return;File.Delete("Temp/Mok-standalone-build");Build();}
 static void Build(){string company=PlayerSettings.companyName,product=PlayerSettings.productName;var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");
 try{
 if(!scene.isLoaded)throw new Exception("JJinCity scene must be open");
 Directory.CreateDirectory("Assets/MokPerformanceTemp");
 if(!EditorSceneManager.SaveScene(scene,"Assets/MokPerformanceTemp/JJinCity.unity",true))throw new Exception("Could not copy current unsaved scene");
 AssetDatabase.Refresh();PlayerSettings.companyName="MokPerformanceSandbox";PlayerSettings.productName="DeliveryPerformance";
 var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/MokPerformanceTemp/JJinCity.unity"},locationPathName="Temp/PerformancePlayer/DeliveryPerformance.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
 File.WriteAllText("Temp/Mok-standalone-build.txt",result.summary.result+" errors="+result.summary.totalErrors+" seconds="+result.summary.totalTime.TotalSeconds);
 }catch(Exception e){File.WriteAllText("Temp/Mok-standalone-build.txt",e.ToString());Debug.LogException(e);}
 finally{PlayerSettings.companyName=company;PlayerSettings.productName=product;AssetDatabase.SaveAssets();AssetDatabase.DeleteAsset("Assets/MokPerformanceTemp");}
 }
}}
#endif
