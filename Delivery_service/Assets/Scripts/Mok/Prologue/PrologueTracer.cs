using UnityEngine;
namespace DeliveryService.Mok {
// 피해 판정은 사격 시 계산하고, 이 선은 탄환의 이동만 표현한다.
public sealed class PrologueTracer : MonoBehaviour {
    static Material shared;
    LineRenderer line;
    Vector3 from, to;
    float elapsed, distance;
    public const float Speed = 150;
    public static PrologueTracer Spawn(Vector3 start, Vector3 end, Color color) {
        if(!shared)shared=new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name="Prologue tracer" };
        var go=new GameObject("Bullet tracer");
        var t=go.AddComponent<PrologueTracer>();t.from=start;t.to=end;t.distance=Vector3.Distance(start,end);
        BulletFlyby.TryPlay(start,end);
        t.line=go.AddComponent<LineRenderer>();t.line.sharedMaterial=shared;
        t.line.positionCount=2;t.line.useWorldSpace=true;t.line.widthMultiplier=.035f;
        t.line.startColor=color;t.line.endColor=color;t.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        t.line.receiveShadows=false;t.line.SetPosition(0,start);t.line.SetPosition(1,start);
        // URP Unlit은 버텍스 색상을 사용하지 않으므로 인스턴스 속성으로 색을 전달한다.
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);t.line.SetPropertyBlock(block);
        return t;
    }
    void Update() {
        elapsed+=Time.deltaTime;
        float head=elapsed*Speed,tail=Mathf.Max(0,head-4);
        if(tail>distance){Destroy(gameObject);return;}
        var direction=(to-from).normalized;
        line.SetPosition(0,from+direction*Mathf.Min(tail,distance));
        line.SetPosition(1,from+direction*Mathf.Min(head,distance));
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetMaterial(){if(shared)Object.Destroy(shared);shared=null;}
}
}
