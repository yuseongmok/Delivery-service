#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonDetailUpgrade
 {
  static PrisonDetailUpgrade(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonDetailUpgrade")){File.Delete("Temp/PrisonDetailUpgrade");try{Apply();PrisonDetailReview.Run();}catch(Exception e){File.WriteAllText("Logs/Prison-detail-upgrade.txt",e.ToString());}}};}
  public static void Apply()
  {
   var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Open PrisonChapter first");
   string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-detail-"+stamp+".unity",true))throw new Exception("Backup failed");
   var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();var cctv=office.cctv;var text=office.evidence.dialogue;
   foreach(var asset in new UnityEngine.Object[]{text,cctv.dialogue})File.Copy(AssetDatabase.GetAssetPath(asset),"Logs/"+asset.GetType().Name+"-"+stamp+".asset",true);
   text.documents[1]="별도 인계 명단\n대상: 민기와 그놈\n정기 호송 출발 이후 별도 이송\n목적지: 공란\n인계자: 박진웅 측";
   for(int i=0;i<text.transfer.Length;i++)text.transfer[i]=text.transfer[i].Replace("우리 이름이 있어","우리를 넘긴다는 내용이 있어");
   text.documents[2]="발신: 박진웅\n약속한 돈은 보냈다.\n민기와 그놈, 오늘 밤 넘겨.\n인계 후에는 내가 처리하지.";
   var lines=cctv.dialogue.interrogation.ToList();if(!lines.Any(x=>x.Contains("옷 벗"))){int at=lines.FindIndex(x=>x.StartsWith("염윤태:")&&x.Contains("보관함"));if(at<0)throw new Exception("Interrogation location line missing");lines.InsertRange(at,new[]{"염윤태: 그걸 말하면 나도 옷 벗어야 해!!","민기: 그건 이 장부가 공개돼도 똑같겠지.","염윤태: 이익… 알겠다."});cctv.dialogue.interrogation=lines.ToArray();}
   cctv.entry.name="CCTV실 들어가기";
   cctv.dialogue.changed="민기: 간수복은 입었어. 자연스럽게 CCTV실로 들어가자.";
   var attention=office.previous.warden.GetComponent<PrisonWardenAttention>();if(!attention)attention=office.previous.warden.gameObject.AddComponent<PrisonWardenAttention>();attention.office=office;
   foreach(var t in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)))if(t.name.Contains("이현우"))t.name=t.name.Replace("이현우","플레이어");
   EditorUtility.SetDirty(text);EditorUtility.SetDirty(cctv.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllText("Logs/Prison-detail-upgrade.txt","Saved editable dialogue, player identity correction, CCTV target name and warden attention. Scene and dialogue backups preserved.\n");
  }
 }
}
#endif
