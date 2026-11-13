#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonConversationSetup
    {
        static PrisonConversationSetup() { EditorApplication.update += Poll; }
        static void Poll()
        {
            const string request = "Temp/PrisonConversation-request";
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            string action = File.ReadAllText(request).Trim(); File.Delete(request);
            try { if(action=="reload"){EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");Build();}else if(action=="build")Build();else if(action=="test")PrisonConversationCheck.Run(); }
            catch(Exception e) { File.WriteAllText("Logs/Prison-conversation-setup.txt",e.ToString());Debug.LogException(e); }
        }
        static void Build()
        {
            var intro=Object.FindFirstObjectByType<PrisonIntroduction>();
            if(!intro || intro.gameObject.scene.name!="PrisonChapter")throw new Exception("감옥 씬에서만 실행합니다.");
            var scene=intro.gameObject.scene;
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-conversation-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Vector3 position=intro.informant.position;Quaternion rotation=intro.informant.rotation;
            var presentation=intro.meetingPresentation;
            if(!presentation)
            {
                var root=new GameObject("최형욱 • 세 인물 대화 연출");root.transform.SetParent(intro.transform,false);
                presentation=root.AddComponent<PrisonMeetingPresentation>();presentation.introduction=intro;
                var actor=Object.Instantiate(intro.mugshotActor.gameObject,root.transform);actor.name="플레이어 • 운동장 대화 대역";actor.SetActive(false);
                presentation.playerActor=actor.transform;intro.meetingPresentation=presentation;
            }
            var chicken=Object.FindFirstObjectByType<PrisonChickenMission>();
            chicken.chefReaction.playerView=intro.mission.player.GetComponentInChildren<Camera>(true).transform;
            intro.portraitCamera.position=position+intro.informant.forward*2+Vector3.up*1.55f;
            intro.portraitCamera.LookAt(position+Vector3.up*1.4f);
            var population=Object.FindFirstObjectByType<PrisonPopulation>();
            var inmates=population.slots.Where(s=>!s.guard).ToArray();
            // 지역별 배치가 한쪽에 몰리지 않도록 기존 순서에서 고르게 남긴다.
            var keep=Enumerable.Range(0,Mathf.Min(30,inmates.Length)).Select(i=>inmates[Mathf.FloorToInt(i*inmates.Length/30f)]).ToArray();
            foreach(var slot in inmates.Except(keep))if(slot.actor)Object.DestroyImmediate(slot.actor.gameObject);
            population.slots=population.slots.Where(s=>s.guard||keep.Contains(s)).ToArray();
            population.gameObject.name="감옥 생활 인원 • 죄수 30 / 간수 20";
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-conversation-setup.txt",$"PASS preserved informant position {position}, rotation {rotation.eulerAngles}\nInmates {keep.Length}; guards {population.slots.Count(s=>s.guard)}\n");
        }
    }
}
#endif
