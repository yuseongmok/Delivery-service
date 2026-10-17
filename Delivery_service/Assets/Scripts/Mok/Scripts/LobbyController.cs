using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DeliveryService.Yun.Settings;
namespace DeliveryService.Mok {
// 로비 전용 연출과 메뉴. 게임 시간이나 주문 시스템을 실행하지 않는다.
public sealed class LobbyController : MonoBehaviour {
    public string gameScene="Assets/Scenes/JJinCity.unity";
    public Button startButton,settingsButton,quitButton,backButton;
    public GameObject settingsPanel;
    public CanvasGroup menu,fade;
    public Text status,bgmValue,sfxValue,screenValue;
    public Slider bgm,sfx;
    public Button screenButton;
    public GameAudioSettings audioSettings;
    public Camera lobbyCamera;
    public Light[] warmLights;
    float[] brightness; bool loading; Vector3 initialCamera;
    public bool IsLoading=>loading;
    void Awake(){
        Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        initialCamera=lobbyCamera.transform.position;fade.alpha=1;fade.blocksRaycasts=false;
        settingsPanel.SetActive(false);audioSettings.Load();
        bgm.SetValueWithoutNotify(audioSettings.BgmVolume);sfx.SetValueWithoutNotify(audioSettings.SfxVolume);
        bgm.onValueChanged.AddListener(v=>{audioSettings.SetBgmVolume(v);UpdateValues();});
        sfx.onValueChanged.AddListener(v=>{audioSettings.SetSfxVolume(v);UpdateValues();});
        startButton.onClick.AddListener(StartGame);settingsButton.onClick.AddListener(OpenSettings);quitButton.onClick.AddListener(Quit);
        backButton.onClick.AddListener(CloseSettings);screenButton.onClick.AddListener(ToggleScreen);
        brightness=new float[warmLights.Length];for(int i=0;i<warmLights.Length;i++)brightness[i]=warmLights[i]?warmLights[i].intensity:0;
        UpdateValues();StartCoroutine(Reveal());
    }
    IEnumerator Reveal(){for(float t=0;t<1;t+=Time.unscaledDeltaTime){fade.alpha=1-t;yield return null;}fade.alpha=0;}
    void Update(){
        for(int i=0;i<warmLights.Length;i++)if(warmLights[i])warmLights[i].intensity=brightness[i]*(1+.025f*Mathf.Sin(Time.unscaledTime*1.6f+i));
        if(!loading&&Input.GetKeyDown(KeyCode.Escape)&&settingsPanel.activeSelf)CloseSettings();
    }
    public void OpenSettings(){if(loading)return;settingsPanel.SetActive(true);menu.interactable=false;menu.blocksRaycasts=false;EventSystem.current?.SetSelectedGameObject(bgm.gameObject);}
    public void CloseSettings(){audioSettings.Save();settingsPanel.SetActive(false);menu.interactable=true;menu.blocksRaycasts=true;EventSystem.current?.SetSelectedGameObject(settingsButton.gameObject);}
    void UpdateValues(){bgmValue.text=Mathf.RoundToInt(bgm.value*100)+"%";sfxValue.text=Mathf.RoundToInt(sfx.value*100)+"%";screenValue.text=Screen.fullScreen?"전체 화면":"창 모드";}
    void ToggleScreen(){Screen.fullScreen=!Screen.fullScreen;StartCoroutine(ScreenLabel());}
    IEnumerator ScreenLabel(){yield return null;yield return null;UpdateValues();}
    public void StartGame(){if(loading)return;if(!Application.CanStreamedLevelBeLoaded(gameScene)){status.text="게임 씬을 불러올 수 없습니다.";return;}loading=true;StartCoroutine(EnterGame());}
    IEnumerator EnterGame(){
        audioSettings.Save();menu.interactable=false;menu.blocksRaycasts=false;fade.blocksRaycasts=true;status.text="배달 준비 중…";
        for(float t=0;t<1.2f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/1.2f);lobbyCamera.transform.position=initialCamera+lobbyCamera.transform.forward*(f*.8f);menu.alpha=1-f;fade.alpha=f;yield return null;}
        fade.alpha=1;Time.timeScale=1;
        // 로비로 돌아온 뒤 다시 시작해도 첫날의 자금과 재고로 시작한다.
        if(MoneyManager.Instance!=null)Destroy(MoneyManager.Instance.gameObject);
        NewGameStockReset.ResetForNewGame();yield return null;
        var op=SceneManager.LoadSceneAsync(gameScene,LoadSceneMode.Single);while(!op.isDone)yield return null;
    }
    public void Quit(){audioSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}
}
