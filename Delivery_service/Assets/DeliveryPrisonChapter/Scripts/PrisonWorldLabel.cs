using UnityEngine;
namespace DeliveryService.PrisonChapter {
// 동적 폰트 아틀라스를 월드 라벨에 연결해 벽 뒤로 비치는 글자를 방지한다.
[ExecuteAlways,RequireComponent(typeof(TextMesh))]
public sealed class PrisonWorldLabel:MonoBehaviour {
 void OnEnable(){Font.textureRebuilt+=Refresh;Apply();}
 void OnDisable(){Font.textureRebuilt-=Refresh;}
 void Refresh(Font font){if(font==GetComponent<TextMesh>().font)Apply();}
 void Apply(){var t=GetComponent<TextMesh>();var r=GetComponent<MeshRenderer>();if(t.font&&r.sharedMaterial)r.sharedMaterial.mainTexture=t.font.material.mainTexture;}
 void LateUpdate(){Apply();}
}
}
