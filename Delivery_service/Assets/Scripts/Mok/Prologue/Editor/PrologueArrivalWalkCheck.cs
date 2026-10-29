#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class PrologueArrivalWalkCheck {
    static string log;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Prologue-arrival-walk-check.txt",log);}
    public static void Apply(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-arrival-walk-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
        var a=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrologueShopArrival>(true)).Single();
        var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");
        if(!controller)throw new Exception("Walk controller missing");
        a.animator.runtimeAnimatorController=controller;a.animator.applyRootMotion=false;a.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(a.animator);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/Prologue-arrival-walk-apply.txt","PASS saved arrival actor uses StoryWalk; scene backed up");
    }
    static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
    public static IEnumerator Verify(){
        log="Arrival walk verification\n";
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.AcceptInput=false;d.cityVista.SetActive(true);
        var a=d.shopArrival;Check(a.animator.runtimeAnimatorController.name=="StoryWalk","saved walking controller assigned");
        var oldCar=d.story.carConversation;var oldShop=d.story.shopConversation;
        try{
            typeof(BankPrologue).GetProperty("Current").SetValue(d,BankPrologue.Phase.CityReveal);
            yield return Wait(1);var engine=d.drive.GetComponent<PrologueDriveFeedback>().engine;
            Check(engine.isPlaying&&engine.volume<.1f,"city entry has quiet engine");
            Check(!d.GetComponent<PrologueMissionMusic>().Source.isPlaying&&d.GetComponent<StorySoundscape>().City.isPlaying,"city arrival replaces mission music with outdoor ambience");
            d.story.carConversation=new[]{new PrologueLine("민기: 테스트",1.5f)};d.story.shopConversation=new PrologueLine[0];a.StartCoroutine(a.Play());
            yield return Wait(.7f);Check(engine.isPlaying&&engine.volume<=.056f,"car dialogue keeps quieter engine");
            float until=Time.time+3;while(a.animator.GetFloat("MoveSpeed")<.1f&&Time.time<until)yield return null;
            yield return Wait(.3f);
            Check(a.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"walk state plays during approach");
            var leg=a.animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var rotation=leg.localRotation;var position=a.actor.position;
            yield return Wait(.2f);
            Check(Quaternion.Angle(rotation,leg.localRotation)>2,"leg pose advances while actor moves");Check(Vector3.Distance(position,a.actor.position)>.05f,"actor advances toward shutter");
            d.SetPaused(true);position=a.actor.position;yield return null;yield return null;Check(Vector3.Distance(position,a.actor.position)<.001f,"pause holds approach position");d.SetPaused(false);
            until=Time.time+5;while(a.Stage!=3&&Time.time<until)yield return null;
            var shutter=a.shop.GetComponentsInChildren<AudioSource>().FirstOrDefault(source=>source.clip==SuppliedAudio.Current.shutter);
            Check(shutter&&shutter.isPlaying&&Vector3.Distance(shutter.transform.position,a.shop.TransformPoint(new Vector3(3.22f,1.4f,3.15f)))<.01f,"supplied shutter recording plays at shop door");
            Check(!engine.isPlaying,"engine stops after leaving car");
            until=Time.time+8;while(!a.Completed&&Time.time<until)yield return null;
            Check(!shutter.isPlaying,"shutter sound stops when opening ends");
            Check(a.Completed&&a.Opened&&a.animator.GetFloat("MoveSpeed")==0,"arrival finishes with shutter open and idle actor");
        }finally{d.story.carConversation=oldCar;d.story.shopConversation=oldShop;d.SetPaused(false);}
    }
}
}
#endif
