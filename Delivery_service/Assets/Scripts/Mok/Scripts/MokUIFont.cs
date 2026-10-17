using UnityEngine;
namespace DeliveryService.Mok {
// 프로젝트 원본 글꼴을 공유하므로 UI에서 이 에셋을 파괴하면 안 된다.
public sealed class MokUIFont : ScriptableObject {
    public Font jua;
    public static Font Font => Resources.Load<MokUIFont>("MokUIFont").jua;
}
}
