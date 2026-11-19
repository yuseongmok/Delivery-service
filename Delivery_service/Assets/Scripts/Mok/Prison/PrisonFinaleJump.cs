using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok
{
    // 독립된 새 감옥 씬에서 선행 미션을 완료해 연출/카메라 잔여 상태를 넘기지 않는다.
    public sealed class PrisonFinaleJump : MonoBehaviour
    {
        public static bool Running{get;private set;}
        public static string LastError{get;private set;}
        string status="감옥 탈옥 피날레 준비 중…";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Running=false;LastError="";}
        public static bool Begin()
        {
            if(Running||PrisonPreviewTransition.IsLoading||!Application.CanStreamedLevelBeLoaded(PrisonPreviewTransition.SceneName))return false;
            Running=true;LastError="";var go=new GameObject("감옥 피날레 바로 시작");DontDestroyOnLoad(go);go.AddComponent<PrisonFinaleJump>();return true;
        }
        IEnumerator Start()
        {
            Time.timeScale=1;GameModeSession.Select(false);
            var oldKeys=FindFirstObjectByType<PrisonKeysMission>();if(oldKeys){var theft=oldKeys.GetComponent<PrisonPickpocket>();if(theft)theft.End(false);oldKeys.CancelLock();}
            var oldCctv=FindFirstObjectByType<PrisonCctvMission>();if(oldCctv&&oldCctv.wirePanel)oldCctv.wirePanel.Abort();
            yield return SceneManager.LoadSceneAsync(PrisonPreviewTransition.SceneName,LoadSceneMode.Single);
            yield return null;yield return null;
            float deadline=Time.realtimeSinceStartup+60;
            while(PrisonMissionSkip.Target!="CCTV 무력화")
            {
                string before=PrisonMissionSkip.Target;
                if(string.IsNullOrEmpty(before)||Time.realtimeSinceStartup>deadline){Fail("감옥 선행 미션 준비에 실패했습니다. 다시 시도해 주세요.");yield break;}
                status="탈옥 피날레 준비 · "+before;
                if(!PrisonMissionSkip.Complete()){yield return null;continue;}
                while(PrisonMissionSkip.Target==before){if(Time.realtimeSinceStartup>deadline){Fail("미션 전환 대기 시간이 초과됐습니다.");yield break;}yield return null;}
            }
            yield return null;
            var mission=FindFirstObjectByType<PrisonCctvMission>();
            var prison=FindFirstObjectByType<PrisonMission>();
            if(!mission||!prison){Fail("감옥 피날레 구성요소를 찾지 못했습니다.");yield break;}
            var player=prison.player;var point=mission.legacyEscape.changePoint;
            // 수납장을 피해 기존 상호작용 위치의 옆에 세운다.
            Vector3 location=point.position-point.forward*1.2f;
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=location+Vector3.up*1.1f;cc.enabled=true;
            var heading=point.position-player.transform.position;heading.y=0;if(heading.sqrMagnitude>.01f)player.transform.rotation=Quaternion.LookRotation(heading);
            player.View.transform.localRotation=Quaternion.identity;player.View.enabled=true;
            player.GetComponentInChildren<CameraMove>().SyncViewFromTransform();
            var friend=prison.introduction.mingi;
            if(friend.agent.enabled&&friend.agent.isOnNavMesh)friend.agent.Warp(mission.office.evidence.mingiStand.position);
            friend.ResumeFollowing();Physics.SyncTransforms();
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            Running=false;Destroy(gameObject);
        }
        void Fail(string text){LastError=text;Debug.LogError(text);Running=false;Destroy(gameObject);}
        void OnDestroy(){Running=false;}
        void OnGUI(){if(!Running)return;GUI.depth=-32000;var old=GUI.color;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(0,Screen.height*.45f,Screen.width,100),status,new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=26,alignment=TextAnchor.MiddleCenter});GUI.color=old;}
    }
}
