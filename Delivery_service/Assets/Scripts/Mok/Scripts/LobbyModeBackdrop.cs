using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 로비 카메라를 낮은 해상도로 받아 UI 뒤에만 흐리게 표시한다.
public sealed class LobbyModeBackdrop : MonoBehaviour {
    public Camera source;
    public RawImage display;
    RenderTexture buffer, previous;
    Camera screenOutput;
    void OnEnable(){
        previous=source.targetTexture;
        // 배경 카메라는 텍스처로 렌더링하지만 디스플레이에도 출력 카메라가 있어야 한다.
        var output=new GameObject("Mode screen output",typeof(Camera));
        output.transform.SetParent(transform,false);screenOutput=output.GetComponent<Camera>();
        screenOutput.cullingMask=0;screenOutput.clearFlags=CameraClearFlags.SolidColor;
        screenOutput.backgroundColor=Color.black;screenOutput.depth=-100;screenOutput.targetDisplay=source.targetDisplay;
        screenOutput.allowHDR=false;screenOutput.allowMSAA=false;
        Resize();
    }
    void Update(){if(buffer.width!=Mathf.Max(320,Screen.width/3)||buffer.height!=Mathf.Max(180,Screen.height/3))Resize();}
    void Resize(){
        if(buffer){source.targetTexture=previous;buffer.Release();Destroy(buffer);}
        buffer=new RenderTexture(Mathf.Max(320,Screen.width/3),Mathf.Max(180,Screen.height/3),24){name="Lobby soft background",filterMode=FilterMode.Bilinear};
        buffer.Create();source.targetTexture=buffer;display.texture=buffer;
    }
    void OnDisable(){if(screenOutput){screenOutput.enabled=false;Destroy(screenOutput.gameObject);}if(source)source.targetTexture=previous;if(display)display.texture=null;if(buffer){buffer.Release();Destroy(buffer);}}
}
}
