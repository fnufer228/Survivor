using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class InteractOutline : MonoBehaviour
{
    public static float OuterWidth = 6f;
    public static float WhiteWidth = 4.5f;
    public static float InnerWidth = 1.5f;
    public static Color White = Color.white;
    public static Color Black = new Color(0.02f, 0.02f, 0.02f, 1f);

    const string ChildName = "__InteractOutline";

    static readonly Dictionary<Mesh, Mesh> smoothCache = new Dictionary<Mesh, Mesh>();
    static Material[] sharedMaterials;

    readonly List<GameObject> outlineObjects = new List<GameObject>();
    bool built;

    public static void Show(Component target)
    {
        if (target == null) return;
        var o = target.GetComponent<InteractOutline>();
        if (o == null) o = target.gameObject.AddComponent<InteractOutline>();
        o.SetVisible(true);
    }

    public static void Hide(Component target)
    {
        if (target == null) return;
        var o = target.GetComponent<InteractOutline>();
        if (o != null) o.SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (visible && !built) Build();
        foreach (var go in outlineObjects)
            if (go != null) go.SetActive(visible);
    }

    void OnDisable() => SetVisible(false);

    void Build()
    {
        built = true;
        var materials = GetMaterials();
        if (materials == null) return;

        foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.gameObject.name == ChildName) continue;
            if (mf.GetComponent<MeshRenderer>() == null) continue;

            var go = new GameObject(ChildName);
            go.layer = mf.gameObject.layer;
            go.transform.SetParent(mf.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetSmoothMesh(mf.sharedMesh);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = materials;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            go.SetActive(false);
            outlineObjects.Add(go);
        }
    }

    static Material[] GetMaterials()
    {
        if (sharedMaterials != null) return sharedMaterials;
        var shader = Resources.Load<Shader>("Shaders/InteractOutline");
        if (shader == null) shader = Shader.Find("Survivor/InteractOutline");
        if (shader == null)
        {
            Debug.LogError("InteractOutline: shader Survivor/InteractOutline not found");
            return null;
        }

        Material Make(Color c, float w, int order)
        {
            var m = new Material(shader) { name = "InteractOutline_" + order, hideFlags = HideFlags.DontSave };
            m.SetColor("_Color", c);
            m.SetFloat("_Width", w);
            m.renderQueue = (int)RenderQueue.Geometry + 10 + order;
            return m;
        }

        sharedMaterials = new[]
        {
            Make(Black, OuterWidth, 0),
            Make(White, WhiteWidth, 1),
            Make(Black, InnerWidth, 2),
        };
        return sharedMaterials;
    }

    static Mesh GetSmoothMesh(Mesh src)
    {
        if (smoothCache.TryGetValue(src, out var cached) && cached != null) return cached;

        Mesh result = src;
        if (src.isReadable)
        {
            var verts = src.vertices;
            var normals = src.normals;
            if (normals == null || normals.Length != verts.Length)
            {
                var tmp = Instantiate(src);
                tmp.RecalculateNormals();
                normals = tmp.normals;
                Destroy(tmp);
            }

            var sum = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));
            for (int i = 0; i < verts.Length; i++)
            {
                var k = Key(verts[i]);
                sum.TryGetValue(k, out var s);
                sum[k] = s + normals[i];
            }
            var smooth = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++) smooth[i] = sum[Key(verts[i])].normalized;

            var tris = new List<int>();
            for (int s = 0; s < src.subMeshCount; s++) tris.AddRange(src.GetTriangles(s));

            var m = new Mesh { name = src.name + "_Outline", hideFlags = HideFlags.DontSave };
            m.indexFormat = verts.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.vertices = verts;
            m.normals = smooth;
            m.SetTriangles(tris, 0);
            m.bounds = src.bounds;
            result = m;
        }
        smoothCache[src] = result;
        return result;
    }
}
