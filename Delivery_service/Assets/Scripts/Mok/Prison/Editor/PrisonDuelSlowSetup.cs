#if UNITY_EDITOR
using UnityEngine;using UnityEditor;using System.IO;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] static class PrisonDuelSlowSetup {
 static PrisonDuelSlowSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonDuelSlowSetup"))return;File.Delete("Temp/PrisonDuelSlowSetup");const string p="Assets/DeliveryPrisonChapter/MedicineMission/김정민 결투 대사와 난이도.asset";Directory.CreateDirectory("Logs");File.Copy(p,"Logs/Duel-settings-before-slow-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".asset");var s=AssetDatabase.LoadAssetAtPath<PrisonDuelSettings>(p);s.slowScale=.045f;s.slowSeconds=.8f;s.tutorial="← Q    회피    R →";EditorUtility.SetDirty(s);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Prison-duel-slow-setup.txt","4.5% speed for 0.8 real seconds at punch near face. Dialogue retained.");}
}
}
#endif
