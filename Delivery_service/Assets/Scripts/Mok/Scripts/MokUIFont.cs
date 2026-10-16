using UnityEngine;
namespace DeliveryService.Mok {
// References the original project font; shared assets must never be destroyed by UI owners.
public sealed class MokUIFont : ScriptableObject {
    public Font jua;
    public static Font Font => Resources.Load<MokUIFont>("MokUIFont").jua;
}
}
