#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]
public static class StoryCoverInstaller {
    static StoryCoverInstaller() { EditorApplication.update += Poll; }
    static void Poll() {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/StoryCover-request")) return;
        File.Delete("Temp/StoryCover-request");
        try { Apply(); File.WriteAllText("Temp/StoryCover-result.txt", "PASS: cover story and editable dialogue saved"); }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("Temp/StoryCover-result.txt", e.ToString()); }
    }
    public static void Apply() {
        var scene = SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(StoryTutorialBuilder.Main, OpenSceneMode.Additive);
        Directory.CreateDirectory("Logs");
        if (!EditorSceneManager.SaveScene(scene, "Logs/Before-cover-story-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true)) throw new Exception("Scene backup failed");
        T One<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).First();
        var t = One<StoryTutorial>();
        var transition = t.GetComponent<StoryCoverTransition>();
        bool first = !transition;
        if (first) transition = t.gameObject.AddComponent<StoryCoverTransition>();
        transition.tutorial = t; t.coverTransition = transition;
        var inbox = One<StoryPhoneInbox>();
        if (first) {
            t.dialogue[0] = "민기: 얼굴이 알려졌으니 당분간은 여기서 조용히 지내자. 진짜 배달도 해야 의심을 안 받지.";
            t.dialogue[1] = "민기: 첫 주문이 들어왔어. 컴퓨터에서 치즈피자 주문을 수락해 봐. 재료는 내가 준비했어.\n나: 좋아. 오늘부터는 평범한 배달원인 거네.";
            t.dialogue[13] = "민기: 첫 배달은 어땠어?\n나: 가게가 다시 열려서 좋대. 적어도 손님 눈에는 그냥 배달원으로 보였겠지.";
            t.dialogue[14] = transition.settlingLine;
            inbox.invitation = transition.invitation;
            inbox.nightMessage = "민기: 드디어 실장한테 연락이 왔어. 조직이 자기를 없애려고 한다니 무슨 일인지 들어 봐야겠어. 휴대폰 미션 앱에서 약속 장소를 확인해.";
        }
        var book = AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
        if (!book) throw new Exception("Missing editable dialogue catalog");
        void Add(string text, string id) {
            if (book.entries.Any(e => e.original == text)) return;
            book.entries.Add(new GameDialogue.Entry { id = id, group = "03 첫 배달 튜토리얼", original = text, text = text });
        }
        for (int i = 0; i < t.dialogue.Length; i++) Add(t.dialogue[i], "cover.tutorial." + i);
        Add(transition.settlingLine, "cover.settling"); Add(transition.timeCaption, "cover.time");
        Add(transition.passageLine, "cover.passage"); Add(transition.invitation, "cover.invitation"); Add(inbox.nightMessage, "cover.reply");
                var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
        if(plan){
            File.Copy(AssetDatabase.GetAssetPath(plan),"Logs/Before-cover-plan-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".asset",true);
            plan.premise="야마자키파의 금고에서 돈을 회수한 뒤 연락책이 사라졌습니다. 두 사람은 피자 배달원으로 신분을 숨기고 지내다가, 며칠 뒤 자신을 구해 달라는 실장의 연락을 받습니다. 단서를 따라 박진웅의 속내를 확인하세요.";
            foreach(var item in plan.missions)if(item.id=="broker")item.description="며칠 만에 연락해 온 실장이 조직원들에게 쫓기고 있습니다. 휴대폰에 표시된 약속 장소로 가서 그를 구출하고 연락이 끊겼던 이유를 확인하세요.";
            EditorUtility.SetDirty(plan);
        }
        EditorUtility.SetDirty(book); EditorUtility.SetDirty(t); EditorUtility.SetDirty(transition); EditorUtility.SetDirty(inbox);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); GameDialogue.Invalidate();
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }
}
}
#endif

