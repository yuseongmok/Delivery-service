using UnityEngine;
namespace DeliveryService.Mok {
// 다음 날로 넘어갈 때 일일 초기화 전에 게임오버를 판정한다.
public sealed class BankruptcyGameOver : MonoBehaviour {
    public int SurvivedDays {get;private set;}
    Font font; float previousScale;
    public static bool TryEndDay(DayNightCycle clock){
        if(MoneyManager.Instance==null||MoneyManager.Instance.currentMoney>=0)return false;
        if(FindFirstObjectByType<BankruptcyGameOver>()!=null)return true;
        var screen=new GameObject("Mok Bankruptcy Game Over").AddComponent<BankruptcyGameOver>();
        screen.SurvivedDays=clock.currentDay;
        screen.previousScale=Time.timeScale;
        clock.isShopOpen=false;
        // 입력·코루틴·물리·시간 진행을 멈춘다.
        foreach(var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)){
            if(behaviour==screen||behaviour is IncomeFeedback||behaviour.GetType().Assembly!=typeof(PlayerCombat).Assembly)continue;
            behaviour.StopAllCoroutines();behaviour.enabled=false;
        }
        Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        screen.font=MokUIFont.Font;
        return true;
    }
    void LateUpdate(){Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    void OnGUI(){
        GUI.depth=-10000;
        GUI.color=new Color(.025f,.035f,.055f,1);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
        var title=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(28,Screen.height/16),alignment=TextAnchor.MiddleCenter};
        title.normal.textColor=new Color(1,.4f,.36f);
        GUI.Label(new Rect(0,Screen.height*.32f,Screen.width,Screen.height*.14f),"파산했습니다",title);
        title.fontSize=Mathf.Max(20,Screen.height/28);title.normal.textColor=Color.white;
        GUI.Label(new Rect(0,Screen.height*.49f,Screen.width,Screen.height*.1f),$"버틴 일차: {SurvivedDays}일",title);
        title.fontSize=Mathf.Max(14,Screen.height/40);title.normal.textColor=Color.gray;
        GUI.Label(new Rect(0,Screen.height*.62f,Screen.width,Screen.height*.08f),"GAME OVER",title);
    }
    void OnDestroy(){Time.timeScale=previousScale;}
}
}
