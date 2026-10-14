using UnityEngine;
namespace DeliveryService.Mok {
// Dynamic fonts can rebuild their atlas after loading; preserve depth-tested sign text.
[ExecuteAlways]
public sealed class GarageTextAtlas : MonoBehaviour {
void OnEnable(){Font.textureRebuilt+=Rebind;Refresh();}
void OnDisable(){Font.textureRebuilt-=Rebind;}
void Rebind(Font font){Refresh();}
void Refresh(){foreach(var text in GetComponentsInChildren<TextMesh>(true)){if(text.font==null||text.font.material.mainTexture==null)continue;var renderer=text.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetTexture("_MainTex",text.font.material.mainTexture);renderer.SetPropertyBlock(block);}}
}
}
