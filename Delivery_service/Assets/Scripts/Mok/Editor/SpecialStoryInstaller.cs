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
public static class SpecialStoryInstaller {
    static SpecialStoryInstaller(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/SpecialStory-request"))return;
        File.Delete("Temp/SpecialStory-request");
        try{Apply();File.WriteAllText("Temp/SpecialStory-result.txt","PASS");}
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/SpecialStory-result.txt",e.ToString());}
    }
    static void Apply(){
        var scene=SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Before-special-story-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        T One<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).First();
        var broker=One<BrokerMission>();var special=One<SpecialOrderMission>();var inbox=One<StoryPhoneInbox>();
        special.interactionRange=3.5f;
        var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
        // 재적용해도 사용자가 바꾼 대사는 보존한다.
        if(!book.entries.Any(e=>e.id=="special.story.bridge")){
            broker.endingLines[1]="나: 박진웅을 직접 만나 보라는 거야?\n실장: 정면으로 찾아가면 죽어. 곧 비공개 행사에 나올 거야. 납품 담당자 연락처를 줄 테니, 배달원으로 들어가 무슨 말을 하는지 들어 봐.";
            broker.endingLines[2]="민기: 연락처 받았어. 행사에 피자를 납품할 업체를 찾는대. 먼저 시식용 피자부터 가져오라는데, 가게 이름으로 승인받으면 자연스럽게 들어갈 수 있겠어.";
            inbox.followup="실장이 준 연락처로 납품 담당자와 얘기했어. 지금 시식 접수를 받고 있대. 먼저 담당자를 만나 요구하는 피자를 확인하고, 우리 가게 이름으로 납품 승인을 받자. 휴대폰 미션 앱에서 ‘특별 주문’을 시작해.";
            special.briefing[0]="담당자: 연락 주신 이얏호 피자죠? 소개는 받았어요. 이번 행사 납품 업체로 등록하려면 시식부터 통과해야 합니다.";
            special.ending[1]="민기: 이제 행사장에 들어갈 명분이 생겼네. 박진웅에게 따지러 가는 게 아니라, 피자를 배달하러 가는 거야. 들키지 않고 속내부터 알아내자.";
            special.ending[2]="나: 납품 승인만 있으면 안전한 건가?\n민기: 아니. 경비가 어디 있는지, 들켰을 때 어디로 빠질지는 알아 둬야지. 다음은 행사장 주변을 살펴보자.";
            book.entries.Add(new GameDialogue.Entry{id="special.story.bridge",group="11 휴대폰 문자",original=inbox.followup,text=inbox.followup});
        }
        void Add(string text,string id,string group){if(book.entries.Any(e=>e.original==text))return;book.entries.Add(new GameDialogue.Entry{id=id,group=group,original=text,text=text});}
        for(int i=0;i<broker.endingLines.Length;i++)Add(broker.endingLines[i],"special.broker."+i,"04 연락이 끊긴 중개인");
        int n=0;foreach(var text in special.briefing.Concat(special.approval).Concat(special.ending).Append(special.completionMessage))Add(text,"special.dialogue."+n++,"05 특별 주문");
        var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
        foreach(var item in plan.missions)if(item.id=="special_order")item.description="실장에게 얻은 연락처로 행사 납품 담당자를 만나세요. 요구하는 치즈피자를 만들어 시식에 통과하면 피자 배달원으로 행사장에 들어갈 납품 승인을 받을 수 있습니다. 기본 재료는 민기가 지원합니다.";
        foreach(var obj in new UnityEngine.Object[]{broker,special,inbox,book,plan})EditorUtility.SetDirty(obj);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
        if(opened)EditorSceneManager.CloseScene(scene,true);
    }
}
}
#endif

