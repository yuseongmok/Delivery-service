#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DeliveryService.Mok.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// JJinCity와 같은 평면 내비 지도(바닥·도로·건물 윤곽)를 현재 씬에 만들어 저장한다.
// JJinCity의 Ground/Roads/Buildings 재질을 그대로 써서 세 씬의 지도 모양을 통일한다.
public static class ChapterNavigationMapSetup
{
    const string Folder = "Assets/Scripts/Mok/Art/NavigationMap";
    const string RootName = "Mok Navigation Map";

    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<int>[] tris = { new List<int>(), new List<int>(), new List<int>() };

    static readonly string[] roadWords = { "asphalt", "road", "도로", "노면", "골목" };
    static readonly string[] groundWords = { "paving", "sidewalk", "pavement", "seawall", "garden", "grass", "보도", "산책로", "광장", "기반", "바닥", "주차" };
    static readonly string[] notBuilding = { "tree", "pole", "veh_", "car", "bench", "lamp", "light", "cloud", "fence", "sign", "trash", "plant", "bush", "rock", "난간", "가로등", "나무", "볼라드", "벽등", "sky", "cloud" };

    [MenuItem("Tools/Delivery Service/Mok/Install Chapter Navigation Map (current scene)")]
    public static void InstallMenu() => Debug.Log(Install(true));

    public static string Install(bool replace)
    {
        var scene = EditorSceneManager.GetActiveScene();
        var nav = UnityEngine.Object.FindFirstObjectByType<NavigationManager>();
        if (!nav) throw new Exception("NavigationManager가 씬에 없습니다.");
        var existing = GameObject.Find(RootName);
        if (existing)
        {
            if (!replace) return "이미 지도가 있습니다: " + scene.name;
            Undo.DestroyObjectImmediate(existing);
        }
        var trackGo = GameObject.Find("NavigationCamera");
        var track = trackGo ? trackGo.GetComponent<Camera>() : null;
        if (!track) throw new Exception("NavigationCamera가 없습니다.");
        var overviewGo = GameObject.Find("NavigationOverviewCamera");
        var overview = overviewGo ? overviewGo.GetComponent<Camera>() : null;

        vertices.Clear(); foreach (var t in tris) t.Clear();
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.gameObject.layer != NavigationMapStyle.MapLayer)
            .ToArray();

        var roads = new List<Bounds>(); var grounds = new List<Bounds>(); var buildings = new List<Bounds>();
        foreach (var r in renderers)
        {
            var b = r.bounds; string key = Key(r);
            bool flat = b.size.y < 1.2f && b.min.y < 1.2f && b.min.y > -2f;
            float area = b.size.x * b.size.z;
            if (flat && area > 4 && area < 200000)
            {
                if (r.name.StartsWith("SM_Env_Road_") && !r.name.Contains("Lines") && !r.name.Contains("Arrow")) { roads.Add(b); continue; }
                if (r.name.StartsWith("SM_Env_Sidewalk")) { grounds.Add(b); continue; }
                string own = OwnKey(r); // 도로 판정은 부모 이름(예: "외곽 도로" 그룹) 대신 자기 이름·재질로만 한다.
                if (roadWords.Any(own.Contains) && !own.Contains("점선") && !own.Contains("line") && !groundWords.Any(own.Contains)) { roads.Add(b); continue; }
                if (groundWords.Any(key.Contains)) { grounds.Add(b); continue; }
            }
            // 벽 조각으로 지은 매장은 지붕 판으로 윤곽을 잡는다.
            if (b.min.y > 2.5f && b.size.y < 2f && area > 20 && (key.Contains("지붕") || key.Contains("roof") || key.Contains("천장"))) { buildings.Add(b); continue; }
            bool tall = b.size.y > 2.5f && b.min.y < 1.5f;
            bool footprint = b.size.x > 1.5f && b.size.z > 1.5f && area > 6;
            if (tall && footprint && (r.name.StartsWith("SM_Bld_") || !notBuilding.Any(key.Contains))) buildings.Add(b);
        }
        if (roads.Count + grounds.Count == 0) throw new Exception("도로/바닥 렌더러를 찾지 못했습니다.");

