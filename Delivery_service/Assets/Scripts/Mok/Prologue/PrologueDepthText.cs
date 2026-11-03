using UnityEngine;
namespace DeliveryService.Mok {
// 동적 주아체 아틀라스가 바뀌어도 깊이 검사를 하는 간판 재질을 유지한다.
[ExecuteAlways,RequireComponent(typeof(TextMesh))]
public sealed class PrologueDepthText : MonoBehaviour {
    public Material depthMaterial;
    static Material fallback;
    TextMesh label;
    Renderer display;
    MaterialPropertyBlock block;
    void OnEnable(){label=GetComponent<TextMesh>();display=GetComponent<Renderer>();block=new MaterialPropertyBlock();Font.textureRebuilt+=Rebuilt;Refresh();}
    void OnDisable(){Font.textureRebuilt-=Rebuilt;}
    void Rebuilt(Font font){if(label&&label.font==font)Refresh();}
    void Refresh(){if(!label||!label.font||!display)return;
        if(!depthMaterial){
            if(display.sharedMaterial&&display.sharedMaterial.shader.name=="Mok/GarageDepthText")depthMaterial=display.sharedMaterial;
            else {if(!fallback){var shader=Shader.Find("Mok/GarageDepthText");if(!shader)return;fallback=new Material(shader){name="World text depth fallback",hideFlags=HideFlags.HideAndDontSave};}depthMaterial=fallback;}
        }
        display.sharedMaterial=depthMaterial;
        block.SetTexture("_MainTex",label.font.material.mainTexture);display.SetPropertyBlock(block);
    }
}
}
