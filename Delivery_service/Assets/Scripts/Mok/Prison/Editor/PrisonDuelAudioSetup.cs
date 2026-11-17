#if UNITY_EDITOR
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] static class PrisonDuelAudioSetup {
 static PrisonDuelAudioSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonDuelAudioSetup"))return;File.Delete("Temp/PrisonDuelAudioSetup");var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-duel-audio-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))return;var d=Object.FindFirstObjectByType<PrisonMedicineDuel>();var a=d.GetComponent<PrisonDuelAudio>();if(!a)a=d.gameObject.AddComponent<PrisonDuelAudio>();a.punchSwing=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/주먹 공격.mp3");a.punchHit=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/주먹 피격.mp3");a.crowdCheer=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/관중들 환호.mp3");a.battleMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/미션브금2.mp3");if(!a.punchSwing||!a.punchHit||!a.crowdCheer||!a.battleMusic)throw new System.Exception("Missing duel audio");d.presentation.duelAudio=a;EditorUtility.SetDirty(d.presentation);EditorUtility.SetDirty(a);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-duel-audio-setup.txt","Saved four clips. Seconds: swing="+a.punchSwing.length+" hit="+a.punchHit.length+" crowd="+a.crowdCheer.length+" music="+a.battleMusic.length);}
}
}
#endif