        var all = roads.Concat(grounds).ToList();
        var bounds = all[0]; foreach (var b in all) bounds.Encapsulate(b);
        Quad(bounds, 0, -.2f);                       // 지도 바탕
        foreach (var b in grounds) Quad(b, 0, -.1f);  // 보도/광장(바탕색)
        foreach (var b in roads) Quad(b, 1, 0);       // 도로
        // 하늘·원경 같은 거대한 배경이나 지도 범위 밖 물체는 건물에서 뺀다.
        buildings = buildings.Where(b => b.size.x < 80 && b.size.z < 80 && bounds.Contains(new Vector3(b.center.x, bounds.center.y, b.center.z))).ToList();
        foreach (var b in buildings) Quad(b, 2, .15f);// 건물

        var mesh = new Mesh { name = scene.name + "NavigationMap", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(tris[i], i);
        mesh.RecalculateBounds(); mesh.RecalculateNormals();
        string meshPath = Folder + "/" + scene.name + "NavigationMap.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (old) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; }
        else AssetDatabase.CreateAsset(mesh, meshPath);

        var root = new GameObject(RootName) { layer = NavigationMapStyle.MapLayer };
        Undo.RegisterCreatedObjectUndo(root, "Install chapter navigation map");
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { Load("Ground"), Load("Roads"), Load("Buildings") };
        mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;

        var lineGo = GameObject.Find("NavigationPathLine");
        if (lineGo)
        {
            var line = lineGo.GetComponent<LineRenderer>();
            lineGo.layer = 17; line.sharedMaterial = Load("Route");
            line.startColor = line.endColor = Color.white; line.widthMultiplier = 1.5f;
            line.numCornerVertices = 4; line.numCapVertices = 4;
        }

        NavigationMapStyle.Configure(track); track.orthographicSize = 40;
        if (overview)
        {
            overview.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 80, bounds.center.z), Quaternion.Euler(90, 0, 0));
            overview.orthographicSize = Mathf.Max(bounds.extents.z + 12, (bounds.extents.x + 12) / (850f / 520f));
            NavigationMapStyle.Configure(overview); overview.enabled = false;
        }
        var style = root.AddComponent<NavigationMapStyle>(); style.trackingCamera = track; style.overviewCamera = overview;
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != track && c != overview) c.cullingMask &= ~(1 << NavigationMapStyle.MapLayer);

        var planner = UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>(FindObjectsInactive.Include);
        if (planner && overview && !planner.overviewCamera) { planner.overviewCamera = overview; planner.overviewTexture = overview.targetTexture; EditorUtility.SetDirty(planner); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"{scene.name}: 도로 {roads.Count}, 바닥 {grounds.Count}, 건물 {buildings.Count}, 범위 {bounds.size.x:F0}x{bounds.size.z:F0} · 저장 완료";
    }

    static string Key(Renderer r)
    {
        string mat = r.sharedMaterial ? r.sharedMaterial.name : "";
        string parent = r.transform.parent ? r.transform.parent.name : "";
        return (r.name + " " + mat + " " + parent).ToLowerInvariant();
    }

    static string OwnKey(Renderer r) => (r.name + " " + (r.sharedMaterial ? r.sharedMaterial.name : "")).ToLowerInvariant();

    static Material Load(string name)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat");
        if (!m) throw new Exception(name + ".mat 이 없습니다.");
        return m;
    }

    static void Quad(Bounds b, int m, float h)
    {
        int n = vertices.Count;
        vertices.Add(new Vector3(b.min.x, h, b.min.z)); vertices.Add(new Vector3(b.min.x, h, b.max.z));
        vertices.Add(new Vector3(b.max.x, h, b.max.z)); vertices.Add(new Vector3(b.max.x, h, b.min.z));
        tris[m].AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
    }
}
#endif
