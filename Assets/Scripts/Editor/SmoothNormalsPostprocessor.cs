using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SmoothNormalsPostprocessor : AssetPostprocessor
{
    const string Folder = "Assets/Assets/Models/Camp/";

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;

        var done = new HashSet<Mesh>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null && done.Add(filter.sharedMesh)) Bake(filter.sharedMesh);
        foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (skin.sharedMesh != null && done.Add(skin.sharedMesh)) Bake(skin.sharedMesh);
    }

    static void Bake(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        if (normals == null || normals.Length != vertices.Length) return;

        var sums = new Dictionary<Vector3Int, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3Int key = Key(vertices[i]);
            sums.TryGetValue(key, out Vector3 sum);
            sums[key] = sum + normals[i];
        }

        var smooth = new List<Vector3>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 n = sums[Key(vertices[i])];
            smooth.Add(n.sqrMagnitude > 1e-8f ? n.normalized : normals[i]);
        }
        mesh.SetUVs(3, smooth);
    }

    static Vector3Int Key(Vector3 v) =>
        new Vector3Int(Mathf.RoundToInt(v.x * 10000f), Mathf.RoundToInt(v.y * 10000f), Mathf.RoundToInt(v.z * 10000f));
}
