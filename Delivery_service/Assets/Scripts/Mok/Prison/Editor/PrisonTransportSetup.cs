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
    [InitializeOnLoad] public static class PrisonTransportSetup
    {
        static PrisonTransportSetup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonTransportSetup"))return;
            var action=File.ReadAllText("Temp/PrisonTransportSetup").Trim();File.Delete("Temp/PrisonTransportSetup");try{if(action=="polish")Polish();else Build();}catch(Exception e){File.WriteAllText("Logs/Prison-transport-setup.txt",e.ToString());}
        }
        static void Polish()
        {
            var m=Object.FindFirstObjectByType<PrisonTransportEscape>();var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-transport-polish.unity",true);
            m.eye.localPosition=new Vector3(-.43f,.10f,-.85f);m.eye.localRotation=Quaternion.Euler(-5,0,0);
            m.playerHang.localPosition=new Vector3(-.43f,.13f,1);m.friendHang.localPosition=new Vector3(.43f,.13f,1);
            foreach(var pose in new[]{m.playerActor,m.friendActor}){var a=pose.leftGrip.localPosition;a.y=.23f;pose.leftGrip.localPosition=a;a=pose.rightGrip.localPosition;a.y=.23f;pose.rightGrip.localPosition=a;}
            foreach(Transform t in m.vehicle)if(t.name=="차량 하부 가로 지지대"){var a=t.localPosition;a.y=.23f;t.localPosition=a;}
            if(!m.hands)
            {
                var source=Object.FindFirstObjectByType<PrisonDuelPresentation>().playerHands;
                var copy=Object.Instantiate(source.gameObject,m.transform);copy.name="차량 지지대를 잡는 실제 플레이어 팔";copy.SetActive(false);
                var rig=copy.GetComponent<PrisonTheftHands>();rig.enabled=false;
                m.hands=copy.AddComponent<PrisonTransportHands>();m.hands.rig=rig;m.hands.view=m.film;m.hands.leftGrip=m.playerActor.leftGrip;m.hands.rightGrip=m.playerActor.rightGrip;
                foreach(var rr in copy.GetComponentsInChildren<Renderer>(true)){var materials=rr.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&(materials[i].name.Contains("주황")||materials[i].name.Contains("죄수복")))materials[i]=m.previous.previous.legacyEscape.uniform;rr.sharedMaterials=materials;}
            }
            foreach(var rr in m.hands.GetComponentsInChildren<Renderer>(true)){var materials=rr.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&(materials[i].name.Contains("소매")||materials[i].name.Contains("죄수")))materials[i]=m.previous.previous.legacyEscape.uniform;rr.sharedMaterials=materials;}
            m.dialogue.ready="민기: 바퀴 사이로 들어가. 아래쪽 철제 지지대를 잡으면 돼.";EditorUtility.SetDirty(m.dialogue);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-transport-polish.txt","PASS hand-only first person rig and underbody clearance");
        }
        static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Prison only");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-transport-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            if(Object.FindFirstObjectByType<PrisonTransportEscape>())throw new Exception("Already installed");
            var root=new GameObject("30_호송차량 하부 탈출").transform;
            var m=root.gameObject.AddComponent<PrisonTransportEscape>();m.previous=Object.FindFirstObjectByType<PrisonNightMission>();
            var old=m.previous.previous.legacyEscape;var intro=m.previous.previous.office.previous.previous.prison.introduction;
            m.vehicle=old.vehicle;m.gateLeft=old.gateLeft;m.gateRight=old.gateRight;m.film=old.filmCamera;
            m.dialogue=ScriptableObject.CreateInstance<PrisonTransportText>();AssetDatabase.CreateAsset(m.dialogue,"Assets/DeliveryPrisonChapter/Escape/호송차량 하부 탈출 대사.asset");
            Transform At(string name,Transform parent,Vector3 point,Quaternion rotation){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=point;t.localRotation=rotation;return t;}
            m.start=At("호송 출발 체크포인트",root,m.vehicle.position,m.vehicle.rotation);
            var center=(m.gateLeft.position+m.gateRight.position)*.5f;
            m.destination=At("호송차량 정문 밖 정차",root,new Vector3(center.x,m.vehicle.position.y,center.z+14),m.vehicle.rotation);
            m.playerExit=At("플레이어 탈출 후 위치",root,m.destination.position+new Vector3(-3,-.15f,0),Quaternion.Euler(0,90,0));
            m.friendExit=At("민기 탈출 후 위치",root,m.destination.position+new Vector3(-1.5f,-.15f,0),Quaternion.Euler(0,270,0));
            m.playerHang=At("플레이어 하부 자세",m.vehicle,new Vector3(-.43f,.23f,1),Quaternion.Euler(-90,0,0));
            m.friendHang=At("민기 하부 자세",m.vehicle,new Vector3(.43f,.23f,1),Quaternion.Euler(-90,0,0));
            m.eye=At("하부 1인칭 시점",m.vehicle,new Vector3(-.43f,.25f,-.85f),Quaternion.Euler(-12,0,0));
            PrisonTransportPose Actor(Transform source,string name,Transform anchor)
            {
                var copy=Object.Instantiate(source.gameObject,root);copy.name=name;copy.SetActive(false);
                foreach(var b in copy.GetComponentsInChildren<Behaviour>(true))if(!(b is Animator))Object.DestroyImmediate(b);
                foreach(var c in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
                foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
                var pose=copy.AddComponent<PrisonTransportPose>();pose.animator=copy.GetComponentInChildren<Animator>(true);pose.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                pose.leftGrip=At(name+" 왼손 지지대",m.vehicle,new Vector3(anchor.localPosition.x-.22f,.57f,-.25f),Quaternion.identity);
                pose.rightGrip=At(name+" 오른손 지지대",m.vehicle,new Vector3(anchor.localPosition.x+.22f,.57f,-.25f),Quaternion.identity);
                foreach(var rr in copy.GetComponentsInChildren<Renderer>(true)){var materials=rr.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&(materials[i].name.Contains("주황")||materials[i].name.Contains("죄수복")))materials[i]=old.uniform;rr.sharedMaterials=materials;}
                copy.transform.SetPositionAndRotation(anchor.position,anchor.rotation);return pose;
            }
            m.playerActor=Actor(intro.mugshotActor,"플레이어 · 차량 하부 대역",m.playerHang);
            m.friendActor=Actor(intro.mingi.transform,"민기 · 차량 하부 대역",m.friendHang);
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.15f,.18f,.2f)};AssetDatabase.CreateAsset(mat,"Assets/DeliveryPrisonChapter/Escape/차량 하부 지지대.mat");
            foreach(float z in new[]{-.25f,.9f}){var bar=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bar.name="차량 하부 가로 지지대";bar.transform.SetParent(m.vehicle,false);bar.transform.localPosition=new Vector3(0,.57f,z);bar.transform.localRotation=Quaternion.Euler(0,0,90);bar.transform.localScale=new Vector3(.055f,.83f,.055f);bar.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(bar.GetComponent<Collider>());}
            if(!m.vehicle.GetComponent<TrafficWheelVisual>())
            {
                var wheels=m.vehicle.GetComponentsInChildren<Transform>().Where(t=>t.name.ToLowerInvariant().Contains("wheel")&&t.GetComponent<MeshRenderer>()).ToArray();
                if(wheels.Length>0){var v=m.vehicle.gameObject.AddComponent<TrafficWheelVisual>();v.wheels=wheels;v.radii=wheels.Select(w=>Mathf.Max(.1f,w.GetComponent<Renderer>().bounds.extents.y)).ToArray();}
            }
            Object.FindFirstObjectByType<PrisonSharedPresentation>().transport=m;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-transport-setup.txt","PASS saved transport escape. start="+m.start.position+" gate="+center+" end="+m.destination.position+"\n"+string.Join("\n",m.vehicle.GetComponentsInChildren<Renderer>().Select(rr=>rr.name+" "+rr.bounds)));
        }
    }
}
#endif
