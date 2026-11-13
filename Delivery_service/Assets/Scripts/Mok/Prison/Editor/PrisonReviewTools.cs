#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonReviewTools
    {
        static PrisonReviewTools(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||!File.Exists("Temp/PrisonReviewProbe"))return;var action=File.ReadAllText("Temp/PrisonReviewProbe").Trim();File.Delete("Temp/PrisonReviewProbe");Physics.SyncTransforms();
            var text="";foreach(var c in Physics.OverlapBox(new Vector3(-5.44f,4,11),new Vector3(3,1,3)))text+=c.name+" "+c.bounds+" parent="+c.transform.parent?.name+"\n";
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();foreach(var c in m.previous.prison.cart.GetComponentsInChildren<Collider>())text+="CART "+c.name+" "+c.bounds+"\n";
            if(action=="cart"){
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Prison-before-cart-review.unity",true);
                m.previous.prison.cart.controller.cartRotationOffset=new Vector3(0,90,0);
                EditorUtility.SetDirty(m.previous.prison.cart.controller);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(m.gameObject.scene);
            }
            foreach(var g in m.diners.Concat(new[]{m.warden})){
                var a=g.GetComponent<Animator>();var skin=g.GetComponentInChildren<SkinnedMeshRenderer>();
                text+="RIG "+g.name+" hand="+a.GetBoneTransform(HumanBodyBones.RightHand).GetInstanceID()+"\n";
                for(int i=0;i<skin.bones.Length;i++)if(skin.bones[i].name.Contains("Arm.R")||skin.bones[i].name=="Hand.R")text+=i+" "+skin.bones[i].name+" id="+skin.bones[i].GetInstanceID()+"\n";
            }
            File.WriteAllText("Logs/Prison-review-probe.txt",text);
        }
    }
}
#endif

