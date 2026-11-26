using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BuildGhost : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    class Part
    {
        public BuildPart Source;
        public Renderer Renderer;
        public Material[] GhostMaterials;
        public bool Filled;
    }

    Renderer[] renderers;
    MaterialPropertyBlock block;
    Color currentColor;
    bool hasColor;
    readonly List<Part> parts = new List<Part>();
    readonly HashSet<Renderer> filledRenderers = new HashSet<Renderer>();

    public Bounds LocalBounds { get; private set; }
    public bool HasParts => parts.Count > 0;

    public static BuildGhost Create(BuildingRecipe recipe, Material material)
    {
        GameObject root = recipe.ghostPrefab != null
            ? Instantiate(recipe.ghostPrefab)
            : BuildFromMeshes(recipe.buildingPrefab);

        root.name = $"Ghost_{recipe.name}";
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            Destroy(col);

        var partRenderers = new Dictionary<Renderer, BuildPart>();
        var keepMaterials = new HashSet<Renderer>();
        foreach (BuildPart part in root.GetComponentsInChildren<BuildPart>(true))
        {
            if (part.TryGetComponent(out Renderer partRenderer))
            {
                partRenderers[partRenderer] = part;
                keepMaterials.Add(partRenderer);
            }
            if (part.outline != null) keepMaterials.Add(part.outline);
        }

        Renderer[] found = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer r in found)
        {
            if (r.enabled && r.gameObject.activeInHierarchy)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (material != null && !keepMaterials.Contains(r))
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        BuildGhost ghost = root.AddComponent<BuildGhost>();
        ghost.renderers = found;
        ghost.block = new MaterialPropertyBlock();
        ghost.LocalBounds = hasBounds ? bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);

        foreach (var pair in partRenderers)
        {
            ghost.parts.Add(new Part
            {
                Source = pair.Value,
                Renderer = pair.Key,
                GhostMaterials = pair.Key.sharedMaterials
            });
        }
        ghost.parts.Sort((a, b) => a.Source.transform.GetSiblingIndex().CompareTo(b.Source.transform.GetSiblingIndex()));
        return ghost;
    }

    static GameObject BuildFromMeshes(GameObject prefab)
    {
        var root = new GameObject();
        root.transform.localScale = prefab.transform.localScale;
        Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;

        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer source)) continue;

            var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(root.transform, false);

            Matrix4x4 local = toRoot * filter.transform.localToWorldMatrix;
            part.transform.localPosition = local.GetPosition();
            part.transform.localRotation = local.rotation;
            part.transform.localScale = local.lossyScale;

            part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        }

        return root;
    }

    public void SetProgress(int costIndex, int delivered, int required)
    {
        int count = 0;
        foreach (Part p in parts)
            if (p.Source.costIndex == costIndex) count++;
        if (count == 0) return;

        int filled = required <= 0
            ? count
            : Mathf.Clamp(Mathf.FloorToInt((float)delivered * count / required + 0.0001f), 0, count);

        int index = 0;
        foreach (Part p in parts)
        {
            if (p.Source.costIndex != costIndex) continue;
            SetFilled(p, index < filled);
            index++;
        }
    }

    void SetFilled(Part part, bool value)
    {
        if (part.Filled == value) return;
        part.Filled = value;
        Renderer r = part.Renderer;

        if (value)
        {
            Material[] solid = part.Source.solidMaterials;
            if (solid != null && solid.Length > 0) r.sharedMaterials = solid;
            r.SetPropertyBlock(null);
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            filledRenderers.Add(r);
        }
        else
        {
            r.sharedMaterials = part.GhostMaterials;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            filledRenderers.Remove(r);
            if (hasColor) ApplyColor(r, currentColor);
        }

        if (part.Source.outline != null) part.Source.outline.enabled = !value;
    }

    public void SetColor(Color color)
    {
        if (hasColor && color == currentColor) return;
        hasColor = true;
        currentColor = color;

        foreach (Renderer r in renderers)
        {
            if (r == null || filledRenderers.Contains(r)) continue;
            ApplyColor(r, color);
        }
    }

    void ApplyColor(Renderer r, Color color)
    {
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        r.SetPropertyBlock(block);
    }
}
