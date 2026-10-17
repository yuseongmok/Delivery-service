using UnityEngine;
namespace DeliveryService.Mok {
// 동적 글꼴 아틀라스 갱신 후에도 간판 글자의 깊이 판정을 유지한다.
[ExecuteAlways]
public sealed class GarageTextAtlas : MonoBehaviour {
void OnEnable(){Font.textureRebuilt+=Rebind;Refresh();}
void OnDisable(){Font.textureRebuilt-=Rebind;}
void Rebind(Font font){Refresh();}
void Refresh(){foreach(var text in GetComponentsInChildren<TextMesh>(true)){if(text.font==null||text.font.material.mainTexture==null)continue;var renderer=text.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetTexture("_MainTex",text.font.material.mainTexture);renderer.SetPropertyBlock(block);}}
}
}
