#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class StoryRevisionInstaller {
 [Serializable]public class Change{public string before,after;}
 [Serializable]public class Field{public string type,field;public string[] values;public bool array;}
 [Serializable]public class Data{public Change[] changes;public Field[] fields;}
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("JJinCity.unity"))throw new Exception("Open JJinCity first");
 string dir="Logs/StoryRevision-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(dir);
 if(!EditorSceneManager.SaveScene(scene,dir+"/Before.unity",true))throw new Exception("Scene backup failed");
 var data=JsonUtility.FromJson<Data>(File.ReadAllText("Logs/StoryRevision.json"));int changed=0;
 var targets=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c&&c.GetType().Namespace=="DeliveryService.Mok").Cast<UnityEngine.Object>().ToList();
 foreach(var kind in new[]{"PrologueStory","NightPreparationPlan"})foreach(var guid in AssetDatabase.FindAssets("t:"+kind)){var path=AssetDatabase.GUIDToAssetPath(guid);File.Copy(path,dir+"/"+Path.GetFileName(path),true);targets.Add(AssetDatabase.LoadMainAssetAtPath(path));}
 foreach(var target in targets){
 var so=new SerializedObject(target);string before=EditorJsonUtility.ToJson(target,true);var prop=so.GetIterator();bool dirty=false;
 while(prop.Next(true)){if(prop.propertyType!=SerializedPropertyType.String)continue;var change=data.changes.FirstOrDefault(c=>c.before==prop.stringValue);if(change!=null){prop.stringValue=change.after;dirty=true;changed++;}}
 foreach(var field in data.fields.Where(f=>f.type==target.GetType().Name)){var p=so.FindProperty(field.field);if(p==null)throw new Exception(field.type+"."+field.field);if(field.array){if(p.arraySize!=field.values.Length&&!(field.type=="VenueFinale"&&field.field=="opening"&&p.arraySize==2))throw new Exception("Dialogue count changed: "+field.type+"."+field.field+" actual="+p.arraySize+" expected="+field.values.Length);for(int i=0;i<p.arraySize;i++)p.GetArrayElementAtIndex(i).stringValue=field.values[i];}else p.stringValue=field.values[0];dirty=true;}
 if(dirty){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);File.WriteAllText(dir+"/"+target.GetInstanceID()+"-before.json",before);File.WriteAllText(dir+"/"+target.GetInstanceID()+"-after.json",EditorJsonUtility.ToJson(target,true));}
 }
 var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
 foreach(var item in plan.missions){switch(item.id){
 case "broker":item.description="어젯밤, 연락이 끊겼던 실장이 조직에게 쫓긴다는 문자를 보냈습니다. 오늘 마감 후 약속 장소에서 만나 사정을 듣고 함께 탈출하세요.";item.result="박진웅의 행사 정보와 납품 담당자 연락처 확보 · 다음 일차에 특별 주문";break;
 case "special_order":item.description="실장을 구출한 다음 일차 밤, 예약한 시식 납품을 진행합니다. 치즈피자를 전달해 가게 이름으로 행사장 출입 승인을 받으세요.";item.result="정식 납품 승인 · 다음 일차에 행사장 주변 조사";break;
 case "backdoor":item.description="납품 승인을 받은 다음 일차 밤, 행사장 주변을 조사합니다. 박진웅에게 들키지 않을 자리와 탈출 동선을 확보하세요.";item.result="출입구·사각지대·탈출로 확보 · 다음 일차에 정비사와 약속";break;
 case "mechanic":item.description="현장 조사를 마친 다음 일차 밤, 정비사의 부품을 되찾아 주고 탈출 차량을 확보하세요. 낮에는 생활비와 준비 비용을 벌어 둡니다.";item.result="탈출 차량 지원 확보 · 다음 날 큰 사건으로 출발 가능";break;
 case "finale":item.description="준비가 끝났습니다. 출발하면 다음 날로 넘어가 영업 대신 행사장에 잠입합니다. 피자를 납품하고 박진웅의 속내를 엿들은 뒤, 들키면 준비한 차량으로 탈출하세요.";item.result="은행 의뢰의 진실과 조직의 제거 계획 확인";break;
 }}EditorUtility.SetDirty(plan);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/StoryRevision-result.txt","Saved story dialogue and mission descriptions. Replaced strings: "+changed+"\nBackup and text-only comparison: "+dir+"\nMission IDs: "+string.Join(",",plan.missions.Select(x=>x.id)));
 }
}
}
#endif
