#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class MechanicTheftDialogueSetup {
 static MechanicTheftDialogueSetup(){EditorApplication.update+=Run;}
 static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/MechanicTheftDialogue"))return;
 File.Delete("Temp/MechanicTheftDialogue");var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool added=!scene.isLoaded;
 if(added)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(scene,"Logs/City-before-racer-dialogue-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 MechanicMission m=null;foreach(var root in scene.GetRootGameObjects()){m=root.GetComponentInChildren<MechanicMission>(true);if(m)break;}
 if(!m)throw new System.Exception("Mechanic mission missing");Undo.RecordObject(m,"Unify stolen parts story");
m.briefing=new string[]{"정비사: 민기가 보냈지? 행사장에서 빠져나올 차가 필요하다면서.","나: 눈에 안 띄고, 확실하게 움직이는 차면 돼.","정비사: 차는 준비할 수 있어. 대신 내 일 하나만 해결해 줘. 서안골목 거래처에 시동 제어기를 팔았는데 아직 대금을 못 받았어.","정비사: 돈을 달라고 해도 자꾸 말을 돌리더군. 직접 가서 무슨 일인지 알아봐 줘.","나: 알겠어. 거래처부터 만나 볼게."};
m.spotDialogue=new string[]{"나: 정비사가 보냈어. 부품은 받아 놓고 왜 대금을 안 주는 거지?","거래처: 떼먹으려던 건 아니야. 정비사한테 산 시동 제어기를 레이서가 훔쳐 갔어.","나: 도난당했다고? 정비사한테는 왜 말하지 않았어?","거래처: 되찾아서 처리하려고 했지. 그런데 그 녀석이 돌려줄 생각을 안 해. 겁이 나서 더 따지지도 못했고.","나: 그럼 내가 찾아오지. 부품을 정비사한테 돌려주는 걸로 거래를 취소해도 되겠어?","거래처: 그래. 정비사한테는 내가 직접 사과할게. 레이서는 승부에 미친 녀석이야. 그냥 달라고 해서는 안 줄 거다."};
m.racerDialogue=new string[]{"나: 서안골목 거래처에서 훔친 시동 제어기, 돌려줘. 정비사한테 가져가야겠어.","레이서: 그 부품 때문에 여기까지 왔어? 가져간 건 맞는데 순순히 내놓기는 싫은걸.","나: 남의 물건 훔쳐 놓고 조건까지 붙이겠다고?","레이서: 그럼 실력으로 가져가. 내 차를 상대로 레이스에서 이기면 돌려주지.","나: 좋아. 대신 내가 이기면 딴소리하지 마.","레이서: 약속하지. 출발선에서 보자."};
m.racerWinDialogue=new string[]{"레이서: 실력 괜찮은데? 졌다는 건 인정하지. 약속대로 부품은 돌려줄게.","나: 다음부터 남의 물건에 손대지 마."};
m.racerLoseDialogue=new string[]{"레이서: 그 실력으로 부품을 가져가겠다고? 돌려받고 싶으면 다시 도전해 봐.","나: 아직 끝난 거 아니야. 다시 붙자."};
m.ending=new string[]{"정비사: 돈 대신 부품을 가져왔네? 무슨 일이 있었던 거야?","나: 거래처가 부품을 도난당했더군. 레이서한테서 되찾아 왔어. 거래처도 부품을 돌려주는 걸로 거래를 취소하기로 했고, 직접 사과하겠대.","정비사: 그래서 말을 못 했던 거군. 대금 문제는 내가 정리할게. 마침 이 시동 제어기를 쓰면 네가 필요한 차도 준비할 수 있겠어.","나: 행사장 골목 끝, 큰길 합류 지점이야. 민기가 위치를 보내 줄 거야.","정비사: 받았어. 차와 열쇠를 거기 두고 먼저 빠질게. 탈출 후에는 지정한 곳에 차를 버려. 추적될 수 있으니까.","정비사: 이건 예비 열쇠야. 부품을 되찾아 준 빚은 차량 지원으로 갚을게.","나: 좋아. 그날 부탁할게."};
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Mechanic-theft-dialogue.txt","Saved all six editable dialogue arrays; mission references and progression unchanged.");if(added)EditorSceneManager.CloseScene(scene,true);
 }
}}
#endif
