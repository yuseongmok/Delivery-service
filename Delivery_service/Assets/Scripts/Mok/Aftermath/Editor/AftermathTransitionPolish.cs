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
    public static class AftermathTransitionPolish
    {
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-transition-"+stamp+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>();
                File.Copy(AssetDatabase.GetAssetPath(film.dialogue),"Logs/Aftermath-dialogue-before-transition-"+stamp+".asset");
                var bridge=film.GetComponent<AftermathEscapeBridge>();if(!bridge)bridge=film.gameObject.AddComponent<AftermathEscapeBridge>();
                bridge.player=film.firstPerson.forestPlayer;
                bridge.mingi=film.shots[0].motions.First(m=>m.actor && m.actor!=bridge.player).actor;
                film.escapeBridge=bridge;film.shots[0].fadeOut=false;
                if(film.dialogue.escapeBridge==null || film.dialogue.escapeBridge.Length==0)
                    film.dialogue.escapeBridge=new[]{
                        new AftermathDialogue.Line{scene="숲속 · 다음 행선지",subtitle="플레이어: 이제 어디로 가? 이 옷으로는 오래 못 버텨.",seconds=3.8f},
                        new AftermathDialogue.Line{scene="민기가 숨겨둔 돈",subtitle="민기: 잡히기 전에 숨겨 둔 돈이 있어. 조직에도 말 안 한 창고야.",seconds=4.8f},
                        new AftermathDialogue.Line{scene="도로를 피해 이동",subtitle="민기: 일단 도로에서 벗어나자. 어두워지면 그 창고로 가는 거야. 따라와.",seconds=6.2f}
                    };
                var news=film.shots[8];var anxious=film.shots[9];var call=film.shots[10];
                if(!anxious.lookFrom){anxious.lookFrom=new GameObject("뉴스를 보던 시선").transform;anxious.lookFrom.SetParent(film.transform);}
                anxious.lookFrom.position=news.lookAt.position;
                anxious.lookAt.position=news.lookAt.position+Vector3.down*.35f;
                anxious.fieldOfView=50;
                if(!call.lookFrom){call.lookFrom=new GameObject("불안하게 멈춘 시선").transform;call.lookFrom.SetParent(film.transform);}
                call.lookFrom.position=anxious.lookAt.position;
                film.dialogue.lines[9].seconds=3.2f;
                EditorUtility.SetDirty(film.dialogue);AssetDatabase.SaveAssets();
                string checks="";
                foreach(int index in new[]{8,9})
                {
                    film.ApplyShot(index,1);var p=film.film.transform.position;var q=film.film.transform.rotation;float f=film.film.fieldOfView;
                    film.ApplyShot(index+1,0);
                    float gap=Vector3.Distance(p,film.film.transform.position),angle=Quaternion.Angle(q,film.film.transform.rotation),zoom=Mathf.Abs(f-film.film.fieldOfView);
                    checks+=$"{index}->{index+1}: position={gap}, angle={angle}, fov={zoom}\n";
                    if(gap>.001f || angle>.05f || zoom>.01f)throw new Exception("Camera boundary jump: "+checks);
                }
                File.WriteAllText("Logs/Aftermath-transition-check.txt",checks);
                foreach(var shot in film.shots)shot.set.SetActive(false);
                film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
                EditorSceneManager.SaveScene(scene);AftermathReview.Record();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
