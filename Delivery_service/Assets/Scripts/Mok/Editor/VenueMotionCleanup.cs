#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class VenueMotionCleanup {
 public static void Apply(){var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();if(!m||EditorApplication.isPlaying)throw new Exception("JJinCity edit mode required");if(!EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Venue-before-motion-cleanup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");var w=m.interior.transform;
 // 이전 탈출 경로의 참조를 새 접수실 쪽으로 옮긴 뒤 불필요한 서비스 통로와 골목을 제거한다.
 for(int i=2;i<m.interiorPosts.Length;i++)m.interiorPosts[i].position=w.TransformPoint(new Vector3(-24+(i-2)*5,.04f,-4));
 var old=w.Find("Service corridor");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 foreach(string groupName in new[]{"Synty venue architectural finish","Synty furnished reception"}){var group=w.Find(groupName);if(!group)continue;foreach(Transform t in group.Cast<Transform>().ToArray()){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);if(w.InverseTransformPoint(b.center).z<-14)UnityEngine.Object.DestroyImmediate(t.gameObject);}}
 if(!w.Find("Closed former service exit")){var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Closed former service exit";wall.transform.SetParent(w,false);wall.transform.localPosition=new Vector3(0,2.2f,-14);wall.transform.localScale=new Vector3(20,4.4f,.3f);wall.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/VenueFinale/Venue plaster.mat");}
 var stealth=m.stealth;stealth.detectionSeconds=3.5f;
 var original=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/Robber/Robber.controller");var walking=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Walking.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
 string path="Assets/Scripts/Mok/Art/VenueFinale/VenuePatrolWalk.overrideController";var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);if(!controller){controller=new AnimatorOverrideController(original);AssetDatabase.CreateAsset(controller,path);}var pairs=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(pairs);for(int i=0;i<pairs.Count;i++)if(pairs[i].Key.name.Contains("Run",StringComparison.OrdinalIgnoreCase))pairs[i]=new System.Collections.Generic.KeyValuePair<AnimationClip,AnimationClip>(pairs[i].Key,walking);controller.ApplyOverrides(pairs);if(!pairs.Any(p=>p.Value==walking))throw new Exception("Run clip not found: "+string.Join(",",pairs.Select(p=>p.Key.name)));
 foreach(var guard in stealth.guards){guard.walkController=controller;guard.chaseController=original;guard.GetComponentInChildren<Animator>(true).runtimeAnimatorController=controller;EditorUtility.SetDirty(guard);}
 // 확대된 둥근 스프라이트 대신 단색 트랙과 너비가 변하는 얇은 막대를 쓴다.
 var panel=stealth.suspicionPanel;panel.sprite=null;panel.type=Image.Type.Simple;panel.rectTransform.sizeDelta=new Vector2(330,48);panel.color=new Color(.025f,.045f,.06f,.88f);
 var track=panel.transform.Find("Clean suspicion track");if(!track){var go=new GameObject("Clean suspicion track",typeof(RectTransform),typeof(Image));go.transform.SetParent(panel.transform,false);track=go.transform;}
 var rect=(RectTransform)track;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,9);rect.sizeDelta=new Vector2(302,5);var image=track.GetComponent<Image>();image.color=new Color(.25f,.31f,.34f);image.raycastTarget=false;
 var fill=stealth.suspicionFill;fill.transform.SetParent(track,false);fill.sprite=null;fill.type=Image.Type.Simple;fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=new Vector2(0,1);fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
 stealth.suspicionText.fontSize=19;stealth.suspicionText.rectTransform.offsetMin=new Vector2(10,16);stealth.suspicionText.rectTransform.offsetMax=new Vector2(-10,-3);panel.gameObject.SetActive(false);
 EditorUtility.SetDirty(stealth);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);VenueStealthBuilder.Rebuild();VenueEscapeUpgrade.Validate();File.WriteAllText("Logs/Venue-motion-cleanup.txt","Saved: old service corridor/alley removed; 3.5s detection; walking override and chase controller; sprite-free gauge.\nWalking clip: "+walking.name);
 }
}
}
#endif
