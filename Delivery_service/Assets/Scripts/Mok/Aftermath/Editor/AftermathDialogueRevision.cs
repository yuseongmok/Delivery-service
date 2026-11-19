#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathDialogueRevision
    {
        public static void Apply()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                var film=Object.FindFirstObjectByType<AftermathFilm>();
                var dialogue=film.dialogue;
                File.Copy(AssetDatabase.GetAssetPath(dialogue),"Logs/Aftermath-dialogue-before-revision-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".asset");
                if(dialogue.escapeBridge==null || dialogue.escapeBridge.Length!=3)throw new Exception("Expected the existing three forest dialogue lines");
                dialogue.escapeBridge[1].subtitle="민기: 야마자키파 금고에서 털었던 돈부터 다시 찾으러 가자.";
                dialogue.escapeBridge[2].subtitle="민기: 그리고 새 출발 하는 거야.";
                EditorUtility.SetDirty(dialogue);AssetDatabase.SaveAssets();
                AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
