#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonNightSetup
    {
        static PrisonNightSetup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonNightSetup"))return;
            var action=File.ReadAllText("Temp/PrisonNightSetup").Trim();File.Delete("Temp/PrisonNightSetup");
            try{if(action=="survey")Survey();else if(action=="polish")Polish();else Build();}catch(Exception e){File.WriteAllText("Logs/Prison-night-setup.txt",e.ToString());}
        }
        static void Survey()
        {
            var m=Object.FindFirstObjectByType<PrisonNightMission>();
            var go=new GameObject("Night survey camera");var cam=go.AddComponent<Camera>();cam.transform.position=new Vector3(0,80,25);cam.transform.LookAt(new Vector3(0,0,25));cam.farClipPlane=250;
            PrisonChickenCheck.Capture(cam,"Logs/Prison-night-overview.png");Object.DestroyImmediate(go);
            var report="START "+m.checkpoint.position+" VAN "+m.previous.legacyEscape.vehicle.position+" POINT "+m.vehiclePoint.position+"\n";foreach(var light in m.searchlights){foreach(var hit in Physics.RaycastAll(light.transform.position,(new Vector3(-8,.15f,28)-light.transform.position).normalized,80))report+=light.name+" HIT "+hit.transform.name+" at "+hit.point+"\n";}
            File.WriteAllText("Logs/Prison-night-survey.txt",report);
        }
        static void Polish()
        {
            var m=Object.FindFirstObjectByType<PrisonNightMission>();var scene=EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-night-polish.unity",true);
            m.checkpoint.position=new Vector3(-10,0,22);m.companionStart.position=new Vector3(-11.5f,0,22);
            m.previous.legacyEscape.vehicle.SetPositionAndRotation(new Vector3(0,.15f,42),Quaternion.identity);
            m.vehiclePoint.position=new Vector3(-1.8f,.15f,42);
            m.curfewActors=Object.FindObjectsByType<NPCControl>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(n=>n.transform!=m.Friend.transform&&n.transform!=m.Player.transform&&n.transform.position.z>20&&n.transform.position.z<48&&Mathf.Abs(n.transform.position.x)<48).Select(n=>n.gameObject).ToArray();
            m.establishingShot.position=new Vector3(-12,4,26);m.establishingShot.LookAt(new Vector3(0,1,38));
            var positions=new[]{new Vector3(-14,0,25.2f),new Vector3(-6.5f,0,30.5f),new Vector3(-2,0,35.5f)};
            for(int i=0;i<positions.Length;i++)m.coverProps.transform.GetChild(i).position=positions[i];
            foreach(var light in m.searchlights){light.transform.position=new Vector3(light.transform.position.x,11,light.transform.position.z);light.lamp.intensity=3000;light.ResetSweep();}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-night-polish.txt","PASS");
        }
        static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.name!="PrisonChapter")throw new Exception("Prison scene only");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-night-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            if(Object.FindFirstObjectByType<PrisonNightMission>())throw new Exception("Already installed");
            const string folder="Assets/DeliveryPrisonChapter/Escape/";
            var root=new GameObject("29_저녁 탐조등 잠입 · 호송차량까지").transform;
            var m=root.gameObject.AddComponent<PrisonNightMission>();
            m.previous=Object.FindFirstObjectByType<PrisonCctvMission>();
            m.dialogue=ScriptableObject.CreateInstance<PrisonNightText>();AssetDatabase.CreateAsset(m.dialogue,folder+"저녁 탐조등 잠입 대사.asset");
            m.evening=root.gameObject.AddComponent<PrisonEvening>();m.film=m.previous.office.previous.camera;
            Transform At(string name,Vector3 point){var t=new GameObject(name).transform;t.SetParent(root);t.position=point;return t;}
            m.checkpoint=At("야간 잠입 시작 · 건물 그늘",new Vector3(-10,0,18));
            m.companionStart=At("민기 잠입 시작",new Vector3(-11.5f,0,18));
            m.vehiclePoint=At("호송차량 옆 · 민기와 합류",m.previous.legacyEscape.boardingPoint.position);
            m.route=new[]{At("보급 상자 뒤로 이동",new Vector3(-12,0,24)),At("불빛이 지나가면 다음 엄폐물로",new Vector3(-8,0,30)),At("호송차량 쪽으로 이동",new Vector3(-4,0,35))};
            m.establishingShot=At("저녁 운동장 소개 카메라",new Vector3(-15,5,17));m.establishingShot.LookAt(new Vector3(0,.5f,34));
            m.coverProps=At("호송 준비 보급 상자 · 저녁에 사용",Vector3.zero).gameObject;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_01.prefab");
            foreach(var at in new[]{new Vector3(-14,0,24),new Vector3(-9.5f,0,30),new Vector3(-5.5f,0,35)})
            {
                var prop=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);prop.name="탐조등 엄폐용 보급 상자";prop.transform.SetParent(m.coverProps.transform);prop.transform.position=at;
                var rs=prop.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var rr in rs)bounds.Encapsulate(rr.bounds);
                prop.transform.localScale*=1.8f/bounds.size.y;
                bounds=rs[0].bounds;foreach(var rr in rs)bounds.Encapsulate(rr.bounds);prop.transform.position+=Vector3.up*(at.y-bounds.min.y);
                var nav=prop.AddComponent<NavMeshObstacle>();nav.carving=true;nav.shape=NavMeshObstacleShape.Box;nav.center=prop.transform.InverseTransformPoint(bounds.center);nav.size=new Vector3(bounds.size.x/prop.transform.lossyScale.x,bounds.size.y/prop.transform.lossyScale.y,bounds.size.z/prop.transform.lossyScale.z);
            }
            var metal=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.15f,.18f,.2f)};AssetDatabase.CreateAsset(metal,folder+"탐조등 금속.mat");
            var glow=new Material(Shader.Find("Universal Render Pipeline/Unlit"));glow.SetColor("_BaseColor",new Color(1,.9f,.65f,.045f));glow.SetFloat("_Surface",1);glow.SetFloat("_Blend",0);glow.SetFloat("_SrcBlend",5);glow.SetFloat("_DstBlend",10);glow.SetFloat("_ZWrite",0);glow.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glow.renderQueue=3000;AssetDatabase.CreateAsset(glow,folder+"탐조등 빛줄기.mat");
            m.searchlights=new PrisonSearchlight[2];
            for(int i=0;i<2;i++)
            {
                var t=At("감시탑 탐조등 "+(i+1),new Vector3(i==0?-50:50,9,47));
                var search=t.gameObject.AddComponent<PrisonSearchlight>();m.searchlights[i]=search;
                search.sweepFrom=new Vector3(i==0?-22:-8,0,i==0?23:32);search.sweepTo=new Vector3(i==0?5:15,0,i==0?28:38);search.period=i==0?16:19;search.phase=i==0?0:.5f;
                search.lamp=t.gameObject.AddComponent<Light>();search.lamp.type=LightType.Spot;search.lamp.range=100;search.lamp.spotAngle=7;search.lamp.innerSpotAngle=5;search.lamp.intensity=60;search.lamp.color=new Color(1,.9f,.66f);search.lamp.shadows=LightShadows.Hard;
                var housing=GameObject.CreatePrimitive(PrimitiveType.Cylinder);housing.name="탐조등 하우징";housing.transform.SetParent(t,false);housing.transform.localRotation=Quaternion.Euler(90,0,0);housing.transform.localScale=new Vector3(.7f,.35f,.7f);housing.GetComponent<Renderer>().sharedMaterial=metal;Object.DestroyImmediate(housing.GetComponent<Collider>());
                var beam=t.gameObject.AddComponent<LineRenderer>();search.beam=beam;beam.positionCount=2;beam.useWorldSpace=true;beam.startWidth=.08f;beam.endWidth=3;beam.sharedMaterial=glow;beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;beam.receiveShadows=false;
                search.ResetSweep();search.SetRunning(false);
            }
            Object.FindFirstObjectByType<PrisonSharedPresentation>().night=m;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-night-setup.txt","PASS night mission, dialogue, two tower lights, cover props and HUD saved.");
        }
    }
}
#endif
