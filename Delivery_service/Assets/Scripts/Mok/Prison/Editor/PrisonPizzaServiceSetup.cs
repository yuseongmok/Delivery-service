#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonPizzaServiceSetup
    {
        static PrisonPizzaServiceSetup(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonPizzaServiceSetup"))return;File.Delete("Temp/PrisonPizzaServiceSetup");try{Build();}catch(Exception e){File.WriteAllText("Logs/Prison-pizza-service-setup.txt",e.ToString());}}
        static void Build(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;if(scene.name!="PrisonChapter")throw new Exception("Prison only");if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-plate-service-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var service=m.GetComponent<PrisonPizzaService>();if(!service)service=m.gameObject.AddComponent<PrisonPizzaService>();m.serving=service;service.mission=m;service.platePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plate_01.prefab");
            const string folder="Assets/DeliveryPrisonChapter/KeysMission/";var data=AssetDatabase.LoadAssetAtPath<PizzaToppingData>(folder+"수면제 재료.asset");
            if(!data){data=ScriptableObject.CreateInstance<PizzaToppingData>();data.toppingName="수면제";data.toppingType=(ToppingType)8;data.spawnCount=0;data.cost=0;data.sellingPrice=0;AssetDatabase.CreateAsset(data,folder+"수면제 재료.asset");}
            var model=Object.Instantiate(m.previous.medicine.gameObject);model.name="수면제 봉투 모델";model.SetActive(true);foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);foreach(var text in model.GetComponentsInChildren<TextMesh>(true))Object.DestroyImmediate(text.gameObject);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            data.toppingPrefab=PrefabUtility.SaveAsPrefabAsset(model,folder+"수면제 봉투 모델.prefab");Object.DestroyImmediate(model);EditorUtility.SetDirty(data);service.medicine=data;
            var old=m.cookingSupplies.transform.Find("수면제 재료 집기");if(!old){var g=Object.Instantiate(data.toppingPrefab,m.cookingSupplies.transform);g.name="수면제 재료 집기";g.transform.position=m.maker.transform.position+new Vector3(.52f,1.02f,-.35f);g.transform.rotation=Quaternion.identity;var supply=g.AddComponent<PrisonPizzaSupply>();supply.mission=m;supply.ingredient=data;if(!g.GetComponentInChildren<Collider>())g.AddComponent<BoxCollider>().size=Vector3.one*.2f;
                var label=new GameObject("수면제 안내").AddComponent<TextMesh>();label.transform.SetParent(g.transform,false);label.transform.localPosition=Vector3.up*.23f;label.transform.rotation=Quaternion.Euler(0,180,0);label.text="수면제";label.font=MokUIFont.Font;label.fontSize=48;label.characterSize=.018f;label.anchor=TextAnchor.MiddleCenter;label.GetComponent<Renderer>().sharedMaterial=label.font.material;
            }
            m.previous.prison.cart.gameObject.SetActive(false);m.pizzaOnCart.SetActive(false);
            foreach(var guard in m.diners.Concat(new[]{m.warden}))if(!guard.GetComponent<PrisonGuardSleep>())guard.gameObject.AddComponent<PrisonGuardSleep>();
            m.dialogue.cook="민기: 재료를 올릴 때 수면제도 넣어. 다 만들면 오븐에서 구워서 가져가자.";
            m.dialogue.baked="민기: 잘 구워졌네. 접시에 담아서 직접 가져가자.";
            m.dialogue.load=new[]{"민기: 접시에 담았어. 간수들에게 한 조각씩 나눠 주자."};
            if(m.dialogue.officer.Length>3)m.dialogue.officerTaste=m.dialogue.officer[3];
            m.dialogue.officer=m.dialogue.officer.Take(3).ToArray();m.dialogue.officeOpen="민기: 잠들었어. 조용히 간수장실로 들어가자.";EditorUtility.SetDirty(m.dialogue);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-pizza-service-setup.txt","PASS saved medicine ingredient / automatic plate service / cart hidden / sleeping Z / taste-after-bite dialogue");
        }
    }
}
#endif
